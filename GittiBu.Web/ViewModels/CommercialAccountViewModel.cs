using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class CommercialAccountViewModel
    {
        public List<PaymentRequest> Buys { get; set; }
        public List<PaymentRequest> Sales { get; set; }
        public List<AdvertDoping> Dopings { get; set; }
        public List<Banner> LogoBanners { get; set; }
        public List<Banner> Banners { get; set; }
        public string Text { get; set; }
    }
}