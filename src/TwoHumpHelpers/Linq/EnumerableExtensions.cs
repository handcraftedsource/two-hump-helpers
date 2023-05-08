using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Memory;

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
}
