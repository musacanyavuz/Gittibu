using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using GittiBu.Common;
using GittiBu.Common.Iyzico;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GittiBu.Web.Controllers
{
    public class SupportController : BaseController
    {
        // GET
        [Route("Destek/Hakkimizda")]
        [Route("Support/AboutUs")]
        public IActionResult AboutUs()
        {
            using (var service = new TextService())
            {
                var lang = GetLang();
                var text = service.GetText((int)Enums.Texts.HakkimizdaYazisi, lang);
                return View("AboutUs", text);
            }
        }

        [Route("Destek/Videolar")]
        [Route("Support/Videos")]
        public IActionResult Videos()
        {
            using (var service = new TextService())
            {
                var lang = GetLang();
                var text = service.GetText((int)Enums.Texts.VideolarYazisi, lang);
                return View("Videos", text);
            }
        }

        [Route("Destek/Yardim")]
        [Route("Support/Help")]
        public IActionResult Help()
        {
            using (var service = new TextService())
            {
                var lang = GetLang();
                var texts = service.GetContentArray((int)Enums.ContentArrayType.Yardim, lang);
                return View("Help", texts);
            }
        }

        [Route("Destek/KullanimKosullari")]
        [Route("Support/TermsOfUse")]
        public IActionResult TermsOfUse()
        {
            using (var service = new TextService())
            {
                var lang = GetLang();
                var texts = service.GetContentArray((int)Enums.ContentArrayType.KullanimKosullari, lang);
                return View("TermsOfUse", texts);
            }
        }

        [Route("Destek/GuvenliOdemeGittiBu")]
        [Route("Support/SecurePayment")]
        public IActionResult SecurePayment()
        {
            using (var service = new TextService())
            {
                var lang = GetLang();
                var texts = service.GetContentArray((int)Enums.ContentArrayType.GuvenliOdeme, lang);
                return View("SecurePayment", texts);
            }
        }

        [Route("Destek/Ucretler")]
        [Route("Support/Fees")]
        public IActionResult Prices()
        {

            using (var dopingService = new PublicService())
            using (var service = new TextService())
            {
                var lang = GetLang();
                var text = service.GetText((int)Enums.Texts.Ucretler, lang);
                ViewBag.Prices = dopingService.GetDopingTypes().OrderBy(d => d.Name).ToList();
                return View("Prices", text);
            }
        }

        [Route("Destek/Iletisim")]
        [Route("Support/Contact")]
        public IActionResult Contact()
        {
            return View("Contact");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PostForm(IFormCollection collection)
        {
            var lang = GetLang();
            try
            {
                var validation = false;
                if (collection.Keys.Contains("g-recaptcha-response"))
                {
                    var response = collection["g-recaptcha-response"];
                    const string secret = Constants.RecaptchaSecretKey;
                    var client = new WebClient();
                    var reply =
                        client.DownloadString(
                            string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", secret,
                                response));
                    var captchaResponse = JsonConvert.DeserializeObject<CaptchaResponse>(reply);
                    validation = captchaResponse.Success;
                }

                if (!validation)
                    Notification = new UiMessage(NotyType.error, "Doğrulama işlemi başarısız oldu.", "Verification failed.", lang);

                if (validation)
                {
                    var model = new FormReturn()
                    {
                        UserID = GetLoginID(),
                        Name = collection["name"],
                        Email = collection["email"],
                        Subject = collection["subject"],
                        Message = collection["message"],
                        CreatedDate = DateTime.Now,
                        IpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString()
                    };
                    using (var settings = new SystemSettingService())
                    using (var mailing = new MailingService())
                    using (var service = new FormReturnService())
                    {
                        var insert = service.Insert(model);
                        if (!insert)
                            Notification = new UiMessage(NotyType.error, "Mesajınız iletilemedi.", "Your message send failed.", lang);

                        Notification = new UiMessage(NotyType.success, "Mesajınız kayıt edildi. En kısa zamanda sizinle iletişime geçeceğiz.",
                            "Your message has been saved. We will contact you as soon as possible.", lang);

                        var admin = settings.GetSystemSettings().Single(x => x.Name == Enums.SystemSettingName.IletisimFormuMail).Value;
                        var message = "Bir iletişim formu dolduruldu.<br/>" +
                        "Ad Soyad: " + model.Name + " <br/>" +
                        "Email: " + model.Email + " <br/>" +
                        "Konu: " + model.Subject + " <br/>" +
                        "Mesaj: " + model.Message + " <br/>" +
                        "Ip Adresi: " + model.IpAddress + " <br/>" +
                        "Tarih: " + DateTime.Now.ToString("dd.MM.yyyy HH:ss") + " <br/>";

                        mailing.SendMail2Admins(admin, "İletişim Formu Dolduruldu", message);
                    }
                }
            }
            catch (Exception e)
            {
                Notification = new UiMessage(NotyType.error, "Beklenmedik bir hata oluştu.", "An unexpected error has occurred.", lang);
            }
            return Redirect("/");
        }
        [Route("GetInstallmentPrices")]
        [HttpPost]
        public PartialViewResult GetInstallmentPrices(double price)
        {
            var result = IyzicoService.GetInstallmentInfoAllBanks(price);
            if (result.IsSuccess == false)
            {
                return null;
            }

            try
            {
                using (var service = new SystemSettingService())
                {
                    var settings = service.GetSystemSettings();
                    var gittiBuKomisyonuYuzde = double.Parse(settings
                        .Single(x => x.Name == Enums.SystemSettingName.GittiBuKomisyonuYuzde).Value);
                    var iyzicoCommission = double.Parse(settings
                        .Single(x => x.Name == Enums.SystemSettingName.IyzicoKomisyonuYuzde).Value);
                    var iyzicoCommissionTL = double.Parse(settings
                        .Single(x => x.Name == Enums.SystemSettingName.IyzicoKomisyonuTL).Value); //iyzico sabit TL komisyon

                    var list = new List<InstallmentPrice>();
                    foreach (var detail in result.Data.InstallmentDetails)
                    {
                        var card = new InstallmentPrice()
                        {
                            BankCode = detail.BankCode.ToString(),
                            BankName = detail.BankName,
                            CardName = detail.CardFamilyName,
                            Details = new List<InstallmentPriceDetail>()
                        };
                        foreach (var installmentPrice in detail.InstallmentPrices)
                        {
                            //if (installmentPrice.InstallmentNumber == 1)
                            //    continue;

                            var p = double.Parse(installmentPrice.TotalPrice.Replace(".", ","));
                            var bankCommission = p - price; //taksitler için alınan banka komisyonu
                            var apComission = (price * gittiBuKomisyonuYuzde) / 100; //audiophile komisyonu
                            var iyzCommission = 0.0;
                            if (installmentPrice.InstallmentNumber == 1)
                            { //taksitli işlemlerde taksit farkına iyzico komisyonu dahil geliyor
                                iyzCommission = (price * iyzicoCommission) / 100; // iyzico yüzdelik komisyon
                                iyzCommission += iyzicoCommissionTL;
                            }

                            var subMerchantPrice = price - bankCommission - apComission - iyzCommission;

                            card.Details.Add(new InstallmentPriceDetail
                            {
                                InstallmentNumber = installmentPrice.InstallmentNumber,
                                Price = installmentPrice.Price,
                                TotalPrice = price.ToString("0.##"),
                                SubmerchantPrice = subMerchantPrice.ToString("0.##")
                            });
                            installmentPrice.TotalPrice = p.ToString("0.##");
                        }
                        list.Add(card);
                    }
                    return PartialView("~/Views/Partials/InstallmentPrices.cshtml", list);
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}