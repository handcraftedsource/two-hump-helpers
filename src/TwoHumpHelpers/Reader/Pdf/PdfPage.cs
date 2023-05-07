namespace TwoHumpHelpers.Reader.Pdf;

public record PdfPage(int Page, Chapter? Chapter, IndexEntry[] IndexEntries, string[] Words);