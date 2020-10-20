using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class AddBannerViewModel
    {
        public List<DopingType> LogoDopings { get; set; }
        public List<DopingType> BannerDopings { get; set; }
    }
}