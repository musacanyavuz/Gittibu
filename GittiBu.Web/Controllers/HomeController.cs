using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using GittiBu.Common;
using GittiBu.Common.Rss;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SimpleMvcSitemap;

namespace GittiBu.Web.Controllers
{
    public class HomeController : BaseController
    {
        #region Anasayfa

        public IActionResult Index()
        {
            var lang = GetLang();
            using (var advertCategoryService = new AdvertCategoryService())
            using (var bannerService = new BannerService())
            using (var textService = new TextService())
            using (var service = new AdvertService())
            {
                var adverts = service.GetHomePageAdverts(lang, GetLoginID());
                var banners = bannerService.GetBanners();
                //var homepageSplitBanners = banners.Where(x => x.TypeID == Enums.BannerType.Banner)?.OrderBy(b=>b.ID).ToList();
                //var homepageBottomAd = banners.SingleOrDefault(x => x.TypeID == Enums.BannerType.Slider);

                var model = new HomePageViewModel
                {
                    AdvertCategories = advertCategoryService.GetMasterCategories(GetLang()),
                    Banners = banners,
                    //HomepageSplitBanners = homepageSplitBanners,
                    //HomepageBottomAd = homepageBottomAd,
                    Items = Ads2HomePageItems(adverts, lang),
                    SeoValues = new SeoKeys
                    {
                        Title = textService.GetText(Enums.Texts.AnasayfaTitle, lang),
                        Description = textService.GetText(Enums.Texts.AnasayfaAciklama, lang),
                        Keywords = textService.GetText(Enums.Texts.AnasayfaSEOKeywords, lang),
                        Separator = textService.GetText(Enums.Texts.Ayrac, lang)
                    }
                };
                ViewBag.IsMainPage = true;
                return View(model);
            }
        }

        [Route("{id:int}")]
        public IActionResult Index(int id)
        {
            var lang = GetLang();
            using (var catService = new AdvertCategoryService())
            using (var service = new AdvertService())
            {
                var advert = service.Get(id);
                if (advert == null)
                    return Redirect("/");
                var advertCats = catService.GetAll();
                var model = new AdvertDetailsViewModel
                {
                    Advert = advert,
                    AdvertCategories = advertCats,
                    SimilarAdverts = service.GetSimilarAdverts(advert.CategoryID, 4, lang)
                };
                if (lang == (int)Enums.Languages.tr)
                {
                    ViewBag.TranslateUrl = "/Advert/" +  Common.Localization.Slug(advert.Title) + "/" + advert.ID;
                }
                else
                {
                    ViewBag.TranslateUrl = "/Ilan/" +  Common.Localization.Slug(advert.Title) + "/" + advert.ID;
                }
                service.UpdateViewCount(id);
                return View("~/Views/Adverts/Details.cshtml", model);
            }
        }


        public PartialViewResult GetAdverts(int offset)
        {
            var lang = GetLang();
            using (var service = new AdvertService())
            {
                var list = service.GetHomePageAdverts(lang, offset, GetLoginID());
                if (list == null || list.Count == 0)
                {
                    return null;
                }
                return PartialView("~/Views/Shared/AdvertItem_5ColumnList.cshtml", Ads2HomePageItems(list, lang));
            }
        }

        private List<HomePageItem> MergeAdsBlogs(List<Advert> ads, List<BlogPost> posts, int lang, bool take)
        {
            var result = new List<HomePageItem>();

            foreach (var ad in ads)
            {
                result.Add(Ad2HomePageItem(ad, lang));
            }

            return result.OrderByDescending(x => x.ID).ToList();
        }
        [Route("rss")]
        public IActionResult Rss()
        {
            var lang = GetLang();

            using (var blogService = new BlogPostService())
            using (var service = new AdvertService())
            {
                var urlBase = "https://www.gittibu.com";
                var adverts = service.GetAllActiveAdverts(lang, GetLoginID());
                var posts = blogService.GetHomePagePosts();
                var model = MergeAdsBlogs(adverts, posts, lang, false);

                var feed = new Feed
                {
                    Title = "GittiBu",
                    Description = "GittiBu Feed1 Adverts(5) ",
                    Link = new Uri("https://www.gittibu.com/rss"),
                    Copyright = "(c) GittiBu " + DateTime.Now.Year
                };


                foreach (var item in model)
                {
                    feed.Items.Add(new Item
                    {
                        Title = item.Title,
                        Image = $"{urlBase}{item.ImageSource}",
                        Body = item.Description,
                        Link = new Uri(urlBase + item.Url),
                        Permalink = new Uri(urlBase + item.Url).AbsoluteUri,
                        PublishDate = item.LastUpdateDate ?? item.CreatedDate,
                        Categories = new List<string> { item.CategoryName }
                    });
                }
                var rss = feed.Serialize();
                return Content(rss, "application/rss+xml", Encoding.UTF8);
            }
        }

