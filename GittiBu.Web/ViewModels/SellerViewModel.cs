using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class SellerViewModel
    {
        public User Seller { get; set; }
        public List<HomePageItem> Ads { get; set; }
    }
}