using System.Collections.Generic;
using GittiBu.Common;
using GittiBu.Services;
using GittiBu.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GittiBu.Web.Controllers
{
    public class CategoryController : BaseController
    {
        // GET

        [Route("/Kategori/{categorySlug?}")]
        [Route("/Category/{categorySlug?}")]
        public IActionResult Index(string categorySlug)
        {
            using (var textService = new TextService())
            using (var advertService = new AdvertService())
            using (var advertCategoryService = new AdvertCategoryService())
            {
                var lang = GetLang();
                var t = new Localization();

                var masterCategory = advertCategoryService.Get(categorySlug, lang);
                if (masterCategory == null)
                {
                    lang = lang == 1 ? 2 : 1;
                    masterCategory = advertCategoryService.Get(categorySlug, lang);
                    if (masterCategory != null && lang == 1)
                    {
                        SetLang("tr");
                    }
                    else
                    {
                        SetLang("en");
                    }

                }

                if (masterCategory == null)
                {
                    Notification = new UiMessage(NotyType.error,
                        t.Get("Kategori bulunamadı.", "Category not found.", lang));
                    return Redirect("/");
                }

                var masterId = masterCategory?.ID ?? 0;

                var adverts = advertService.GetCategoryPageAdverts(masterCategory.ID, lang, 0, GetLoginID());
                var model = new CategoryViewModel
                {
                    ParentCategory = masterCategory,
                    AdvertCategories = advertCategoryService.GetAll(lang),
                    AdvertsCount = advertCategoryService.GetAdvertsCount(masterCategory),
                    Adverts = Ads2HomePageItems(adverts, lang)
                };

                //kategori sayfasında sayfa içerisinde dil değiştirme işlemi için
                //kategorinin hedef dildeki slug değerlerini çekip translateUrl viewbag'ini doldurur
                // (kategorinin ve üst kategori var ise üst kategorinin)
                var translateLang = (lang == 1) ? 2 : 1;
                var translateUrl = "/" + new Localization().Get("Kategori/", "Category/", translateLang);
                string tMasterSlug = "";
                if (!string.IsNullOrEmpty(categorySlug))
                {
                    if (masterCategory != null)
                        tMasterSlug = textService.GetText(masterCategory.SlugID, translateLang);
                }
                translateUrl += tMasterSlug + (tMasterSlug != "" ? "/" : "");

                ViewBag.TranslateUrl = translateUrl;
                return View(model);
            }
        }

        [HttpPost]
        [Route("Category/Filter")]
        public PartialViewResult Filter(FilterViewModel filterViewModel)
        {
            using (var service = new AdvertService())
            {
                try
                {
                    var lang = GetLang();
                    var list = service.FilterAdverts(filterViewModel.ParentCategoryID,
                    filterViewModel.Content, filterViewModel.Brand, filterViewModel.Model, filterViewModel.AdvertID,
                    filterViewModel.PriceMin, filterViewModel.PriceMax, GetLang(), GetLoginID());
                    return PartialView("~/Views/Shared/AdvertItem_4ColumnList.cshtml", Ads2HomePageItems(list, lang));
                }
                catch (System.Exception ex)
                {
                    return null;
                }
            }
        }



        [HttpPost]
        [Route("Category/GetAdverts")]
        public PartialViewResult GetAdverts(int offset, int categoryId)
        {
            var lang = GetLang();
            using (var service = new AdvertService())
            {
                var list = service.GetCategoryPageAdverts(categoryId, lang, offset, GetLoginID());
                return PartialView("~/Views/Shared/AdvertItem_4ColumnList.cshtml", Ads2HomePageItems(list, lang));
            }
        }
        [HttpPost]
        [Route("Category/GetAdvertsList")]
        public List<HomePageItem> GetAdvertsList(int categoryId, int offset)
        {
            var lang = GetLang();
            using (var service = new AdvertService())
            {
                var list = service.GetCategoryPageAdverts(categoryId, lang, offset, GetLoginID());
                var response = Ads2HomePageItems(list, lang);
                return response;
            }
        }
    }
}