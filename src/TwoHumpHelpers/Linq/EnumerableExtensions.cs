using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;

namespace TwoHumpHelpers.Linq;

public static class EnumerableExtensions
{
    public static IEnumerable<TSource> TakeWhileAggregate<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> func,
        Func<TAccumulate, bool> predicate
    ) {
        TAccumulate accumulator = seed;
        foreach (TSource item in source) {
            accumulator = func(accumulator, item);
            if (predicate(accumulator)) {
                yield return item;
            }
            else {
                yield break;
            }
        }
    }

    public static IEnumerable<MemoryQueryResult> TakeMaxTokens(this IEnumerable<MemoryQueryResult> memories, int maxTokens)
    {
        var inputMemories = memories
            .TakeWhileAggregate(0, (total, current) => total + GPT3Tokenizer.Encode(current.Metadata.Text).Count, total => total < maxTokens);

        return inputMemories;
    }

    public static IEnumerable<string> TakeMaxTokens(this IEnumerable<string> texts, int maxTokens)
    {
        var inputMemories = texts
            .TakeWhileAggregate(0, (total, current) => total + GPT3Tokenizer.Encode(current).Count, total => total < maxTokens);

        return inputMemories;
    }

    public static IEnumerable<string> SplitToParagraphs(this List<string> texts)
    {
        foreach (var text in texts)
        {
            var lines = TextChunker.SplitPlainTextLines(text, 50);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 300);

            foreach (var paragraph in paragraphs)
            {
                yield return paragraph;
            }
        }
        
    }
}
