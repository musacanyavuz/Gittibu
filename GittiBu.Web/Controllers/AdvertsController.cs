using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GittiBu.Common;
using GittiBu.Common.Extensions;
using GittiBu.Common.Iyzico;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.Helpers;
using GittiBu.Web.ViewModels;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using InstallmentPrice = GittiBu.Web.ViewModels.InstallmentPrice;

namespace GittiBu.Web.Controllers
{
    public class AdvertsController : BaseController
    {
        #region STEP_1_IlanEkle
        [Authorize]
        [Route("IlanEkle")]
        [Route("AddListing")]
        [Route("Hesabim/IlanDuzenle/{id}")]
        [Route("MyAccount/EditListing/{id}")]
        public IActionResult AddListing(int? id) //step1.
        {
            var lang = GetLang();
            using (var settingService = new SystemSettingService())
            using (var userService = new UserService())
            using (var service = new AdvertService())
            using (var publicService = new PublicService())
            using (var categoryService = new AdvertCategoryService())
            {
                var user = userService.GetSecurePaymentDetail(GetLoginID());
                if (string.IsNullOrEmpty(user?.IyzicoSubMerchantKey))
                {
                    Notification = new UiMessage(NotyType.info,
                        "İlan girişi yapmak için üyelik tipini satıcı olarak seçip güvenli ödeme bilgilerinizi doldurmalısınız.",
                        "You must choose membertype seller and fill in your secure payment information to make sales.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.UyelikBilgilerim, lang));
                }
                var model = new AddListingStep1ViewModel()
                {
                    AdvertCategories = categoryService.GetAll(lang),
                    Advert = (id != null) ? service.GetMyAdvert(id.Value, GetLoginID()) : null,
                    CanUseSecurePayment = !string.IsNullOrEmpty(user?.IyzicoSubMerchantKey),
                    CargoAreas = publicService.GetCargoAreas(),
                    Cash = int.Parse(settingService.GetSystemSettings().Single(x => x.Name == Enums.SystemSettingName.MinimumOdemeTutari).Value)
                };
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("IlanEkle")]
        [Route("AddListing")]
        [Route("Hesabim/IlanDuzenle/{id}")]
        [Route("MyAccount/EditListing/{id}")]
        public IActionResult AddListing(int id, Advert advert, int[] AvailableInstallments, string SelectedPhotos, int mainImage) //step1 save func.
        {
            try
            {

                AvailableInstallments = AvailableInstallments?.Distinct().ToArray();
                var lang = GetLang();
                if(advert.StockAmount > 10000)
                {
                    Notification = new UiMessage(NotyType.error, "Stok en fazla 10.000 adet olabilir.", "The maximum stock quantity can be 10.000", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.IlanEkle, lang));
                }
                using (var mailing = new MailingService())
                using (var settingService = new SystemSettingService())
                using (var catService = new AdvertCategoryService())
                using (var service = new AdvertService())
                {
                    advert.Title = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(advert.Title.ToLower());
                    advert.Title = advert.Title.Modify();
                    var category = catService.Get(advert.CategoryID);
                    if (id > 0)
                    {
                        var _ad = service.GetMyAdvert(id, GetLoginID());

                        if (_ad != null)
                        {
                            _ad.Title = advert.Title;
                            _ad.Content = advert.Content;
                            _ad.NewProductPrice = advert.NewProductPrice;
                            _ad.Price = advert.Price;
                            _ad.LastUpdateDate = DateTime.Now;
                            _ad.CategoryID = advert.CategoryID;
                            _ad.ProductStatus = advert.ProductStatus;
                            _ad.WebSite = advert.WebSite;
                            _ad.StockAmount = advert.StockAmount;
                            _ad.CargoAreaID = advert.CargoAreaID;
                            _ad.FreeShipping = advert.FreeShipping;
                            _ad.ShippingPrice = advert.ShippingPrice;
                            _ad.ShippingTypeID = advert.ShippingTypeID;
                            _ad.UseSecurePayment = true;
                            _ad.Brand = advert.Brand;
                            _ad.Model = advert.Model;
                            _ad.PaymentMethodID = (int)Enums.PaymentMethods.KrediKartiIle;
                            if (AvailableInstallments == null || AvailableInstallments.Length == 0)
                            {
                                _ad.AvailableInstallments = allInstallments(category.MaxInstallment);
                            }
                            else
                            {
                                _ad.AvailableInstallments = Constants.Array2String(AvailableInstallments);
                            }
                            _ad.IsApproved = false;
                            _ad.ApprovalStatus = Enums.ApprovalStatusEnum.WaitingforApproval;
                            _ad.IsActive = false;
                            var update = service.Update(_ad);
                            if (!update)
                            {
                                TempData.Put("UiMessage",
                                    new UiMessage
                                    {
                                        Class = "danger",
                                        Message = new Localization().Get("İlan güncellenemedi.", "Ad update failed.", lang)
                                    });
                                return RedirectToAction("AddListing");
                            }
                            else
                                advert = _ad;
                        }
                        else
                        {
                            return Redirect("/");
                        }
                    }
                    else
                    {

                        if (string.IsNullOrEmpty(SelectedPhotos))
                        {
                            Notification = new UiMessage(NotyType.error, "Lütfen resim seçiniz.", "Please select image.", lang);
                            return Redirect(Constants.GetURL(Enums.Routing.IlanEkle, lang));
                        }
                        if (string.IsNullOrEmpty(advert.Title))
                        {
                            Notification = new UiMessage(NotyType.error, "Lütfen başlığı oldurunuz.", "Please fill title.", lang);
                            return Redirect(Constants.GetURL(Enums.Routing.IlanEkle, lang));
                        }
                        advert.Title = advert.Title.Replace("&", " ");
                        advert.IsActive = false;
                        advert.CreatedDate = DateTime.Now;
                        advert.UserID = GetLoginID();
                        advert.UseSecurePayment = true;
                        advert.IpAddress = GetIpAddress();
                        advert.MoneyTypeID = (int)Enums.MoneyType.tl;
                        advert.IsDraft = true;
                        advert.IsApproved = false;
                        advert.ApprovalStatus = Enums.ApprovalStatusEnum.WaitingforApproval;
                        if (AvailableInstallments == null || AvailableInstallments.Length == 0)
                        {
                            advert.AvailableInstallments = allInstallments(category.MaxInstallment);
                        }
                        else
                        {
                            advert.AvailableInstallments = Constants.Array2String(AvailableInstallments);
                        }
                        var insert = service.Insert(advert);
                        if (!insert)
                        {
                            TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = new Localization().Get("İlan oluşturulamadı.", "Ad create failed.", lang) });
                            return RedirectToAction("AddListing");
                        }
                    }

                    try
                    {
                        var imageIds = updateNewPhotos(advert.ID, SelectedPhotos);
                        if (mainImage != 0)
                        {
                            var mainPhoto = service.GetPhoto(mainImage);
                            if (mainPhoto != null)
                            {
                                advert.CoverPhoto = mainPhoto.Source;
                                advert.Thumbnail = mainPhoto.Thumbnail;
                                service.Update(advert);
                            }
                        }
                        else if ((string.IsNullOrEmpty(advert.Thumbnail) || string.IsNullOrEmpty(advert.CoverPhoto)) &&
                                imageIds?.Length > 0 && id == 0)
                        {
                            var mainPhoto = service.GetPhoto(imageIds[0]);
                            if (mainPhoto != null)
                            {
                                advert.CoverPhoto = mainPhoto.Source;
                                advert.Thumbnail = mainPhoto.Thumbnail;
                                service.Update(advert);
                            }
                        }

                        var request = new AdvertPublishRequest
                        {
                            AdvertID = advert.ID,
                            IsActive = true,
                            RequestDate = DateTime.Now,
                            UserID = GetLoginID()
                        };
                        service.InsertPublishRequest(request);

                        var admins = settingService.GetSystemSettings()
                       ?.SingleOrDefault(s => s.Name == Enums.SystemSettingName.YoneticiMailleri)?.Value;
                        string message = string.Empty;
                        string mailSubject = string.Empty;
                        const string href = "https://www.GittiBu.com/AdminPanel";
                        if (admins != null)
                        {
                            if (id == 0)
                            {
                                mailSubject = "Yeni ilan girişi yapıldı.";
                                message = $"{advert.ID} id'li ilan girişi yapıldı.<br/>Başlık: {advert.Title}<br/><a href={href}>İlanı Onaylamak İçin Tıkla</a>";
                            }
                            else
                            {
                                mailSubject = "İlan güncellemesi yapıldı.";
                                message = $"{advert.ID} id'li ilan güncellenmiştir .<br/>Başlık: {advert.Title}<br/><a href={href}>İlanı Onaylamak İçin Tıkla</a>";
                            }
                            mailing.SendMail2Admins(admins, mailSubject, message);
                        }
                    }
                    catch (Exception e)
                    {
                        using (var logService = new LogServices())
                        {
                            Log log = new Log()
                            {
                                Function = "AdvertsController.AddListing",
                                Message = GetLoginID() + "id li kullanici AdvertsController.AddListing metot içerisindeki try'da hata aldi.",
                                Params = JsonConvert.SerializeObject(new List<string>
                        {
                            id.ToString(),
                            JsonConvert.SerializeObject(advert),
                            JsonConvert.SerializeObject(AvailableInstallments),
                            SelectedPhotos,
                            mainImage.ToString()
                        }),
                                Detail = JsonConvert.SerializeObject(e),
                                CreatedDate = DateTime.Now
                            };
                            logService.Insert(log);
                        }
                    }
                }

                var route = new Localization().Get("/IlanEkle/Sonuc/", "/AddListing/Summary/", lang) + advert.ID;
                return Redirect(route);
            }
            catch (Exception ex)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        Function = "AdvertsController.AddListing",
                        Message = GetLoginID() + "id li kullanici AdvertsController.AddListing işlem yaparken hata aldi.",
                        Params = JsonConvert.SerializeObject(new List<string>
                        {
                            id.ToString(),
                            JsonConvert.SerializeObject(advert),
                            JsonConvert.SerializeObject(AvailableInstallments),
                            SelectedPhotos,
                            mainImage.ToString()
                        }),
                        Detail = JsonConvert.SerializeObject(ex),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }

                Notification = new UiMessage(NotyType.error, "Sistem bir hata oluştu. Lütfen yazdığınız verileri kontrol ederek tekrar deneyiniz.", "Something went wrong. Please try again..", GetLang());
                return Redirect(Constants.GetURL(Enums.Routing.IlanEkle, GetLang()));
            }
        }

        string allInstallments(int max = 1)
        {
            var ins = new int[] { 1, 2, 3, 6, 9, 12 };
            var result = "";
            foreach (var installment in ins.Where(i => i <= max).ToArray())
            {
                result += installment + ",";
            }

            result = result.Substring(0, result.Length - 1);
            return result;
        }

        int[] updateNewPhotos(int advertId, string selectedPhotos)
        {
            if (!string.IsNullOrEmpty(selectedPhotos))
            {
                var _ids = selectedPhotos.Split(',');
                var ids = new List<int>();
                foreach (var id in _ids)
                {
                    if (tryParseInt(id))
                        ids.Add(int.Parse(id));
                }

                if (ids.Count > 0)
                {
                    using (var service = new AdvertService())
                    {
                        var update = service.UpdateAdvertPhotos(advertId, ids.ToArray());
                        if (update)
                            return ids.ToArray();
                    }
                }
            }
            return null;
        }

        bool tryParseInt(string s)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }



        #endregion

        #region STEP_2_IlanFotograflari


        [Authorize]
        [HttpPost]
        [Route("AddListing/UploadPhoto")]
        public IActionResult UploadPhoto(int id)
        {
            using (var fileService = new FileService())
            using (var imageService = new ImageService())
            using (var service = new AdvertService())
            {

                var files = HttpContext.Request.Form.Files;
                string path = "";
                string errorMessage = "";
                for (var index = 0; index < files.Count; index++)
                {
                    var token = Common.Encryptor.GenerateToken();
                    try
                    {
                        var directory = "/Upload/Sales/";
                        var file = files[index];
                        errorMessage += file.FileName + ",";
                        var imgFileSize = file.Length;
                        

                        if (imgFileSize > 0 && imgFileSize < 5242880) // 5 MB -5242880
                        {
                          
                            var ext = Path.GetExtension(file.FileName);
                            path = fileService.FileUpload(GetLoginID(), file, "/Upload/Sales/", token + "_" + (index + 1) + "" + ext);
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                //var fileName = token + "_" + (index + 1);
                                //var optimize75 = imageService.Optimize75(path, directory, fileName);
                                var thumbName = token + "_" + (index + 1) + "_thumb" + ".jpg";
                                var result = imageService.CreateThumbnail(GetLoginID(), path, directory, thumbName);
                                if (result)
                                {
                                    var advertPhoto = new AdvertPhoto
                                    {
                                        AdvertID = 0,
                                        Source = path,
                                        Thumbnail = directory + thumbName,
                                        CreatedDate = DateTime.Now
                                    };
                                    var insert = service.InsertAdvertPhoto(advertPhoto);
                                    if (insert)
                                    {
                                        return Json(new
                                        {
                                            isSuccess = true,
                                            source = path,
                                            thumbnail = directory + thumbName,
                                            id = advertPhoto.ID
                                        });
                                    }
                                }
                            }
                            else
                            {
                                using (var logService = new LogServices())
                                {
                                    Log log = new Log()
                                    {
                                        Function = "AdvertsController.UploadPhoto",
                                        Message = GetLoginID() + " id li kullanici AdvertsController.UploadPhoto işlem yaparken hata aldi. path hatası ",
                                        Detail = " path  : " + path,
                                        CreatedDate = DateTime.Now
                                    };
                                    logService.Insert(log);
                                }
                            }
                        }
                        else
                        {
                            using (var logService = new LogServices())
                            {
                                Log log = new Log()
                                {
                                    Function = "AdvertsController.UploadPhoto",
                                    Message = GetLoginID() + " id li kullanici AdvertsController.UploadPhoto işlem yaparken hata aldi. file.Length > 0 && file.Length < 5242880",
                                    Detail = " file.Length : " + file.Length,
                                    CreatedDate = DateTime.Now
                                };
                                logService.Insert(log);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        using (var logService = new LogServices())
                        {
                            Log log = new Log()
                            {
                                Function = "AdvertsController.UploadPhoto",
                                Message = GetLoginID() + "id li kullanici AdvertsController.UploadPhoto işlem yaparken hata aldi.",
                                Detail = e.ToString(),
                                CreatedDate = DateTime.Now
                            };
                            logService.Insert(log);
                        }
                    }
                }

                var languageId = GetLang();
                var tran = new Localization();
                var locMessage = tran.Get(" ismiyle sunucuya yüklemeye çalıştığınız resim yüklenemedi!", " the images you tried to upload to the server with their names could not be uploaded", languageId);
                return Json(new { isSuccess = false, Message = errorMessage + locMessage });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("DeleteAdPhoto")]
        public JsonResult DeletePhoto(int id)
        {
            using (var service = new AdvertService())
            {
                var lang = GetLang(false);
                var t = new Localization();
                var photo = service.GetPhoto(id);
                if (photo == null || photo.Advert == null)
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Fotoğrafa erişilemedi.",
                        "Unable to access the photo.", lang)
                    });
                if (photo.Advert.UserID != GetLoginID())
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Size ait olmayan bir ilanı güncelleyemezsiniz.",
                        "You cannot update a post that does not belong to you.", lang)
                    });
                var photosCount = service.GetPhotos(photo.AdvertID).Count;
                if (photosCount == 1)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Üründe en az bir görsel olmak zorundadır.",
                        "There must be at least one visual on the product.", lang)
                    });
                }
                var adverts = service.GetAdvert(photo.AdvertID);
                if (adverts.Thumbnail == photo.Thumbnail)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Fotoğraf silinemedi. Ana resim olduğundan dolayı silenememektedir.",
                            "Photo delete failed. Main photo not delete. ", lang)
                    });
                }
                var del = service.DeletePhoto(photo);
                if (del)
                {
                    new FileService().DeleteFile(photo.Source);
                    new FileService().DeleteFile(photo.Thumbnail);
                    return Json(new { isSuccess = true, message = t.Get("Fotoğraf silindi.", "Photo has been deleted.", lang) });
                }

                return Json(new
                {
                    isSuccess = false,
                    message = t.Get("Fotoğraf silinemedi.",
                    "Photo delete failed.", lang)
                });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("UpdatePhotosOrder")]
        public JsonResult UpdatePhotosOrder(int id, int[] photos)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new AdvertService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });
                if (photos == null || photos.Length == 0)
                    return Json(new { isSuccess = false, message = t.Get("İlan fotoğrafı bulunamadı", "Could not found advert photo", lang) });

                for (int i = 0; i < photos.Length; i++)
                {
                    var update = service.UpdatePhotoOrder(photos[i], (i + 1));
                    if (!update)
                    {
                        return Json(new
                        {
                            isSuccess = false,
                            message = t.Get("Sıralama güncellenirken bir hata oluştu",
                            "Ad photo order update failed", lang)
                        });
                    }
                }

                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("Fotoğraf sıralaması güncellendi", "Updated order of advert photos", lang)
                });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("UpdateMainPhoto")]
        public JsonResult UpdateMainPhoto(int id, int photoId)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new AdvertService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });
                var photo = service.GetPhoto(photoId);
                if (photo == null)
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });

                ad.Thumbnail = photo.Thumbnail;
                var update = service.Update(ad);
                if (!update)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("İlan güncellenirken bir hata oluştu",
                        "Ad update failed", lang)
                    });
                }
                var allPhotosOfAdvert = service.GetPhotos(id);//.Where(x => x.OrderNumber == 0 || x.Thumbnail == ad.Thumbnail);

                var photoFirstOrder = allPhotosOfAdvert.FirstOrDefault(x => x.OrderNumber == 1); // eski ilk foto al.               
                var newFirstOrderPhoto = allPhotosOfAdvert.FirstOrDefault(x => x.ID == photoId);// yeni foto al

                service.UpdatePhotoOrder(photoFirstOrder.ID, newFirstOrderPhoto.OrderNumber);
                service.UpdatePhotoOrder(photoId, 1);
                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("İlan güncellendi", "Advert updated", lang)
                });
            }
        }

        #endregion

        #region STEP_5_Sonuc

        [Authorize]
        [Route("IlanEkle/Sonuc/{id}")]
        [Route("AddListing/Summary/{id}")]
        public IActionResult Summary(int id)
        {
            using (var service = new AdvertService())
            {
                var advert = service.GetMyAdvert(id, GetLoginID());
                advert.IsDraft = false;
                service.Update(advert);               
                return View(advert);
            }
        }

        #endregion

        #region İlanDetay


        [Route("Ilan/{categorySlug}/{subcategorySlug}/{title}/{id}")]
        [Route("Advert/{categorySlug}/{subcategorySlug}/{title}/{id}")]
        [Route("Ilan/{categorySlug}/{title}/{id}")]
        [Route("Advert/{categorySlug}/{title}/{id}")]
        [Route("Ilan/{title}/{id}")]
        [Route("Advert/{title}/{id}")]
        public IActionResult Details(int id)
        {
            var lang = GetLang();
            using (var catService = new AdvertCategoryService())
            using (var service = new AdvertService())
            using (var userService = new UserService())
            using (var publicService = new PublicService())
            {
                var advert = service.Get(id);
                if (advert == null)
                    return Redirect("/");
                advert.User = userService.Get(advert.UserID);
                var advertCats = catService.GetAll();
                var category = catService.Get(advert.CategoryID, lang);
                advert.CategoryNameTr = category.Name;
                var model = new AdvertDetailsViewModel
                {
                    Advert = advert,
                    AdvertCategories = advertCats,
                    SimilarAdverts = service.GetCategoryPageAdverts(advert.CategoryID, lang, 0, GetLoginID(), 4),
                    CargoArea = publicService.GetCargoAreaById(advert.CargoAreaID)
                };
                if (lang == (int)Enums.Languages.tr)
                {
                    ViewBag.TranslateUrl = "/Advert/" + Common.Localization.Slug(advert.Title) + "/" + advert.ID;
                }
                else
                {
                    ViewBag.TranslateUrl = "/Ilan/" + Common.Localization.Slug(advert.Title) + "/" + advert.ID;
                }
                service.UpdateViewCount(id);               
                return View(model);
            }
        }

        #endregion

        #region Doping Ödeme

        private ThreedsInitialize BuildPayment(User user, UserAddress userAddress, string ip, string cvc, string fullname,
            string month, string year, string number, int paymentRequestId, int price, string paymentName, string callback)
        {
            Address address;
            string addressLine;
            if (userAddress == null)
            {
                addressLine = user?.District?.Name + " " + user?.City?.Name ?? "Istanbul" + " " + user?.Country?.Name ?? "Turkey";
                address = new Address
                {
                    City = user?.City?.Name ?? "Istanbul",
                    Country = user?.Country?.Name ?? "Turkey",
                    ContactName = user?.Name,
                    Description = addressLine
                };
            }
            else
            {
                addressLine = userAddress.Address;
                address = new Address
                {
                    City = userAddress.City?.Name ?? userAddress.CityText,
                    Country = userAddress.Country?.Name ?? "Turkey",
                    ContactName = user.Name,
                    Description = userAddress.Address
                };
            }

            user.Name = user.Name.Trim();
            var name = user.Name;
            var surName = "GittiBu.com";
            if (user.Name.Contains(" "))
            {
                name = user.Name.Substring(0, user.Name.LastIndexOf(' ') + 1);
                surName = user.Name.Substring(user.Name.LastIndexOf(' ') + 1);
            }

            var buyer = new Buyer
            {
                Id = user.ID.ToString(),
                Name = name,
                Surname = surName,
                City = address.City,
                Country = address.Country,
                Email = user.Email,
                GsmNumber = user.MobilePhone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""),
                IdentityNumber = (!string.IsNullOrEmpty(user.TC)) ? user.TC : "99999999999",
                Ip = ip,
                RegistrationAddress = addressLine
            };
            var card = new PaymentCard
            {
                Cvc = cvc,
                CardHolderName = fullname,
                ExpireMonth = month,
                ExpireYear = year,
                CardNumber = number,
                RegisterCard = 0
            };

            return IyzicoService.PayBanner(paymentRequestId, price, card,
                buyer, address, address, paymentRequestId,
                paymentName, callback);
        }


        [Route("Doping/Callback")]
        public IActionResult Callback(Iyzico3dCallback callback)
        {
            var lang = GetLang();
            var profileUrl = Constants.GetURL(Enums.Routing.ProfilSayfam, lang);

            if (callback == null || callback.ConversationId == 0)
            {
                Notification = new UiMessage(NotyType.error,
                    new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                    10000);
                return Redirect(profileUrl);
            }
            if (string.IsNullOrEmpty(callback.PaymentId) || callback.PaymentId == "0")
            {
                Notification = new UiMessage(NotyType.error,
                    new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                    10000);
                return Redirect(profileUrl);
            }
            using (var dopingService = new AdvertService())
            using (var prService = new PaymentRequestService())
            {
                var prId = Convert.ToInt32(callback.ConversationId);
                var pr = prService.Get(prId);
                if (pr == null)
                {
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "Ödeme kaydı bulunamadı. Lütfen site yönetimi ile iletişime geçin."
                            , "Payment record not found.. Please contact the site administration.", lang),
                        10000);
                    return Redirect(profileUrl);
                }
                var route = new Localization().Get("/IlanEkle/Sonuc/", "/AddListing/Summary/", lang) + pr.AdvertID;
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
                    pr.IsSuccess = true;
                    pr.Status = Enums.PaymentRequestStatus.OnlineOdemeYapildi;
                    pr.ResponseDate = DateTime.Now;
                    pr.PaymentId = threedsPayment.PaymentId;
                    pr.PaymentTransactionID = threedsPayment.PaymentItems.First().PaymentTransactionId;
                    var update = prService.Update(pr);
                    if (!update)
                    {
                        Notification = new UiMessage(NotyType.success, new Localization().Get(
                                "Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin."
                                , "Payment completed but could not be saved in the system. Please contact the site administration.", lang),
                            10000);
                        return Redirect(profileUrl);
                    }
                    else
                    {
                        //dopingService.ActivateAllPendingDopings(pr.AdvertID);
                        //dopingler otomatik aktifleşmeyecek, admin panelinden aktifleştirilecek.
                        Notification = new UiMessage(NotyType.success, new Localization().Get(
                                "Doping alım işleminiz tamamlandı."
                                , "Your doping purchase process is completed.", lang),
                            10000);
                        return Redirect(route);
                    }
                }
                else
                {
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "Doping alım işlemi başarısız. "
                            , "Doping purchase process is failed. ", lang) + threedsPayment.ErrorMessage,
                        10000);
                    return Redirect(route);
                }
            }
        }


        #endregion



        [Route("GetInstallmentPrices")]
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

        [Route("Ilanlar/Satici/{id}/{username}")]
        [Route("Adverts/Seller/{id}/{username}")]
        [Route("{id:int}/{username}")]
        public IActionResult SellerAds(int id)
        {
            var lang = GetLang();
            using (var adService = new AdvertService())
            using (var userService = new UserService())
            {
                var user = userService.Get(id);
                if (user == null)
                {
                    Notification = new UiMessage(NotyType.error, "Satıcı bulunamadı.", "Seller not found.", lang);
                    return Redirect("/");
                }

                var ads = adService.GetUserAdverts(id, GetLoginID(), lang);
                var list = Ads2HomePageItems(ads, lang);
                list.ForEach(ad => ad.UserName = user.UserName);

                if (string.IsNullOrEmpty(user.ProfilePicture))
                {
                    user.ProfilePicture = "/Content/img/avatar-no-image.png";
                }
                var model = new SellerViewModel
                {
                    Ads = list,
                    Seller = user
                };
                return View(model);
            }
        }


    }
}