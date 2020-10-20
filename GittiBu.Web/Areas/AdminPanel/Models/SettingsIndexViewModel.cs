using System.Collections.Generic;
using GittiBu.Models;

namespace GittiBu.Web.Areas.AdminPanel.Models
{
    public class SettingsIndexViewModel
    {
        public List<SystemSetting> SystemSettins { get; set; }
        public List<DopingType> Dopings { get; set; }
    }
}