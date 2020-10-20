using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class BlogPostDetailViewModel
    {
        public BlogPost BlogPost { get; set; }
        public List<AdvertCategory> AdvertCategories { get; set; }
        public int PostCount { get; set; }
        public string Title { get; set; }
    }
}