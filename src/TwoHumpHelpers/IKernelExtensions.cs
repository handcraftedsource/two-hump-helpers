using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel;
using TwoHumpHelpers.Linq;

namespace TwoHumpHelpers;

public static class IKernelExtensions
{
    public static async Task<string> GetRelevantContext(this IKernel kernel, string collection, string query, int limit = 10, double minRelevanceScore = 0.8, double percentileCutoff = 0.8, int maxTokens = 3097, CancellationToken cancellationToken = default)
    {
        var memories = await kernel
            .Memory
            .SearchAsync(collection, query, limit, minRelevanceScore, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);
        
        var embeddingGeneration = kernel.GetService<IEmbeddingGeneration<string, float>>();
        var relevanceSelector = new RelevantSentenceSelector(embeddingGeneration);
        
        var allTexts = memories.OrderBy(m => m.Metadata.Id).Select(m => m.Metadata.Text).ToList();
        var paragraphs = allTexts.SplitToParagraphs().ToList();
        var relevantParagraphs = await relevanceSelector.GetRelevantSentences(paragraphs, query, percentileCutoff, cancellationToken: cancellationToken);
        var trimmed = relevantParagraphs.TakeMaxTokens(maxTokens);
        var input = string.Join($"{Environment.NewLine}{Environment.NewLine}", trimmed);

        return input;
    }
}

