using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class BlogExtendTimeViewModel
    {
        public List<DopingType> Dopings { get; set; }
        public BlogPost Post { get; set; }
    }
}