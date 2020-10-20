﻿using System.Linq;

namespace GittiBu.Common.Extensions
{
    public static class StringExtensions
    {
        public static string Modify(this string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return string.Empty;
            }
            val = new string(val.Select((ch, index) => (index == 0) ? ch : char.ToLower(ch)).ToArray());
            return char.ToUpper(val[0]) + val.Substring(1);
        }
    }
}