using System;
using System.Collections.Generic;
using System.Linq;
using GittiBu.Common;
using GittiBu.Common.Iyzico;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.ViewModels;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InstallmentPrice = GittiBu.Web.ViewModels.InstallmentPrice;

namespace GittiBu.Web.Controllers
{
    public class CheckoutController : BaseController
    {
        // GET
        [Route("SatinAl/AdresSec")]
        [Route("Checkout/SelectAddress")]
        [Authorize]
        public IActionResult SelectAddress(int id, int amount)
        {
            using (var userService = new UserService())
            using (var adService = new AdvertService())
            {

                var loginId = GetLoginID();
                var model = new CheckoutSelectAddressViewModel
                {
                    Advert = adService.Get(id),
                    Addresses = userService.GetUserAddresses(GetLoginID()),
                    Amount = amount
                };
                LogInsert(loginId + " idli kullanıcı " + id + " ürünü almak için adres sayfasına giriş yaptı.", Enums.LogType.Payment, loginId, id);
                return View(model);
            }
        }

        [Route("SatinAl/Odeme")]
        [Route("Checkout/Payment")]
        [Authorize]
        public IActionResult Payment(int id, int amount, int shippingAddressId, int invoiceAddressId)
        {
            var lang = GetLang();
            //using (var userService = new UserService())
            using (var adService = new AdvertService())
            {
                var advert = adService.Get(id);
                var loginId = GetLoginID();

                var result = IyzicoService.GetInstallmentInfoAllBanks(advert.Price);
                if (result.IsSuccess == false)
                {
                    Notification = new UiMessage(NotyType.error, "Taksit bilgilerine erişilemedi. " + result.Message,
                        "Couldn't access to installment choices.", lang);
                    LogInsert(loginId + " idli kullanıcı " + id + " ürünü alırken taksitlendirme bilgilerine erişlemedi." + result.Message, Enums.LogType.Payment, loginId, id);

                }
                var model = new CheckoutPaymentViewModel
                {
                    ShippingAddressID = shippingAddressId,
                    InvoiceAddressID = invoiceAddressId,
                    InstallmentDetails = result.Data.InstallmentDetails,
                    Amount = amount,
                    Advert = advert
                };
                LogInsert(loginId + " idli kullanıcı " + id + " ürünü almak için kredi kartı bilgilerini doldurma sayfasına yönlendirildi.", Enums.LogType.Payment, loginId, id);

                return View(model);
            }
        }

