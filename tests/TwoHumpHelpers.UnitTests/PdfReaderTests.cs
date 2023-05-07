using FluentAssertions;
using TwoHumpHelpers.Reader.Pdf;

namespace TwoHumpHelpers.UnitTests;

public class PdfReaderTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Read_Sample_ReturnsElevenPages()
    {
        var sampleFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets", "Sample.pdf");

        using var reader = PdfReader.Open(sampleFilePath);

        var pages = reader.GetContent().ToList();
        Assert.That(pages.Count, Is.EqualTo(11));
    }

    [Test]
    public void Read_Sample_ReturnsPagesWithoutChapters()
    {
        var sampleFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets", "Sample.pdf");

        using var reader = PdfReader.Open(sampleFilePath);

        var pages = reader.GetContent().ToList();

        pages.ForEach(x => x.Chapter.Should().BeNull());
    }

    [Test]
    public void Read_SampleWithBookmarks_ReturnsElevenPages()
    {
        var sampleFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets", "Sample with bookmarks.pdf");

        using var reader = PdfReader.Open(sampleFilePath);

        var pages = reader.GetContent().ToList();
        Assert.That(pages.Count, Is.EqualTo(11));
    }

    [Test]
    public void Read_SampleWithBookmarks_ReturnsPagesWithChapters()
    {
        var sampleFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets", "Sample with bookmarks.pdf");

        using var reader = PdfReader.Open(sampleFilePath);

        var pages = reader.GetContent().ToList();

        var chaptersByPage = new Dictionary<int, string?>
        {
            {1, null},
            {2, null},
            {3, "Authors"},
            {4, "Authors"},
            {5, "Finally"},
            {6, "When while"},
            {7, "Business"},
            {8, "Nemo enim"},
            {9, "Et harum"},
            {10, "Et harum"},
            {11, "Index"},
        };

        foreach (var page in pages)
        {
            var expected = chaptersByPage[page.Page];
            if (expected != null)
            {
                Assert.NotNull(page.Chapter, $"Expected page {page.Page} to have a chapter.");
            }
            
            page.Chapter?.Title.Should().Be(expected);
        }
    }

    [Test]
    public void Read_SampleWithBookmarks_ReturnsPagesWithIndexEntries()
    {
        var sampleFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets", "Sample with bookmarks.pdf");

        using var reader = PdfReader.Open(sampleFilePath);

        var pagesByNumber = reader.GetContent().ToDictionary(p => p.Page);

        var index = new List<IndexEntry>
        {
            new("At", new[] { 5, 6, 7 }, new[] { "II", "III" }),
            new("business", new[] { 4 }, Array.Empty<string>()),
            new("collection", new[] { 4 }, Array.Empty<string>()),
            new("eyes", new[] { 3, 4 }, Array.Empty<string>()),
            new("Far", new[] { 2 }, Array.Empty<string>()),
            new("head", new[] { 4 }, Array.Empty<string>()),
            new("Lorem", new[] { 2 }, new[] { "II", "III" }),
            new("soul", new[] { 3 }, Array.Empty<string>()),
            new("Text", new[] { 2 }, Array.Empty<string>()),
            new("than", new[] { 3, 4 }, Array.Empty<string>()),
            new("voluptatem", new[] { 5, 6 }, Array.Empty<string>())
        };

        var indexEntriesByPage = index
            .SelectMany(i => i.Pages.Select(p => (p, i)))
            .ToLookup(x => x.p, x => x.i);
        
        foreach (var indexEntries in indexEntriesByPage.OrderBy(x => x.Key))
        {
            pagesByNumber.Should().ContainKey(indexEntries.Key);
            var page = pagesByNumber[indexEntries.Key];

            var expectedEntries = indexEntries.ToList();
            page.IndexEntries.Length.Should().Be(expectedEntries.Count, $"Page {page.Page} should have the expected index entries.");
            page.IndexEntries.Should().BeEquivalentTo(expectedEntries, $"Page {page.Page} should have the expected index entries.");
        }
    }
}