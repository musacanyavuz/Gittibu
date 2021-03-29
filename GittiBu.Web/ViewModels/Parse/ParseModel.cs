using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;

namespace GittiBu.Web.ViewModels.Parse
{
    public class ParseModel
    {
        public int ID { get; set; }
        public IFormFile file { get; set; }
        public Stream FileStream { get; set; }
        public string fileLink { get; set; }
        public string RootName { get; set; }
        public string ProductID { get; set; }

        public List<string> Fields { get; set; }
        public string[] Title { get; set; }
        public string[] Content { get; set; }
        public string Price { get; set; }
        public string NewProductPrice { get; set; }
        public string ProductStatus { get; set; }
        public string Category { get; set; }
        public string[] Photos { get; set; }
        public string StockAmount { get; set; }
        public string CargoAreaID { get; set; }
        public string ShippingPrice { get; set; }
        public int UserID { get; set; }
        public string IsActive { get; set; }
        public int PriceFilter { get; set; }
        public int StockFilter { get; set; }
        public int PriceDiscount { get; set; }
        public int NewPriceDiscount { get; set; }
        public bool FreeShipping { get; set; }
        public string Supplier { get; set; }
        public int MaxInstallment { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ChildNames { get; set; }
        public int[] XMLCategoryMatches { get; set; }
        public string[] XMLCategoryNames { get; set; }
    }
}
