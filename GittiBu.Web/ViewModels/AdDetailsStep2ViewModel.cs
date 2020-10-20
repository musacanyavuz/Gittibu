using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class AdDetailsStep2ViewModel
    {
        public Advert Advert { get; set; }
        public List<CargoArea> CargoAreas { get; set; }
    }
}