        [Authorize]
        public IActionResult SecurePayment(CheckoutPaymentPostViewModel model)
        {
            var lang = GetLang();
            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            var loginId = GetLoginID();
            if (string.IsNullOrEmpty(model.CardName) || string.IsNullOrEmpty(model.CardNumber) ||
                string.IsNullOrEmpty(model.CardExpireMonth) || string.IsNullOrEmpty(model.CardExpireYear) ||
                string.IsNullOrEmpty(model.CardCVC))
            {
                Notification = new UiMessage(NotyType.error, "Lütfen tüm kredi kartı bilgilerini doldurunuz.",
                    "Please fill in all credit card information.", lang);
                LogInsert(loginId + " idli kullanıcı " + model.id + " ürünü almak için kredi kartı bilgilerini eksik doldurdu hatasıyla karşılaştı.", Enums.LogType.Payment, loginId, model.id);

                return RedirectToAction("Payment", new
                {
                    model.id,
                    amount = model.Amount,
                    shippingAddressId = model.ShippingAddressId,
                    invoiceAddressId = model.InvoiceAddressId
                });
            }
            using (var uService = new UserService())
            using (var prService = new PaymentRequestService())
            using (var sysService = new SystemSettingService())
            using (var adService = new AdvertService())
            {
                var ad = adService.Get(model.id);
                var user = uService.Get(GetLoginID());

                if (ad == null)
                {
                    Notification = new UiMessage(NotyType.error, "İlana erişilemedi.", "Could not access to ad.", lang);
                    LogInsert(loginId + " idli kullanıcı " + model.id + " ürünü alırken ilana erişilemedi hatasıyla karşılaştı.", Enums.LogType.Payment, loginId, model.id);
                    return Redirect("/");
                }
                if (user == null)
                {
                    Notification = new UiMessage(NotyType.error, "Kullanıcı bilgilerinize erişilemedi.", "Could not access to your user account.", lang);
                    LogInsert(loginId + " idli kullanıcı " + model.id + " ürünü alırken kullanıcı bilgilerinize erişilemedi hatasıyla karşılaştı.", Enums.LogType.Payment, loginId, model.id);
                    return Redirect("/");
                }
                var sellerInfo = uService.GetSecurePaymentDetail(ad.UserID);
                if (sellerInfo == null)
                {
                    Notification = new UiMessage(NotyType.error, "Satıcının satış bilgilerine erişilemedi. Lütfen bizimle irtibata geçiniz.",
                        "The vendor's sales information could not be accessed. Please contact us.", lang);
                    LogInsert(loginId + " idli kullanıcı " + model.id + " ürünü alırken satıcının satış bilgilerine erişilemedi. Bizimle irtibata geçiniz hatasıyla karşılaştı.", Enums.LogType.Payment, loginId, model.id);
                    return Redirect("/");
                }
                var totalPrice = ad.Price * model.Amount;

                var result = IyzicoService.GetInstallmentInfoAllBanks(model.CardNumber.Replace(" ", "").Substring(0, 6), totalPrice.ToString("0.##"));
                if (!result.IsSuccess)
                {
                    Notification = new UiMessage(NotyType.error, "Taksit bilgilerine erişilemedi.", "Could not access to installment details.", lang);
                    LogInsert(loginId + " idli kullanıcı " + model.id + " ürünü alırken taksit bilgilerine erişlemedi hatasıyla karşılaştı.", Enums.LogType.Payment, loginId, model.id);
                    return Redirect("/");
                }

                #region subMerchantPrice

                var bankTotalPrice = result.Data.InstallmentDetails.First().InstallmentPrices
                    .Single(x => x.InstallmentNumber == model.InstallmentNumber).TotalPrice;
                var bankComission = double.Parse(bankTotalPrice.Replace(".", ",")) - totalPrice;

                var settings = sysService.GetSystemSettings();
                var beniOdeCommission = double.Parse(settings
                    .Single(x => x.Name == Enums.SystemSettingName.GittiBuKomisyonuYuzde).Value);
                var iyzicoCommission = double.Parse(settings
                    .Single(x => x.Name == Enums.SystemSettingName.IyzicoKomisyonuYuzde).Value);
                var iyzicoCommissionTL = double.Parse(settings
                    .Single(x => x.Name == Enums.SystemSettingName.IyzicoKomisyonuTL).Value); //iyzico sabit TL komisyon

                var apComission = (totalPrice * beniOdeCommission) / 100; //audiophile komisyonu
                var iyzCommission = 0.0;
                if (model.InstallmentNumber == 1)
                {
                    iyzCommission = (totalPrice * iyzicoCommission) / 100; // iyzico yüzdelik komisyon
                    iyzCommission += iyzicoCommissionTL;
                }

                var subMerchantPrice = totalPrice - bankComission - apComission - iyzCommission;

                #endregion

                user.Addresses = uService.GetUserAddresses(GetLoginID());
                var shippingAddress = user.Addresses.Single(x => x.ID == model.ShippingAddressId);
                var invoiceAddress = user.Addresses.Single(x => x.ID == model.InvoiceAddressId);

                var shippingAddressLine =
                    $"{shippingAddress.Address} {shippingAddress.City?.Name} {shippingAddress.District?.Name}{shippingAddress.CityText} {shippingAddress?.Country?.Name}";
                var invoiceAddressLine =
                    $"{invoiceAddress.Address} {invoiceAddress.City?.Name} {shippingAddress.District?.Name}{invoiceAddress.CityText} {invoiceAddress?.Country?.Name}";

                var paymentRequest = new PaymentRequest
                {
                    Price = totalPrice,
                    InstallmentNumber = model.InstallmentNumber,
                    Description = $"#{GetLoginID()} {user.Name} isimli kullanıcı ürün satın alma işlemi başlattı: #{ad.ID} {ad.Title}",
                    CreatedDate = DateTime.Now,
                    AdvertID = ad.ID,
                    IsSuccess = false,
                    IpAddress = ip,
                    SellerID = ad.UserID,
                    SecurePayment = true,
                    Type = Enums.PaymentType.Ilan,
                    UserID = GetLoginID(),
                    ShippingAddress = shippingAddressLine,
                    InvoiceAddress = invoiceAddressLine,
                    Amount = model.Amount,
                    Status = Enums.PaymentRequestStatus.Bekleniyor
                };
                var prInsert = prService.Insert(paymentRequest);
                LogInsert(loginId + " idli kullanıcı " + model.id + " ürünü satın alma işlemini başlattı..", Enums.LogType.Payment, loginId, model.id);

                if (!prInsert)
                {
                    Notification = new UiMessage(NotyType.error, "Online ödeme kaydınız oluşturulamadı.", "Your online payment registration could not be saved..", lang);
                    LogInsert(
                        $"{loginId} idli kullanıcı {model.id} ürünü satın alma esnasında ödeme kaydı oluşturulamadı hatası ile karşılaşıldı.", Enums.LogType.Payment, loginId, model.id);
                    return Redirect("/");
                }

                var card = new PaymentCard
                {
                    CardNumber = model.CardNumber?.Replace(" ", ""),
                    CardHolderName = model.CardName,
                    Cvc = model.CardCVC,
                    ExpireMonth = model.CardExpireMonth,
                    ExpireYear = model.CardExpireYear
                };
                user.Name = user.Name.Trim();
                var name = user.Name;
                var surName = "";
                if (user.Name.Contains(" "))
                {
                    name = user.Name.Substring(0, user.Name.LastIndexOf(' ') + 1);
                    surName = user.Name.Substring(user.Name.LastIndexOf(' ') + 1);
                }
                var buyer = new Buyer
                {
                    Name = name,
                    Surname = surName,
                    Email = user.Email,
                    Id = user.ID.ToString(),
                    IdentityNumber = sellerInfo.TC,
                    GsmNumber = string.IsNullOrEmpty(user.MobilePhone) ? null : user.MobilePhone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""),
                    Ip = ip,
                    City = shippingAddress.City?.Name ?? shippingAddress.CityText,
                    Country = shippingAddress?.Country?.Name ?? "Türkiye",
                    RegistrationAddress = shippingAddressLine
                };

                var _shippingAddress = new Address
                {
                    City = shippingAddress.City?.Name ?? shippingAddress.CityText,
                    Country = shippingAddress.Country?.Name ?? "Turkey",
                    ContactName = user.Name,
                    Description = shippingAddress.Address + " " + shippingAddress.District?.Name + " " + shippingAddress.City?.Name +
                        shippingAddress.CityText + " " + shippingAddress.Country?.Name,
                };
                var _invoiceAddress = new Address()
                {
                    City = invoiceAddress.City?.Name ?? invoiceAddress.CityText,
                    Country = invoiceAddress.Country?.Name ?? "Turkey",
                    ContactName = user.Name,
                    Description = invoiceAddress.Address + " " + invoiceAddress.District?.Name + " " + shippingAddress.City?.Name
                        + invoiceAddress.CityText + " " + invoiceAddress.Country?.Name,
                };

                var securePayment = IyzicoService.SecurePayment(paymentRequest.ID, totalPrice, card, buyer,
                    _shippingAddress,
                    _invoiceAddress, ad.ID, "Checkout/SecurePaymentCallback", sellerInfo.IyzicoSubMerchantKey,
                    subMerchantPrice.ToString("0.##"),
                    model.InstallmentNumber, ad.Title, ad.SubCategorySlugTr, ad.SubCategorySlugEn);
                if (securePayment.Status == "success") return View("PayRedirect", securePayment.HtmlContent);
                if (securePayment.ErrorCode == "12")
                {

                }

                switch (securePayment.ErrorCode)
                {
                    case "15":
                        Notification = new UiMessage(NotyType.error, "Güvenli ödeme hatası. CVC Kodu hatalı.",
                            "CVC is wrong. Secure Payment Error: " + securePayment.ErrorMessage, lang);
                        break;
                    case "17":
                        Notification = new UiMessage(NotyType.error, "Güvenli ödeme hatası. Son kullanım tarihi hatalı .",
                            "Defective month and year. Secure Payment Error: " + securePayment.ErrorMessage, lang);
                        break;
                    case "12":
                        Notification = new UiMessage(NotyType.error, "Güvenli ödeme hatası. Geçersiz kart.",
                            "Invalid card. Secure Payment Error: " + securePayment.ErrorMessage, lang);
                        break;
                    default:
                        Notification = new UiMessage(NotyType.error, "Güvenli ödeme hatası. Güvenli Ödeme Bilgilerinizde bir Hata veya Eksikliğiniz var, Entegrasyon Oluşturulamıyor",
                            "Secure Payment Error: " + securePayment.ErrorMessage, lang);
                        break;
                }

                LogInsert(loginId + " idli kullanıcı " + model.id + " ürünü satın alırken güvenli ödeme hatasıyla karşılaştı. Hata Kodu:" + securePayment.ErrorCode + " Hata Mesajı: " + securePayment.ErrorMessage, Enums.LogType.Payment, loginId, model.id);

                return Redirect("/");
            }
        }

