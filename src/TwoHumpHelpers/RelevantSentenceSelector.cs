using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.AI.Embeddings.VectorOperations;
using TwoHumpHelpers.Linq;

namespace TwoHumpHelpers;

/// <summary>
/// The RelevantSentenceSelector class is designed to optimize the selection of sentences based on their relevance to a given query.
/// It takes a list of sentences and a query, and returns the sentences that are most relevant to the query.
/// The relevance of a sentence is determined by computing its embedding and comparing this to the embedding of the query.
/// The class provides flexibility in defining what is considered 'relevant' through the use of percentile and threshold cutoffs.
/// The percentile cutoff specifies that only the top N percent of sentences, ranked by relevance, should be returned.
/// The threshold cutoff is an absolute measure of relevance, and sentences with a score below this are excluded.
/// These two measures can be used in combination to fine-tune the selection of relevant sentences.
/// </summary>
public class RelevantSentenceSelector
{
    private readonly IEmbeddingGeneration<string, float> _embeddingGenerator;

    public RelevantSentenceSelector(IEmbeddingGeneration<string, float> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    /// <summary>
    /// This method takes a list of sentences and a query, and returns the sentences most relevant to the query.
    /// </summary>
    /// <param name="sentences">The list of sentences to be ranked for relevance to the query.</param>
    /// <param name="query">The query to which the sentences' relevance is to be compared.</param>
    /// <param name="percentileCutoff">The percentile at which to cut off the sentences. Only the top percentile of sentences, ranked by relevance to the query, will be returned. For example, a percentileCutoff of 0.5 means that only the top 50% of sentences will be used.</param>
    /// <param name="thresholdCutoff">The absolute threshold at which sentences are considered irrelevant. Sentences with relevance scores below this threshold will be excluded. For example, a thresholdCutoff of 0.7 means that only sentences with a relevance score of 0.7 or higher will be used. These cutoffs can be used together.</param>
    /// <param name="cancellationToken">Optional cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of sentences ranked by their relevance to the query, with sentences considered irrelevant based on the percentileCutoff and thresholdCutoff excluded.</returns>
    public async Task<List<string>> GetRelevantSentences(List<string> sentences, string query, double percentileCutoff = 0.8, double thresholdCutoff = 0.7, CancellationToken cancellationToken = default)
    {
        // Get the sentence embeddings for the sentences and the query
        var sentenceEmbeddings = await _embeddingGenerator.GenerateEmbeddingsAsync(sentences, cancellationToken);
        var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query, cancellationToken);

        // Compute the relevance of each sentence to the query
        var relevanceScores = ComputeRelevanceScores(sentenceEmbeddings.Select(x => x.Vector.ToArray()).ToList(), queryEmbedding.Vector.ToArray());

        // Apply the percentile and threshold cutoffs
        var cutoff = Math.Max(relevanceScores.Percentile(percentileCutoff), thresholdCutoff);
        var relevantSentences = sentences.Where((sentence, index) => relevanceScores[index] >= cutoff).ToList();

        return relevantSentences;
    }

    /// <summary>
    /// This method computes the relevance of each sentence to the query.
    /// </summary>
    /// <param name="sentenceEmbeddings">The list of sentence embeddings.</param>
    /// <param name="queryEmbedding">The query embedding.</param>
    /// <returns>A list of relevance scores for each sentence.</returns>
    private static List<double> ComputeRelevanceScores(List<float[]> sentenceEmbeddings, float[] queryEmbedding)
    {
        var relevanceScores = new List<double>();

        foreach (var sentenceEmbedding in sentenceEmbeddings)
        {
            var similarity = sentenceEmbedding.CosineSimilarity(queryEmbedding);
            relevanceScores.Add(similarity);
        }

        return relevanceScores;
    }
}