using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OCR
{
    public static class Helper
    {
        public static bool IsChinese(char inputData)
        {
            Regex RegChinese = new Regex("[\u4e00-\u9fa5]");
            Match m = RegChinese.Match(inputData.ToString());
            return m.Success;
        }

        public static bool IsLetterOrNumber(char inputData)
        {
            Regex RegChinese = new Regex("[^A-Za-z0-9]");
            Match m = RegChinese.Match(inputData.ToString());
            return m.Success;
        }

        public static bool IsSeparater(char inputData)
        {
            var separaters = new List<char>() { ',', '.', ':', ';', '。', '“', '”', '‘', '’', '？', '!', '，', '：', '；', '、', '\'', '"', '?', '!' };
            return separaters.Contains(inputData);
        }
    }
}
