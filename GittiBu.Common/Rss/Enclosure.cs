using System.Collections.Specialized;

namespace GittiBu.Common.Rss
{
    public class Enclosure
    {
        public Enclosure()
        {
            Values = new NameValueCollection();
        }

        public NameValueCollection Values { get; set; }
    }
}