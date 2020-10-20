using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.ViewModels
{
    public class EditAddressViewModel
    {
        public UserAddress UserAddress { get; set; }
        public List<Country> Countries { get; set; }
        public List<City> Cities { get; set; }
        public List<District> Districts { get; set; }
    }
}