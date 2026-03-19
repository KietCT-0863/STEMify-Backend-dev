using Shared.Protos.Classroom;

namespace Shared.Helper
{
    public static class StatisticsHelper
    {
        public static (double mean, double median, double min, double max, double q1, double q3, List<double> outliers)
            CalculateStatistics(List<double> scores)
        {
            if (scores == null || !scores.Any())
            {
                return (0, 0, 0, 0, 0, 0, new List<double>());
            }

            var sortedScores = scores.OrderBy(s => s).ToList();

            // Mean
            double mean = scores.Average();

            // Min and Max
            double min = sortedScores.First();
            double max = sortedScores.Last();

            // Median
            double median = CalculateMedian(sortedScores);

            // Q1 and Q3
            int n = sortedScores.Count;
            var lowerHalf = sortedScores.Take(n / 2).ToList();
            var upperHalf = n % 2 == 0
                ? sortedScores.Skip(n / 2).ToList()
                : sortedScores.Skip(n / 2 + 1).ToList();

            double q1 = CalculateMedian(lowerHalf);
            double q3 = CalculateMedian(upperHalf);

            // Outliers (using IQR method)
            double iqr = q3 - q1;
            double lowerBound = q1 - 1.5 * iqr;
            double upperBound = q3 + 1.5 * iqr;

            var outliers = sortedScores
                .Where(s => s < lowerBound || s > upperBound)
                .ToList();

            return (mean, median, min, max, q1, q3, outliers);
        }

        private static double CalculateMedian(List<double> sortedValues)
        {
            if (!sortedValues.Any())
                return 0;

            int n = sortedValues.Count;

            if (n % 2 == 0)
            {
                return (sortedValues[n / 2 - 1] + sortedValues[n / 2]) / 2.0;
            }
            else
            {
                return sortedValues[n / 2];
            }
        }

        public static List<HistogramBin> BuildHistogram(List<double> scores)
        {
            var bins = new List<HistogramBin>();

            if (scores == null || scores.Count == 0)
                return bins;

            const double binSize = 10.0;
            const double min = 0.0;
            const double max = 100.0;

            int totalBins = (int)((max - min) / binSize); // 10 bins: 0-10 ... 90-100

            for (int i = 0; i < totalBins; i++)
            {
                double start = min + i * binSize;
                double end = start + binSize;

                bool isLastBin = (i == totalBins - 1);

                // Bin cuối inclusive end (<=)
                int count = scores.Count(s =>
                    isLastBin
                        ? (s >= start && s <= end)
                        : (s >= start && s < end)
                );

                bins.Add(new HistogramBin
                {
                    RangeStart = start,
                    RangeEnd = end,
                    Count = count
                });
            }

            return bins;
        }

    }
}
