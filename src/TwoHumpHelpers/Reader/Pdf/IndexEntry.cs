using System.Security.AccessControl;

namespace TwoHumpHelpers.Reader.Pdf;

public record IndexEntry(string Name, int[] Pages, string[] FrontMatterPages)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Name) || !(Pages.Any() || FrontMatterPages.Any());
}