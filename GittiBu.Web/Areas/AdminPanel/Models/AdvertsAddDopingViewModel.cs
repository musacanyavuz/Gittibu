using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.Areas.AdminPanel.Models
{
    public class AdvertsAddDopingViewModel
    {
        public Advert Advert { get; set; }
        public List<DopingType> DopingTypes { get; set; }
    }
    public class AdvertEditModel
    {
        public string Title { get; set; }
        public int Id { get; set; }
    }
}