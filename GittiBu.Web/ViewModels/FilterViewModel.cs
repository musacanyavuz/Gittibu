namespace GittiBu.Web.ViewModels
{
    public class FilterViewModel
    {
        public int ParentCategoryID { get; set; }
        public int CategoryID { get; set; }
        public string Content { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int AdvertID { get; set; }
        public int PriceMin { get; set; }
        public int PriceMax { get; set; }
    }
}