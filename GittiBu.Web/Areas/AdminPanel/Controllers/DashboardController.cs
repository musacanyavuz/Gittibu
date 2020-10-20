using System;
using System.Linq;
using GittiBu.Common;
using GittiBu.Common.Extensions;
using GittiBu.Common.Iyzico;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.Areas.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;


namespace GittiBu.Web.Areas.AdminPanel.Controllers
{
    public class DashboardController : AdminBaseController
    {
        // GET
        public IActionResult Index()
        {
            using (var payService = new PaymentRequestService())
            using (var userService = new UserService())
            using (var adService = new AdvertService())
            {
                var publishRequests = adService.GetPublishRequests();
                var paymentRequests = payService.GetWaitingPaymentRequests();
                var users = userService.GetVerifyPendingUsers();
                paymentRequests = paymentRequests.ToList();
                publishRequests = publishRequests.DistinctBy(x => x.AdvertID).OrderByDescending(p => p.RequestDate).ToList();
                var model = new DashboardViewModel
                {
                    AdvertPublishRequests = publishRequests,
                    PaymentRequests = paymentRequests,
                    Users = users
                };               
               
                return View(model);
            }
        }

        #region İlan Girişi Onayla Reddet

        [HttpPost]
        [Route("AdminPanel/AdvertPublish")]
        public JsonResult AdvertPublish(int id)
        {
            using (var mailing = new MailingService())
            using (var service = new AdvertService())
            {
                var publishRequest = service.GetPublishRequest(id);
                if (publishRequest == null)
                    return Json(new { isSuccess = false, message = "Yayınlama isteği bulunamadı. " });
                if (publishRequest.Advert?.User == null) //ilan veya user boş ise
                    return Json(new { isSuccess = false, message = "İlan bulunamadı." });

                publishRequest.Advert.IsActive = true;
                var adUpdate = service.Update(publishRequest.Advert);
                if (!adUpdate)
                    return Json(new { isSuccess = false, message = "İlan güncellenemedi." });

                publishRequest.IsActive = false;
                var requestUpdate = service.Update(publishRequest);
                if (!requestUpdate)
                    return Json(new
                    { isSuccess = false, message = "İlan güncellendi ancak yayınlama talebi güncellenemedi." });

                var t = new Localization();
                var slug = Localization.Slug(publishRequest.Advert.Title);

                var url = t.Get("Ilan/" + slug + "/" + publishRequest.AdvertID,
                    "Advert/" + slug + "/" + publishRequest.AdvertID, publishRequest.Advert.User.LanguageID);

                SendNotificationMail(publishRequest.Advert.User, url);
                service.DeletePublishRequests(id);
                return Json(new { isSuccess = true, message = "İlan yayınlandı!" });
            }
        }

        private static void SendNotificationMail(User user, string url)
        {
            using (var mailing = new MailingService())
            using (var text = new TextService())
            {
                var content = text.GetContent(Enums.Texts.IlanOnay, user.LanguageID);
                var html = content.TextContent.Replace("@adSoyad", user.Name).Replace("@link", url);
                mailing.Send(html, user.Email, user.Name, content.Name);
            }
        }

        private static void SendNotificationMailRejected(User user, string url, string message)
        {
            using (var mailing = new MailingService())
            using (var text = new TextService())
            {
                var content = text.GetContent(Enums.Texts.IlanRed, user.LanguageID);
                var html = content.TextContent.Replace("@adSoyad", user.Name).Replace("@link", url)
                    .Replace("@redMesaj", message);
                mailing.Send(html, user.Email, user.Name, content.Name);
            }
        }

