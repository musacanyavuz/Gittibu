using System.Text;

namespace GittiBu.Common
{
    public class Localization
    {
        public string Get(string tr, string en, int lang)
        {
            switch (lang)
            {
                case (int)Enums.Languages.tr:
                    byte[] bytes = Encoding.Default.GetBytes(tr);
                    tr = Encoding.UTF8.GetString(bytes);
                    return System.Text.Encoding.UTF8.GetString(bytes); 
                case (int)Enums.Languages.en:
                    return en;
            }
            return tr;
        } 
        
        public static string Slug(string s)
        {
            return s.Replace("ı", "i").Replace("İ", "I").Replace("ö", "o").Replace("Ö", "O").Replace("ğ", "g")
                .Replace("Ğ", "G").Replace("ü", "u").Replace("Ü", "U").Replace("ş", "s").Replace("Ş", "S")
                .Replace("ç", "c").Replace("Ç", "C").Replace(".", "").Replace(",", "").Replace(":", "")
                .Replace(";", "").Replace("!", "").Replace("?", "").Replace("'", "").Replace("<", "").Replace(">", "")
                .Replace("&", "").Replace("-", "").Replace(" ", "-").Replace("_", "").Replace("%", "").Replace("/", "")
                .Replace("(", "").Replace(")", "").Replace("\\", "")
                .Replace("+","");
        }
    }
}