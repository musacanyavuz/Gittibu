using System.Collections.Generic;

namespace GittiBu.Web.ViewModels.Parse
{
    public class ParseMatchModel
    {
        public string FieldName { get; set; }
        public bool IsRequired { get; set; }
        public string Setting { get; set; }
        public List<string> DataSampleList { get; set; }
    }
}
