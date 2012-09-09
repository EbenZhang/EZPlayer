using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EZPlayer
{
    class LevenshteinDistance
    {
        /// <summary>
        /// 编辑距离（Levenshtein Distance）
        /// </summary>
        /// <param name="source">源串</param>
        /// <param name="target">目标串</param>
        /// <param name="isCaseSensitive">是否大小写敏感</param>
        /// <returns>输出：相似度，值在0～１</returns>
        public static double CalculateSimilarity(String source, String target, Boolean isCaseSensitive = false)
        {
            double similarity = 0;
            if (String.IsNullOrEmpty(source))
            {
                return String.IsNullOrEmpty(target) ? 1 : 0;
            }
            else if (String.IsNullOrEmpty(target))
            {
                return 0;
            }

            String From, To;
            if (isCaseSensitive)
            {   // 大小写敏感
                From = source;
                To = target;
            }
            else
            {   // 大小写无关
                From = source.ToLower();
                To = target.ToLower();
            }

            // 初始化
            Int32 m = From.Length;
            Int32 n = To.Length;
            Int32[,] H = new Int32[m + 1, n + 1];
            for (Int32 i = 0; i <= m; i++) H[i, 0] = i;  // 注意：初始化[0,0]
            for (Int32 j = 1; j <= n; j++) H[0, j] = j;

            for (Int32 i = 1; i <= m; i++)
            {
                Char SI = From[i - 1];
                for (Int32 j = 1; j <= n; j++)
                {   // 删除（deletion） 插入（insertion） 替换（substitution）
                    if (SI == To[j - 1])
                        H[i, j] = H[i - 1, j - 1];
                    else
                        H[i, j] = Math.Min(H[i - 1, j - 1], Math.Min(H[i - 1, j], H[i, j - 1])) + 1;
                }
            }

            // 计算相似度
            Int32 MaxLength = Math.Max(m, n);   // 两字符串的最大长度
            similarity = ((Double)(MaxLength - H[m, n])) / MaxLength;

            return similarity;
        }
    }
}