        public enum ErrorCode
        {
            CVCHata = 15,
            AyYilHata = 17,
            GecersizKart = 12

        }

        [Route("Checkout/SecurePaymentCallback")]
        public IActionResult SecurePaymentCallback(Iyzico3dCallback callback)

        {
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.Alislarim, lang);
            var getUserId = GetLoginID();
            if (string.IsNullOrEmpty(callback.PaymentId) || callback.PaymentId == "0")
            {
                Notification = new UiMessage(NotyType.error, new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                    10000);
                if (callback.Status != null && callback.PaymentId != null)
                {
                    LogInsert(getUserId + " idli kullanıcı  satın alırken 3D Güvenlik hatasını aldı." + callback.Status, Enums.LogType.Payment, GetLoginID(), int.Parse(callback.PaymentId));

                }
                else
                {
                    LogInsert(callback.ConversationId + " idli  ödeme (Conversation) satın alırken 3D Güvenlik hatasını aldı.", Enums.LogType.Payment, GetLoginID());

                }
                return Redirect(route);
            }
            var request = new CreateThreedsPaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = callback.ConversationId.ToString(),
                PaymentId = callback.PaymentId,
                ConversationData = callback.ConversationData
            };

            var threedsPayment = ThreedsPayment.Create(request, IyzicoService.GetOptions());
            if (threedsPayment.Status == "success")
            {
                using (var adService = new AdvertService())
                using (var uService = new UserService())
                using (var notifyService = new NotificationService())
                using (var service = new PaymentRequestService())
                {
                    var payReq = service.Get(Convert.ToInt32(callback.ConversationId));
                    if (payReq != null)
                    {
                        payReq.PaymentId = threedsPayment.PaymentId;
                        payReq.ResponseDate = DateTime.Now;
                        payReq.IsSuccess = true;
                        payReq.PaymentTransactionID = threedsPayment.PaymentItems?.First().PaymentTransactionId;
                        var update = service.Update(payReq);
                        if (update)
                        {
                            var ad = adService.Get(payReq.AdvertID);
                            if (ad != null)
                            {
                                ad.StockAmount = ad.StockAmount - payReq.Amount;
                                ad.IsActive = (ad.StockAmount > 0);
                                adService.Update(ad);
                            }

                            var seller = uService.Get(payReq.SellerID);
                            var buyer = uService.Get(payReq.UserID);
                            var sellerInfo = uService.GetSecurePaymentDetail(seller.ID);

                            //mailing.SendSaleMail2Seller(seller, buyer, ad, payReq.ShippingAddress, payReq.InvoiceAddress, payReq.Amount);
                            SendSaleMail2Seller(seller, buyer, ad, payReq.InvoiceAddress, payReq.Amount, payReq.ID);
                            SendSaleMail2Buyer(seller, buyer, ad, payReq.ShippingAddress, payReq.Amount, payReq.ID);

                            var notify = new Notification()
                            {
                                UserID = seller.ID,
                                CreatedDate = DateTime.Now,
                                TypeID = Enums.NotificationType.IlanSatildi,
                                Message = new Localization().Get(
                                    "Sattığınız " + ad.ID + " nolu ürünün ödemesi yapıldı, kargoya vermeniz gerekiyor.",
                                    "Item " + ad.ID + " has been sold, you need to shipping to buyer.", seller.LanguageID),
                                Image = ad.Thumbnail,
                                Url = Constants.GetURL((int)Enums.Routing.Satislarim, seller.LanguageID),
                                SenderUserID = buyer.ID
                            };
                            notifyService.Insert(notify);
                            var notifyBuyer = new Notification
                            {
                                UserID = buyer.ID,
                                Image = ad.Thumbnail,
                                CreatedDate = DateTime.Now,
                                Url = Constants.GetURL((int)Enums.Routing.Alislarim, seller.LanguageID),
                                TypeID = Enums.NotificationType.SistemMesaji,
                                Message = new Localization().Get("GittiBu'dan bir ürün satın aldınız. Alıcının kargolaması bekleniyor.", "You purchased a product and waiting for delivery", lang)
                            };
                            notifyService.Insert(notifyBuyer);
                            LogInsert(getUserId + " idli kullanıcı " + ad.ID + " idli ürünün ödeme işlemi tamamlandı.", Enums.LogType.Payment, GetLoginID(), ad.ID);
                            Notification = new UiMessage(NotyType.success, new Localization().Get("Ödeme işlemi tamamlandı", "Payment completed", lang),
                                10000);
                            return Redirect(route);
                        }
                    }
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin."
                            , "Payment completed but could not be saved in the system. Please contact the site administration.", lang),
                        10000);
                    LogInsert(getUserId + " idli kullanıcı " + callback.PaymentId + " ödeme idli alırken Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin hatasını aldı", Enums.LogType.Payment, GetLoginID(), int.Parse(callback.PaymentId));
                    return Redirect(route);
                }

            }

            Notification = new UiMessage(NotyType.error, threedsPayment.ErrorMessage, 10000);
            LogInsert(getUserId + " idli kullanıcı " + callback.PaymentId + " ödeme idli ürünü alırken " + threedsPayment.ErrorMessage + " hatasını aldı.", Enums.LogType.Payment, GetLoginID(), int.Parse(callback.PaymentId));

            return Redirect(route);
        }

        private void SendSaleMail2Seller(User seller, User buyer, Advert ad, string payReqInvoiceAddress, int payReqAmount, int id)
        {
            using (var mail = new MailingService())
            using (var setting = new SystemSettingService())
            using (var text = new TextService())
            {
                var day = setting.GetSystemSettings()
                    .FirstOrDefault(p => p.Name == Enums.SystemSettingName.OtomatikOdemeOnayi)
                    ?.Value;
                var lang = GetLang();
                var content = text.GetContent(Enums.Texts.SaticiyaUrunSatildi, lang);
                var html = content.TextContent;
                var title = content.Name;
                var replacedHtml = html.Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@urunAdi", ad.Title)
                    .Replace("@urunAdet", payReqAmount.ToString())
                    .Replace("@siparisNo", id.ToString())
                    .Replace("@cargo", (ad.FreeShipping ? "Satıcı Öder" : "Alıcı Öder"))
                    .Replace("@aliciAdSoyad", buyer.Name)
                    .Replace("@aliciMail", buyer.Email)
                    .Replace("@aliciTel", buyer.MobilePhone)
                    .Replace("@aliciAdres", payReqInvoiceAddress)
                    .Replace("@onaySuresi", day)
                    .Replace("@prId", id.ToString());
                mail.Send(replacedHtml, seller.Email, seller.Name, title);
            }
        }
        private void SendSaleMail2Buyer(User seller, User buyer, Advert ad, string payReqShippingAddress,
            int reqAmount,
            int id)
        {
            using (var mail = new MailingService())
            using (var text = new TextService())
            {
                var lang = GetLang();
                var content = text.GetContent(Enums.Texts.AliciyaUrunSatinAldiniz, lang);
                var html = content.TextContent;
                var title = content.Name;
                var replacedHtml = html.Replace("@aliciAdSoyad", buyer.Name)
                    .Replace("@urunAdi", ad.Title)
                    .Replace("@urunAdet", reqAmount.ToString())
                    .Replace("@siparisNo", id.ToString())
                    .Replace("@cargo", (ad.FreeShipping ? "Satıcı Öder" : "Alıcı Öder"))
                    .Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@saticiMail", seller.Email)
                    .Replace("@saticiTel", seller.MobilePhone)
                    .Replace("@saticiAdres", payReqShippingAddress)
                    .Replace("@prId", id.ToString());
                mail.Send(replacedHtml, buyer.Email, buyer.Name, title);
            }
        }


        public PartialViewResult GetInstallmentOptions(string cardNumber, string totalPrice, int id)
        {
            if (cardNumber == null || cardNumber.Length < 6)
                return null;

            var result = IyzicoService.GetInstallmentInfoAllBanks(cardNumber.Replace(" ", "").Substring(0, 6), totalPrice);
            if (result.IsSuccess == false)
                return null;

            var data = result.Data.InstallmentDetails.First();
            var model = new InstallmentPrice
            {
                BankName = data.BankName,
                CardName = data.CardFamilyName,
                Details = new List<InstallmentPriceDetail>()
            };
            using (var service = new AdvertService())
            {
                var ad = service.Get(id);
                if (ad == null)
                    return null;

                foreach (var installmentPrice in data.InstallmentPrices)
                {
                    if (ad.AvailableInstallments.Contains(installmentPrice.InstallmentNumber?.ToString()))
                    {
                        var detail = new InstallmentPriceDetail
                        {
                            TotalPrice = totalPrice,
                            InstallmentNumber = installmentPrice.InstallmentNumber,
                            Price = (double.Parse(totalPrice) / installmentPrice.InstallmentNumber)?.ToString("0.##")
                        };
                        model.Details.Add(detail);
                    }
                }
            }

            return PartialView("~/Views/Partials/CheckoutInstallmentOptions.cshtml", model);
        }
    }
}