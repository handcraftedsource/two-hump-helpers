namespace TwoHumpHelpers.Linq;

public static class ListExtensions
{
    public static double Percentile(this List<double> sequence, double percentile)
    {
        if (!sequence.Any())
        {
            return 1;
        }

        var orderedSequence = sequence.OrderBy(x => x).ToList();
        var count = orderedSequence.Count;
        var n = (count - 1) * percentile + 1;

        if (count == 1)
            return orderedSequence[0]; // if only one element just return it

        if (Math.Abs(n - 1d) < 0.001)
            return orderedSequence[0]; // first data point

        if (Math.Abs(n - count) < 0.001)
            return orderedSequence[count - 1]; // last data point

        var k = (int)n;
        var d = n - k;
        return orderedSequence[k - 1] + d * (orderedSequence[k] - orderedSequence[k - 1]); // linear interpolation
    }
}