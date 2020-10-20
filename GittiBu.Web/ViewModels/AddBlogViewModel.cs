using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class AddBlogViewModel
    {
        public BlogPost BlogPost { get; set; }
        public List<DopingType> Dopings { get; set; }
    }
}