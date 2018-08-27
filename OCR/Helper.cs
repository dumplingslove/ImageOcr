using Google.Cloud.Vision.V1;
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
        public static Encoding gb = Encoding.GetEncoding("gb2312");

        public static bool IsChinese(char inputData)
        {
            Regex RegChinese = new Regex("[\u4e00-\u9fa5]");
            Match m = RegChinese.Match(inputData.ToString());
            return m.Success;
        }

        public static bool IsSimplifiedChinese(char inputData)
        {
            string str = inputData.ToString();
            byte[] gb2312Bytes = gb.GetBytes(str);

            if (gb2312Bytes.Length == 2)
            {
                if (gb2312Bytes[0] >= 0xB0 &&
                    gb2312Bytes[1] <= 0xF7 &&
                    gb2312Bytes[1] >= 0xA1 &&
                    gb2312Bytes[1] <= 0xFE)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
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
