using GittiBu.Common;
using GittiBu.Common.Extensions;
using GittiBu.Common.Iyzico;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.Helpers;
using GittiBu.Web.ViewModels;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static GittiBu.Common.Enums;

namespace GittiBu.Web.Controllers
{
    public class AccountController : BaseController
    {
        #region Login

        [Route("GirisYap")]
        [Route("Login")]
        public IActionResult Login()
        {
            GetLang();
            return View();
        }

        [HttpPost]
        [Route("GirisYap")]
        [Route("Login")]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe)
        {
            var lang = GetLang(false);
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Notification = new UiMessage(NotyType.error, "Kullanıcı adı ya da şifre geçersiz.",
                "Username or password is not valid.", lang);
                return RedirectToAction("Login");
            }
            using (var service = new UserService())
            {
                password = Encryptor.EncryptData(password);
                var user = service.Get(username, password);
                if (user == null)
                {
                    Notification = new UiMessage(NotyType.error, "Hatalı kullanıcı adı ya da şifre",
                        "Incorrect username or password", lang);
                    return RedirectToAction("Login");
                }
                if (!user.IsActive)
                {
                    Notification = new UiMessage(NotyType.error, "E-posta adresinize gönderilen aktifleştirme bağlantısı ile hesabınızı aktifleştirmelisiniz.",
                        "You must activate your account with the activation link sent to your e-mail address.", lang);
                    return RedirectToAction("Login");
                }
                await AddCookieAuth(user.UserName, user.Name, user.Email, user.ProfilePicture, user.Role, user.ID.ToString(), user.LanguageID == 2 ? "en" : "tr", rememberMe: rememberMe);
                SetLang(user.LanguageID == 1 ? "tr" : "en");
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task AddCookieAuth(string userName, string name, string email, string photo, string role, string userId, string lang, bool rememberMe = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.UserData, userId),
                new Claim(ClaimTypes.Locality, lang),
                new Claim(ClaimTypes.Thumbprint, photo ?? "/Content/img/avatar-no-image.png"),
                new Claim("Username",userName)
            };
            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                IsPersistent = rememberMe,
            };

            await HttpContext.SignInAsync(
                scheme: CookieAuthenticationDefaults.AuthenticationScheme,
                principal: principal, properties: authProperties
            );

        }

        [Route("CikisYap")]
        [Route("SignOut")]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Route("SifremiUnuttum")]
        [Route("ForgottenPassword")]
        public IActionResult ForgottenPassword()
        {
            GetLang();
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> FBLogin(short oauth_Provider, ExternalLoginViewModel userData)
        {
            using (var service = new UserService())
            {
                var userRegisteredSystem = service.Get(userData.Email);
                if (userRegisteredSystem != null)
                {
                    userRegisteredSystem.OAuth_Provider = oauth_Provider;
                    userRegisteredSystem.OAuth_Uid = userData.OAuth_Uid;
                    await AddCookieAuth(userRegisteredSystem.UserName, userRegisteredSystem.Name, userRegisteredSystem.Email, userRegisteredSystem.ProfilePicture, userRegisteredSystem.Role, userRegisteredSystem.ID.ToString(), userRegisteredSystem.LanguageID == 2 ? "en" : "tr");
                    SetLang(userRegisteredSystem.LanguageID == 1 ? "tr" : "en");
                    return Json(service.Update(userRegisteredSystem));
                }
                else
                {
                    var user = service.GetFromExternalLoginId(oauth_Provider, userData.OAuth_Uid);

                    if (user != null)
                    {
                        await AddCookieAuth(user.UserName, user.Name, user.Email, user.ProfilePicture, user.Role, user.ID.ToString(), user.LanguageID == 2 ? "en" : "tr");
                        SetLang(user.LanguageID == 1 ? "tr" : "en");
                        return Json(true);
                    }
                    else
                    {
                        user = new User
                        {
                            Name = userData.Name,
                            Email = userData.Email,
                            ProfilePicture = userData.PictureUrl,
                            OAuth_Provider = oauth_Provider,
                            OAuth_Uid = userData.OAuth_Uid,
                            Role = "User",
                            IsActive = true,
                            LanguageID = 1,
                            UserName = userData.Email,
                            CreatedDate = DateTime.Now
                        };
                        var result = service.Insert(user);
                        await AddCookieAuth(user.UserName, user.Name, user.Email, user.ProfilePicture, user.Role, user.ID.ToString(), user.LanguageID == 2 ? "en" : "tr");
                        SetLang(user.LanguageID == 1 ? "tr" : "en");
                        return Json(result);
                    }
                }
            }
        }

        #endregion

        #region ResetPassword

        [Route("SifremiUnuttum")]
        [Route("ForgottenPassword")]
        [HttpPost]
        public IActionResult ForgottenPassword(string user)
        {
            var lang = GetLang();
            using (var mailing = new MailingService())
            using (var service = new UserService())
            using (var textService = new TextService())
            {
                var account = service.Get(user);
                if (account == null)
                {
                    //var title = new Localization().Get("Hata", "Error", lang);
                    var message = new Localization().Get("Kullanıcı Bulunamadı. Kullanıcı Adı veya E-Posta Adresinizi Doğru Yazdığınızdan Emin Olunuz. ", "User not found. Please be sure to write your username or e-mail address correctly.", lang);
                    return View(new UiMessage { Message = message, Type = NotyType.error });
                }
                var token = Encryptor.GenerateToken();
                account.Token = token;
                var update = service.Update(account);
                var msgTr = update ? Constants.messageSuccessTr : Constants.messageDangerTr;
                var msgEn = update ? Constants.messageSuccessEn : Constants.messageDangerEn;
                if (!update)
                {
                    return View(new UiMessage
                    {
                        Type = NotyType.error,
                        Message = new Localization().Get(msgTr, msgEn, lang)
                    });
                }

                var content = textService.GetContent(401, lang);
                var link = Constants.GetURL((int)Enums.Routing.SifremiSifirla, account.LanguageID) + "/" + token;
                content.TextContent = content.TextContent.Replace("@adSoyad", account.Name).Replace("@Email", account.Email).Replace("@link", link);
                var send = mailing.Send(content.TextContent, account.Email, account.Name, content.Name);
                msgTr = send ? Constants.messageSuccessTr : Constants.messageDangerTr;
                msgEn = send ? Constants.messageSuccessEn : Constants.messageDangerEn;
                return View(new UiMessage
                {
                    Type = send ? NotyType.success : NotyType.error,
                    Message = new Localization().Get(msgTr, msgEn, lang)
                });
            }
        }

        [Route("SifremiSifirla/{token}")]
        [Route("ResetPassword/{token}")]
        public IActionResult ResetPassword(string token)
        {
            GetLang();
            try
            {
                using (var service = new UserService())
                {
                    if (string.IsNullOrEmpty(token))
                        return RedirectToAction("Index", "Home");

                    var user = token.Length < 10 ? service.Get(int.Parse(token)) : service.GetFromToken(token);
                    if (user != null)
                        return View(user);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return RedirectToAction("Index", "Home");
        }

        [Route("SifremiSifirla/{token}")]
        [Route("ResetPassword/{token}")]
        [HttpPost]
        public IActionResult ResetPassword(string token, string password, string password2)
        {
            var lang = GetLang();
            if (password == null || password2 == null)
            {
                TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = new Localization().Get("Şifre geçersiz.", "Password is invalid.", lang) });
                return RedirectToAction("ResetPassword", new { token });
            }
            if (password != password2)
            {
                TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = new Localization().Get("Şifreler aynı değil.", "Please check if passwords are not the same.", lang) });
                return RedirectToAction("ResetPassword", new { token });
            }
            if (password.Length < 6)
            {
                TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = new Localization().Get("Şifreniz en az 6 karakter olmalıdır.", "Your password must be at least 6 characters.", lang) });
                return RedirectToAction("ResetPassword", new { token });
            }

            using (var service = new UserService())
            {
                var user = service.GetFromToken(token);
                password = Encryptor.EncryptData(password);
                user.Password = password;
                user.Token = null;
                var update = service.Update(user);
                if (update)
                {
                    TempData.Put("UiMessage", new UiMessage
                    {
                        Class = "success",
                        Message = new Localization().Get("Şifreniz güncellendi.", "Your password has been updated.",
                            lang)
                    });
                    return RedirectToAction("ResetPassword", new { token = user.ID });
                }
                else
                {
                    TempData.Put("UiMessage", new UiMessage
                    {
                        Class = "danger",
                        Message = new Localization().Get("Şifre güncelleme işlemi başarısız oldu.", "Password update failed.",
                            lang)
                    });
                }
            }

            return RedirectToAction("ResetPassword", new { token });
        }

        #endregion

        #region Register

        [Route("UyeOl")]
        [Route("Register")]
        public IActionResult Register()
        {
            GetLang();
            using (var publicService = new PublicService())
            {
                var model = new RegisterViewModel();
                //{
                //    Countries = publicService.GetCountries(),
                //    Cities = publicService.GetCities(),
                //    Districts = publicService.GetDistricts()
                //};
                return View(model);
            }
        }

        [Route("UyeOl")]
        [Route("Register")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            int lang = GetLang();
            try
            {
                var validation = false;
                if (Request.Form.Keys.Contains("g-recaptcha-response"))
                {
                    var response = Request.Form["g-recaptcha-response"];
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
                    var t = new Localization();

                    if (registerViewModel.MemberType == (int)MemberType.Seller)
                    {
                        if (string.IsNullOrWhiteSpace(registerViewModel.TC) || registerViewModel.TC.Length != 11)
                        {
                            ModelState.AddModelError("TC", "TC 11 karakter olmak zorundadir");
                        }
                        if (!string.IsNullOrWhiteSpace(registerViewModel.TC) && !Regex.Match(registerViewModel.TC, "([0-9]+)").Success)
                        {
                            ModelState.AddModelError("TC", "Lütfen düzgün tc numarası giriniz");
                        }
                        if (string.IsNullOrWhiteSpace(registerViewModel.IBAN) || !(registerViewModel.IBAN.Length > 25 && registerViewModel.IBAN.Length < 34))
                        {
                            ModelState.AddModelError("IBAN", "Iban numarası hatalıdır");
                        }
                        if (string.IsNullOrWhiteSpace(registerViewModel.SenderAddress) || registerViewModel.SenderAddress.Length < 5)
                        {
                            ModelState.AddModelError("SenderAddress", "Gönderici adresi 5 karakterden büyük olmak zorundadır");
                        }
                        if (string.IsNullOrWhiteSpace(registerViewModel.InvoiceAddress) || registerViewModel.InvoiceAddress.Length < 5)
                        {
                            ModelState.AddModelError("InvoiceAddress", "Fatura adresi 5 karakterden büyük olmak zorundadır");
                        }
                        if (!string.IsNullOrWhiteSpace(registerViewModel.CompanyName))
                        {
                            if (string.IsNullOrWhiteSpace(registerViewModel.CompanyEmail))
                            {
                                ModelState.AddModelError("CompanyEmail", "Şirket emaili boş olamaz");
                            }
                            else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(registerViewModel.CompanyEmail))
                            {
                                ModelState.AddModelError("CompanyEmail", "Geçerli bir e-mail giriniz");
                            }
                            if (string.IsNullOrWhiteSpace(registerViewModel.TaxNumber))
                            {
                                ModelState.AddModelError("TaxNumber", "Vergi Numarası girilmesi zorunludur.");
                            }
                            if (string.IsNullOrWhiteSpace(registerViewModel.TaxOffice))
                            {
                                ModelState.AddModelError("TaxOffice", "Vergi Dairesi girilmesi zorunludur.");
                            }
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        using (var publicService = new PublicService())
                        {
                            var model = new RegisterViewModel
                            {
                                Countries = publicService.GetCountries(),
                                Cities = publicService.GetCities(),
                                Districts = publicService.GetDistricts(),
                                MemberType = registerViewModel.MemberType,
                                CompanyName = registerViewModel.CompanyName,
                                CompanyEmail = registerViewModel.CompanyEmail,
                                TaxNumber = registerViewModel.TaxNumber,
                                TaxOffice = registerViewModel.TaxOffice,
                                CompanyPhone = registerViewModel.CompanyPhone

                            };
                            return View(model);
                        }
                    }
                    using (var mailing = new MailingService())
                    using (var service = new UserService())
                    {

                        registerViewModel.Name = registerViewModel.Name.Modify();
                        registerViewModel.Surname = registerViewModel.Surname.Modify();
                        registerViewModel.UserName = registerViewModel.UserName.Modify();
                        var user = new User
                        {
                            Name = $"{registerViewModel.Name.Trim()} {registerViewModel.Surname.Trim()}",
                            UserName = registerViewModel.UserName,
                            Email = registerViewModel.Email,
                            MobilePhone = registerViewModel.MobilePhone,
                            About = registerViewModel.About,
                            Role = "User",
                            CreatedDate = DateTime.Now,
                            ProfilePictureFile = registerViewModel.ProfilePictureFile,
                            GenderID = registerViewModel.GenderID,
                            WebSite = registerViewModel.WebSite,
                            CountryID = registerViewModel.CountryID,
                            CityID = registerViewModel.CityID,
                            DistrictID = registerViewModel.DistrictID,
                            LanguageID = registerViewModel.LanguageID,
                            BirthDate = registerViewModel.BirthDate,
                            WorkPhone = registerViewModel.WorkPhone
                        };

                        var usernameIsUsable = service.UsernameIsUsable(user.UserName, GetLoginID());
                        var emailIsUsable = service.EmailIsUsable(user.Email, GetLoginID());
                        if (!usernameIsUsable || !emailIsUsable)
                        {
                            TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = t.Get(Messages.RegisterUniqFail_tr, Messages.RegisterUniqFail_en, lang) });
                            return RedirectToAction("Register");
                        }

                        if (registerViewModel.Password != registerViewModel.Password2)
                        {
                            TempData.Put("UiMessage", new UiMessage
                            {
                                Class = "danger",
                                Message = t.Get("Şifreler aynı değil.", "Passwords do not match.", lang)
                            });
                            return RedirectToAction("Register");
                        }

                        user.Token = Encryptor.GenerateToken();
                        user.Password = Encryptor.EncryptData(registerViewModel.Password);

                        if (registerViewModel.MemberType == (int)MemberType.Seller)
                        {
                            if (!string.IsNullOrEmpty(registerViewModel.TC))
                            {
                                if (service.GetFromTC(registerViewModel.TC) != null)
                                {
                                    Notification = new UiMessage(NotyType.error, "Bu TC Kimlik numarası ile kayıtlı bir kullanıcı zaten mevcut.",
                                        "This identity number is already registered.", lang);
                                    return Redirect(Constants.GetURL(Enums.Routing.UyeOl, user.LanguageID));
                                }
                                user.TC = registerViewModel.TC;
                                /*
                                var names = registerViewModel.Name.ToUpper().Split(' ');
                                var firstName = registerViewModel.Name.ToUpper();
                                var lastName = names[names.Length - 1];
                                firstName = firstName.Replace(lastName, "").Trim();

                                var responseFromServer = HttpTcKimlikDogrula(ulong.Parse(registerViewModel.TC), firstName,
                                    lastName, (ushort)registerViewModel.BirthDate.Year);
                                if (responseFromServer.Contains("true"))
                                {
                                    user.TC = registerViewModel.TC;
                                }
                                else
                                {
                                    Notification = new UiMessage(NotyType.error,
                                        "TC Kimlik doğrulaması başarısız oldu. Adınızı, doğum tarihinizi ve TC Kimik Numaranızı kontrol ediniz.",
                                        "TC Authentication failed. Please check your name, date of birth and TC ID.", user.LanguageID);
                                    return Redirect(Constants.GetURL(Enums.Routing.UyeOl, user.LanguageID));
                                }
                                */
                            }
                            else
                            {
                                Notification = new UiMessage(NotyType.error,
                                    "TC Kimlik doğrulaması başarısız oldu. Adınızı, doğum tarihinizi ve TC Kimik Numaranızı kontrol ediniz.",
                                    "TC Authentication failed. Please check your name, date of birth and TC ID.", user.LanguageID);
                                return Redirect(Constants.GetURL(Enums.Routing.UyeOl, user.LanguageID));
                            }
                        }

                        var insert = service.Insert(user);
                        if (!insert)
                        {
                            TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = t.Get(Messages.InsertError_tr, Messages.InsertError_en, lang) });
                            return RedirectToAction("Register");
                        }
                        if (user.ProfilePictureFile != null && user.ProfilePictureFile.Length > 0)
                        {
                            var fileName = user.UserName + "_" + user.ID;
                            var extension = Path.GetExtension(user.ProfilePictureFile.FileName);
                            fileName += extension;
                            var filePath = new FileService().FileUpload(GetLoginID(), user.ProfilePictureFile, "/Upload/Users/", fileName);
                            new ImageService().Resize(filePath, 300);
                            user.ProfilePicture = "/Upload/Users/" + fileName;
                            service.Update(user);
                        }

                        if (registerViewModel.MemberType == (int)MemberType.Seller)
                        {
                            if (string.IsNullOrEmpty(registerViewModel.CompanyName))
                            { //bireysel satıcı bilgileri insert
                                var iyzicoResult = InsertPersonalSellerInformation(user.ID, registerViewModel);
                                if (iyzicoResult.IsSuccess == false)
                                {
                                    service.DeleteUser(user.ID.ToString());
                                    TempData.Put("UiMessage", new UiMessage
                                    {
                                        Class = "danger",
                                        Message = iyzicoResult.Message
                                    });
                                    Notification = new UiMessage(NotyType.error, iyzicoResult.Message);
                                    return RedirectToAction("Register");
                                }
                            }
                            else
                            { //kurumsal satıcı bilgileri insert
                                var iyzicoResult = InsertCommercialSellerInformation(user.ID, registerViewModel);
                                if (iyzicoResult.IsSuccess == false)
                                {
                                    service.DeleteUser(user.ID.ToString());
                                    TempData.Put("UiMessage", new UiMessage
                                    {
                                        Class = "danger",
                                        Message = iyzicoResult.Message
                                    });
                                    Notification = new UiMessage(NotyType.error, iyzicoResult.Message);
                                    return RedirectToAction("Register");
                                }
                            }
                        }

                        var mail = SendActivationMail(user);
                        //await AddCookieAuth(user.Name, user.Role, user.ID.ToString());

                        TempData.Put("UiMessage",
                            mail
                                ? new UiMessage
                                {
                                    Class = "success",
                                    Message = t.Get(Messages.RegisterSuccess_tr, Messages.RegisterSuccess_en, lang)
                                }
                                : new UiMessage
                                {
                                    Class = "danger",
                                    Message = t.Get(Messages.RegisterMailError_tr, Messages.RegisterMailError_en, lang)
                                });
                        var route = user.LanguageID == 1 ? "/Bildirim" : "/Notification";
                        return Redirect(route);
                    }
                }
            }
            catch (Exception)
            {
                Notification = new UiMessage(NotyType.error, "Beklenmedik bir hata oluştu.", "An unexpected error has occurred.", lang);
            }
            return Redirect("/");
        }

        private bool SendActivationMail(User user)
        {
            var lang = GetLang();
            var tran = new Localization();
            using (var mailing = new MailingService())
            using (var content = new TextService())
            {
                var link = Constants.GetURL((int)Enums.Routing.HesabimiAktiflestir, user.LanguageID) + "/" + user.Token;
                var mailHtml = content.GetText(Enums.Texts.MailAktivasyon, lang);
                mailHtml = mailHtml
                    .Replace("@adSoyad", $"{user.Name}{user.Surname}")
                    .Replace("@link", link);
                var title = $"GittiBu.com | {tran.Get("Hesabınızı Aktifleştirin", "Account Activation", lang)}";
                return mailing.Send(mailHtml, user.Email, user.Name, title);
            }
        }
        private IyzicoResult<string> InsertPersonalSellerInformation(int userId, RegisterViewModel registerViewModel)
        {
            registerViewModel.IBAN = registerViewModel.IBAN.Replace(" ", "").ToUpper();

            var model = new UserSecurePaymentDetail
            {
                Name = registerViewModel.Name.Trim(),
                Surname = registerViewModel.Surname.Trim(),
                TC = registerViewModel.TC,
                IBAN = registerViewModel.IBAN,
                MobilePhone = registerViewModel.MobilePhone,
                Email = registerViewModel.Email,
                SenderAddress = registerViewModel.SenderAddress,
                InvoiceAddress = registerViewModel.InvoiceAddress,
                UserID = userId,
                IyzicoSubMerchantKey = "subMerchantkey",
                Type = SubMerchantType.PERSONAL,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            using (var service = new UserService())
            {

                var insert = service.InsertSecurePaymentDetail(model);
                if (!insert)
                    return new IyzicoResult<string> { IsSuccess = false, Message = "Beklenmedik hata ile karşılaşıldı." };
                var request = new SubMerchant
                {
                    ConversationId = userId.ToString(),
                    // SubMerchantExternalId = GetLoginID().ToString(),
                    SubMerchantType = SubMerchantType.PERSONAL.ToString(),
                    Address = registerViewModel.InvoiceAddress,
                    Email = registerViewModel.Email,
                    GsmNumber = registerViewModel.MobilePhone.Replace("(", "").Replace(")", "").Replace("-", "")
                        .Replace(" ", ""),
                    Name = registerViewModel.Name,
                    Iban = registerViewModel.IBAN?.Replace(" ", ""),
                    Locale = "TR",
                    Currency = "TRY",
                    ContactName = registerViewModel.Name,
                    ContactSurname = registerViewModel.Surname,
                    IdentityNumber = registerViewModel.TC,
                    SubMerchantExternalId = model.ID.ToString()
                };
                var result = IyzicoService.CreateSubMerchant(request);
                if (result.IsSuccess || result.ErrorCode == "2002")
                {

                    model.IyzicoSubMerchantKey = result.ErrorCode == "2002" ? IyzicoService.GetSubMerchantKey(request).Replace(" ", "").Replace("/r/n", "") : result.Data.SubMerchantKey.Replace(" ", "").Replace("/r/n", "");
                    var resultUpdate = service.UpdateSecurePaymentDetail(model);
                    if (resultUpdate)
                    {
                        return new IyzicoResult<string> { IsSuccess = true, Message = "Kayıt işlemi başarıyla tamamlanmıştır." };
                    }

                    var sub2 = service.GetSecurePaymentDetail(userId);
                    service.DeleteSubMerchant(sub2.ID);
                    return new IyzicoResult<string> { IsSuccess = false, Message = "İşlem başarısız." };
                }
                var sub = service.GetSecurePaymentDetail(userId);
                service.DeleteSubMerchant(sub.ID);
                return new IyzicoResult<string> { IsSuccess = false, Message = result.Message };

            }
        }

        private IyzicoResult<string> InsertCommercialSellerInformation(int userId, RegisterViewModel registerViewModel)
        {
            var model = new UserSecurePaymentDetail
            {
                Name = registerViewModel.Name.Trim(),
                Surname = registerViewModel.Surname.Trim(),
                TC = registerViewModel.TC,
                IBAN = registerViewModel.IBAN,
                MobilePhone = registerViewModel.MobilePhone,
                Email = registerViewModel.Email,
                SenderAddress = registerViewModel.SenderAddress,
                InvoiceAddress = registerViewModel.InvoiceAddress,
                UserID = userId,
                IyzicoSubMerchantKey = "",
                Type = registerViewModel.SubMerchantType,
                CompanyName = registerViewModel.CompanyName,
                CompanyEmail = registerViewModel.CompanyEmail,
                CompanyFax = registerViewModel.CompanyFax,
                CompanyPhone = registerViewModel.CompanyPhone,
                TaxNumber = registerViewModel.TaxNumber,
                TaxOffice = registerViewModel.TaxOffice,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            using (var service = new UserService())
            {

                var insert = service.InsertSecurePaymentDetail(model);
                if (insert)
                {

                    var request = new SubMerchant
                    {
                        ConversationId = userId.ToString(),
                        SubMerchantExternalId = service.GetSecurePaymentDetail(userId).ID.ToString(),
                        SubMerchantType = registerViewModel.SubMerchantType.ToString(),
                        Address = registerViewModel.InvoiceAddress,
                        Email = registerViewModel.CompanyEmail,
                        GsmNumber = registerViewModel.MobilePhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                        Name = registerViewModel.CompanyName,
                        Iban = registerViewModel.IBAN?.Replace(" ", ""),
                        Locale = "TR",
                        Currency = "TRY",
                        ContactName = registerViewModel.Name,
                        ContactSurname = registerViewModel.Surname,
                        TaxNumber = registerViewModel.TaxNumber,
                        TaxOffice = registerViewModel.TaxOffice,
                        LegalCompanyTitle = registerViewModel.CompanyName
                    };
                    var result = IyzicoService.CreateSubMerchant(request);
                    if (result.IsSuccess)
                    {
                        model.IyzicoSubMerchantKey = result.Data.SubMerchantKey.Replace(" ", "").Replace("/r/n", "");
                        var updateResult = service.UpdateSecurePaymentDetail(model);
                        if (updateResult)
                        {
                            return new IyzicoResult<string> { IsSuccess = true, Message = "Bilgileriniz başarıyla kayıt edildi." };
                        }
                        else
                        {
                            var sub2 = service.GetSecurePaymentDetail(userId);
                            service.DeleteSubMerchant(sub2.ID);
                            return new IyzicoResult<string> { IsSuccess = true, Message = "Güncelleme Sırasında hata oluştu." };
                        }

                    }
                    else
                    {
                        var sub3 = service.GetSecurePaymentDetail(userId);
                        service.DeleteSubMerchant(sub3.ID);
                        return new IyzicoResult<string> { IsSuccess = false, Message = result.Message };

                    }
                }

                var sub = service.GetSecurePaymentDetail(userId);
                service.DeleteSubMerchant(sub.ID);
                return new IyzicoResult<string> { IsSuccess = false, Message = "Beklenmedik bir hata oluştu." };
            }
        }

        private IyzicoResult<string> UpdatePersonalSellerInformation(UserSecurePaymentDetail detail, RegisterViewModel registerViewModel)
        {
            detail.Name = registerViewModel.Name;
            detail.Surname = registerViewModel.Surname;
            detail.TC = registerViewModel.TC;
            detail.IBAN = registerViewModel.IBAN;
            detail.MobilePhone = registerViewModel.MobilePhone;
            detail.Email = registerViewModel.Email;
            detail.SenderAddress = registerViewModel.SenderAddress;
            detail.CompanyName = registerViewModel.CompanyName;
            detail.CompanyPhone = registerViewModel.CompanyPhone;
            detail.CompanyEmail = registerViewModel.CompanyEmail;
            detail.CompanyFax = registerViewModel.CompanyFax;
            detail.TaxOffice = registerViewModel.TaxOffice;
            detail.TaxNumber = registerViewModel.TaxNumber;
            detail.InvoiceAddress = registerViewModel.InvoiceAddress;
            detail.IsActive = true;
            using (var service = new UserService())
            {

                var update = service.UpdateSecurePaymentDetail(detail);
                if (update)
                {
                    var request = new SubMerchant
                    {
                        ConversationId = GetLoginID().ToString(),
                        SubMerchantExternalId = detail.ID.ToString(),
                        SubMerchantType = SubMerchantType.PERSONAL.ToString(),
                        Address = registerViewModel.InvoiceAddress,
                        Email = registerViewModel.Email,
                        GsmNumber = registerViewModel.MobilePhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                        Name = registerViewModel.Name,
                        Iban = registerViewModel.IBAN?.Replace(" ", ""),
                        Locale = "TR",
                        Currency = "TRY",
                        ContactName = registerViewModel.Name,
                        ContactSurname = registerViewModel.Surname,
                        IdentityNumber = registerViewModel.TC,
                        SubMerchantKey = detail.IyzicoSubMerchantKey
                    };
                    if (request.SubMerchantKey == "" || request.SubMerchantKey == "subMerchantkey")
                    {
                        var result2 = IyzicoService.CreateSubMerchant(request);
                        if (result2.IsSuccess || result2.ErrorCode == "2002")
                        {

                            request.SubMerchantKey = result2.ErrorCode == "2002" ? IyzicoService.GetSubMerchantKey(request).Replace(" ", "").Replace("/r/n", "") : result2.Data.SubMerchantKey.Replace(" ", "").Replace("/r/n", "");

                        }
                    }

                    var result = IyzicoService.UpdateSubMerchant(request);
                    if (result.IsSuccess)
                    {
                        return new IyzicoResult<string> { IsSuccess = true, Message = "İşlem başarı bir şekilde gerçekleşti." };
                    }
                    else
                    {
                        return new IyzicoResult<string> { IsSuccess = false, Message = result.Message };
                    }


                }
                else
                {
                    return new IyzicoResult<string> { IsSuccess = false, Message = "Güncelleme sırasında hata oluştu." };
                }

            }
        }

        private IyzicoResult<string> UpdateCommercialSellerInformation(UserSecurePaymentDetail detail, RegisterViewModel registerViewModel)
        {
            detail.Name = registerViewModel.Name;
            detail.Surname = registerViewModel.Surname;
            detail.TC = registerViewModel.TC;
            detail.IBAN = registerViewModel.IBAN;
            detail.MobilePhone = registerViewModel.MobilePhone;
            detail.Email = registerViewModel.Email;
            detail.SenderAddress = registerViewModel.SenderAddress;
            detail.InvoiceAddress = registerViewModel.InvoiceAddress;
            detail.Type = registerViewModel.SubMerchantType;
            detail.CompanyName = registerViewModel.CompanyName;
            detail.CompanyEmail = registerViewModel.CompanyEmail;
            detail.CompanyFax = registerViewModel.CompanyFax;
            detail.CompanyPhone = registerViewModel.CompanyPhone;
            detail.TaxNumber = registerViewModel.TaxNumber;
            detail.TaxOffice = registerViewModel.TaxOffice;
            detail.IsActive = true;
            using (var service = new UserService())
            {
                var update = service.UpdateSecurePaymentDetail(detail);
                if (update)
                {
                    var request = new SubMerchant
                    {
                        ConversationId = GetLoginID().ToString(),
                        SubMerchantExternalId = GetLoginID().ToString(),
                        SubMerchantType = registerViewModel.SubMerchantType.ToString(),
                        Address = registerViewModel.InvoiceAddress,
                        Email = registerViewModel.CompanyEmail,
                        GsmNumber = registerViewModel.MobilePhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                        Name = registerViewModel.CompanyName,
                        Iban = registerViewModel.IBAN?.Replace(" ", ""),
                        Locale = "TR",
                        Currency = "TRY",
                        ContactName = registerViewModel.Name,
                        ContactSurname = registerViewModel.Surname,
                        TaxNumber = registerViewModel.TaxNumber,
                        TaxOffice = registerViewModel.TaxOffice,
                        SubMerchantKey = detail.IyzicoSubMerchantKey,
                        IdentityNumber = registerViewModel.TC,
                        LegalCompanyTitle = registerViewModel.CompanyName
                    };
                    var result = IyzicoService.UpdateSubMerchant(request);
                    if (result.IsSuccess)
                    {
                        return new IyzicoResult<string> { IsSuccess = true, Message = "Bilgileriniz güncellendi." };
                    }
                    else
                    {
                        return new IyzicoResult<string> { IsSuccess = false, Message = result.Message };
                    }

                }

            }

            return new IyzicoResult<string> { IsSuccess = false, Message = "Beklenmedik hata ile karşılaşıldı." };




        }

        private static void ActivationApprove(User user)
        {
            using (var mailing = new MailingService())
            using (var text = new TextService())
            {
                var content = text.GetContent(Enums.Texts.AktivasyonOnay, user.LanguageID);
                var html = content.TextContent.Replace("@adSoyad", user.Name);
                mailing.Send(html, user.Email, user.Name, content.Name);
            }
        }

        [Route("HesabimiAktiflestir/{token}")]
        [Route("AccountActivation/{token}")]
        public async Task<IActionResult> Activation(string token)
        {
            GetLang();
            try
            {
                using (var service = new UserService())
                {
                    if (string.IsNullOrEmpty(token))
                        return RedirectToAction("Index", "Home");

                    var user = service.GetFromToken(token);
                    if (user == null)
                        return RedirectToAction("Index", "Home");
                    user.IsActive = true;
                    user.Token = null;
                    var update = service.Update(user);
                    if (update)
                    {
                        await AddCookieAuth(user.UserName, user.Name, user.Email, user.ProfilePicture, user.Role, user.ID.ToString(), user.LanguageID == 2 ? "en" : "tr");
                        TempData.Put("UiMessage", new UiMessage { Class = "success", Message = new Localization().Get("Hesabınız aktifleştirildi.", "Your account has been activated.", user.LanguageID) });
                        ActivationApprove(user);
                    }
                    else
                    {
                        TempData.Put("UiMessage", new UiMessage { Class = "success", Message = new Localization().Get("Hesap aktifleştirme işlemi başarısız oldu.", "Account activation failed.", user.LanguageID) });
                    }
                    return Redirect(user.LanguageID == 1 ? "/Bildirim" : "/Notification");
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public JsonResult UserNameIsUsable(string username)
        {
            var t = new Localization();
            var lang = GetLang();
            if (string.IsNullOrEmpty(username))
                return Json(new
                { isSuccess = false, message = t.Get("Geçersiz kullanıcı adı.", "Invalid user name.", lang) });
            using (var service = new UserService())
            {
                var usernameIsUsable = service.UsernameIsUsable(username, GetLoginID());
                if (!usernameIsUsable)
                    return Json(new
                    { isSuccess = false, message = t.Get(Messages.RegisterUserNameFail_tr, Messages.RegisterUserNameFail_en, lang) });
                return Json(new { isSuccess = true });
            }
        }

        [HttpPost]
        public JsonResult EmailIsUsable(string email)
        {
            var t = new Localization();
            var lang = GetLang();
            if (string.IsNullOrEmpty(email))
                return Json(new
                { isSuccess = false, message = t.Get("Geçersiz e-posta adresi.", "Invalid email.", lang) });
            using (var service = new UserService())
            {
                var usernameIsUsable = service.EmailIsUsable(email, GetLoginID());
                if (!usernameIsUsable)
                    return Json(new
                    { isSuccess = false, message = t.Get(Messages.RegisterEmailFail_tr, Messages.RegisterEmailFail_en, lang) });
                return Json(new { isSuccess = true });
            }
        }


        #endregion

        #region UyelikBilgilerim

        [Route("Hesabim/Uyelik-Bilgilerim")]
        [Route("MyAccount/Personal-Information")]
        [Authorize]
        public IActionResult PersonalInformation()
        {
            using (var publicService = new PublicService())
            using (var service = new UserService())
            {
                var me = service.Get(GetLoginID());
                if (me == null)
                    return Redirect("/");
                var name = me.Name.Trim();
                var splits = name.Split(' ');
                if (splits.Length > 1)
                {
                    me.Surname = splits[splits.Length - 1];
                    me.Name = me.Name.Replace(me.Surname, "");
                }

                var model = new PersonalInformationViewModel
                {
                    User = me,
                    Cities = publicService.GetCities(),
                    Countries = publicService.GetCountries(),
                    Districts = publicService.GetDistricts(),
                    UserSecurePaymentDetail = service.GetSecurePaymentDetail(GetLoginID())
                };
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("Hesabim/Uyelik-Bilgilerim")]
        [Route("MyAccount/Personal-Information")]
        public async Task<IActionResult> PersonalInformation(RegisterViewModel registerViewModel)
        {
            var t = new Localization();
            var lang = GetLang();
            if (registerViewModel.MemberType == (int)MemberType.Seller)
            {
                if (string.IsNullOrWhiteSpace(registerViewModel.TC) || registerViewModel.TC.Length != 11)
                {
                    Notification = new UiMessage(NotyType.error,
                    "TC 11 karakter olmak zorundadir",
                    "TC Identity max length 11", lang);
                    ModelState.AddModelError("TC", "");
                }
                if (!string.IsNullOrWhiteSpace(registerViewModel.TC) && !Regex.Match(registerViewModel.TC, "([0-9]+)").Success)
                {
                    Notification = new UiMessage(NotyType.error,
                    "Lütfen düzgün tc numarası giriniz",
                    "Please must TC Identity", lang);
                    ModelState.AddModelError("TC", "");
                }
                if (string.IsNullOrWhiteSpace(registerViewModel.IBAN) || !(registerViewModel.IBAN.Length > 25 && registerViewModel.IBAN.Length < 34))
                {
                    Notification = new UiMessage(NotyType.error,
                    "Iban hatalidir",
                    "Wrong Iban", lang);
                    ModelState.AddModelError("IBAN", "");
                }
                if (string.IsNullOrWhiteSpace(registerViewModel.SenderAddress) || registerViewModel.SenderAddress.Length < 5)
                {
                    Notification = new UiMessage(NotyType.error,
                    "Gönderici adresi 5 karakterden büyük olmak zorundadır",
                    "Sender address must be 5 character length", lang);
                    ModelState.AddModelError("SenderAddress", "");
                }
                if (string.IsNullOrWhiteSpace(registerViewModel.InvoiceAddress) || registerViewModel.InvoiceAddress.Length < 5)
                {
                    Notification = new UiMessage(NotyType.error,
                    "Fatura adresi 5 karakterden büyük olmak zorundadır",
                    "Invoice address must be 5 character length", lang);
                    ModelState.AddModelError("InvoiceAddress", "");

                }
                if (!new System.ComponentModel.DataAnnotations.PhoneAttribute().IsValid(registerViewModel.MobilePhone))
                {
                    Notification = new UiMessage(NotyType.error,
                        "Geçerli bir telefon giriniz",
                        "Must be valid mobile phone", lang);
                    ModelState.AddModelError("MobilePhone", "");
                }
                if (!string.IsNullOrWhiteSpace(registerViewModel.CompanyName))
                {
                    if (string.IsNullOrWhiteSpace(registerViewModel.CompanyEmail))
                    {
                        Notification = new UiMessage(NotyType.error,
                        "Şirket emaili boş olamaz",
                        "Must be firm e-mail", lang);
                        ModelState.AddModelError("CompanyEmail", "");
                    }
                    else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(registerViewModel.CompanyEmail))
                    {
                        Notification = new UiMessage(NotyType.error,
                        "Geçerli bir e-mail giriniz",
                        "Must be valid e-mail", lang);
                        ModelState.AddModelError("CompanyEmail", "");
                    }
                    if (string.IsNullOrWhiteSpace(registerViewModel.TaxNumber))
                    {
                        Notification = new UiMessage(NotyType.error,
                       "Vergi Numarası girilmesi zorunludur.",
                       "Tax number is required", lang);
                        ModelState.AddModelError("TaxNumber", "");
                    }
                    if (string.IsNullOrWhiteSpace(registerViewModel.TaxOffice))
                    {
                        Notification = new UiMessage(NotyType.error,
                       "Vergi Dairesi girilmesi zorunludur.",
                       "Tax office is required", lang);
                        ModelState.AddModelError("TaxOffice", "");
                    }
                }
            }

            if (!ModelState.IsValid && ModelState.ErrorCount > 4)
            {
                using (var publicService = new PublicService())
                using (var service = new UserService())
                {
                    var me = service.Get(GetLoginID());
                    if (me == null)
                        return Redirect("/");
                    var name = me.Name.Trim();
                    var splits = name.Split(' ');
                    if (splits.Length > 1)
                    {
                        me.Surname = splits[splits.Length - 1];
                        me.Name = me.Name.Replace(me.Surname, "");
                    }
                    var model = new PersonalInformationViewModel
                    {
                        User = me,
                        Cities = publicService.GetCities(),
                        Countries = publicService.GetCountries(),
                        Districts = publicService.GetDistricts(),
                        UserSecurePaymentDetail = service.GetSecurePaymentDetail(GetLoginID()),
                    };
                    if (model.UserSecurePaymentDetail == null) //Sistemdeki validasyonlar ve formdaki bilgilerin kalmasi icin eklendi. Ancak model duzgun yazilmadigi icin bu kontrol yazilmak zorunda kalindi.
                                                               //Bu kontrol aslinda kotu bir tasarım sonucu ortaya zorunlu bir kontrol.
                    {
                        if (!string.IsNullOrWhiteSpace(registerViewModel.TC) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.IBAN) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.SenderAddress) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.InvoiceAddress) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.CompanyName) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.TaxOffice) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.TaxNumber) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.CompanyPhone) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.CompanyFax) ||
                           !string.IsNullOrWhiteSpace(registerViewModel.CompanyEmail))
                            model.UserSecurePaymentDetail = new UserSecurePaymentDetail
                            {
                                TC = registerViewModel.TC,
                                IBAN = registerViewModel.IBAN,
                                SenderAddress = registerViewModel.SenderAddress,
                                InvoiceAddress = registerViewModel.InvoiceAddress,
                                CompanyName = registerViewModel.CompanyName,
                                TaxOffice = registerViewModel.TaxOffice,
                                TaxNumber = registerViewModel.TaxNumber,
                                CompanyPhone = registerViewModel.CompanyPhone,
                                CompanyFax = registerViewModel.CompanyFax,
                                CompanyEmail = registerViewModel.CompanyEmail,
                                Type = registerViewModel.SubMerchantType
                            };
                    }
                    else
                    {
                        model.UserSecurePaymentDetail.TC = registerViewModel.TC;
                        model.UserSecurePaymentDetail.IBAN = registerViewModel.IBAN;
                        model.UserSecurePaymentDetail.SenderAddress = registerViewModel.SenderAddress;
                        model.UserSecurePaymentDetail.InvoiceAddress = registerViewModel.InvoiceAddress;
                        model.UserSecurePaymentDetail.CompanyName = registerViewModel.CompanyName;
                        model.UserSecurePaymentDetail.TaxOffice = registerViewModel.TaxOffice;
                        model.UserSecurePaymentDetail.TaxNumber = registerViewModel.TaxNumber;
                        model.UserSecurePaymentDetail.CompanyPhone = registerViewModel.CompanyPhone;
                        model.UserSecurePaymentDetail.CompanyFax = registerViewModel.CompanyFax;
                        model.UserSecurePaymentDetail.CompanyEmail = registerViewModel.CompanyEmail;
                        model.UserSecurePaymentDetail.Type = registerViewModel.SubMerchantType;
                    }
                    return View(model);
                }
            }

            var route = Request.Path;
            var userId = GetLoginID();
            if (registerViewModel == null)
                return Redirect(route);

            using (var service = new UserService())
            {
                registerViewModel.Name = registerViewModel.Name.Modify();
               // registerViewModel.Surname = registerViewModel.Surname.Modify();
                registerViewModel.UserName = registerViewModel.UserName.Modify();
                var u = service.Get(userId);
                if (u == null) return Redirect(route);

                if (u.Email.ToLower() != registerViewModel.Email?.ToLower())
                {
                    var emailIsAvailable = service.EmailIsUsable(registerViewModel.Email, userId);
                    if (!emailIsAvailable)
                    {
                        Notification = new UiMessage(NotyType.error, "E-mail adresi geçersiz.", "E-Mail address is fail.", registerViewModel.LanguageID);
                        return Redirect(route);
                    }
                }

                if (u.UserName.ToLower() != registerViewModel.UserName?.ToLower())
                {
                    var usernameIsAvailable = service.UsernameIsUsable(registerViewModel.UserName, userId);
                    if (!usernameIsAvailable)
                    {
                        Notification = new UiMessage(NotyType.error, "Kullanıcı adı adresi geçersiz.", "Username is fail.", registerViewModel.LanguageID);
                        return Redirect(route);
                    }
                }

                u.UserName = registerViewModel.UserName;
                u.Name = $"{registerViewModel.Name.Trim()} {registerViewModel.Surname.Trim()}";
                u.Email = registerViewModel.Email;
                u.MobilePhone = registerViewModel.MobilePhone;
                u.GenderID = registerViewModel.GenderID;
                u.WebSite = registerViewModel.WebSite;
                u.CountryID = registerViewModel.CountryID;
                u.CityID = registerViewModel.CityID;
                u.DistrictID = registerViewModel.DistrictID;
                u.LanguageID = registerViewModel.LanguageID;
                u.WorkPhone = registerViewModel.WorkPhone;
                u.BirthDate = registerViewModel.BirthDate;
                u.About = registerViewModel.About;
                u.TC = registerViewModel.TC;
                u.InMailing = registerViewModel.InMailing;
                /*if (!string.IsNullOrEmpty(registerViewModel.TC))
                {
                    var names = registerViewModel.Name.ToUpper().Split(' ');
                    var firstName = registerViewModel.Name.ToUpper();
                    var lastName = names[names.Length - 1];
                    firstName = firstName.Replace(lastName, "").Trim();
                    
                    var responseFromServer = HttpTcKimlikDogrula(ulong.Parse(registerViewModel.TC), firstName,
                        lastName, (ushort)registerViewModel.BirthDate.Year);
                    if (responseFromServer.Contains("true"))
                    {
                        u.TC = registerViewModel.TC;
                    }
                    else
                    {
                        Notification = new UiMessage(NotyType.error,
                            "TC Kimlik doğrulaması başarısız oldu. Adınızı, doğum tarihinizi ve TC Kimik Numaranızı kontrol ediniz.",
                            "TC Authentication failed. Please check your name, date of birth and TC ID.", u.LanguageID);
                        return Redirect(Constants.GetURL(Enums.Routing.UyelikBilgilerim, u.LanguageID));
                    }
                } */


                if (registerViewModel.MemberType == (int)MemberType.Buyer)
                {
                    var securePaymentDetail = service.GetSecurePaymentDetail(userId);
                    if (securePaymentDetail != null)
                    {
                        securePaymentDetail.IsActive = false;
                        service.UpdateSecurePaymentDetail(securePaymentDetail);
                    }

                }

                var update = service.Update(u);
                if (!update)
                {
                    Notification = new UiMessage(NotyType.success, "Bilgileriniz güncellenirken bir hata oluştu.", "There was an error updating your information.", registerViewModel.LanguageID);
                }

                if (registerViewModel.ProfilePictureFile != null && registerViewModel.ProfilePictureFile.Length > 0 && registerViewModel.ProfilePictureFile.Length < 10000000)
                {
                    try
                    {
                        using (var fileService = new FileService())
                        {
                            if (u.ProfilePictureFile != null)
                            {


                                var oldFileName = u.ProfilePicture.Replace("/Upload/Users/", "");
                                fileService.DeleteFile("/Upload/Users/", oldFileName);
                            }
                            var fileName = $"{u.UserName}_{u.ID}_{Guid.NewGuid()}.jpg";
                            var filePath = fileService.FileUpload(GetLoginID(), registerViewModel.ProfilePictureFile, "/Upload/Users/", fileName);
                            new ImageService().Resize(filePath, 300);
                            u.ProfilePicture = "/Upload/Users/" + fileName;
                            var pp = service.Update(u);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                if (registerViewModel.MemberType == (int)MemberType.Seller)
                {
                    var details = new List<UserSecurePaymentDetail>
                {
                    service.GetSecurePaymentDetail(userId),
                    service.GetSecurePaymentDetail(userId, false)
                };

                    foreach (var detail in details)
                    {
                        if (detail != null)
                        {
                            detail.IsActive = false;

                            if (!service.UpdateSecurePaymentDetail(detail))
                            {
                                Notification = new UiMessage(NotyType.error, "Beklenmedik bir hata ile karşılaşıldı.", "There was an error updating your information.", registerViewModel.LanguageID);
                                return Redirect(route);
                            }

                        }
                    }
                    var operation = "create";
                    if (string.IsNullOrEmpty(registerViewModel.CompanyName))
                    { //bireysel satıcı bilgileri insert
                        registerViewModel.IBAN = registerViewModel.IBAN.Replace(" ", string.Empty).ToUpper();
                        int i;
                        for (i = 0; i < details.Count; i++)
                        {
                            if (details[i] == null) continue;
                            if (details[i].Type == SubMerchantType.PERSONAL)
                            {
                                operation = "update";
                                break;
                            }
                            else
                            {
                                operation = "create";
                            }

                        }
                        if (operation == "create")
                        {

                            if (registerViewModel.IBAN.Length == 26)
                            {
                                var result = InsertPersonalSellerInformation(userId, registerViewModel);
                                Notification = result.IsSuccess ? new UiMessage(NotyType.success, result.Message, "Your user information has been updated.", registerViewModel.LanguageID) : new UiMessage(NotyType.error, result.Message, "There was an error updating your information.", registerViewModel.LanguageID);

                            }
                        }
                        if (operation == "update")
                        {
                            registerViewModel.IBAN = registerViewModel.IBAN.Replace(" ", string.Empty).ToUpper();
                            if (registerViewModel.IBAN.Length == 26)
                            {
                                var result = UpdatePersonalSellerInformation(details[i], registerViewModel);
                                Notification = result.IsSuccess ? new UiMessage(NotyType.success, result.Message, "Your user information has been updated.", registerViewModel.LanguageID) : new UiMessage(NotyType.error, result.Message, "There was an error updating your information.", registerViewModel.LanguageID);
                            }
                        }
                    }
                    else
                    { //kurumsal satıcı bilgileri insert

                        int i;
                        for (i = 0; i < details.Count; i++)
                        {
                            if (details[i] != null)
                            {
                                if (details[i].Type == SubMerchantType.PRIVATE_COMPANY || details[i].Type == SubMerchantType.LIMITED_OR_JOINT_STOCK_COMPANY)
                                {
                                    operation = "update";
                                    break;
                                }
                                else
                                {
                                    operation = "create";
                                }
                            }

                        }

                        if (operation == "create")
                        {
                            registerViewModel.IBAN = registerViewModel.IBAN.Replace(" ", string.Empty).ToUpper();
                            if (registerViewModel.IBAN.Length == 26)
                            {

                                var result = InsertCommercialSellerInformation(userId, registerViewModel);
                                Notification = result.IsSuccess ? new UiMessage(NotyType.success, result.Message, "Your user information has been updated.", registerViewModel.LanguageID) : new UiMessage(NotyType.error, result.Message, "There was an error updating your information.", registerViewModel.LanguageID);
                            }

                        }
                        if (operation == "update")
                        {

                            registerViewModel.IBAN = registerViewModel.IBAN.Replace(" ", string.Empty).ToUpper();
                            if (registerViewModel.IBAN.Length == 26)
                            {
                                var result = UpdateCommercialSellerInformation(details[i], registerViewModel);
                                Notification = result.IsSuccess ? new UiMessage(NotyType.success, "Bilgileriniz güncellendi.", "Your user information has been updated.", registerViewModel.LanguageID) : new UiMessage(NotyType.error, result.Message, "There was an error updating your information.", registerViewModel.LanguageID);
                            }
                        }
                    }

                }



                // else if(registerViewModel.CompanyName != detail.CompanyName || u.TC != detail.TC ||
                //         registerViewModel.SenderAddress != detail.SenderAddress || 
                //         registerViewModel.InvoiceAddress != detail.InvoiceAddress || registerViewModel.TaxNumber != detail.TaxNumber||
                //         registerViewModel.TaxOffice != detail.TaxOffice ||registerViewModel.CompanyPhone != detail.CompanyPhone ||
                //         registerViewModel.SubMerchantType != detail.Type)
                // {//update
                //     if (string.IsNullOrEmpty(registerViewModel.CompanyName))
                //     { //bireysel satıcı bilgileri update
                //         updatePersonalSellerInformation(detail, registerViewModel);
                //     }
                //     else
                //     { //kurumsal satıcı bilgileri update
                //         updateCommercialSellerInformation(detail, registerViewModel);
                //     }
                // }
            }
            return Redirect(route);
        }
        #endregion

        #region AdresBilgilerim

        [Route("Hesabim/Adres-Bilgilerim")]
        [Route("MyAccount/Postage-Address")]
        [Authorize]
        public IActionResult AddressInformation()
        {
            using (var service = new UserService())
            {
                var list = service.GetUserAddresses(GetLoginID());
                return View(list);
            }
        }

        [Route("Hesabim/Adres-Bilgilerim/Adres-Ekle")]
        [Route("MyAccount/Postage-Address/Add-Address")]
        [Authorize]
        public IActionResult AddAddress()
        {
            using (var service = new PublicService())
            {
                var model = new AddAddressViewModel
                {
                    Countries = service.GetCountries(),
                    Cities = service.GetCities(),
                    Districts = service.GetDistricts()
                };
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult SaveAddAddress(UserAddress userAddress)
        {
            var lang = GetLang();
            var route = Request.Path;
            if (userAddress == null)
                return Redirect(route);
            using (var service = new UserService())
            {
                userAddress.UserID = GetLoginID();
                if (userAddress.CountryID > 1)
                {
                    userAddress.CityID = 0;
                    userAddress.DistrictID = 0;
                }
                else
                    userAddress.CityText = null;

                var insert = service.InsertAddress(userAddress);
                if (insert)
                {
                    Notification = new UiMessage(NotyType.success, "Adres eklendi.", "Address has been added.", lang);
                    return Redirect(Constants.GetURL((int)Enums.Routing.AdresBilgierim, lang));
                }

                Notification = new UiMessage(NotyType.error, "Adres eklenemedi.", "Address add failed.", lang);
                return Redirect(route);
            }

        }

        [Route("Hesabim/Adres-Bilgilerim/Adres-Duzenle/{id}")]
        [Route("MyAccount/Postage-Address/Edit-Address/{id}")]
        [Authorize]
        public IActionResult EditAddress(int id)
        {
            using (var addressService = new UserService())
            using (var service = new PublicService())
            {
                var address = addressService.GetUserAddress(GetLoginID(), id);
                if (address == null)
                    return Redirect(Constants.GetURL((int)Enums.Routing.AdresBilgierim, GetLang(false)));
                var model = new EditAddressViewModel
                {
                    UserAddress = address,
                    Countries = service.GetCountries(),
                    Cities = service.GetCities(),
                    Districts = service.GetDistricts()
                };
                return View(model);
            }
        }

        [Route("Hesabim/Adres-Bilgilerim/Adres-Duzenle/{id}")]
        [Route("MyAccount/Postage-Address/Edit-Address/{id}")]
        [HttpPost]
        [Authorize]
        public IActionResult EditAddress(UserAddress userAddress)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var addressService = new UserService())
            {
                var address = addressService.GetUserAddress(GetLoginID(), userAddress.ID);
                if (address == null)
                    return Redirect(Constants.GetURL((int)Enums.Routing.AdresBilgierim, GetLang(false)));

                userAddress.UserID = GetLoginID();
                if (userAddress.CountryID > 1)
                {
                    userAddress.CityID = 0;
                    userAddress.DistrictID = 0;
                }
                else
                    userAddress.CityText = null;
                var update = addressService.UpdateAddress(userAddress);
                if (update)
                    Notification = new UiMessage(NotyType.success,
                        t.Get("Adres bilgileriniz güncellendi.", "Address has been updated.", lang));
                else
                    Notification = new UiMessage(NotyType.error,
                        t.Get("Adres bilgileriniz güncellenemedi.", "Could not update your address information.", lang));

                return Redirect(Constants.GetURL((int)Enums.Routing.AdresBilgierim, GetLang(false)));
            }
        }

        [Authorize]
        [HttpPost]
        [Route("UserAddress/Delete")]
        public JsonResult Delete(int id)
        {
            using (var service = new UserService())
            {
                var user = service.Get(GetLoginID());
                if (user == null)
                    return Json(new { isSuccess = false, message = new Localization().Get("Adres silme işlemi başarısız.", "Address delete failed.", 1) });
                var delete = service.DeleteAddress(id, user.ID);
                if (delete)
                {
                    return Json(new
                    {
                        isSuccess = true,
                        message = new Localization().Get("Adres silindi.", "Address has been deleted.", user.LanguageID)
                    });
                }
                return Json(new
                {
                    isSuccess = false,
                    message = new Localization().Get("Adres silme işlemi başarısız.", "Address delete failed.", user.LanguageID)
                });
            }
        }

        #endregion

        #region SifremiDegistir

        [Route("Hesabim/Sifre-Degistir")]
        [Route("MyAccount/Change-Password")]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Route("Hesabim/Sifre-Degistir")]
        [Route("MyAccount/Change-Password")]
        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(string password, string newPassword, string newPassword2)
        {
            var lang = GetLang();
            var route = Request.Path;
            if (newPassword == null || newPassword != newPassword2)
            {
                Notification = new UiMessage(NotyType.error, "Şifreler aynı değil.", "Passwords are not the same.", lang);
                return Redirect(route);
            }
            if (newPassword.Length < 6)
            {
                Notification = new UiMessage(NotyType.error, "Şifreniz 6 karakter veya daha uzun olmalı.", "Your password must be 6 characters or longer.", lang);
                return Redirect(route);
            }
            using (var service = new UserService())
            {
                var user = service.Get(GetLoginID());
                if (user.Password == Encryptor.EncryptData(password))
                {
                    user.Password = Encryptor.EncryptData(newPassword);
                    var update = service.Update(user);
                    Notification = update ? new UiMessage(NotyType.success, "Şifreniz güncellendi.", "Your password has been updated.", lang) : new UiMessage(NotyType.error, "Şifre güncellenemedi.", "Password update fail.", lang);
                }
                else
                {
                    Notification = new UiMessage(NotyType.error, "Mevcut şifre doğru değil.", "Current password is not correct.", lang);
                }
            }
            return Redirect(route);
        }

        #endregion

        #region GuvenliOdemeBilgilerim

        [Authorize]
        public IActionResult SecurePayment1(UserSecurePaymentDetail userSecurePaymentDetail)
        {
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.UyelikBilgilerim, lang);
            if (userSecurePaymentDetail == null)
                return Redirect(route);
            using (var service = new UserService())
            {
                userSecurePaymentDetail.UserID = GetLoginID();
                var details = service.GetSecurePaymentDetail(GetLoginID());
                if (details == null)
                {
                    var insert = service.InsertSecurePaymentDetail(userSecurePaymentDetail);
                    if (!insert)
                    {
                        Notification = new UiMessage(NotyType.error, "Güvenli ödeme bilgileri kayıt edilemedi.",
                            "Secure payment options save failed.", lang);
                        return Redirect(route);
                    }
                    details = userSecurePaymentDetail;
                }
                else
                {
                    details.Name = userSecurePaymentDetail.Name;
                    details.Surname = userSecurePaymentDetail.Surname;
                    details.TC = userSecurePaymentDetail.TC;
                    details.MobilePhone = userSecurePaymentDetail.MobilePhone;
                    details.IBAN = userSecurePaymentDetail.IBAN;
                    details.Email = userSecurePaymentDetail.Email;
                    details.InvoiceAddress = userSecurePaymentDetail.InvoiceAddress;
                    details.SenderAddress = userSecurePaymentDetail.SenderAddress;
                }

                details.Type = SubMerchantType.PERSONAL;
                var type = details.Type;
                var request = new SubMerchant
                {
                    ConversationId = GetLoginID().ToString(),
                    SubMerchantExternalId = GetLoginID().ToString(),
                    SubMerchantType = type.ToString(),
                    Address = details.InvoiceAddress,
                    Email = details.Email,
                    GsmNumber = details.MobilePhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                    Name = details.CompanyName,
                    Iban = details.IBAN?.Replace(" ", ""),
                    Locale = "TR",
                    Currency = "TRY",
                    ContactName = details.Name,
                    ContactSurname = details.Surname,
                    IdentityNumber = details.TC
                };
                var result = IyzicoService.CreateSubMerchant(request);
                if (result.IsSuccess)
                {
                    details.IyzicoSubMerchantKey = result.Data.SubMerchantKey.Replace(" ", "").Replace("/r/n", "");
                    var update = service.UpdateSecurePaymentDetail(details);
                    if (update)
                    {
                        Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz bireysel kategorisinden kayıt edilmiştir.",
                            "Your secure payment information has been recorded in the individual category.", lang);
                    }
                    else
                    {
                        Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                            "#2 Secure payment options save failed.", lang);
                    }
                }
                else
                {
                    Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. " + result.Message,
                        "#3 Secure payment options save failed." + result.Message, lang);
                }
            }
            return Redirect(route);
        }

        [Authorize]
        public IActionResult SecurePayment2(UserSecurePaymentDetail userSecurePaymentDetail)
        {
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.UyelikBilgilerim, lang);
            if (userSecurePaymentDetail == null)
                return Redirect(route);
            using (var service = new UserService())
            {
                userSecurePaymentDetail.UserID = GetLoginID();
                var details = service.GetSecurePaymentDetail(GetLoginID());
                if (details == null)
                {
                    var insert = service.InsertSecurePaymentDetail(userSecurePaymentDetail);
                    if (!insert)
                    {
                        Notification = new UiMessage(NotyType.error, "Güvenli ödeme bilgileri kayıt edilemedi.",
                            "Secure payment options save failed.", lang);
                        return Redirect(route);
                    }
                    details = userSecurePaymentDetail;
                }

                details.Type = userSecurePaymentDetail.Type;
                var type = details.Type;
                var request = new SubMerchant
                {
                    ConversationId = GetLoginID().ToString(),
                    SubMerchantExternalId = GetLoginID().ToString(),
                    SubMerchantType = type.ToString(),
                    Address = userSecurePaymentDetail.InvoiceAddress,
                    Email = userSecurePaymentDetail.Email,
                    GsmNumber = userSecurePaymentDetail.MobilePhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                    Name = details.CompanyName,
                    Iban = details.IBAN?.Replace(" ", ""),
                    Locale = "TR",
                    Currency = "TRY",
                    TaxOffice = userSecurePaymentDetail.TaxOffice,
                    LegalCompanyTitle = userSecurePaymentDetail.CompanyName,
                };
                if (type == SubMerchantType.PRIVATE_COMPANY)
                    request.IdentityNumber = details.TC;
                else if (type == SubMerchantType.LIMITED_OR_JOINT_STOCK_COMPANY)
                    request.TaxNumber = userSecurePaymentDetail.TaxNumber?.Trim();

                var result = IyzicoService.CreateSubMerchant(request);
                if (result.IsSuccess)
                {
                    details.CompanyName = userSecurePaymentDetail.CompanyName;
                    details.CompanyFax = userSecurePaymentDetail.CompanyFax;
                    details.CompanyEmail = userSecurePaymentDetail.CompanyEmail;
                    details.CompanyPhone = userSecurePaymentDetail.CompanyPhone;
                    details.CompanyTypeID = userSecurePaymentDetail.CompanyTypeID;

                    details.IyzicoSubMerchantKey = result.Data.SubMerchantKey.Replace(" ", "").Replace("/r/n", "");
                    var update = service.UpdateSecurePaymentDetail(details);
                    if (update)
                    {
                        var typeCategory = (type == SubMerchantType.PRIVATE_COMPANY)
                            ? new Localization().Get("Şahıs Şirketi", "Private Company", lang)
                            : new Localization().Get("Kurumsal Şirket", "Corporate Company", lang);
                        Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz " + typeCategory + " kategorisinden kayıt edilmiştir.",
                            "Your secure payment information has been recorded in the " + typeCategory + " category.", lang);
                    }
                    else
                    {
                        Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                            "#2 Secure payment options save failed.", lang);
                    }
                }
                else
                {
                    Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. " + result.Message,
                        "#3 Secure payment options save failed." + result.Message, lang);
                }
            }
            return Redirect(route);
        }

        #endregion

        #region ProfilSayfam

        [Route("Hesabim/Profilim")]
        [Route("MyAccount/Profile")]
        [Authorize]
        public IActionResult MyProfile()
        {
            using (var adService = new AdvertService())
            using (var service = new UserService())
            {
                var user = service.Get(GetLoginID());
                if (user == null)
                    return Redirect("/");

                user.Adverts = adService.GetUserAdverts(user.ID);
                return View(user);
            }
        }

        #endregion

        #region CariHesabim

        [Route("Hesabim/Cari-Hesabim")]
        [Route("Hesabim/alislarim-satislarim")]
        [Route("MyAccount/Commercial-Account")]
        [Authorize]
        public IActionResult CommercialAccount()
        {
            using (var sUser = new UserService())
            using (var sDoping = new DopingService())
            using (var sBanner = new BannerService())
            {
                var banners = sBanner.GetBannerHistroy(GetLoginID());
                var model = new CommercialAccountViewModel
                {
                    Buys = sUser.GetBuys(GetLoginID()),
                    Sales = sUser.GetSales(GetLoginID()),
                    Dopings = sDoping.GetDopingHistory(GetLoginID()),
                    Banners = banners?.Where(x => x.TypeID == Enums.BannerType.Banner).ToList(),
                    LogoBanners = banners?.Where(x => x.TypeID == Enums.BannerType.Logo).ToList(),
                };
                return View(model);
            }
        }

        [Route("Hesabim/Alislarim")]
        [Route("MyAccount/MyBuying")]
        [Authorize]
        public IActionResult Purchases()
        {
            var lang = GetLang();
            using (var sUser = new UserService())
            using (var sDoping = new DopingService())
            using (var textService = new TextService())
            using (var sBanner = new BannerService())
            {
                var text = textService.GetText(Enums.Texts.AliciOnaylamamaNedeni, lang);
                var banners = sBanner.GetBannerHistroy(GetLoginID());
                var model = new CommercialAccountViewModel
                {
                    Buys = sUser.GetBuys(GetLoginID()),
                    Dopings = sDoping.GetDopingHistory(GetLoginID()),
                    Banners = banners?.Where(x => x.TypeID == Enums.BannerType.Banner).ToList(),
                    LogoBanners = banners?.Where(x => x.TypeID == Enums.BannerType.Logo).ToList(),
                    Text = text
                };
                return View(model);
            }
        }
        [Route("Hesabim/Satislarim")]
        [Route("MyAccount/MySales")]
        [Authorize]
        public IActionResult Sales()
        {
            using (var sUser = new UserService())
            using (var sDoping = new DopingService())
            using (var sBanner = new BannerService())
            {
                var banners = sBanner.GetBannerHistroy(GetLoginID());
                var model = new CommercialAccountViewModel
                {
                    Sales = sUser.GetSales(GetLoginID()),
                    Dopings = sDoping.GetDopingHistory(GetLoginID()),
                    Banners = banners?.Where(x => x.TypeID == Enums.BannerType.Banner).ToList(),
                    LogoBanners = banners?.Where(x => x.TypeID == Enums.BannerType.Logo).ToList(),
                };
                return View(model);
            }
        }

        [Authorize]
        public IActionResult UpdateCargoDetails(int id, string cargoFirm, string cargoNo, DateTime cargoDate, Enums.PaymentRequestStatus status)
        {
            var t = new Localization();
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.Satislarim, lang);

            using (var prService = new PaymentRequestService())
            using (var service = new UserService())
            using (var mailing = new MailingService())
            using (var setting = new SystemSettingService())
            using (var ad = new AdvertService())
            using (var text = new TextService())
            {
                var pr = service.GetSale(id, GetLoginID());
                if (pr != null && pr.SellerID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, t.Get("Erişim hatası.", "Access error.", lang));
                    return Redirect(route);
                }
                if (pr == null || pr.Status == Enums.PaymentRequestStatus.Onaylandi ||
                    pr.Status == Enums.PaymentRequestStatus.AliciIptalEtti || pr.Status == Enums.PaymentRequestStatus.Iptal ||
                    pr.Status == Enums.PaymentRequestStatus.SaticiIptalEtti
                    || pr.Status == Enums.PaymentRequestStatus.AliciIptalTalebiOlusturdu)
                {
                    Notification = new UiMessage(NotyType.error, t.Get("Erişim hatası.", "Access error.", lang));
                    return Redirect(route);
                }
                if (status == Enums.PaymentRequestStatus.Iptal)
                {
                    status = pr.SellerID == GetLoginID() ? Enums.PaymentRequestStatus.SaticiIptalEtti : Enums.PaymentRequestStatus.AliciIptalEtti;
                }

                pr.CargoFirm = cargoFirm;
                pr.CargoNo = cargoNo;
                pr.CargoDate = cargoDate;
                pr.CargoDeliveryDate = null;
                pr.Status = status;
                var update = prService.Update(pr);
                if (!update)
                {
                    Notification = new UiMessage(NotyType.error,
                        t.Get("Güncelleme sırasında beklenmedik bir hata oluştu.",
                            "An unexpected error occurred during the update.", lang));
                    return Redirect(route);
                }
                Notification = new UiMessage(NotyType.success, t.Get("Teslimat bilgileri güncellendi.",
                        "Shipment information has been updated.", lang));

                if (status == Enums.PaymentRequestStatus.KargoyaVerildi)
                {
                    var content = text.GetContent(Enums.Texts.UruneOnayVermenizGereklidir, lang);
                    var buyer = service.Get(pr.UserID);
                    var seller = service.Get(pr.SellerID);
                    var advert = ad.GetAdvert(pr.AdvertID);
                    var day = setting.GetSetting(Enums.SystemSettingName.OtomatikOdemeOnayi).Value;
                    var html = content.TextContent
                        .Replace("@aliciAdSoyad", buyer.Name)
                        .Replace("@otomotikOnay", day)
                        .Replace("@urunAdi", advert.Title)
                        .Replace("@urunAdet", pr.Amount.ToString())
                        .Replace("@siparisNo", pr.ID.ToString())
                        .Replace("@saticiAdSoyad", seller.Name)
                        .Replace("@saticiMail", seller.Email)
                        .Replace("@saticiTel", seller.MobilePhone)
                        .Replace("@saticiAdres", pr.ShippingAddress)
                        .Replace("@prId", pr.ID.ToString());
                    mailing.Send(html, buyer.Email, buyer.Name, content.Name);
                    PaymentRequestCargoInfoUpdated(pr, t);

                }
                else if (status == Enums.PaymentRequestStatus.SaticiIptalEtti)
                {
                    PaymentRequestSellerCanceled(pr, t);
                    var buyer = service.Get(pr.UserID);
                    var seller = service.Get(pr.SellerID);
                    var sellerSecurePaymentDetail = service.GetSecurePaymentDetail(pr.SellerID);
                    if (buyer != null && seller != null && sellerSecurePaymentDetail != null)
                    {

                        SendNotificationMailBuyerCanceled(pr, buyer, seller, sellerSecurePaymentDetail.SenderAddress);
                    }
                }
                else if (status == Enums.PaymentRequestStatus.AliciIptalEtti)
                    PaymentRequestBuyerCanceled(pr, t);
                else if (status == Enums.PaymentRequestStatus.Onaylandi)
                {
                    var s = PaymentRequestDelivered(pr, t, pr.Seller.LanguageID);
                    if (!s)
                    {
                        return RedirectToAction("Sales");
                    }
                }

                return Redirect(route);
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult ReceivedCargo(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var settingService = new SystemSettingService())
            using (var mailing = new MailingService())
            using (var userService = new UserService())
            using (var service = new PaymentRequestService())
            {
                var settings = settingService.GetSystemSettings();
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Access Error" });
                if (pr.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = "Access Error" });

                pr.Status = Enums.PaymentRequestStatus.Onaylandi;
                pr.BuyerApproval = true;
                pr.IsSuccess = true;
                pr.BuyerApprovalDate = DateTime.Now;
                pr.CargoDeliveryDate = DateTime.Now;
                var update = service.Update(pr);
                if (update)
                {
                    PaymentRequestDelivered(pr, t, lang);
                    var adminMails = settings.Single(s => s.Name == Enums.SystemSettingName.YoneticiMailleri).Value;
                    var seller = userService.Get(pr.SellerID);
                    //if (seller != null && seller.IdentityPhotosApproved)
                    if (seller != null)
                    {
                        var moneyTransfer = MoneyTransfer2Seller(pr);
                        if (moneyTransfer != "success")
                        {
                            mailing.SendMail2Admins(adminMails, "Onaylama Hatası.",
                                "Ödeme ID: " + pr.ID + " Kullanıcı ürünü onayladım dedi ancak iyzicoda onaylanmadi " +
                                "Iyzico Payment Id: " + pr.PaymentId + " Iyzico PaymentTransactionId: " + pr.PaymentTransactionID +
                                " Hata Mesajı: " + moneyTransfer);
                        }
                        else
                        {
                            mailing.SendMail2Admins(adminMails, pr.ID + "ID'li sipariş alıcı tarafından onaylandı.",
                               $"#{pr.ID} 'li sipariş alıcı tarafından onaylandı." +
                               $" Sipariş No: #<a href=\"https://www.gittibu.com/AdminPanel/PaymentRequests/Details/{pr.ID}\" target=\"_blank\">{pr.ID}</a> <br /> İşlem Tarihi: {pr.CreatedDate:dd.MM.yyyy HH:mm}");
                            SendApproveOrderMailToSeller(pr, seller);
                        }
                    }
                    else
                    {
                        mailing.SendMail2Admins(adminMails, "Onaylama Hatası.",
                        "Ödeme ID: " + pr.ID +
                        " Satıcı Bulunumadı." +
                        " Iyzico Payment Id: " + pr.PaymentId + " Iyzico PaymentTransactionId: " + pr.PaymentTransactionID);
                        return Json(new
                        {
                            isSuccess = true,
                            message = t.Get("Sipariş bilgileri güncellenirken hata oluştu. Lütfen tekrar deneyiniz.",
                                "Error updating order information. Please try again.", lang)
                        });
                    }

                    return Json(new { isSuccess = true, message = t.Get("Sipariş bilgileri güncellendi.", "Order information updated.", lang) });
                }
                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("Sipariş bilgileri güncellenirken hata oluştu. Lütfen tekrar deneyiniz.",
                    "Error updating order information. Please try again.", lang)
                });
            }
        }

        private string MoneyTransfer2Seller(PaymentRequest pr)
        {
            var approval = IyzicoService.PaymentApproval(pr.ID, pr.PaymentTransactionID);
            return approval.Status == "success" ? "success" : approval.ErrorMessage;
        }

        [Authorize]
        [HttpPost]
        public JsonResult OrderCancel(int id, string message)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var service = new PaymentRequestService())
            {
                var pr = service.GetOrder(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Access Error" });
                if (pr.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = "Access Error" });

                pr.Status = Enums.PaymentRequestStatus.AliciIptalTalebiOlusturdu;
                pr.BuyerApproval = false;
                pr.BuyerApprovalDate = DateTime.Now;
                var update = service.Update(pr);
                if (update)
                {

                    PaymentRequestBuyerCanceled(pr, t);
                    //var refund = IyzicoService.PaymentRefund(pr.ID, pr.PaymentTransactionID, GetIpAddress(),
                    // pr.Price.ToString());

                    SendOrderCancelMailToSeller(pr, message);
                    return Json(new
                    {
                        isSuccess = true,
                        message = t.Get("Siparişiniz için iptal talebiniz oluşturuldu.",
                        "Your cancellation request has been created for your order.", lang)
                    });
                }
                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("Sipariş bilgileri güncellenirken hata oluştu. Lütfen tekrar deneyiniz.",
                    "Error updating order information. Please try again.", lang)
                });
            }
        }

        private static void SendApproveOrderMailToSeller(PaymentRequest pr, User seller)
        {
            using (var mailing = new MailingService())
            using (var userService = new UserService())
            using (var text = new TextService())
            using (var adService = new AdvertService())

            {
                var ad = adService.GetAdvert(pr.AdvertID);

                var buyer = userService.Get(pr.UserID);
                var content = text.GetContent(Enums.Texts.AliciOnay, seller.LanguageID);
                var html = content.TextContent
                    .Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@siparisNo", pr.ID.ToString())
                    .Replace("@urunAdi", ad.Title)
                    .Replace("@urunAdet", pr.Amount.ToString())
                    .Replace("@aliciAdSoyad", buyer.Name)
                    .Replace("@aliciMail", buyer.Email)
                    .Replace("@aliciTel", buyer.MobilePhone)
                    .Replace("@aliciAdres", pr.InvoiceAddress)
                    .Replace("@prId", pr.ID.ToString());

                mailing.Send(html, seller.Email, seller.Name, content.Name);
            }
        }

        private static void SendOrderCancelMailToSeller(PaymentRequest pr, string message)
        {
            using (var mailing = new MailingService())
            using (var settingService = new SystemSettingService())
            using (var userService = new UserService())
            using (var text = new TextService())
            {
                var settings = settingService.GetSystemSettings();
                var seller = userService.Get(pr.SellerID);
                var buyer = userService.Get(pr.UserID);
                var content = text.GetContent(Enums.Texts.AliciRed, seller.LanguageID);
                var html = content.TextContent
                    .Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@redMesaj", message)
                    .Replace("@siparisNo", pr.ID.ToString())
                    .Replace("@buyeraliciAdSoyad", buyer.Name)
                    .Replace("@aliciMail", buyer.Email)
                    .Replace("@aliciTel", buyer.MobilePhone)
                    .Replace("@aliciAdres", pr.InvoiceAddress)
                    .Replace("@prId", pr.ID.ToString());

                mailing.Send(html, seller.Email, seller.Name, content.Name);
                var adminMails = settings.Single(s => s.Name == Enums.SystemSettingName.YoneticiMailleri).Value;
                mailing.SendMail2Admins(adminMails, "Para İadesi Talebi",
                    $"Ödeme ID:<a href=\"https://www.gittibu.com/AdminPanel/PaymentRequests/Details/{pr.ID}\" target=\"_blank\">{pr.ID} </a> <br /> Kullanıcı siparişi iptal talebi oluşturdu. <br />  Iyzico Payment Id: {pr.PaymentId} <br /> Iyzico PaymentTransactionId: {pr.PaymentTransactionID} <br /> Neden : {message}");
            }
        }


        #region PaymentRequest_Update_Notification

        private void PaymentRequestCargoInfoUpdated(PaymentRequest pr, Localization t)
        {
            var notify = new Notification
            {
                UserID = pr.UserID,
                Image = "/Content/img/cargo.png",
                Message = t.Get(
                    "Satın aldığınız " + pr.AdvertID + " ilan numaralı ürün kargoya verildi. " +
                    "Kargo Firması: " + pr.CargoFirm + " Takip Numarası: " + pr.CargoNo,
                    "The product you purchased is numbered " + pr.AdvertID + ". " +
                    "Cargo Firm: " + pr.CargoFirm + " Cargo Tracking Number: " + pr.CargoNo, pr.Buyer.LanguageID),
                CreatedDate = DateTime.Now,
                TypeID = Enums.NotificationType.IlanKargoBilgisiGuncellemesi,
                SenderUserID = GetLoginID(),
                Url = Constants.GetURL((int)Enums.Routing.Alislarim, pr.Buyer.LanguageID)
            };
            using (var service = new NotificationService())
            {
                service.Insert(notify);
            }
        }

        private void PaymentRequestSellerCanceled(PaymentRequest pr, Localization t)
        {
            var notify = new Notification
            {
                UserID = pr.UserID,
                Image = "/Content/img/cancel.png",
                Message = t.Get(
                    pr.ID + " numaralı siparişiniz satıcı tarafından iptal edildi.",
                    "Your order number " + pr.ID + " has been canceled by the seller.",
                    pr.Buyer.LanguageID),
                CreatedDate = DateTime.Now,
                TypeID = Enums.NotificationType.IlanKargoBilgisiGuncellemesi,
                SenderUserID = GetLoginID(),
                Url = Constants.GetURL((int)Enums.Routing.Alislarim, pr.Buyer.LanguageID)
            };
            using (var service = new NotificationService())
            {
                service.Insert(notify);
            }
        }

        private static void SendNotificationMailBuyerCanceled(PaymentRequest pr, User buyer, User seller, string sellerAddress)
        {
            using (var mailing = new MailingService())
            using (var text = new TextService())
            {
                var content = text.GetContent(Enums.Texts.SiparisIptalEdildi, buyer.LanguageID);
                var html = content.TextContent.Replace("@aliciAdSoyad", buyer.Name)
                    .Replace("@urunAdi", pr.Advert?.Title)
                    .Replace("@urunAdet", pr.Amount.ToString())
                    .Replace("@siparisNo", pr.ID.ToString())
                    .Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@saticiMail", seller.Email)
                    .Replace("@saticiTel", seller.MobilePhone)
                    .Replace("@saticiAdres", sellerAddress);
                mailing.Send(html, buyer.Email, buyer.Name, content.Name);
            }
        }

        private void PaymentRequestBuyerCanceled(PaymentRequest pr, Localization t)
        {
            var notify = new Notification
            {
                UserID = pr.SellerID,
                Image = "/Content/img/cancel.png",
                Message = t.Get(
                    pr.ID + " numaralı siparişiniz alıcı tarafından iptal edildi.",
                    "Your order number " + pr.ID + " has been canceled by the buyer.",
                    pr.Seller.LanguageID),
                CreatedDate = DateTime.Now,
                TypeID = Enums.NotificationType.IlanIptal,
                SenderUserID = pr.UserID,
                Url = Constants.GetURL(Enums.Routing.Satislarim, pr.Seller.LanguageID)
            };
            using (var service = new NotificationService())
            {
                service.Insert(notify);
            }
        }

        private static bool PaymentRequestDelivered(PaymentRequest pr, Localization t, int lang)
        {
            try
            {
                var notify = new Notification
                {
                    UserID = pr.SellerID,
                    Image = "/Content/img/success-128.png",
                    Message = t.Get(
                        pr.ID + " numaralı sipariş alıcıya teslim edildi.",
                        "Order " + pr.ID + " was delivered to the buyer.",
                        lang),
                    CreatedDate = DateTime.Now,
                    TypeID = Enums.NotificationType.IlanKargoBilgisiGuncellemesi,
                    SenderUserID = 0,
                    Url = Constants.GetURL((int)Enums.Routing.Alislarim, pr.Seller.LanguageID)
                };
                var notify2 = new Notification
                {
                    UserID = pr.UserID,
                    Image = "/Content/img/success-128.png",
                    Message = t.Get(
                        pr.ID + " numaralı siparişiniz teslim edildi olarak güncellendi.",
                        "Order " + pr.ID + " has been updated as delivered.",
                        lang),
                    CreatedDate = DateTime.Now,
                    TypeID = Enums.NotificationType.IlanKargoBilgisiGuncellemesi,
                    SenderUserID = pr.SellerID,
                    Url = Constants.GetURL((int)Enums.Routing.Alislarim, pr.Buyer.LanguageID)
                };
                using (var service = new NotificationService())
                {

                    service.Insert(notify);
                    return service.Insert(notify2);
                }

            }
            catch (Exception)
            {
                return false;
            }

        }

        #endregion





        [Authorize]
        [HttpPost]
        public JsonResult GetPaymentRequest(int id)
        {
            using (var service = new UserService())
            {
                var pr = service.GetSale(id, GetLoginID());
                if (pr == null)
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Erişim hatası.", "Access error.", GetLang())
                    });
                else
                    return Json(new { isSuccess = true, data = pr });
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult GetPaymentRequestBuyer(int id)
        {
            using (var service = new PaymentRequestService())
            {

                var pr = service.Get(id);
                if (pr == null)
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Erişim hatası.", "Access error.", GetLang())
                    });
                else
                    return Json(new { isSuccess = true, data = pr });
            }
        }

        #endregion

        #region Ilanlarim

        [Route("Hesabim/Ilanlarim")]
        [Route("MyAccount/My-Listing")]
        [Authorize]
        public IActionResult MyListing(int page = 1, int advertCount = 30)
        {
            using (var service = new AdvertService())
            {
                var list = service.GetUserAdverts(GetLoginID());
                MyListingViewModel myListingViewModel = SearchMyListing(list, page, advertCount);
                return View(myListingViewModel);
            }
        }

        [Route("Hesabim/Ilanlarim/Arama/{queryListing}")]
        [Route("MyAccount/My-Listing/Search/{queryListing}")]
        [Authorize]
        public IActionResult MyListing(string queryListing, int page = 1, int advertCount = 30)
        {
            if (string.IsNullOrEmpty(queryListing))
                return Redirect("/");

            var lang = GetLang(false);
            int userId = GetLoginID();

            using (var adService = new AdvertService())
            {
                var filteredAdverts = adService.Search(queryListing, GetLoginID(), lang);
                var userAdverts = filteredAdverts.Where(s => s.UserID == userId).ToList();

                MyListingViewModel myListingViewModel = SearchMyListing(userAdverts, page, advertCount);
                return View(myListingViewModel);
            }
        }

        [Route("MyAccount/PublishRequest")]
        [HttpPost]
        [Authorize]
        public JsonResult PublishRequest(int id)
        {
            var lang = GetLang(false);
            using (var service = new AdvertService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null)
                    return Json(new { isSuccess = false, message = new Localization().Get("İlana erişilemedi.", "Ad was not reached.", lang) });
                var request = new AdvertPublishRequest
                {
                    IsActive = true,
                    RequestDate = DateTime.Now,
                    AdvertID = id,
                    UserID = GetLoginID()
                };
                var insert = service.InsertPublishRequest(request);
                if (!insert)
                    return Json(new { isSuccess = false, message = new Localization().Get("İlan yayınlanırken bir sorun oluştu.", "Ad publish failed.", lang) });

                if (ad.IsDeleted)
                {
                    ad.IsDeleted = false;
                    service.Update(ad);
                }

                return Json(new
                {
                    isSuccess = true,
                    message = new Localization().Get(
                    "Talebiniz oluşturuldu. Yönetici onayından sonra ilanınız yayınlanacak."
                    , "Your request has been created. Your ad will be published after the administrator's approval.", lang)
                });
            }
        }

        [Route("MyAccount/Unpublish")]
        [HttpPost]
        [Authorize]
        public JsonResult Unpublish(int id)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new AdvertService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null || ad.UserID != GetLoginID())
                {
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası.", "Access error.", lang) });
                }
                ad.IsActive = false;
                var update = service.Update(ad);
                if (!update)
                {
                    return Json(new { isSuccess = false, message = t.Get("Güncelleme işlemi sırasında bir hata oluştu.", "Unpublish is failed.", lang) });
                }
                service.DeletePublishRequests(id);

                return Json(new { isSuccess = true, message = t.Get("İlanınız  yayından alındı.", "Your ad has been hidden.", lang) });
            }
        }


        #endregion

        #region Bannerlarim

        [Route("Hesabim/Bannerlarim")]
        [Route("MyAccount/MyBanners")]
        [Authorize]
        public IActionResult MyBanners()
        {
            using (var sBanner = new BannerService())
            {
                var banners = sBanner.GetBannerHistroy(GetLoginID());
                return View(banners);
            }
        }

        [Route("Hesabim/Banner-Ekle")]
        [Route("MyAccount/Add-Banner")]
        [Authorize]
        public IActionResult AddBanner()
        {
            using (var service = new PublicService())
            {
                var doping = service.GetDopingTypes();
                if (doping != null && doping.Any())
                {
                    var model = new AddBannerViewModel
                    {
                        LogoDopings = doping.Where(x => x.Group == Enums.DopingGroup.LogoBanner).ToList(),
                        BannerDopings = doping.Where(x => x.Group == Enums.DopingGroup.AnasayfaBanner).ToList()
                    };
                    return View(model);
                }
            }
            return View();
        }

        [Route("MyAccount/AddLogoBanner")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddLogoBanner(int dopingId, string title, string url, bool paymentType, string fullname,
            string number, string month, string year, string cvc, string file64, IFormFile gifFile, int fileType)
        {
            var t = new Localization();
            var lang = GetLang(false);
            var route = Constants.GetURL((int)Enums.Routing.BannerEkle, lang);
            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            try
            {
                using (var mailingService = new MailingService())
                using (var settingService = new SystemSettingService())
                using (var userService = new UserService())
                using (var payRequestService = new PaymentRequestService())
                using (var bannerService = new BannerService())
                using (var publicService = new PublicService())
                {
                    var droppings = publicService.GetDopingTypes();
                    var doping = droppings.SingleOrDefault(x => x.ID == dopingId);

                    var adminMails = settingService.GetSystemSettings().SingleOrDefault(s => s.Name == Enums.SystemSettingName.YoneticiMailleri)?.Value;

                    #region ValidationsVeFileUpload

                    if (doping == null)
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Seçtiğiniz banner tipi tespit edilemedi.", "The banner type you selected could not be detected.", lang));
                        return Redirect(route);
                    }
                    if ((fileType == 1 || fileType == 2) && string.IsNullOrEmpty(file64))
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Dosya bulunamadı", "File is not found", lang));
                        return Redirect(route);
                    }
                    else if (fileType == 3 && string.IsNullOrEmpty(file64))
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Logo dosyası bulunamadı", "Logo File is not found", lang));
                        return Redirect(route);
                    }
                    else if (fileType == 4 && gifFile == null)
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Gif Dosyası bulunamadı", "Gif File is not found", lang));
                        return Redirect(route);
                    }
                    else if (fileType != 1 && fileType != 2 && fileType != 3 && fileType != 4)
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Banner türü algılanamadı.", "Banner type could not be detected.", lang));
                        return Redirect(route);
                    }
                    var fileResult = "";
                    if (fileType == 1 || fileType == 2 || fileType == 3) //slider resim banner veya resim logo
                    {
                        file64 = file64.Replace("data:image/png;base64,", "");
                        file64 = file64.Replace("data:image/jpg;base64,", "");
                        file64 = file64.Replace("data:image/jpeg;base64,", "");
                        Byte[] bytes = Convert.FromBase64String(file64);
                        var src = "/Upload/Dopings/";
                        var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".jpg";
                        var upload = new FileService().FileUpload(bytes, src, fileName);
                        if (!upload)
                        {
                            Notification = new UiMessage(NotyType.error, t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                            return Redirect(route);
                        }
                        fileResult = new ImageService().Optimize75(src + fileName, src, fileName, addExt: false);
                    }
                    else if (fileType == 4) //gif logo
                    {
                        var src = "/Upload/Dopings/";
                        var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".gif";
                        var upload = new FileService().FileUpload(GetLoginID(), gifFile, src, fileName);
                        if (string.IsNullOrEmpty(upload))
                        {
                            Notification = new UiMessage(NotyType.error, t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                            return Redirect(route);
                        }
                        fileResult = new ImageService().OptimizeGif(src + fileName);
                    }
                    if (string.IsNullOrEmpty(fileResult))
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Resim optimizasyon sorunu. Geçerli bir resim yüklediğinizden emin olunuz.",
                            "Image optimization problem. Be sure to upload a valid image.", lang), 10000);
                        return Redirect(route);
                    }
                    var user = userService.Get(GetLoginID());
                    if (user == null)
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Kullanıcı bilginize erişilemedi. Lütfen giriş yaptıktan sonra tekrar deneyin.",
                            "Your user information could not be accessed. Please try again after login.", lang));
                        return Redirect(route);
                    }
                    #endregion

                    var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);

                    var bannerType = Enums.BannerType.Banner;

                    if (fileType == 1) //anasayfa banner
                    {
                        bannerType = Enums.BannerType.Banner;
                    }
                    else if (fileType == 2)//slider banner
                    {
                        bannerType = Enums.BannerType.Slider;
                    }
                    else if (fileType == 3 || fileType == 4) //logo veya gif
                    {
                        bannerType = Enums.BannerType.Logo;
                    }
                    var banner = new Banner
                    {
                        Title = title,
                        CreatedDate = DateTime.Now,
                        UserID = GetLoginID(),
                        ImageSource = fileResult,
                        Url = url,
                        TypeID = bannerType,
                        Price = doping.Price,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddDays(doping.Day)
                    };
                    var bannerInsert = bannerService.Insert(banner);
                    if (!bannerInsert)
                    {
                        Notification = new UiMessage(NotyType.info, t.Get(
                            "Banner kaydedildi ancak onay talebi oluşturulamadı. Lütfen site yönetimine bilgi veriniz.",
                            "The banner was saved but the confirmation request could not be created. Please inform the website management.", lang));
                        return Redirect(route);
                    }

                    var paymentRequest = new PaymentRequest
                    {
                        ForeignModelID = banner.ID,
                        UserID = GetLoginID(),
                        Price = doping.Price,
                        Description = User.Identity.Name + " " + doping.Name + " " + doping.Day + " gün satın alıyor. ",
                        Type = Enums.PaymentType.LogoBanner,
                        CreatedDate = DateTime.Now,
                        SellerID = 0,
                        IpAddress = ip,
                        SecurePayment = paymentType,
                        Amount = doping.Day,
                        Status = Enums.PaymentRequestStatus.Bekleniyor
                    };
                    var insertRequest = payRequestService.Insert(paymentRequest);

                    if (insertRequest && Production)
                    {
                        var adminMailContent = "#" + user.ID + " ID'li " + user.Name + " isimli kullanıcı banner girişi yaptı.";
                        mailingService.SendMail2Admins(adminMails, user.Name + " isimli kullanıcı banner girişi yaptı.", adminMailContent);
                    }

                    if (insertRequest && paymentType)
                    {
                        var init = BuildPayment(user, userAddress, ip, cvc, fullname, month, year, number,
                            paymentRequest.ID, doping, IyzicoService.callbackName);
                        if (init.Status != "success")
                        {
                            Notification = new UiMessage(NotyType.error, init.ErrorMessage);
                            return Redirect(route);
                        }
                        return View("Blank", init.HtmlContent);
                    }
                    else if (insertRequest)
                    {
                        Notification = new UiMessage(NotyType.success, t.Get(
                            "İşleminiz tamamlandı. Yönetici onayından sonra banner yayınlanacak.",
                            "Your transaction is complete. The banner will be published after the administrator's approval.", lang));
                        return Redirect(route);
                    }
                }
            }
            catch (Exception)
            {
                Notification = new UiMessage(NotyType.success, t.Get(
                    "Beklenmedik bir hata oluştu. Lütfen tekrar deneyin.",
                    "An unexpected error has occurred. Please try again.", lang));
            }
            return Redirect(route);
        }


        [Route("Hesabim/Banner-Duzenle/{id}")]
        [Route("MyAccount/Edit-Banner/{id}")]
        [Authorize]
        public IActionResult EditBanner(int id)
        {
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner != null && banner.UserID == GetLoginID())
                    return View(banner);

                return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, GetLang(false)));
            }
        }

        [Route("Hesabim/Banner-Duzenle/{id}")]
        [Route("MyAccount/Edit-Banner/{id}")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditBanner(int id, string title, string url, string file64, IFormFile gifFile, int fileType)
        {
            var lang = GetLang(false);
            var route = Constants.GetURL(Enums.Routing.Bannerlarim, lang);
            var t = new Localization();
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null || banner.UserID != GetLoginID())
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));

                banner.Title = title;
                banner.Url = url;

                #region FileUpload

                if (!string.IsNullOrEmpty(file64))
                {
                    var fileResult = "";
                    if (fileType == 1) //slider resim banner veya resim logo
                    {
                        file64 = file64.Replace("data:image/png;base64,", "");
                        file64 = file64.Replace("data:image/jpg;base64,", "");
                        file64 = file64.Replace("data:image/jpeg;base64,", "");
                        Byte[] bytes = Convert.FromBase64String(file64);
                        var src = "/Upload/Dopings/";
                        var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".jpg";
                        var upload = new FileService().FileUpload(bytes, src, fileName);
                        if (!upload)
                        {
                            Notification = new UiMessage(NotyType.error,
                                t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                            return Redirect(route);
                        }

                        fileResult = new ImageService().Optimize75(src + fileName, src, fileName, addExt: false);
                    }
                    else if (fileType == 2) //gif logo
                    {
                        var src = "/Upload/Dopings/";
                        var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".gif";
                        var upload = new FileService().FileUpload(GetLoginID(), gifFile, src, fileName);
                        if (string.IsNullOrEmpty(upload))
                        {
                            Notification = new UiMessage(NotyType.error,
                                t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                            return Redirect(route);
                        }
                        fileResult = new ImageService().OptimizeGif(src + fileName);
                    }
                    if (string.IsNullOrEmpty(fileResult))
                    {
                        Notification = new UiMessage(NotyType.error, t.Get(
                            "Resim optimizasyon sorunu. Geçerli bir resim yüklediğinizden emin olunuz.",
                            "Image optimization problem. Be sure to upload a valid image.", lang), 10000);
                        return Redirect(route);
                    }

                    banner.ImageSource = fileResult;
                }

                #endregion


                var update = service.Update(banner);
                if (!update)
                {
                    Notification = new UiMessage(NotyType.error,
                        t.Get("Banner güncellenirken bir hata oluştu.", "Banner update failed.", lang));
                    return Redirect(route);
                }

                Notification = new UiMessage(NotyType.success,
                    t.Get("Banner güncellendi.", "Banner has been updated.", lang));
                return Redirect(route);
            }
        }

        [Route("MyAccount/UnpublishBanner")]
        [HttpPost]
        [Authorize]
        public JsonResult UnpublishBanner(int id)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new BannerService())
            {
                var ad = service.Get(id);
                if (ad == null || ad.UserID != GetLoginID())
                {
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası.", "Access error.", lang) });
                }
                ad.IsActive = false;
                var update = service.Update(ad);
                if (!update)
                {
                    return Json(new { isSuccess = false, message = t.Get("Güncelleme işlemi sırasında bir hata oluştu.", "Unpublish is failed.", lang) });
                }

                return Json(new { isSuccess = true, message = t.Get("Bannerınız yayından alındı.", "Your banner has been hidden.", lang) });
            }
        }

        [Route("MyAccount/ExtendBannerTime/{id}")]
        [Authorize]
        public IActionResult ExtendBannerTime(int id)
        {
            var lang = GetLang(false);
            using (var pubService = new PublicService())
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null || banner.UserID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, "Erişim hatası.", "Access error.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }
                var droppings = new List<DopingType>();
                droppings = banner.TypeID == Enums.BannerType.Logo ? pubService.GetDopingTypes().Where(x => x.Group == Enums.DopingGroup.LogoBanner).OrderBy(x => x.Day).ToList() : droppings.Where(x => x.Group == Enums.DopingGroup.AnasayfaBanner).ToList();
                var model = new ExtendBannerTimeViewModel
                {
                    Banner = banner,
                    Dopings = droppings
                };
                return View(model);
            }
        }

        [Route("MyAccount/ExtendBannerTime/{id}")]
        [HttpPost]
        [Authorize]
        public IActionResult ExtendBannerTime(int id, bool paymentType, int DopingID, string CardName, string CardNumber,
            string Month, string Year, string CVC)
        { //paymentType: true = kredi kartı ile ödeme
            var lang = GetLang(false);

            using (var prService = new PaymentRequestService())
            using (var userService = new UserService())
            using (var pubService = new PublicService())
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null || banner.UserID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, "Erişim hatası.", "Access error.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                var doping = pubService.GetDopingTypes().SingleOrDefault(d => d.ID == DopingID);
                if (doping == null)
                {
                    Notification = new UiMessage(NotyType.error, "Geçersiz doping seçimi yaptınız.", "You have chosen an invalid doping.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                var user = userService.Get(GetLoginID());
                if (user == null)
                {
                    Notification = new UiMessage(NotyType.error, "Üyelik bilgilerinize erişilemedi, lütfen tekrar giriş yapınız.",
                        "Your membership information could not be accessed, please login again.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                if (!paymentType)
                {
                    var pr = new PaymentRequest
                    {
                        Amount = doping.Day,
                        UserID = GetLoginID(),
                        ForeignModelID = banner.ID,
                        Type = Enums.PaymentType.BannerSureUzatma,
                        Price = doping.Price,
                        SecurePayment = false,
                        Description = user.Name + " (" + user.UserName + ") isimli kullanıcı banka havalesi ile banner süresini uzatma talebi oluşturdu.",
                        CreatedDate = DateTime.Now,
                        IpAddress = GetIpAddress(),
                        Status = Enums.PaymentRequestStatus.Bekleniyor
                    };
                    var insert = prService.Insert(pr);
                    if (!insert)
                    {
                        Notification = new UiMessage(NotyType.error, "Ödeme kaydınız oluşturulamadı. Lütfen tekrar deneyiniz.",
                            "Your payment registration process failed. Please try again.", lang);
                        return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                    }

                    Notification = new UiMessage(NotyType.success, "Ödeme kaydınız oluşturuldu. Yönetici onayından sonra yazınız güncellenecek.",
                        "Your payment registration has been created. Will be updated after the approval of the administrator.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                if (string.IsNullOrEmpty(CardName) || string.IsNullOrEmpty(CardNumber) || string.IsNullOrEmpty(Month) ||
                    string.IsNullOrEmpty(Year) || string.IsNullOrEmpty(CVC))
                {
                    Notification = new UiMessage(NotyType.error, "Lütfen kredi kartı bilgilerinizi doldurunuz.",
                        "Please fill in your credit card information.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                var userAddresses = userService.GetUserAddresses(user.ID);
                if (userAddresses == null || userAddresses.Count == 0)
                {
                    Notification = new UiMessage(NotyType.error, "Satın alım yapmak için bir adres kaydı oluşturmalısınız.",
                        "You must create an address record to make a purchase.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.AdresBilgierim, lang));
                }
                var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);

                var ip = GetIpAddress();
                var _pr = new PaymentRequest
                {
                    Amount = doping.Day,
                    UserID = GetLoginID(),
                    ForeignModelID = banner.ID,
                    Type = Enums.PaymentType.BannerSureUzatma,
                    Price = doping.Price,
                    SecurePayment = true,
                    Description = $"{user.Name} ({user.UserName}) isimli kullanıcı kredi kartı ile banner süresini uzatma talebi oluşturdu.",
                    CreatedDate = DateTime.Now,
                    IpAddress = GetIpAddress(),
                    Status = Enums.PaymentRequestStatus.Bekleniyor,
                };
                var _insert = prService.Insert(_pr);
                if (!_insert)
                {
                    Notification = new UiMessage(NotyType.error, "Güncelleme işlemi yapılamadı.",
                        "Update failed.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                var payment = BuildPayment(user, userAddress, ip, CVC, CardName, Month, Year, CardNumber, _pr.ID, doping,
                    IyzicoService.callbackName);

                if (payment.Status != "success")
                {
                    Notification = new UiMessage(NotyType.success, "Güvenli Ödeme başlatılamadı. " + payment.ErrorMessage,
                        "Secure payment cannot started. " + payment.ErrorMessage, lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }
                else
                {
                    return View("Blank", payment.HtmlContent);
                }

            }

            //return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
        }


        #endregion

        #region Begendiklerim

        [Route("Hesabim/Begendigim-Ilanlar")]
        [Route("MyAccount/MyLikes")]
        [Authorize]
        public IActionResult MyLikes()
        {
            using (var service = new UserService())
            {
                var list = service.GetMyLikes(GetLoginID());
                return View(list);
            }
        }

        [Route("MyLikes/Unfollow")]
        [HttpPost]
        [Authorize]
        public JsonResult Unfollow(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var service = new UserService())
            {
                var like = service.GetAdvertLike(id);
                if (like == null)
                    return Json(new { isSuccess = false, message = t.Get("Bu ilanı zaten takip etmiyorsunuz.", "You are not already following this ad.", lang) });

                if (like.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = t.Get("Erişiminiz engellendi.", "Access denied.", lang) });

                if (!service.DeleteAdvertLike(like))
                    return Json(new { isSuccess = false, message = t.Get("Takibi bırakma sırasında bir hata oluştu.", "An error occurred during unfollow.", lang) });

                return Json(new { isSuccess = true, message = t.Get("İşleminiz tamamlandı!", "The follow up has been abandoned!", lang) });
            }
        }

        #endregion

        #region Yorumlar

        [Route("Hesabim/Yaptigim-Yorumlar")]
        [Route("MyAccount/MyComments")]
        [Authorize]
        public IActionResult MyComments()
        {
            return View();
        }

        #endregion

        #region Kimlik Fotoğraflarım

        [Authorize]
        [Route("Hesabim/Kimlik-Fotograflarim")]
        [Route("MyAccount/Identity-Photos")]
        public IActionResult IdentityPhotos()
        {
            using (var service = new UserService())
            {
                var user = service.Get(GetLoginID());
                return View(user);
            }
        }

        [Authorize]
        public async Task<IActionResult> UploadIdentityPhotos(IFormFile front, IFormFile back)
        {
            using (var fileService = new FileService())
            using (var settingService = new SystemSettingService())
            using (var mailing = new MailingService())
            using (var userService = new UserService())
            {
                var user = userService.Get(GetLoginID());

                var frontPath = fileService.FileUpload(GetLoginID(), front, "/Upload/Identities/",
                        user.ID + "_front" + Path.GetExtension(front.FileName), false);
                var backPath = fileService.FileUpload(GetLoginID(), back, "/Upload/Identities/",
                    user.ID + "_back" + Path.GetExtension(back.FileName), false);
                user.IdentityPhotoFront = frontPath;
                user.IdentityPhotoBack = backPath;
                if (userService.Update(user))
                {
                    var settings = settingService.GetSystemSettings();
                    var adminMails = settings.Single(s => s.Name == Enums.SystemSettingName.YoneticiMailleri).Value;
                    var link = $"<a href=\"https://www.gittibu.com/AdminPanel/Users/Details/{user.ID}\">Onayla</a>";
                    mailing.SendMail2Admins(adminMails, "TC Kimlik Fotoğrafı Girişi",
                        user.Name + " isimli kullanıcı tc kimlik fotoğraflarını yükledi. " +
                        "Onaylamak için admin paneline giriş yapın ve kullanıcı detay sayfasına gidiniz. <br/> " + link);

                    Notification = new UiMessage(NotyType.success, "Yönetici onayından sonra kimlik bilgileriniz kayıt edilecek.",
                        "Your credentials will be registered after the administrator's approval.", GetLang(false));
                }
                else
                {
                    Notification = new UiMessage(NotyType.error, "Kimlik fotoğraflarınız yüklenirken bir hata oluştu. Lütfen tekrar deneyiniz.",
                        "There was an error uploading your ID photos. Please try again.", GetLang(false));
                }

                return Redirect(Constants.GetURL(Enums.Routing.KimlikFotograflarim, user.LanguageID));
            }
        }

        #endregion

        #region Satın Alma ve Callback

        private ThreedsInitialize BuildPayment(User user, UserAddress userAddress, string ip, string cvc, string fullname,
            string month, string year, string number, int paymentRequestId, DopingType doping, string callback)
        {
            Address address;
            string addressLine;
            if (userAddress == null)
            {
                addressLine = user?.District?.Name + " " + user?.City?.Name;
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

            return IyzicoService.PayBanner(paymentRequestId, doping.Price, card,
                buyer, address, address, doping.ID,
                doping.Name + "_" + doping.Day, callback);
        }

        [Route("Callback")]
        public IActionResult Callback(Iyzico3dCallback callback)
        {
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.ProfilSayfam, lang);

            if (string.IsNullOrEmpty(callback.PaymentId) || callback.PaymentId == "0")
            {
                Notification = new UiMessage(NotyType.error, new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                    10000);
                return Redirect(route);
            }
            var request = new CreateThreedsPaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = callback.ConversationId.ToString(),
                PaymentId = callback.PaymentId,
                ConversationData = callback.ConversationData
            };

            var threesPayment = ThreedsPayment.Create(request, IyzicoService.GetOptions());
            if (threesPayment.Status == "success")
            {
                using (var bannerService = new BannerService())
                using (var blogService = new BlogPostService())
                using (var service = new PaymentRequestService())
                {
                    var payReq = service.Get(Convert.ToInt32(callback.ConversationId));
                    if (payReq != null)
                    {
                        payReq.PaymentId = threesPayment.PaymentId;
                        payReq.ResponseDate = DateTime.Now;
                        payReq.PaymentTransactionID = threesPayment.PaymentItems.First().PaymentTransactionId;
                        payReq.Status = Enums.PaymentRequestStatus.OnlineOdemeYapildi;
                        var update = service.Update(payReq);
                        if (update)
                        {
                            if (payReq.Type == Enums.PaymentType.BlogSureUzatma)
                            { //ödeme işlemi blog süre uzatması için ise, paymentRequest kaydından post'u bul, süresini uzat
                                var post = blogService.Get(payReq.ForeignModelID);
                                post.DopingEndDate = post.DopingEndDate < DateTime.Now ? DateTime.Now.AddDays(payReq.Amount) : post.DopingEndDate?.AddDays(payReq.Amount);

                                var updateBlog = blogService.Update(post);
                                if (updateBlog)
                                {
                                    Notification = new UiMessage(NotyType.success, new Localization().Get("Ödeme işlemi tamamlandı. Yazı süreniz uzatıldı.", "Payment completed. Your post was extended.", lang),
                                        10000);
                                    return Redirect(route);
                                }
                                else
                                {
                                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                                            "Ödeme işlemi tamamlandı fakat yazı süresi uzatılamadı. Lütfen site yönetimi ile iletişime geçin."
                                            , "Payment completed but post doping end time could not be updated. Please contact the site administration.", lang),
                                        10000);
                                }
                            }
                            if (payReq.Type == Enums.PaymentType.BannerSureUzatma)
                            { //ödeme işlemi banner süre uzatması için ise, paymentRequest kaydından banner'ı bul, süresini uzat
                                var banner = bannerService.Get(payReq.ForeignModelID);
                                banner.EndDate = banner.EndDate < DateTime.Now ? DateTime.Now.AddDays(payReq.Amount) : banner.EndDate?.AddDays(payReq.Amount);

                                var updateBanner = bannerService.Update(banner);
                                if (updateBanner)
                                {
                                    Notification = new UiMessage(NotyType.success, new Localization().Get("Ödeme işlemi tamamlandı. Banner süreniz uzatıldı.", "Payment completed. Your banner was extended.", lang),
                                        10000);
                                    return Redirect(route);
                                }
                                else
                                {
                                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                                            "Ödeme işlemi tamamlandı fakat yazı süresi uzatılamadı. Lütfen site yönetimi ile iletişime geçin."
                                            , "Payment completed but post doping end time could not be updated. Please contact the site administration.", lang),
                                        10000);
                                }
                            }

                            Notification = new UiMessage(NotyType.success, new Localization().Get("Ödeme işlemi tamamlandı", "Payment completed", lang),
                                10000);
                            return Redirect(route);
                        }
                    }
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin."
                            , "Payment completed but could not be saved in the system. Please contact the site administration.", lang),
                        10000);
                    return Redirect(route);
                }

            }

            Notification = new UiMessage(NotyType.error, threesPayment.ErrorMessage, 10000);
            return Redirect(route);
        }

        #endregion



        [Authorize]
        [Route("ReadNotification")]
        public JsonResult ReadNotification(int id)
        {
            using (var service = new NotificationService())
            {
                var notify = service.Get(id);
                if (notify != null && notify.UserID == GetLoginID())
                {
                    notify.ReadedDate = DateTime.Now;
                    var update = service.Update(notify);
                    return Json(new { isSuccess = update });
                }
                return Json(new { isSuccess = false });
            }
        }

        public IActionResult TestError()
        {
            var y = 0;
            var x = 5 / y;
            return Content("result: " + x);
        }

        public MyListingViewModel SearchMyListing(List<Advert> adverts, int page, int advertCount)
        {
            MyListingViewModel myListingViewModel = new MyListingViewModel();
            if (adverts != null)
            {
                myListingViewModel.CurrentPage = page;
                myListingViewModel.PagingAdvertCount = advertCount;
                myListingViewModel.TotalPages = (adverts.Count / advertCount) + (adverts.Count % advertCount != 0 ? 1 : 0);

                List<Advert> advertData = adverts.OrderByDescending(x => x.CreatedDate).ToList();
                if (page >= 1 && advertCount >= 1)
                {
                    myListingViewModel.Adverts = advertData?.Skip((page - 1) * advertCount).Take(advertCount).ToList();
                }
                else
                {
                    myListingViewModel.Adverts = advertData?.Skip(0).Take(5).ToList();
                }
            }
            return myListingViewModel;
        }

    }
}
