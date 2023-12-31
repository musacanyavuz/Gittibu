﻿using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace GittiBu.Common.Rss
{
    public static class XDocumentExtensions
    {
        public static string ToStringWithDeclaration(this XDocument what, SerializeOption option)
        {
            if (what == null)
                throw new ArgumentNullException(nameof(what));

            var builder = new StringBuilder();
            using (TextWriter writer = new RssStringWriter(builder, option))
                what.Save(writer);
            return builder.ToString();
        }
    }
}