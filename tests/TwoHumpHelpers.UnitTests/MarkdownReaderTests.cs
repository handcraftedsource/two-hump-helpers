using FluentAssertions;
using TwoHumpHelpers.Reader.Markdown;

namespace TwoHumpHelpers.UnitTests;

public class MarkdownReaderTests
{
    [Test]
    public void Read_Sample_ReturnsMultipleSections()
    {
        var sampleFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets", "ChatGPT Prompt Engineering for Developers.md");

        var reader = MarkdownReader.Open(sampleFilePath);
        var sections = reader.GetSections().ToList();

        sections.Count.Should().Be(1);
    }
}