import AppKit
import Vision
import ImageIO

// 图像留在本机，按行几何位置重建表格 / Images stay local; reconstruct table rows by geometry.
do {
    guard CommandLine.arguments.count == 2 else { throw NSError(domain: "StockOcr", code: 1) }
    let url = URL(fileURLWithPath: CommandLine.arguments[1])
    let info = try url.resourceValues(forKeys: [.fileSizeKey])
    guard (info.fileSize ?? Int.max) <= 15_000_000,
          let source = CGImageSourceCreateWithURL(url as CFURL, nil),
          let image = CGImageSourceCreateImageAtIndex(source, 0, nil),
          image.width > 0, image.height > 0, image.width <= 10000, image.height <= 10000,
          image.width * image.height <= 40_000_000 else { throw NSError(domain: "StockOcr", code: 2) }
    let request = VNRecognizeTextRequest()
    request.recognitionLevel = .accurate
    request.usesLanguageCorrection = false
    request.recognitionLanguages = ["en-US"]
    try VNImageRequestHandler(cgImage: image, options: [:]).perform([request])
    let observations = (request.results ?? []).sorted { $0.boundingBox.midY > $1.boundingBox.midY }
    var groups: [[VNRecognizedTextObservation]] = []
    for observation in observations {
        if let last = groups.last, let first = last.first,
           abs(first.boundingBox.midY - observation.boundingBox.midY) < max(0.008, min(first.boundingBox.height, observation.boundingBox.height) * 0.6) {
            groups[groups.count - 1].append(observation)
        } else { groups.append([observation]) }
    }
    guard groups.count <= 1000 else { throw NSError(domain: "StockOcr", code: 3) }
    let lines = groups.map { row in
        row.sorted { $0.boundingBox.minX < $1.boundingBox.minX }.compactMap { $0.topCandidates(1).first?.string }
            .map { $0.replacingOccurrences(of: "\t", with: " ").replacingOccurrences(of: "\n", with: " ") }.joined(separator: "\t")
    }
    print(lines.joined(separator: "\n"))
} catch {
    fputs("Local OCR failed. Verify image size and legible table columns.\n", stderr)
    exit(1)
}
