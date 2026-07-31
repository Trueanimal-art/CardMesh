import Foundation
import AVFoundation
import CoreVideo
import CoreGraphics
import ImageIO

guard CommandLine.arguments.count >= 7 else {
    fputs("usage: encode_png_sequence_with_audio.swift <framesDir> <silent.mp4> <audio.wav> <master.mp4> <fps> <duration>\n", stderr)
    exit(2)
}

let framesDir = URL(fileURLWithPath: CommandLine.arguments[1])
let silentURL = URL(fileURLWithPath: CommandLine.arguments[2])
let audioURL = URL(fileURLWithPath: CommandLine.arguments[3])
let masterURL = URL(fileURLWithPath: CommandLine.arguments[4])
let fps = Int32(CommandLine.arguments[5]) ?? 30
let duration = Double(CommandLine.arguments[6]) ?? 2.1
let width = 1920
let height = 1080

let fm = FileManager.default
try? fm.removeItem(at: silentURL)
try? fm.removeItem(at: masterURL)

let frameURLs = try fm.contentsOfDirectory(
    at: framesDir,
    includingPropertiesForKeys: nil,
    options: [.skipsHiddenFiles]
).filter { $0.pathExtension.lowercased() == "png" }.sorted { $0.lastPathComponent < $1.lastPathComponent }

guard !frameURLs.isEmpty else {
    fputs("No PNG frames found\n", stderr)
    exit(3)
}

let writer = try AVAssetWriter(outputURL: silentURL, fileType: .mp4)
let videoSettings: [String: Any] = [
    AVVideoCodecKey: AVVideoCodecType.h264,
    AVVideoWidthKey: width,
    AVVideoHeightKey: height,
    AVVideoColorPropertiesKey: [
        AVVideoColorPrimariesKey: AVVideoColorPrimaries_ITU_R_709_2,
        AVVideoTransferFunctionKey: AVVideoTransferFunction_ITU_R_709_2,
        AVVideoYCbCrMatrixKey: AVVideoYCbCrMatrix_ITU_R_709_2
    ],
    AVVideoCompressionPropertiesKey: [
        AVVideoAverageBitRateKey: 12_000_000,
        AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel,
        AVVideoMaxKeyFrameIntervalKey: Int(fps)
    ]
]
let input = AVAssetWriterInput(mediaType: .video, outputSettings: videoSettings)
input.expectsMediaDataInRealTime = false
let attrs: [String: Any] = [
    kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA,
    kCVPixelBufferWidthKey as String: width,
    kCVPixelBufferHeightKey as String: height,
    kCVPixelBufferCGImageCompatibilityKey as String: true,
    kCVPixelBufferCGBitmapContextCompatibilityKey as String: true
]
let adaptor = AVAssetWriterInputPixelBufferAdaptor(
    assetWriterInput: input,
    sourcePixelBufferAttributes: attrs
)
guard writer.canAdd(input) else {
    fputs("Cannot add video input\n", stderr)
    exit(4)
}
writer.add(input)
writer.startWriting()
writer.startSession(atSourceTime: .zero)

