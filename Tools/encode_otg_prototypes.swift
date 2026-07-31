import Foundation
import AVFoundation
import AppKit
import ImageIO
import UniformTypeIdentifiers

let root = "/Users/vnm/Documents/CardMesh/Artifacts/OneTapGarageSplash/PrototypeRound5"
let width = 1136
let height = 704
let fps: Int32 = 30
let frameCount = 48

func pixelBuffer(from image: NSImage, pool: CVPixelBufferPool) -> CVPixelBuffer? {
    var buffer: CVPixelBuffer?
    CVPixelBufferPoolCreatePixelBuffer(nil, pool, &buffer)
    guard let pb = buffer else { return nil }
    CVPixelBufferLockBaseAddress(pb, [])
    defer { CVPixelBufferUnlockBaseAddress(pb, []) }
    guard let ctx = CGContext(
        data: CVPixelBufferGetBaseAddress(pb),
        width: width,
        height: height,
        bitsPerComponent: 8,
        bytesPerRow: CVPixelBufferGetBytesPerRow(pb),
        space: CGColorSpaceCreateDeviceRGB(),
        bitmapInfo: CGImageAlphaInfo.premultipliedFirst.rawValue
    ) else { return nil }
    ctx.clear(CGRect(x: 0, y: 0, width: width, height: height))
    guard let cg = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else { return nil }
    ctx.draw(cg, in: CGRect(x: 0, y: 0, width: width, height: height))
    return pb
}

func encodeMP4(variant: String) throws {
    let output = URL(fileURLWithPath: "\(root)/Prototype_\(variant).mp4")
    try? FileManager.default.removeItem(at: output)
    let writer = try AVAssetWriter(outputURL: output, fileType: .mp4)
    let settings: [String: Any] = [
        AVVideoCodecKey: AVVideoCodecType.h264,
        AVVideoWidthKey: width,
        AVVideoHeightKey: height,
        AVVideoCompressionPropertiesKey: [
            AVVideoAverageBitRateKey: 8_000_000,
            AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel
        ]
    ]
    let input = AVAssetWriterInput(mediaType: .video, outputSettings: settings)
    input.expectsMediaDataInRealTime = false
    let adaptor = AVAssetWriterInputPixelBufferAdaptor(
        assetWriterInput: input,
        sourcePixelBufferAttributes: [
            kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32ARGB,
            kCVPixelBufferWidthKey as String: width,
            kCVPixelBufferHeightKey as String: height
        ]
    )
    writer.add(input)
    writer.startWriting()
    writer.startSession(atSourceTime: .zero)
    for i in 0..<frameCount {
        while !input.isReadyForMoreMediaData { Thread.sleep(forTimeInterval: 0.002) }
        let file = String(format: "\(root)/Prototype_%@_frames/frame_%03d.png", variant, i)
        guard let image = NSImage(contentsOfFile: file),
              let pool = adaptor.pixelBufferPool,
              let pb = pixelBuffer(from: image, pool: pool) else {
            throw NSError(domain: "OTGEncoder", code: 2, userInfo: [NSLocalizedDescriptionKey: "Cannot read \(file)"])
        }
        adaptor.append(pb, withPresentationTime: CMTime(value: CMTimeValue(i), timescale: fps))
    }
    input.markAsFinished()
    let done = DispatchSemaphore(value: 0)
    writer.finishWriting { done.signal() }
    done.wait()
    if writer.status != .completed {
        throw writer.error ?? NSError(domain: "OTGEncoder", code: 3)
    }
}

func encodeGIF(variant: String) throws {
    let output = URL(fileURLWithPath: "\(root)/Prototype_\(variant).gif") as CFURL
    guard let destination = CGImageDestinationCreateWithURL(output, UTType.gif.identifier as CFString, frameCount, nil) else {
        throw NSError(domain: "OTGEncoder", code: 4)
    }
    let gifProps: [CFString: Any] = [
        kCGImagePropertyGIFDictionary: [
            kCGImagePropertyGIFLoopCount: 0
        ]
    ]
    CGImageDestinationSetProperties(destination, gifProps as CFDictionary)
    for i in 0..<frameCount {
        let file = String(format: "\(root)/Prototype_%@_frames/frame_%03d.png", variant, i)
        guard let image = NSImage(contentsOfFile: file),
              let cg = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else { continue }
        let frameProps: [CFString: Any] = [
            kCGImagePropertyGIFDictionary: [
                kCGImagePropertyGIFDelayTime: 1.0 / Double(fps),
                kCGImagePropertyGIFUnclampedDelayTime: 1.0 / Double(fps)
            ]
        ]
        CGImageDestinationAddImage(destination, cg, frameProps as CFDictionary)
    }
    guard CGImageDestinationFinalize(destination) else {
        throw NSError(domain: "OTGEncoder", code: 5)
    }
}

for variant in ["A", "B"] {
    try encodeMP4(variant: variant)
    try encodeGIF(variant: variant)
    print("Encoded Prototype \(variant): MP4 + GIF")
}
