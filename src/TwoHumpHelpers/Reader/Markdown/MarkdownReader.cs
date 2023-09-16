using Markdig;
using Markdig.Syntax;
using System.Text;
using Markdig.Syntax.Inlines;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Markdig.Renderers;

namespace TwoHumpHelpers.Reader.Markdown;

public class MarkdownReader
{
    private readonly MarkdownPipeline _pipeline;
    private readonly MarkdownDocument _document;

    private MarkdownReader(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified markdown file does not exist.", filePath);
        }

        var markdownText = File.ReadAllText(filePath);

        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .ConfigureNewLine(Environment.NewLine)
            .Build();

        _document = Markdig.Markdown.Parse(markdownText, _pipeline);
    }

    public static MarkdownReader Open(string filePath)
    {
        var reader = new MarkdownReader(filePath);
        return reader;
    }

    public IEnumerable<string> GetSections(int maxTokenSize = 500)
    {
        var headerLines = _document.Descendants<HeadingBlock>()
            .Select(h => h.Inline?.FirstChild is LiteralInline literalInline ? literalInline.Content.ToString() : null)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();

        var content = GetPlainTextContent();
        var lines = content.Split(Environment.NewLine).ToList();

        var sections = SplitTextIntoSections(lines, headerLines).ToList();
        var groupedSections = GroupSectionsBySize(sections, maxTokenSize);

        foreach (var groupedSection in groupedSections)
        {
            yield return groupedSection;
        }
    }
    
    public static IEnumerable<string> SplitTextIntoSections(List<string> textLines, List<string> headers)
    {
        var sectionBuilder = new StringBuilder();
        var headerIndex = 0;
        var textLinesCount = textLines.Count;
        var headersCount = headers.Count;

        for (var currentIndex = 0; currentIndex < textLinesCount; currentIndex++)
        {
            if (headerIndex < headersCount && textLines[currentIndex].Equals(headers[headerIndex], StringComparison.OrdinalIgnoreCase))
            {
                if (sectionBuilder.Length > 0)
                {
                    yield return sectionBuilder.ToString();
                    sectionBuilder.Clear();
                }

                sectionBuilder.AppendLine(textLines[currentIndex]);
                headerIndex++;
            }
            else if (!string.IsNullOrWhiteSpace(textLines[currentIndex]))
            {
                sectionBuilder.AppendLine(textLines[currentIndex]);
            }
        }

        if (sectionBuilder.Length > 0)
        {
            yield return sectionBuilder.ToString();
        }
    }

    public IEnumerable<string> GroupSectionsBySize(List<string> sections, int maxSize)
    {
        var stringBuilder = new StringBuilder();
        var currentSize = 0;

        foreach (var section in sections)
        {
            var sectionSize = GetTokenSize(section);

            // If adding the current section exceeds the maximum allowed size,
            // yield return the combined sections and start with a new StringBuilder.
            if (currentSize + sectionSize > maxSize)
            {
                yield return stringBuilder.ToString();

                // Reset variables for the new group.
                stringBuilder.Clear();
                currentSize = 0;
            }

            // Add the current section to the StringBuilder and update the size.
            stringBuilder.AppendLine(section);
            stringBuilder.AppendLine();
            currentSize += sectionSize;
        }

        // Yield return the last combined sections.
        yield return stringBuilder.ToString();
    }

    private string GetPlainTextContent()
    {
        using var writer = new StringWriter();

        var renderer = new HtmlRenderer(writer)
        {
            EnableHtmlForBlock = false,
            EnableHtmlForInline = false,
            EnableHtmlEscape = false,
        };

        _pipeline.Setup(renderer);

        renderer.Render(_document);
        writer.Flush();

        var content = writer.ToString();
        return content;
    }

    private static int GetTokenSize(string text)
    {
        return GPT3Tokenizer.Encode(text).Count;
    }
}