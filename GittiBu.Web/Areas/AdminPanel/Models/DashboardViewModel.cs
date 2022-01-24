using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.Areas.AdminPanel.Models
{
    public class DashboardViewModel
    {
        public List<Advert> Adverts { get; set; }
        public List<AdvertPublishRequest> AdvertPublishRequests { get; set; }
        public List<PaymentRequest> PaymentRequests { get; set; }
        public List<User> Users { get; set; }
    }
}