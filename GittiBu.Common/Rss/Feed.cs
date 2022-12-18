using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace GittiBu.Common.Rss
{
    public class Image
    {
        public string url { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string width { get; set; }
        public string height { get; set; }
    }
    public class Feed
    {
        public string Description { get; set; }
        public Uri Link { get; set; }
        public string Title { get; set; }
        public string Copyright { get; set; }

        public string language { get; set; }
        public string generator { get; set; }
        public string lastBuildDate { get; set; }


        public Image image { get; set; }
        
        public ICollection<Item> Items { get; set; } = new List<Item>();

        /// <summary>Produces well-formatted rss-compatible xml string.</summary>
        public string Serialize(bool isFeedNew = false)
        {
            var defaultOption = new SerializeOption()
            {
                Encoding = Encoding.UTF8,
            };
            return Serialize(defaultOption, isFeedNew);
        }

        public List<XAttribute> SetAttributes(bool isFeedNew)
        {
            var list = new List<XAttribute>
            {
                new XAttribute("version", "2.0"),
                new XAttribute(XNamespace.Xmlns + "atom", "http://www.w3.org/2005/Atom")
            };
            if (!isFeedNew) return list;
            list.Add(new XAttribute(XNamespace.Xmlns + "content", "http://purl.org/rss/1.0/modules/content"));
            list.Add(new XAttribute(XNamespace.Xmlns + "dc", "http://purl.org/dc/elements/1.1"));
            return list;

        }

        public string Serialize(SerializeOption option, bool isFeedNew)
        {
            XNamespace nsAtom = "http://www.w3.org/2005/Atom";
            XNamespace content = "http://purl.org/rss/1.0/modules/content";
            XNamespace dc = "http://purl.org/dc/elements/1.1";
            var doc = new XDocument(new XElement("rss"));
            if (doc.Root == null) return doc.ToStringWithDeclaration(option);
            doc.Root.Add(SetAttributes(isFeedNew));
            var channel = new XElement("channel");


            channel.Add(new XElement("title", Title));
            channel.Add(
                new XElement(nsAtom + "link",
                    new XAttribute("href", Link.AbsoluteUri),
                    new XAttribute("rel", "self"),
                    new XAttribute("type", "application/rss+xml")
                ));
            if (isFeedNew)
            {
                channel.Add(
                    new XElement(dc + "link",
                        new XAttribute("href", Link.AbsoluteUri),
                        new XAttribute("rel", "self"),
                        new XAttribute("type", "application/rss+xml")
                    ));
                channel.Add(
                    new XElement(content + "link",
                        new XAttribute("href", Link.AbsoluteUri),
                        new XAttribute("rel", "self"),
                        new XAttribute("type", "application/rss+xml")
                    ));
            }
            if (Link != null) channel.Add(new XElement("link", Link.AbsoluteUri));
            channel.Add(new XElement("description", Description));
            // copyright is not a requirement
            if (!string.IsNullOrEmpty(Copyright)) channel.Add(new XElement("copyright", Copyright));
            if (!string.IsNullOrEmpty(language)) channel.Add(new XElement("language", language));

            if (!string.IsNullOrEmpty(generator)) channel.Add(new XElement("generator", generator));
            if (!string.IsNullOrEmpty(lastBuildDate)) channel.Add(new XElement("lastBuildDate", lastBuildDate));

            if (image!=null)
            {
                var itemElement2 = new XElement("image");
                itemElement2.Add(new XElement("url", image.url));
                if (image.title != null) itemElement2.Add(new XElement("title", image.title));
                if (image.link != null) itemElement2.Add(new XElement("link", image.link));
                if (image.width != null) itemElement2.Add(new XElement("width", image.width));


                if (image.height != null) itemElement2.Add(new XElement("height", image.height));

                channel.Add(itemElement2);
            
            }
            doc.Root.Add(channel);



            foreach (var item in Items)
            {
                 var itemElement = new XElement("item");
                itemElement.Add(new XElement("title", item.Title));
                if (item.Link != null) itemElement.Add(new XElement("link", item.Link.AbsoluteUri));
                //if (string.IsNullOrEmpty(item.Image) == false)
                //{
                //    item.Body = $"{item.Body}  <![CDATA[\r\n  Image inside RSS\r\n  <img src=\"{item.Image}\">         \r\n]> ";
                //}
                itemElement.Add(new XElement("logo", item.Logo));
                itemElement.Add(new XElement("description", item.Body));
                if (item.Author != null)
                    itemElement.Add(new XElement("author", $"{item.Author.Email} ({item.Author.Name})"));
                foreach (var c in item.Categories) itemElement.Add(new XElement("category", c));
                if (item.Comments != null) itemElement.Add(new XElement("comments", item.Comments.AbsoluteUri));
                if (!string.IsNullOrWhiteSpace(item.Permalink))
                    itemElement.Add(new XElement("guid", item.Permalink));
                var dateFmt = item.PublishDate.ToString("r");
                if (item.PublishDate != DateTime.MinValue) itemElement.Add(new XElement("pubDate", dateFmt));


                if (item.Enclosures != null && item.Enclosures.Any())
                {
                    foreach (var enclosure in item.Enclosures)
                    {
                        var enclosureElement = new XElement("enclosure");
                        foreach (var key in enclosure.Values.AllKeys)
                        {
                            enclosureElement.Add(new XAttribute(key, enclosure.Values[key]));
                        }

                        itemElement.Add(enclosureElement);
                    }
                }
                channel.Add(itemElement);
            }

            return doc.ToStringWithDeclaration(option);
        }
    }

   
} 