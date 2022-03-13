using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using GittiBu.Common;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.Helpers;
using GittiBu.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GittiBu.Web.Controllers
{
    public class BaseController : Controller
    {
        public bool Production = true;
        public BaseController()
        {

        }

        public int GetLang(bool setTranslate = true)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userLang = User.Claims.Single(x => x.Type == ClaimTypes.Locality)?.Value;
                if (userLang != null)
                {
                    SetLang(userLang);
                    return Constants.GetLang(userLang);
                }
            }
           

            Localization();
            var lang = Constants.GetLang(HttpContext.Session.GetString("lang"));
            if (setTranslate)
            {
                var x = Request.Path;
                var y = Constants.UrlTranslate(x, lang);
                ViewBag.TranslateUrl = y;
            }
            return lang;
        }

        public string GetIpAddress()
        {
            try
            {
                return HttpContext.Connection.RemoteIpAddress.ToString();
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public void Localization()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("lang")))
            {
                var headerLang = Request.Headers["Accept-Language"].ToString();
                var userLang = Constants.GetUserBrowserLanguage(headerLang);
                if (userLang != null)
                {
                    userLang = userLang == "tr" ? "tr" : "en";
                    SetLang(userLang);
                }
               
            }
        }

        public void SetLang(string lang)
        {
            HttpContext.Session.SetString("lang", lang);
        }

        private int _userId;
        public int UserId
        {
            get
            {
                _userId = int.Parse(User.Claims.Single(x => x.Type == ClaimTypes.UserData).Value);
                return _userId;
            }
        }

        public int GetLoginID()
        {
            try
            {
                var id = int.Parse(User.Claims.Single(x => x.Type == ClaimTypes.UserData).Value);
                return id;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public UiMessage Notification
        {
            get => TempData["ErrorMessage"] == null ? null : TempData.Get<UiMessage>("Notification");
            set => TempData.Put("Notification", new UiMessage() { Type = value.Type, Message = value.Message });
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userLang = User.Claims.Single(x => x.Type == ClaimTypes.Locality)?.Value;
                if (userLang != null)
                {
                    SetLang(userLang);
                }
                using (var uService = new UserService())
                using (var service = new NotificationService())
                using (var parseService = new ParseService())
                using (var advertService = new AdvertService())
                {
                    int loginID = GetLoginID();
                    ViewBag.Notifications = service.GetNotifications(loginID);
                    var list = uService.GetSales(loginID);
                    if (list != null && list.Any())
                    {
                        var count = list.Count(x => x.Status == Enums.PaymentRequestStatus.OnlineOdemeYapildi ||
                                                    x.Status == Enums.PaymentRequestStatus.KargoyaVerildi ||
                                                    x.Status == Enums.PaymentRequestStatus.Onaylandi ||
                                                    x.Status == Enums.PaymentRequestStatus.Bekleniyor);
                        if (count > 0)
                        {
                            ViewBag.PendingCargo = count.ToString();
                        }
                    }

                    var buys = uService.GetBuys(loginID);
                    if (buys != null && buys.Any())
                    {
                        var count = buys.Count(p => p.Status == Enums.PaymentRequestStatus.KargoyaVerildi ||
                                                    p.Status == Enums.PaymentRequestStatus.OnlineOdemeYapildi ||
                                                    p.Status == Enums.PaymentRequestStatus.Onaylandi ||
                                                    p.Status == Enums.PaymentRequestStatus.Bekleniyor
                        );
                        if (count > 0)
                        {
                            ViewBag.Buys = count.ToString();
                        }
                    }
                    ViewBag.AdvertCount = advertService.GetUserAdvertCount(loginID);
                    var xmlCount = parseService.GetUserFilesById(loginID)?.Count;
                    ViewBag.XMLCount = xmlCount > 0 ? xmlCount : null;

                    #region XML Start - Stop Hours Config

                    using (var systemService = new SystemSettingService())
                    {
                        var systemSettings = systemService.GetSetting(Enums.SystemSettingName.xmlstart);
                        if (systemSettings != null)
                        {
                            ViewBag.xmlstart = systemSettings.Value;
                        }
                        systemSettings = systemService.GetSetting(Enums.SystemSettingName.xmlstop);
                        if (systemSettings != null)
                        {
                            ViewBag.xmlstop = systemSettings.Value;
                        }
                    }

                    using (var textService = new TextService())
                    {
                        string xmlMessage = textService.GetText(Enums.Texts.XMLSaatUyarisi, GetLang());
                        ViewBag.xmlMessage = xmlMessage.Replace("@xmlstart", ViewBag.xmlstart).Replace("@xmlstop", ViewBag.xmlstop);
                    }

                    #endregion

                }
            }

            using (var service = new AdvertCategoryService())
            {
                ViewBag.Categories = service.GetMasterCategories(GetLang());
            }

            base.OnActionExecuting(filterContext);
        }

        //dil değiştirildiğinde aynı sayfa içerisinde kalıp routingin de dil değiştirmesi için
        //dil değiştirme linklerine diğer dil linkini ekliyorum
        /*
        public void SetOtherUrl(int routing, int lang)
        { 
            if (lang == 1)
                ViewBag.OtherUrl = Constants.GetURL(routing, 2);
            else
                ViewBag.OtherUrl = Constants.GetURL(routing, 1);
        }
        */
        public FisterResultViewModel Ads2HomePageItems(List<Advert> ads, int totalAdvertCount,int lang)
        {
            var result = new FisterResultViewModel();
            var list = new List<HomePageItem>();
            foreach (var ad in ads)
            {
                list.Add(Ad2HomePageItem(ad, lang));
            }
            result.dataset = list;
            result.TotalAdvertCount = totalAdvertCount;
            return result;
        }
        public List<HomePageItem> Ads2HomePageItems(List<Advert> ads, int lang)
        {
            var result = new List<HomePageItem>();
            if(ads != null)
            foreach (var ad in ads)
            {
                result.Add(Ad2HomePageItem(ad, lang));
            }
            return result;
        }

        public HomePageItem Ad2HomePageItem(Advert ad, int lang)
        {
            var result = new HomePageItem();
            result.ID = ad.ID;
            result.Title = ad.Title;
            result.ILiked = ad.IsILiked;
            result.ViewCount = ad.ViewCount;
            result.Type = Enums.HomePageItemType.Ilan;
            result.CreatedDate = ad.CreatedDate;
            result.ImageSource = ad.Thumbnail;
            result.LikesCount = ad.LikesCount;
            result.AdvertOrder = ad.AdvertOrder;
            result.Price = ad.Price.ToString();
            result.NewProductPrice = $"{ad.NewProductPrice:N} {Constants.GetMoney(ad.MoneyTypeID)}";
            result.DecPrice = $"{ad.Price:N} {Constants.GetMoney(ad.MoneyTypeID)}";
            result.PriceCurrency = Constants.GetMoney(ad.MoneyTypeID);
            result.OldPrice = ad.NewProductPrice + " " + Constants.GetMoney(ad.MoneyTypeID);
            result.Description = ad.Content;
            result.Brand = ad.Brand;
            result.ProductStatus = ad.ProductStatus;
            result.StockAmount = ad.StockAmount.ToString();
            result.CategoryName = lang == 1 ? ad.CategoryNameTr : ad.CategoryNameEn;
            result.LastUpdateDate = ad.LastUpdateDate;
            result.UserName = ad.UserName;
            result.Content = ad.Content;
            result.Url = $"{Constants.GetURL((int)Enums.Routing.Ilan, lang)}/{Common.Localization.Slug(ad.Title)}/{ad.ID}";

            if (ad.LabelDopingModel != null)
            {
                result.LabelDopingType = ad.LabelDopingModel.Name;
            }
            result.YellowDoping = ad.YellowFrameDoping > 0;
            if (ad.UseSecurePayment && !string.IsNullOrEmpty(ad.AvailableInstallments))
            {
                var items = ad.AvailableInstallments.Split(',');
                int[] installments = Array.ConvertAll(items, s => int.Parse(s));
                result.SecurePayment = t("Kredi Kartına ", "Credit Card ", lang) +
                                       installments.Max() +
                                       t(" Taksit", " Settlem", lang);
            }

            return result;
        }

        public static string t(string tr, string en, int lang)
        {
            switch (lang)
            {
                case (int)Enums.Languages.tr:
                    return tr;
                case (int)Enums.Languages.en:
                    return en;
            }
            return tr;
        }

        public string HttpTcKimlikDogrula(ulong tcKimlikNo, string ad, string soyad, ushort dogumYili)
        {
            var tcKimlik = new TCKimlikDogrulama.TCKimlikNoDogrula
            {
                TCKimlikNo = tcKimlikNo,
                Ad = ad,
                Soyad = soyad,
                DogumYili = dogumYili
            };

            var tcKimlikEnvelopeBody = new TCKimlikDogrulama.EnvelopeBody { TCKimlikNoDogrula = tcKimlik };

            var tcKimlikEnvelope = new TCKimlikDogrulama.Envelope { Body = tcKimlikEnvelopeBody };

            var xsSubmit = new XmlSerializer(typeof(TCKimlikDogrulama.Envelope));
            var subReq = new TCKimlikDogrulama.Envelope();
            var xml = "";

            using (var sww = new StringWriter())
            {
                using (var writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, tcKimlikEnvelope);
                    xml = sww.ToString();
                }
            }
            var doc = XDocument.Parse(xml);
            var sDoc = doc.ToString();
            var byteArray = Encoding.UTF8.GetBytes(sDoc);
            var cookies = new CookieContainer();
            var request = (HttpWebRequest)WebRequest.Create("https://tckimlik.nvi.gov.tr/Service/KPSPublic.asmx");
            //request.CookieContainer = cookies;
            //request.Timeout = 999999;
            request.Method = "POST";
            request.ContentType = "text/xml;charset=\"utf-8\"";
            request.ContentLength = byteArray.Length;
            request.Headers.Add("SOAPAction", "http://tckimlik.nvi.gov.tr/WS/TCKimlikNoDogrula");
            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                dataStream = response.GetResponseStream();
                var reader = new StreamReader(dataStream);
                var responseFromServer = reader.ReadToEnd();
                var status = ((HttpWebResponse)response).StatusDescription;
                reader.Close();
                dataStream.Close();
                response.Close();
                return responseFromServer;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public void LogInsert(string message, Enums.LogType type, int? key1 = null, int? key2 = null, int? key3 = null, int? key4 = null, int? key5 = null)
        {
            using (var logService = new LogServices())
            {
                Log log = new Log()
                {
                    Message = message,
                    Type = type,
                    Key1 = key1,
                    Key2 = key2,
                    Key3 = key3,
                    Key4 = key4,
                    Key5 = key5,
                    CreatedDate = DateTime.Now
                };
                logService.Insert(log);
            }

        }
    }
}