using GittiBu.Models;
using System.Collections.Generic;

namespace GittiBu.Web.ViewModels.Parse
{
    public class ParseViewModel
    {
        public List<ParseMatchModel> ParseFieldMatches { get; set; }
        public List<ComboModel> CargoAreas { get; set; }
        public List<ComboModel> ProductStatus { get; set; }
        public List<AdvertCategory> AdvertCategories { get; set; }
        public string MinimumPrice { get; set; } 
    }
}
