using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class AddListingStep1ViewModel
    {
        public List<AdvertCategory> AdvertCategories { get; set; }
        public Advert Advert { get; set; }
        public bool CanUseSecurePayment { get; set; }
        public List<CargoArea> CargoAreas { get; set; }
        public int Cash { get; set; }
    }
}