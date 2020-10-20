using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class BlogIndexViewModel
    {
        public List<AdvertCategory> AdvertCategories { get; set; }
        public List<BlogPost> BlogPosts { get; set; }
        public int PostCount { get; set; }
        public string Title { get; set; }
    }
}