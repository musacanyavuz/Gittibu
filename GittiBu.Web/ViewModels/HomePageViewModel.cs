using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class HomePageViewModel
    {
        public List<AdvertCategory> AdvertCategories { get; set; }   
        public List<Banner> Banners { get; set; }
        public List<Banner> HomepageSplitBanners { get; set; }
        public Banner HomepageBottomAd { get; set; }
        public List<HomePageItem> Items { get; set; }
        public List<Advert> Adverts { get; set; }
        public SeoKeys SeoValues { get; set; }
    }
}