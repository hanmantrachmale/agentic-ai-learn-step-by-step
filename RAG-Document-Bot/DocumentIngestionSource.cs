// =========================================================================
// DocumentIngestionSource.cs
//
// Walks a folder of .pdf/.docx files, extracts text from each, chunks it,
// and returns the result in the SAME SHAPE as MockIncidentData.Incidents:
// a List<(string Id, string Text)>. This means Program.cs's ingestion
// loop (embed -> build IncidentRecord -> UpsertAsync) doesn't need to
// change AT ALL - only the data source changes, exactly like swapping in
// the real YOOBIC API in POC2.
// =========================================================================

public static class DocumentIngestionSource
{
    private static readonly string[] SupportedExtensions = { ".pdf", ".docx" };
    public static List<(string Id, string Text)> LoadAndChunkDocuments(
        string folderPath,
        int maxWordsPerChunk = 200,
        int overlapWords = 40
    )
    {
        var results = new List<(string Id, string Text)>();
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"[WARN] Documents folder not found: {folderPath}");
            return results;
        }
        var files = Directory.GetFiles(folderPath)
                        .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .ToList();

        Console.WriteLine($" Found {files.Count} supported file(s) in '{folderPath}'.");

        foreach (var filePath in files)
        {
            string rawText;
            try
            {
                rawText = DocumentTextExtractor.ExtractText(filePath);
            }
            catch (Exception ex)
            {
                // Don't let one bad/corrupt file stop the whole ingestion run.
                Console.WriteLine($"[WARN] Failed to read '{Path.GetFileName(filePath)}': {ex.Message}");
                continue;
            }
            var chunks = SimpleTextChunker.ChunkText(rawText, maxWordsPerChunk, overlapWords);
            var fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            for (int i = 0; i < chunks.Count; i++)
            {
                // e.g. "IncidentReport-2026-08-chunk-000"
                var id = $"{fileNameNoExt}-chunk-{i:D3}";
                results.Add((id, chunks[i]));
            }
            Console.WriteLine($" {Path.GetFileName(filePath)} -> {chunks.Count} chunk(s)");
        }
        return results;
    }
}