        [Route("feed2")]
        public IActionResult Feed2()
        {
            //var lang = GetLang();

            //using (var blogService = new BlogPostService())
            //using (var service = new AdvertService())
            //{
            //    var urlBase = "https://www.gittibu.com";
            //    var adverts = service.GetAllActiveAdverts(lang, GetLoginID()).Take(10).ToList();
            //    var posts = blogService.GetHomePagePosts();
            //    var model = MergeAdsBlogs(adverts, posts, lang, false);

            //    var feed = new Feed
            //    {
            //        Title = "GittiBu",
            //        Description = "GittiBu Feed2 Adverts(10)",
            //        Link = new Uri("https://www.gittibu.com/feed2"),
            //        Copyright = "(c) GittiBu " + DateTime.Now.Year
            //    };

            //    foreach (var item in model)
            //    {
            //        feed.Items.Add(new Item
            //        {
            //            Title = item.Title,
            //            Body = item.Description,
            //            Link = new Uri(urlBase + item.Url),
            //            Image = $"{urlBase}{item.ImageSource}",
            //            Permalink = new Uri(urlBase + item.Url).AbsoluteUri,
            //            PublishDate = item.LastUpdateDate ?? item.CreatedDate,
            //            Categories = new List<string> { item.CategoryName }
            //        });
            //    }
            //    var rss = feed.Serialize(true);
            //    return Content(rss, "application/rss+xml", Encoding.GetEncoding("utf-16"));
            //}
            var lang = GetLang();

            using (var blogService = new BlogPostService())
            using (var service = new AdvertService())
            {
                var urlBase = "https://www.gittibu.com";
                var adverts = service.GetAllActiveAdverts(lang, GetLoginID()).Take(20).ToList();
                var posts = blogService.GetHomePagePosts();
                var model = MergeAdsBlogs(adverts, posts, lang, false);


                var feed = new Feed
                {
                    Title = "GittiBu",
                    Description = "GittiBu Feed2  Adverts(10)",
                    Link = new Uri("https://www.gittibu.com/feed2"),
                    Copyright = "(c) GittiBu " + DateTime.Now.Year,
                    image = new Image
                    {
                        url = "https://www.gittibu.com/favicon.png",
                        title = "GittiBu",
                        link = "https://www.gittibu.com/feed2",
                        width = "32",
                        height = "32"
                    }
                };



                foreach (var item in model)
                {
                    feed.Items.Add(new Item
                    {
                        Title = item.Title,
                        Body = item.Description,
                        Link = new Uri(urlBase + item.Url),
                        Image = $"{urlBase}{item.ImageSource}",
                        Permalink = new Uri(urlBase + item.Url).AbsoluteUri,
                        PublishDate = item.LastUpdateDate ?? item.CreatedDate,
                        Categories = new List<string> { item.CategoryName }
                    });
                }
                var rss = feed.Serialize();
                return Content(rss, "application/rss+xml", Encoding.UTF8);
            }
        }
        [Route("feed")]
        public IActionResult Feed()
        {
            var lang = GetLang();

            using (var blogService = new BlogPostService())
            using (var service = new AdvertService())
            {
                var urlBase = "https://www.gittibu.com";
                var adverts = service.GetAllActiveAdverts(lang, GetLoginID()).Take(3).ToList();
                var posts = blogService.GetHomePagePosts();
                var model = MergeAdsBlogs(adverts, posts, lang, false);

                var feed = new Feed
                {
                    Title = "GittiBu",
                    Description = "GittiBu Feed0 Adverts(3)",
                    Link = new Uri("https://www.gittibu.com/feed"),
                    Copyright = "(c) GittiBu " + DateTime.Now.Year
                };

                foreach (var item in model)
                {
                    feed.Items.Add(new Item
                    {
                        Title = item.Title,
                        Body = item.Description,
                        Link = new Uri(urlBase + item.Url),
                        Image = $"{urlBase}{item.ImageSource}",
                        Permalink = new Uri(urlBase + item.Url).AbsoluteUri,
                        PublishDate = item.LastUpdateDate ?? item.CreatedDate,
                        Categories = new List<string> { item.CategoryName }
                    });
                }
                var rss = feed.Serialize();
                return Content(rss, "application/rss+xml", Encoding.UTF8);
            }
        }