for (index, url) in frameURLs.enumerated() {
    while !input.isReadyForMoreMediaData {
        usleep(1_000)
    }
    guard
        let source = CGImageSourceCreateWithURL(url as CFURL, nil),
        let image = CGImageSourceCreateImageAtIndex(source, 0, nil)
    else {
        fputs("Cannot read frame \(url.path)\n", stderr)
        exit(5)
    }
    var maybeBuffer: CVPixelBuffer?
    CVPixelBufferCreate(
        nil,
        width,
        height,
        kCVPixelFormatType_32BGRA,
        attrs as CFDictionary,
        &maybeBuffer
    )
    guard let buffer = maybeBuffer else {
        fputs("Cannot allocate pixel buffer\n", stderr)
        exit(6)
    }
    CVPixelBufferLockBaseAddress(buffer, [])
    let base = CVPixelBufferGetBaseAddress(buffer)
    let bytesPerRow = CVPixelBufferGetBytesPerRow(buffer)
    guard let ctx = CGContext(
        data: base,
        width: width,
        height: height,
        bitsPerComponent: 8,
        bytesPerRow: bytesPerRow,
        space: CGColorSpaceCreateDeviceRGB(),
        bitmapInfo: CGBitmapInfo.byteOrder32Little.rawValue | CGImageAlphaInfo.premultipliedFirst.rawValue
    ) else {
        fputs("Cannot create CGContext\n", stderr)
        exit(7)
    }
    ctx.setFillColor(CGColor(gray: 0, alpha: 1))
    ctx.fill(CGRect(x: 0, y: 0, width: width, height: height))
    let scale = max(
        CGFloat(width) / CGFloat(image.width),
        CGFloat(height) / CGFloat(image.height)
    )
    let drawWidth = CGFloat(image.width) * scale
    let drawHeight = CGFloat(image.height) * scale
    let drawRect = CGRect(
        x: (CGFloat(width) - drawWidth) * 0.5,
        y: (CGFloat(height) - drawHeight) * 0.5,
        width: drawWidth,
        height: drawHeight
    )
    ctx.draw(image, in: drawRect)
    CVPixelBufferUnlockBaseAddress(buffer, [])
    let time = CMTime(value: CMTimeValue(index), timescale: fps)
    guard adaptor.append(buffer, withPresentationTime: time) else {
        fputs("Failed to append frame \(index): \(writer.error?.localizedDescription ?? "unknown")\n", stderr)
        exit(8)
    }
}

input.markAsFinished()
let writeDone = DispatchSemaphore(value: 0)
writer.finishWriting {
    writeDone.signal()
}
writeDone.wait()
guard writer.status == .completed else {
    fputs("Video writer failed: \(writer.error?.localizedDescription ?? "unknown")\n", stderr)
    exit(9)
}

let videoAsset = AVURLAsset(url: silentURL)
let audioAsset = AVURLAsset(url: audioURL)
let composition = AVMutableComposition()
let targetDuration = CMTime(seconds: duration, preferredTimescale: 600)

guard
    let sourceVideo = videoAsset.tracks(withMediaType: .video).first,
    let videoTrack = composition.addMutableTrack(
        withMediaType: .video,
        preferredTrackID: kCMPersistentTrackID_Invalid
    )
else {
    fputs("Cannot create video composition track\n", stderr)
    exit(10)
}
try videoTrack.insertTimeRange(
    CMTimeRange(start: .zero, duration: min(videoAsset.duration, targetDuration)),
    of: sourceVideo,
    at: .zero
)
videoTrack.preferredTransform = sourceVideo.preferredTransform

if let sourceAudio = audioAsset.tracks(withMediaType: .audio).first,
   let audioTrack = composition.addMutableTrack(
       withMediaType: .audio,
       preferredTrackID: kCMPersistentTrackID_Invalid
   ) {
    try audioTrack.insertTimeRange(
        CMTimeRange(start: .zero, duration: min(audioAsset.duration, targetDuration)),
        of: sourceAudio,
        at: .zero
    )
}

guard let exporter = AVAssetExportSession(
    asset: composition,
    presetName: AVAssetExportPresetHighestQuality
) else {
    fputs("Cannot create exporter\n", stderr)
    exit(11)
}
exporter.outputURL = masterURL
exporter.outputFileType = .mp4
exporter.timeRange = CMTimeRange(start: .zero, duration: targetDuration)
exporter.shouldOptimizeForNetworkUse = true
let exportDone = DispatchSemaphore(value: 0)
exporter.exportAsynchronously {
    exportDone.signal()
}
exportDone.wait()
guard exporter.status == .completed else {
    fputs("Export failed: \(exporter.error?.localizedDescription ?? "unknown")\n", stderr)
    exit(12)
}

print("Encoded \(frameURLs.count) frames at \(fps) fps")
print("Silent: \(silentURL.path)")
print("Master: \(masterURL.path)")
