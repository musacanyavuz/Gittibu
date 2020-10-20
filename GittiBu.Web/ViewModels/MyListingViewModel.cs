using GittiBu.Models;
using System.Collections.Generic;

namespace GittiBu.Web.ViewModels
{
    public class MyListingViewModel
    {
        public List<Advert> Adverts { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PagingAdvertCount { get; set; }

    }
}