        [Route("feed3")]
        public IActionResult Feed3()
        {
            var lang = GetLang();

            using (var blogService = new BlogPostService())
            using (var service = new AdvertService())
            {
                var urlBase = "https://www.gittibu.com";
                var adverts = service.GetAllActiveAdverts(lang, GetLoginID()).Take(20).ToList();
                var posts = blogService.GetHomePagePosts();
                var model = MergeAdsBlogs(adverts, posts, lang, false);


                var feed = new Feed
                {
                    Title = "GittiBu",
                    Description = "GittiBu Feed3  Adverts(20)",
                    Link = new Uri("https://www.gittibu.com/feed3"),
                    Copyright = "(c) GittiBu " + DateTime.Now.Year,
                    image = new Image
                    {
                        url = "https://www.gittibu.com/favicon.png",
                        title = "GittiBu",
                        link = "https://www.gittibu.com/feed3",
                        width = "32",
                        height = "32"
                    }
                };



                foreach (var item in model)
                {
                    feed.Items.Add(new Item
                    {
                        Title = item.Title,
                        Body = item.Description,
                        Link = new Uri(urlBase + item.Url),
                        Image = $"{urlBase}{item.ImageSource}",
                        Permalink = new Uri(urlBase + item.Url).AbsoluteUri,
                        PublishDate = item.LastUpdateDate ?? item.CreatedDate,
                        Categories = new List<string> { item.CategoryName }
                    });
                }
                var rss = feed.Serialize();
                return Content(rss, "application/rss+xml", Encoding.UTF8);
            }
        }