        [HttpPost]
        [Route("AdminPanel/AdvertReject")]
        public JsonResult AdvertReject(int id, string message)
        {
            using (var mailing = new MailingService())
            using (var service = new AdvertService())
            {
                var pr = service.GetPublishRequest(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Yayınlama isteği bulunamadı. " });
                if (pr.Advert?.User == null) //ilan veya user boş ise
                    return Json(new { isSuccess = false, message = "İlan bulunamadı." });

                pr.IsActive = false;
                var requestUpdate = service.Update(pr);
                if (!requestUpdate)
                    return Json(new
                    { isSuccess = false, message = "Yayınlama talebi güncellenemedi." });

                var t = new Localization();
                var slug = Localization.Slug(pr.Advert.Title);
                var url = t.Get($"Ilan/{slug}/{pr.AdvertID}",
                    $"Advert/{slug}/{pr.AdvertID}", pr.Advert.User.LanguageID);

                SendNotificationMailRejected(pr.Advert.User, url, message);
                return Json(new { isSuccess = true, message = "İlan reddedildi." });
            }
        }

        #endregion

        #region Doping Ödemeleri Onayla Reddet

        [Route("AdminPanel/AcceptDopingPayment")]
        [HttpPost]
        public JsonResult AcceptDopingPayment(int id)
        {
            using (var adService = new AdvertService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı bulunamadı" });

                pr.Status = Enums.PaymentRequestStatus.OtomatikOnaylandi;
                pr.IsSuccess = true;
                pr.UpdatedUserID = GetLoginID();
                var update = service.Update(pr);
                if (!update)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı güncellenemedi" });

                var updateDopings = adService.ActivateAllPendingDopings(pr.AdvertID, pr.ID);

                return Json(new
                {
                    isSuccess = true,
                    message = "Ödeme kaydı onaylandı. " + updateDopings + " adet doping aktifleştirildi."
                });
            }
        }

        [Route("AdminPanel/RejectDopingPayment")]
        [HttpPost]
        public JsonResult RejectDopingPayment(int id)
        {
            using (var adService = new AdvertService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı bulunamadı" });

                pr.Status = Enums.PaymentRequestStatus.Reddedildi;
                pr.IsSuccess = false;
                pr.UpdatedUserID = GetLoginID();
                var update = service.Update(pr);
                if (!update)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı güncellenemedi" });

                adService.PassiveAllPendingDopings(pr.AdvertID, pr.ID);

                if (!string.IsNullOrEmpty(pr.PaymentId))
                {
                    var cancel = IyzicoService.PaymentCancel(pr.ID, pr.PaymentId, GetIpAddress());
                    if (cancel.Status == "success")
                        return Json(new { isSuccess = true, message = "Ödeme kaydı reddedildi." });
                    else
                        return Json(new { isSuccess = false, message = "Ödeme iptal edilemedi. " + cancel.ErrorMessage });
                }

                return Json(new
                {
                    isSuccess = true,
                    message = "Ödeme kaydı reddedildi."
                });
            }
        }

        #endregion

        #region Banner Girişi Onayla Reddet

        [Route("AdminPanel/AcceptBannerPayment")]
        [HttpPost]
        public JsonResult AcceptBannerPayment(int id)
        {
            using (var bannerService = new BannerService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı bulunamadı" });

                pr.Status = Enums.PaymentRequestStatus.OtomatikOnaylandi;
                pr.IsSuccess = true;
                pr.UpdatedUserID = GetLoginID();
                var update = service.Update(pr);
                if (!update)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı güncellenemedi" });

                var banner = bannerService.Get(pr.ForeignModelID);
                if (banner == null)
                    return Json(new { isSuccess = false, message = "Blog yazısı bulunamadı" });

                banner.IsActive = true;
                banner.EndDate = DateTime.Now.AddDays(pr.Amount);
                banner.ApprovedDate = DateTime.Now;
                banner.ApprovedUserID = GetLoginID();
                banner.IsApproved = true;

                update = bannerService.Update(banner);
                if (!update)
                    return Json(new { isSuccess = false, message = "Banner güncellenemedi" });

                return Json(new
                {
                    isSuccess = true,
                    message = "Ödeme kaydı onaylandı. Banner aktifleştirildi."
                });
            }
        }

        [Route("AdminPanel/RejectBannerPayment")]
        [HttpPost]
        public JsonResult RejectBannerPayment(int id)
        {
            using (var bannerService = new BannerService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı bulunamadı" });

                pr.Status = Enums.PaymentRequestStatus.Reddedildi;
                pr.IsSuccess = true;
                pr.UpdatedUserID = GetLoginID();
                var update = service.Update(pr);
                if (!update)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı güncellenemedi" });

                var banner = bannerService.Get(pr.ForeignModelID);
                if (banner == null)
                    return Json(new { isSuccess = false, message = "Blog yazısı bulunamadı" });

                banner.IsActive = false;
                banner.IsApproved = false;
                update = bannerService.Update(banner);
                if (!update)
                    return Json(new { isSuccess = false, message = "Banner güncellenemedi" });

                return Json(new
                {
                    isSuccess = true,
                    message = "Ödeme kaydı reddedildi. Banner yayınlanmayacak."
                });
            }
        }

        #endregion

        #region İlan Satın Alma

        [Route("AdminPanel/AcceptOnlineBuy")]
        [HttpPost]
        public JsonResult AcceptOnlineBuy(int id)
        {
            using (var adService = new AdvertService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı bulunamadı" });

                if (string.IsNullOrEmpty(pr.PaymentId) || string.IsNullOrEmpty(pr.PaymentTransactionID))
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = "Online ödeme tamamlanmamış. Bu işlem onaylanamaz."
                    });
                }

