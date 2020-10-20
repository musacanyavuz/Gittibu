using System.IO;
using System.Text;

namespace GittiBu.Common.Rss
{
    internal class RssStringWriter : StringWriter
    {
        private readonly SerializeOption _option;

        public RssStringWriter(StringBuilder sb, SerializeOption option) : base(sb)
        {
            _option = option;
        }

        public override Encoding Encoding => _option.Encoding;
    }
}