        [Route("sitemap.xml")]
        public IActionResult Sitemap()
        {
            var nodes = new List<SitemapNode>
            {
                new SitemapNode(Url.Action("Index","Home")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("Help","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("AboutUs","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("Videos","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("TermsOfUse","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("SecurePayment","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("Prices","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("Contact","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
            };
            using (var catService = new AdvertCategoryService())
            using (var service = new AdvertService())
            {
                const string urlBase = "https://www.gittibu.com";
                var lang = GetLang();
                var adverts = service.GetAllActiveAdverts(lang, GetLoginID());
                //var posts = blogService.GetAllActiveAdverts();
                var models = MergeAdsBlogs(adverts, null, lang, false);
                var categories = catService.GetAll(lang);

                foreach (var cat in categories)
                {
                    nodes.Add(new SitemapNode(urlBase + "/Kategori/" + cat.Slug)
                    {
                        Priority = 0.5M,
                        ChangeFrequency = ChangeFrequency.Monthly,
                    });
                }

                foreach (var item in models)
                {
                    var lta = item.LastUpdateDate ?? item.CreatedDate;
                    var w3CDateTime = new DateTime(lta.Year, lta.Month, lta.Day, lta.Hour, lta.Minute, lta.Second, DateTimeKind.Local);
                    nodes.Add(new SitemapNode(urlBase + item.Url)
                    {
                        LastModificationDate = w3CDateTime,
                        Priority = 0.5M,
                        ChangeFrequency = ChangeFrequency.Weekly,
                    });
                }
            }
            var sitemap = new SitemapModel(nodes);

            return new SitemapProvider().CreateSitemap(sitemap);
        }

        #endregion


        #region Like_Unlike

        [HttpPost]
        [Route("LikeAd")]
        public JsonResult LikeAd(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            if (!User.Identity.IsAuthenticated)
                return Json(new { isSuccess = false, message = t.Get("Bu işlem için giriş yapmanız gerekiyor.", "You need to login for this process.", lang) });

            using (var userService = new UserService())
            using (var service = new AdvertService())
            {
                var ad = service.Get(id);
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("İlana erişilemedi.", "Ad was not reached.", lang) });

                var user = userService.Get(GetLoginID());
                if (user == null)
                    return Json(new { isSuccess = false, message = t.Get("Kullanıcı bilgilerinize erişilemedi.", "Your user information could not be accessed.", lang) });

                var isLiked = service.GetAdvertLike(id, GetLoginID());
                if (isLiked != null)
                    return Json(new { isSuccess = false, message = t.Get("Bu ilanı zaten beğenmişsiniz.", "You like this ad already.", lang) });

                var insert = service.InsertAdvertLike(new AdvertLike
                {
                    AdvertID = id,
                    UserID = GetLoginID(),
                    CreatedDate = DateTime.Now
                });
                if (!insert)
                    return Json(new { isSuccess = false, message = t.Get("İlan beğenilirken beklenmedik bir sorun oluştu.", "An unexpected problem occurred while posting.", lang) });

                return Json(new { isSuccess = true, message = "" });
            }
        }

        [HttpPost]
        [Route("UnlikeAd")]
        public JsonResult UnlikeAd(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            if (!User.Identity.IsAuthenticated)
                return Json(new { isSuccess = false, message = t.Get("Bu işlem için giriş yapmanız gerekiyor.", "You need to login for this process.", lang) });

            using (var userService = new UserService())
            using (var service = new AdvertService())
            {
                var ad = service.Get(id);
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("İlana erişilemedi.", "Ad was not reached.", lang) });

                var user = userService.Get(GetLoginID());
                if (user == null)
                    return Json(new { isSuccess = false, message = t.Get("Kullanıcı bilgilerinize erişilemedi.", "Your user information could not be accessed.", lang) });

                var isLiked = service.GetAdvertLike(id, GetLoginID());
                if (isLiked == null)
                    return Json(new { isSuccess = false, message = t.Get("Bu ilanı zaten beğenmemişsiniz.", "You don't like this ad already.", lang) });

                var delete = service.DeleteAdvertLike(isLiked);
                if (!delete)
                    return Json(new { isSuccess = false, message = t.Get("İlan beğenmekten vazgeçilirken beklenmedik bir sorun oluştu.", "There was an unexpected problem while the advertisement was unliked.", lang) });

                return Json(new { isSuccess = true, message = "" });
            }
        }

        #endregion

        [Authorize]
        [Route("DistanceSalesContract/{id}")]
        public IActionResult DistanceSalesContract(int id)
        {
            try
            {
                GetLang(false);
                using (var userService = new UserService())
                using (var service = new PaymentRequestService())
                {
                    var Model = service.GetOrder(id);
                    if (Model == null)
                    {
                        return Redirect("/");
                    }

                    var address = userService.GetUserAddresses(Model.SellerID);
                    if (address != null && address.Any())
                    {
                        var sellerAddress = address.SingleOrDefault(x => x.IsDefault) ?? address.First();
                        ViewBag.SellerAddress = sellerAddress.Address + " " +
                                                (sellerAddress.City?.Name ?? sellerAddress.CityText) + " "
                                                + sellerAddress.District?.Name + " "
                                                + sellerAddress.Country?.Name;
                    }


                    var lang = GetLang();
                    string pageHtml = string.Empty;
                    using (var content = new TextService())
                    {
                        pageHtml = content.GetText(Enums.Texts.MesafeliSatisSozlesmesi, lang);
                        if (!string.IsNullOrEmpty(pageHtml))
                        {
                            pageHtml = pageHtml
                                .Replace("@(Model.Buyer.Name)", Model.Buyer.Name)
                                .Replace("@(Model.Buyer.TC)", Model.Buyer.TC)
                                .Replace("@(Model.ShippingAddress)", Model.ShippingAddress)
                                .Replace("@(Model.Buyer.Email)", Model.Buyer.Email)
                                .Replace("@(Model.Buyer.MobilePhone)", Model.Buyer.MobilePhone)
                                .Replace("@(Model.Seller.Name)", Model.Seller.Name)
                                .Replace("@(Model.Seller.TC)", Model.Seller.TC)
                                .Replace("@(Model.Seller.Email)", Model.Seller.Email)
                                .Replace("@(Model.Seller.MobilePhone)", Model.Seller.MobilePhone)
                                .Replace("@ViewBag.SellerAddress", ViewBag.SellerAddress)
                                .Replace("@(Model.ID)", Model.ID.ToString())

                                .Replace("@(Model.Advert.CreatedDate.ToShortDateString())", Model.Advert?.CreatedDate.ToShortDateString())
                                .Replace("@(Model.Advert.Brand)", Model.Advert?.Brand)
                                .Replace("@(Model.Advert.Model)", Model.Advert?.Model)
                                .Replace("@(Model.Advert.Title)", Model.Advert?.Title)
                                .Replace("@(Model.Advert.ProductStatus)", Model.Advert?.ProductStatus)
                                .Replace("@(Model.Advert.FreeShipping ? \"Satıcıya Ait\":\"Alıcıya Ait\")", Model.Advert.FreeShipping ? "Satıcıya Ait" : "Alıcıya Ait")
                                .Replace("@(Model.Advert.ShippingPrice)", Model.Advert?.ShippingPrice.ToString())
                                .Replace("@(Model.Advert.Price) @Constants.GetMoney(Model.Advert.MoneyTypeID)", Model.Advert?.Price + " " + Constants.GetMoney(Model.Advert.MoneyTypeID))
                                .Replace("@Model.Amount", Model.Amount.ToString())
                                .Replace("@Model.Advert.Content", Model.Advert?.Content)
                                .Replace("@Model.Advert.NewProductPrice @Constants.GetMoney(Model.Advert.MoneyTypeID)", Model.Advert?.NewProductPrice.ToString() + "  " + Constants.GetMoney(Model.Advert.MoneyTypeID))
                                .Replace("@Model.Advert.WebSite", Model.Advert?.WebSite)
                                .Replace("@(Model.Advert.OriginalBox.Kutu)", Model.Advert.OriginalBox ? "Evet" : "Hayır")
                                .Replace("@(Model.Advert.OriginalBox.Klavuz)", Model.Advert.OriginalBox ? "Var" : "Yok")
                                .Replace("@Model.Advert.ProductDefects", Model.Advert?.ProductDefects)
                                .Replace("@Model.InstallmentNumber", Model.InstallmentNumber.ToString())
                                .Replace("@Model.CreatedDate.ToShortDateString()", Model.CreatedDate.ToShortDateString())
                                .Replace("@Model.CreatedDate.ToString(\"g\")", Model.CreatedDate.ToString("g"));

                        }

                    }
                    ViewBag.htmlContent = pageHtml;
                    return View(Model);
                }
            }
            catch (Exception ex)
            {

                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        IsError = true,
                        Function = "getView",
                        Message = ex.Message,
                        Detail = ex.ToString(),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }
                throw;


            }
        }

