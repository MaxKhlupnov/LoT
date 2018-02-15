using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.DNW
{
    public static class ListExtensions
    {
       
            public static long Mean(this List<long> values)
            {
                return values.Count == 0 ? 0 : values.Mean(0, values.Count);
            }

            public static long Mean(this List<long> values, int start, int end)
            {
                long s = 0;

                for (int i = start; i < end; i++)
                {
                    s += values[i];
                }

                return s / (end - start);
            }

            public static long Variance(this List<long> values)
            {
                return values.Variance(values.Mean(), 0, values.Count);
            }

            public static long Variance(this List<long> values, long mean)
            {
                return values.Variance(mean, 0, values.Count);
            }

            public static long Variance(this List<long> values, long mean, int start, int end)
            {
                long variance = 0;

                for (int i = start; i < end; i++)
                {
                    variance += (values[i] - mean) * (values[i] - mean);
                }

                int n = end - start;
                if (start > 0) n -= 1;

                return variance / (n);
            }

            public static double StandardDeviation(this List<long> values)
            {
                return values.Count == 0 ? 0 : values.StandardDeviation(0, values.Count);
            }

            public static double StandardDeviation(this List<long> values, int start, int end)
            {
                long mean = values.Mean(start, end);
                long variance = values.Variance(mean, start, end);

                return Math.Sqrt(Convert.ToDouble(variance));
            }
        
    }
}
