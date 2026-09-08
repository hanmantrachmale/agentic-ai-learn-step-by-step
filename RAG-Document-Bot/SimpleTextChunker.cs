// =========================================================================
// SimpleTextChunker.cs
//
// Splits a long piece of text into smaller overlapping "chunks" before
// embedding. This matters because:
//   1. Embedding a whole 10-page PDF as ONE vector loses precision - the
//      vector becomes an average of everything in the document, so
//      searches return whole-document matches instead of the specific
//      relevant section.
//   2. Smaller, focused chunks produce embeddings that better capture the
//      meaning of that specific passage, giving much more precise
//      retrieval results.
//
// This is a hand-written, word-count-based sliding-window chunker - simple
// to understand. For production use, Semantic Kernel's TextChunker class
// (Microsoft.SemanticKernel.Text) offers smarter, punctuation-aware
// splitting - see the comment at the bottom of this file for that upgrade
// path once you're comfortable with the basic concept here.
// =========================================================================

public static class SimpleTextChunker
{
    /// <summary>
    /// Splits text into overlapping chunks of approximately
    /// maxWordsPerChunk words each. The overlap helps avoid losing context
    /// at chunk boundaries (e.g. a sentence that gets cut in half).
    /// </summary>
    public static List<string> ChunkText(string text, int maxWordsPerChunk=200, int overlapWords = 40)
    {
        var words = text.Split(
            new[]{' ','\n', '\r', '\t'},
            StringSplitOptions.RemoveEmptyEntries);
        
        var chunks = new List<string>();

        if(words.Length==0)
            return chunks;
        
        int start = 0;
        while(start < words.Length)
        {
            int length = Math.Min(maxWordsPerChunk, words.Length - start);
            var chunkWords = words.Skip(start).Take(length);
            chunks.Add(string.Join(' ',chunkWords));

            // Move the window forward, but step back by overlapWords so
            // consecutive chunks share some context at the boundary.
            start += maxWordsPerChunk - overlapWords;
            
            // Safety net: if overlapWords >= maxWordsPerChunk, the window
            // would never advance and this would loop forever.
            if(maxWordsPerChunk <= overlapWords)
                throw new ArgumentException("overlapWords must be smaller than maxWordsPerChunk.");
        }
        return chunks;
    }
}

// -------------------------------------------------------------------
// STRETCH UPGRADE (optional): Semantic Kernel's TextChunker
// -------------------------------------------------------------------
// dotnet add package Microsoft.SemanticKernel
//
// using Microsoft.SemanticKernel.Text;
//
// var lines = TextChunker.SplitPlainTextLines(rawText, maxTokensPerLine: 40);
// var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, maxTokensPerParagraph: 200);
//
// This version splits on sentence/paragraph boundaries first rather than
// a blind word count, which tends to produce more semantically coherent
// chunks - worth trying once the basic pipeline below is working.
// -------------------------------------------------------------------