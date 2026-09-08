using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

public static class DocumentTextExtractor
{
    public static string ExtractText(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
          ".pdf" => ExtractFromPdf(filePath),
          ".docx" => ExtractFromDocx(filePath),
          _ => throw new NotSupportedException($"Unsupported file type '{extension}'. Only .pdf and .docx are supported.")  
        };

        
    }

    private static string ExtractFromDocx(string filePath)
    {
        using WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, isEditable: false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        return body?.InnerText ?? string.Empty;
    }

    private static string ExtractFromPdf(string filePath)
    {
        var sb = new StringBuilder();
        using PdfDocument document = PdfDocument.Open(filePath);
        foreach(var page in document.GetPages())
        {
            sb.AppendLine(ContentOrderTextExtractor.GetText(page));
        }
        return sb.ToString();
    }
}