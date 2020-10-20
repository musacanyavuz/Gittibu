using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class CategoryViewModel
    {
        public AdvertCategory ParentCategory { get; set; }
        public AdvertCategory ChildCategory { get; set; }
        public List<AdvertCategory> AdvertCategories { get; set; }
        public int AdvertsCount { get; set; }
        public List<HomePageItem> Adverts { get; set; }
    }
}