                pr.Status = Enums.PaymentRequestStatus.OtomatikOnaylandi;
                pr.IsSuccess = true;
                pr.UpdatedUserID = GetLoginID();
                var update = service.Update(pr);
                if (!update)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı güncellenemedi" });

                var approval = IyzicoService.PaymentApproval(pr.ID, pr.PaymentTransactionID);
                if (approval.Status != "success")
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = "Iyzico aktarımı yapılamadı. " + approval.ErrorMessage
                    });
                }

                return Json(new
                {
                    isSuccess = true,
                    message = "Ödeme kaydı onaylandı. Iyzico aktarımı yapıldı."
                });
            }
        }

        [Route("AdminPanel/RejectOnlineBuy")]
        [HttpPost]
        public JsonResult RejectOnlineBuy(int id)
        {
            using (var adService = new AdvertService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı bulunamadı" });

                pr.Status = Enums.PaymentRequestStatus.Iptal;
                pr.IsSuccess = false;
                pr.UpdatedUserID = GetLoginID();
                var update = service.Update(pr);
                if (!update)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı güncellenemedi" });

                if (!string.IsNullOrEmpty(pr.PaymentId))
                {
                    var approval = IyzicoService.PaymentCancel(pr.ID, pr.PaymentId, GetIpAddress());
                    if (approval.Status != "success")
                    {
                        return Json(new
                        {
                            isSuccess = false,
                            message = "Iyzico iptali yapılamadı. " + approval.ErrorMessage
                        });
                    }

                    return Json(new
                    {
                        isSuccess = true,
                        message = "Ödeme kaydı iptal edildi. Iyzico aktarımı yapıldı. Para iadesi yapıldı."
                    });
                }

                return Json(new
                {
                    isSuccess = true,
                    message = "Ödeme kaydı iptal edildi."
                });
            }
        }

        #endregion

        #region İptal Talebi Onayla Reddet

        [Route("AdminPanel/AcceptRefund")]
        [HttpPost]
        public JsonResult AcceptRefund(int id)
        {
            using (var adService = new AdvertService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı bulunamadı" });

                if (string.IsNullOrEmpty(pr.PaymentId) || string.IsNullOrEmpty(pr.PaymentTransactionID))
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = "Online ödeme tamamlanmamış. Bu işlem onaylanamaz."
                    });
                }

                pr.Status = Enums.PaymentRequestStatus.AliciIptalEtti;
                pr.IsSuccess = false;
                pr.UpdatedUserID = GetLoginID();
                var update = service.Update(pr);
                if (!update)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı güncellenemedi" });

                var approval = IyzicoService.PaymentRefund(pr.ID, pr.PaymentTransactionID, GetIpAddress(),
                    pr.Price.ToString());
                if (approval.Status != "success")
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = "Iyzico hatası: " + approval.ErrorMessage
                    });
                }

                return Json(new
                {
                    isSuccess = true,
                    message = "İade kaydı onaylandı. Iyzico aktarımı yapıldı."
                });
            }
        }

        #endregion

        [Route("AdminPanel/DeletePaymentRequest")]
        [HttpPost]
        public JsonResult DeletePaymentRequest(int id)
        {
            using (var adService = new AdvertService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Ödeme kaydı bulunamadı" });

                var delete = service.Delete(pr);
                if (delete)
                    return Json(new { isSuccess = true, message = "Ödeme kaydı silindi." });

                return Json(new { isSuccess = false, message = "Ödeme kaydı silinemedi." });
            }
        }
    }
}