        [Route("Arama/{query}")]
        [Route("Search/{query}")]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query))
                return RedirectToAction("Index");

            var lang = GetLang(false);

            using (var advertCategoryService = new AdvertCategoryService())
            using (var blogService = new BlogPostService())
            using (var adService = new AdvertService())
            {
                var ads = adService.Search(query, GetLoginID(), lang);
                var posts = blogService.Search(query);

                var model = new HomePageViewModel()
                {
                    Items = MergeAdsBlogs(ads, posts, lang, false),
                    AdvertCategories = advertCategoryService.GetMasterCategories(GetLang()),
                };

                return View(model);
            }
        }


        [Route("English")]
        [Route("En")]
        public IActionResult English(string otherUrl)
        {
            HttpContext.Session.SetString("lang", Enums.Languages.en.ToString());
         

            if (!string.IsNullOrEmpty(otherUrl))
            {
                return Redirect(otherUrl);
            }
            return RedirectToAction("Index");
        }

        [Route("Turkce")]
        [Route("Tr")]
        public IActionResult Turkce(string otherUrl)
        {
            HttpContext.Session.SetString("lang", Enums.Languages.tr.ToString());         

            if (!string.IsNullOrEmpty(otherUrl))
            {
                return Redirect(otherUrl);
            }
            return RedirectToAction("Index");
        }

        [Route("Bildirim")]
        [Route("Notification")]
        public IActionResult Notification()
        {
            return View();
        }

        [Route("BannerClick")]
        [HttpPost]
        public void BannerClick(int id)
        {
            using (var service = new BannerService())
            {
                if (id > 0)
                    service.IncraseClickCount(id);
            }
        }

        public JsonResult AddNewsletter(string email)
        {
            var lang = GetLang(false);
            var t = new Localization();
            if (string.IsNullOrEmpty(email))
                return Json(new { isSuccess = false, message = t.Get("Geçersiz bir giriş yaptınız.", "Invalid email.", lang) });
            using (var service = new NewsletterService())
            {
                var insert = service.Insert(new NewsletterSubscriber { Email = email, CreatedDate = DateTime.Now });
                if (!insert)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message =
                    t.Get("Kayıt işlemi başarısız oldu. İletişim sayfasından bize ulaşabilirsiniz.",
                    "Registration failed. You can contact us from the contact page.", lang)
                    });
                }
                return Json(new { isSuccess = true, message = t.Get("E-bülten kaydınız oluşturuldu.", "Your newsletter registration has been created.", lang) });
            }
        }

        [Route("404")]
        public IActionResult Error()
        {
            return View();
        }

        public JsonResult GetTCKimlikDogrulama(ulong tcKimlikNo, string ad, string soyad, ushort dogumYili)
        {

            try
            {
                var responseFromServer = HttpTcKimlikDogrula(tcKimlikNo, ad, soyad, dogumYili);
                if (responseFromServer.Contains("<TCKimlikNoDogrulaResult>true</TCKimlikNoDogrulaResult>"))
                    return Json(new { isSuccess = true });
                else
                    return Json(new { isSuccess = false });
            }
            catch (Exception)
            {
                return Json(new { isSuccess = false });
            }
        }
    }
}