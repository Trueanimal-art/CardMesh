import Foundation
import AVFoundation
import ImageIO
import UniformTypeIdentifiers

guard CommandLine.arguments.count >= 4 else {
    fputs("usage: extract_video_frame.swift <video> <seconds> <output.png>\n", stderr)
    exit(2)
}

let input = URL(fileURLWithPath: CommandLine.arguments[1])
let seconds = Double(CommandLine.arguments[2]) ?? 1.0
let output = URL(fileURLWithPath: CommandLine.arguments[3])
let asset = AVURLAsset(url: input)
let generator = AVAssetImageGenerator(asset: asset)
generator.appliesPreferredTrackTransform = true
generator.requestedTimeToleranceBefore = .zero
generator.requestedTimeToleranceAfter = .zero
let image = try generator.copyCGImage(
    at: CMTime(seconds: seconds, preferredTimescale: 600),
    actualTime: nil
)
guard let destination = CGImageDestinationCreateWithURL(
    output as CFURL,
    UTType.png.identifier as CFString,
    1,
    nil
) else {
    fputs("Cannot create image destination\n", stderr)
    exit(3)
}
CGImageDestinationAddImage(destination, image, nil)
guard CGImageDestinationFinalize(destination) else {
    fputs("Cannot finalize PNG\n", stderr)
    exit(4)
}
print("\(image.width)x\(image.height) @ \(seconds)s")
