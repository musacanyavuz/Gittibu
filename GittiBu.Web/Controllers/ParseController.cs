using GittiBu.Common;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.Helpers;
using GittiBu.Web.ViewModels.Parse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace GittiBu.Web.Controllers
{
    [Authorize]
    public class ParseController : BaseController
    {
        private readonly IMyCache _cache;
        public ParseController(IMyCache cache)
        {
            _cache = cache;
        }

        public IActionResult Index()
        {
            string minimumPrice = "";
            int xmlStart = 0;
            int xmlStop = 0;

            using (var systemService = new SystemSettingService())
            {
                var systemSettings = systemService.GetSetting(Enums.SystemSettingName.XMLMinimumPrice);
                if (systemSettings != null)
                {
                    minimumPrice = systemSettings.Value;
                }
                systemSettings = systemService.GetSetting(Enums.SystemSettingName.xmlstart);
                if (systemSettings != null)
                {
                    int.TryParse(systemSettings.Value, out xmlStart);
                }
                systemSettings = systemService.GetSetting(Enums.SystemSettingName.xmlstop);
                if (systemSettings != null)
                {
                    int.TryParse(systemSettings.Value, out xmlStop);
                }
            }

            //belirli saatler dışında request gelirse ilanlarıma yönlendirilecek (ui validasyon mevcut (TopMenu.cshtml)).
            bool isXMLHour = IsXMLHour(xmlStart, xmlStop);
            if (isXMLHour)
            {
                using (var textService = new TextService())
                {
                    ViewBag.Baslik = textService.GetText(Enums.Texts.XMLInsertBaslik, GetLang());
                    ViewBag.Icerik = textService.GetText(Enums.Texts.XMLInsertIcerik, GetLang());
                }

                List<ParseMatchModel> parseFieldMatches = new List<ParseMatchModel> {
                    new ParseMatchModel{FieldName="ProductID", IsRequired=true, DataSampleList = new List<string>(){ }},
                    new ParseMatchModel{FieldName="Title", IsRequired=true},
                    new ParseMatchModel{FieldName="Content", IsRequired=true, DataSampleList = new List<string>(){ }},
                    new ParseMatchModel{FieldName="Price", IsRequired=true, DataSampleList = new List<string>(){ }},
                    new ParseMatchModel{FieldName="NewProductPrice", IsRequired=false, DataSampleList = new List<string>(){ }},
                    new ParseMatchModel{FieldName="Category", IsRequired=true, DataSampleList = new List<string>(){}},
                    new ParseMatchModel{FieldName="Photos", IsRequired=true, DataSampleList = new List<string>(){}},
                    new ParseMatchModel{FieldName="StockAmount", IsRequired=true, DataSampleList = new List<string>(){}},
                    new ParseMatchModel{FieldName="ShippingPrice", IsRequired=false, DataSampleList = new List<string>(){}},
                    new ParseMatchModel{FieldName="IsActive", IsRequired=false, DataSampleList = new List<string>(){}}
                };


                List<AdvertCategory> advertCategories;
                using (var categoryService = new AdvertCategoryService())
                {
                    advertCategories = categoryService.GetAllCategories();
                }

                List<CargoArea> cargoArea;
                using (var cargoService = new CargoService())
                {
                    cargoArea = cargoService.GetAll();
                }

                ParseViewModel viewModel = new ParseViewModel
                {
                    ParseFieldMatches = parseFieldMatches,
                    CargoAreas = new List<ComboModel>(),
                    MinimumPrice = minimumPrice,
                    AdvertCategories = advertCategories
                };

                foreach (CargoArea area in cargoArea)
                {
                    viewModel.CargoAreas.Add(new ComboModel { ID = area.ID.ToString(), Value = area.Name });
                }

                return View(viewModel);
            }
            else
            {
                return RedirectToAction("Ilanlarim", "Hesabim");
            }
        }

        public IActionResult List()
        {
            int xmlStart = 0;
            int xmlStop = 0;

            using (var systemService = new SystemSettingService())
            {
                var systemSettings = systemService.GetSetting(Enums.SystemSettingName.xmlstart);
                if (systemSettings != null)
                {
                    int.TryParse(systemSettings.Value, out xmlStart);
                }
                systemSettings = systemService.GetSetting(Enums.SystemSettingName.xmlstop);
                if (systemSettings != null)
                {
                    int.TryParse(systemSettings.Value, out xmlStop);
                }
            }

            //belirli saatler dışında request gelirse ilanlarıma yönlendirilecek (ui validasyon mevcut (TopMenu.cshtml)).
            bool isXMLHour = IsXMLHour(xmlStart, xmlStop);
            if (isXMLHour)
            {
                using (var textService = new TextService())
                {
                    ViewBag.Baslik = textService.GetText(Enums.Texts.XMLListBaslik, GetLang());
                    ViewBag.Icerik = textService.GetText(Enums.Texts.XMLListIcerik, GetLang());
                }

                return View();
            }
            else
            {
                return RedirectToAction("Ilanlarim", "Hesabim");
            }
        }


        [HttpPost]
        public async Task<JsonResult> GetXMLCategoryList(string[] categoryFields)
        {
            try
            {
                List<string> contextChildNames = HttpContext.Session.GetObject<List<string>>("ChildNames");
                string selectedRootName = HttpContext.Session.GetString("SelectedRootName");
                string filePath = HttpContext.Session.GetString("UploadedFilePath");

                Stream formFileStream = await DownloadFile(filePath);
                XmlDocument doc = GetXmlDoc(formFileStream);

                List<Dictionary<string, object>> dictionaryList = parseXMLFile(doc, selectedRootName, contextChildNames);

                List<string> CategoryNames = new List<string>();
                foreach (var dictionary in dictionaryList)
                {
                    for (int i = 0; i < categoryFields.Length; i++)
                    {
                        string category = dictionary[categoryFields[i]]?.ToString() ?? String.Empty;
                        if (!string.IsNullOrEmpty(category))
                        {
                            if (!CategoryNames.Contains(category))
                            {
                                CategoryNames.Add(category);
                            }
                        }
                    }
                }

                HttpContext.Session.SetObject("XMLCategoryNames", CategoryNames);
                return Json(CategoryNames);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpGet]
        public JsonResult GetFileList()
        {
            try
            {
                using (var service = new ParseService())
                {
                    int userId = GetLoginID();
                    var userFiles = service.GetUserFilesById(userId).OrderByDescending(s => s.ID);
                    return Json(userFiles);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public JsonResult Delete(Parse parse)
        {
            using (var parseService = new ParseService())
            {
                var userFile = parseService.DeleteParseFileWithAdverts(parse.ID);
                if (userFile != null)
                {
                    return Json(userFile);
                }
            }

            return null;
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Parse model)
        {
            try
            {
                using (var service = new ParseService())
                {
                    var userFile = service.Get(model.ID);
                    if (userFile != null)
                    {
                        ParseModel parseModel = new ParseModel
                        {
                            PriceDiscount = model.Discount,
                            NewPriceDiscount = model.NewPriceDiscount,
                            FileName = model.UserFileName,
                            FilePath = model.File,
                            Supplier = model.Supplier,
                            PriceFilter = model.PriceFilter,
                            StockFilter = model.StockFilter,
                            MaxInstallment = model.MaxInstallment,
                            FileStream = await DownloadFile(userFile.File),
                            CargoAreaID = userFile.CargoAreaID.ToString(),
                            FreeShipping = model.FreeShipping,
                            RootName = userFile.RootName,
                            ChildNames = userFile.ChildNames,
                            UserID = userFile.UserID,
                            XMLCategoryMatches = model.CategoryMatches.Split(",").Select(int.Parse).ToArray()
                        };

                        var fieldList = model.Fields?.Split("*,*").ToList();
                        parseModel.Fields = fieldList;
                        string parseKey = ".._..";
                        foreach (var propField in fieldList)
                        {
                            string[] propAndField = propField.Split("---");
                            string prop = propAndField[0]?.Trim();
                            string field = propAndField[1]?.Trim();

                            switch (prop)
                            {
                                case "ProductID":
                                    parseModel.ProductID = field;
                                    break;
                                case "Title":
                                    if (field.Contains(parseKey))
                                    {
                                        parseModel.Title = field.Split(parseKey);
                                    }
                                    else
                                    {
                                        parseModel.Title = new string[] { field };
                                    }
                                    break;
                                case "Content":
                                    if (field.Contains(parseKey))
                                    {
                                        parseModel.Content = field.Split(parseKey);
                                    }
                                    else
                                    {
                                        parseModel.Content = new string[] { field };
                                    }
                                    break;
                                case "Price":
                                    parseModel.Price = field;
                                    break;
                                case "NewProductPrice":
                                    parseModel.NewProductPrice = field;
                                    break;
                                case "Category":
                                    parseModel.Category = field;
                                    break;
                                case "Photos":
                                    if (field.Contains(parseKey))
                                    {
                                        parseModel.Photos = field.Split(parseKey);
                                    }
                                    else
                                    {
                                        parseModel.Photos = new string[] { field };
                                    }
                                    break;
                                case "StockAmount":
                                    parseModel.StockAmount = field;
                                    break;
                                case "ShippingPrice":
                                    parseModel.ShippingPrice = field;
                                    break;
                                case "IsActive":
                                    parseModel.IsActive = field;
                                    break;
                            }
                        }

                        await Insert(parseModel);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(null);
        }

       
        public void DeleteOtherPArses(int userid)
        {
            using (var newService = new ParseService())
            {
                var listOfUserParses = newService.GetUserFilesById(userid).OrderByDescending(x => x.ID);                
                var userFileName = listOfUserParses.First().UserFileName;
                
                int c = 0;
                foreach (var item in listOfUserParses.Where(x=>x.UserFileName.Contains(userFileName)))
                {
                    c++;
                    if (c == 1)
                        continue;
                    item.IsDeleted = true;
                    newService.Update(item);
                   
                }

            }
           
        }

        [HttpPost]
        //[RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue)]
        //[RequestSizeLimit(100000000)]
        public async Task<ActionResult> Insert(ParseModel model)
        {
            try
            {              

                int userId = 0;
                string XMLFileName = "";
                string filePath = "";
                if (model.FileStream == null)
                {
                    XMLFileName = HttpContext.Session.GetString("XMLFileName");
                    filePath = HttpContext.Session.GetString("UploadedFilePath");
                    userId = GetLoginID();
                }


                Stream formFileStream;
                if (model.file != null) //file ile insert
                {
                    formFileStream = model.file.OpenReadStream();
                }
                else if (model.fileLink != null) //link ile insert
                {
                    formFileStream = await DownloadFile(filePath);
                }
                else if (model.FileStream != null) //update
                {
                    XMLFileName = model.FileName;
                    filePath = model.FilePath;
                    formFileStream = model.FileStream;
                    userId = model.UserID;
                    int xd = GetLoginID();
                }
                else
                {
                    return null;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(formFileStream);

                List<string> childNames = new List<string>();
                List<Dictionary<string, object>> advertsFromXML = new List<Dictionary<string, object>>();

                #region File Process
                List<string> xmlElementsFromFile = new List<string>();
                if (model.FileStream == null)
                {
                    xmlElementsFromFile = HttpContext.Session.GetObject<List<string>>("ChildNames");
                }
                if ((model.ChildNames?.Length ?? -1) > 0 && model.FileStream != null)
                {
                    xmlElementsFromFile = model.ChildNames.Split("_._").ToList();
                }
                if (xmlElementsFromFile != null)
                {
                    advertsFromXML = parseXMLFile(doc, model.RootName, xmlElementsFromFile);
                }
                else
                {
                    return RedirectToAction("Ilanlarim", "Hesabim");
                }

                #endregion
                #region Advert Category Contents
                //Kategori tablosundaki NameID'ler, Content tablosundaki Key'ler ile eşleştirilip listeye alınır
                List<Content> contents = new List<Content>();
                List<AdvertCategory> categories = new List<AdvertCategory>();
                using (var adCatService = new AdvertCategoryService())
                using (var parseService = new ParseService())
                {
                    categories = adCatService.GetAll();
                    //contents = parseService.GetCategoryContents(categories);
                }
                #endregion
                #region Current User Adverts
                //Kullanıcının ilanlarını getirir
                List<Advert> userAdverts;
                using (var services = new AdvertService())
                {
                    userAdverts = services.GetUserAdverts(userId);
                }
                #endregion
                #region All Payment Requests for Sell AdvertIds
                //Kullanıcının ilanlarını getirir
                List<PaymentRequest> paymentRequests;
                using (var services = new PaymentRequestService())
                {
                    paymentRequests = services.GetAll();
                }
                #endregion
                #region Add File Datas To Advert List (With Checking Filters)
                List<string> deletedAdvertList = new List<string>();
                //XML dosyasından alınan veriler ve UI'dan alınan filtreler ile, db'ye insert ya da update edilecek alanlar Advert Listesine eklenir.
                List<Advert> advertList = new List<Advert>();
                using (var parseService = new ParseService())
                {
                    foreach (var advertObjInXML in advertsFromXML)
                    {
                        string installment = "1";
                        int categoryID = 0;
                        try
                        {
                            #region Get Max All Installment According To Category Similarity Rate
                            //XML dosyasından alınan Category içerisindeki kelimeler tek tek ele alınarak, 
                            //DB'deki Category verileriyle eşleştirilir ve en yüksek benzerliği olan category id seçilir
                            //Kategori tipine göre maksimum kaç taksit olacağı belirlenir.
                            try
                            {


                                //if (HttpContext.Session.GetObject<List<string>>("XMLCategoryNames") == null && model.Fields != null && model.Fields.Count > 0)
                                //{
                                //    List<string> xmlCatNames = new List<string>();
                                //    foreach (var catNameinXml in model.Fields)
                                //    {
                                //        xmlCatNames.Add(catNameinXml.Split("---")[1]);
                                //    }
                                //  var x=  GetXMLCategoryList(xmlCatNames.ToArray());
                                //}
                                //indekslere göre eşleştirme yapılacak
                                List<string> xmlCategoryNames = HttpContext.Session.GetObject<List<string>>("XMLCategoryNames");
                                List<int> matchedGittibuCategories = model.XMLCategoryMatches.ToList();

                                string categoryValue = advertObjInXML[model.Category]?.ToString();
                                int categoryIndex = xmlCategoryNames.IndexOf(categoryValue);

                                int gittibuCategoryID = matchedGittibuCategories[categoryIndex];

                                AdvertCategory advertCategories = categories.FirstOrDefault(s => s.ID == gittibuCategoryID);

                                int categoryMaxInst = advertCategories?.MaxInstallment ?? 1;
                                int maxInst = (categoryMaxInst > model.MaxInstallment) ? (model.MaxInstallment) : categoryMaxInst;
                                installment = AllInstallments(maxInst);
                                categoryID = advertCategories.ID;
                            }
                            catch (Exception e)
                            {
                                using (var service = new BaseService())
                                {
                                    service.Log(new Log
                                    {
                                        Function = "AdvertService.Insert(CategoryMatch)",
                                        CreatedDate = DateTime.Now,
                                        Message = e.Message,
                                        Detail = e.ToString(),
                                        IsError = true
                                    });
                                }
                            }
                            #endregion
                            #region Filter Configuration
                            double price = Convert.ToDouble(advertObjInXML[model.Price].ToString().Replace('.', ','));
                            double addPrice = (model.PriceDiscount * price) / 100;
                            price = Math.Round((price + addPrice), MidpointRounding.AwayFromZero);

                            double stockAmount = Convert.ToDouble(advertObjInXML[model.StockAmount].ToString().Replace('.', ','));

                            bool isActive = false;
                            if (!string.IsNullOrEmpty(model.IsActive))
                            {
                                object isActiveObj = advertObjInXML[model.IsActive];
                                if (isActiveObj != null)
                                {
                                    if (bool.TryParse(isActiveObj.ToString(), out bool result))
                                    {
                                        isActive = result;
                                    }
                                    else if (int.TryParse(isActiveObj.ToString(), out int resultInt))
                                    {
                                        isActive = Convert.ToBoolean(resultInt);
                                    }
                                }
                                else
                                {
                                    isActive = true;
                                }

                            }
                            else //isActive zorunlu alan değil, eşleştirilmediyse true olacak.
                            {
                                isActive = true;
                            }

                            #endregion
                            #region Filtering

                            string productId = advertObjInXML[model.ProductID]?.ToString();
                            int advertId = userAdverts.FirstOrDefault(s => s.XMLProductID == (productId ?? ".."))?.ID ?? -1;

                            //filtrelere uygun değilse, ürün aktif değilse yükleme.
                            if ((model.PriceFilter >= price || model.StockFilter > stockAmount) || (!isActive))
                            {
                                if (model.FileStream != null && advertId != -1) //update işlemi esnasında
                                {
                                    bool isOk = parseService.DeleteOrUpdateAdvert(paymentRequests, advertId);
                                    if (isOk)
                                    {
                                        deletedAdvertList.Add(advertId.ToString());
                                    }
                                }
                                throw new Exception();
                            }
                            else
                            {

                            }
                            #endregion

                            Advert advertEntity = null;
                            if (advertId != -1)
                            {
                                using (var service = new AdvertService())
                                {
                                    //get advert
                                    advertEntity = service.GetAdvert(advertId);
                                }
                            }
                            if (advertEntity == null) { advertEntity = new Advert(); }
                            advertEntity.AvailableInstallments = installment;
                            advertEntity.CategoryID = categoryID;
                            advertEntity.XMLProductID = productId;
                            advertEntity.CargoAreaID = Convert.ToInt32(model.CargoAreaID);
                            advertEntity.Price = price;
                            advertEntity.StockAmount = stockAmount;
                            advertEntity.ProductStatus = "Yeni"; //model.ProductStatus;
                            advertEntity.MoneyTypeID = (int)Enums.MoneyType.tl;
                            advertEntity.UserID = userId;
                            advertEntity.UseSecurePayment = true;
                            advertEntity.IpAddress = GetIpAddress();
                            advertEntity.IsActive = true; //onaysız
                            advertEntity.IsDeleted = false; //onaysız
                            advertEntity.ProductDefects = "Yeni <span style='padding-left: 5em !important'>" + model.Supplier + "</span>";
                            advertEntity.FreeShipping = model.FreeShipping;

                            #region Price Arrangement
                            if (!string.IsNullOrEmpty(model.ShippingPrice))
                            {
                                string shippingPrice = advertObjInXML[model.ShippingPrice]?.ToString()?.Replace('.', ',');
                                if (double.TryParse(shippingPrice, out double shpPrice))
                                {
                                    advertEntity.ShippingPrice = Math.Round(shpPrice, MidpointRounding.AwayFromZero);
                                    //advert.FreeShipping = advert.ShippingPrice > 0.0 ? false : true;
                                }
                            }

                            if (!string.IsNullOrEmpty(model.NewProductPrice))
                            {
                                string newProductPrice = advertObjInXML[model.NewProductPrice]?.ToString()?.Replace('.', ',');
                                if (double.TryParse(newProductPrice, out double newPrice))
                                {
                                    double addNewPrice = (model.NewPriceDiscount * newPrice) / 100;
                                    newPrice = Math.Round((newPrice + addNewPrice), MidpointRounding.AwayFromZero);
                                    advertEntity.NewProductPrice = newPrice;
                                }
                            }
                            #endregion
                            #region Photo Link Arrangement
                            advertEntity.CoverPhoto = "";
                            for (int i = 0; i < model.Photos.Length; i++)
                            {
                                advertEntity.CoverPhoto += advertObjInXML[model.Photos[i]];
                            }
                            #endregion
                            #region Content Arrangement
                            string contentTemp = "";
                            for (int i = 0; i < model.Content.Length; i++)
                            {
                                string br = "";
                                if (i != 0)
                                {
                                    br = "<br/>";
                                }
                                string data = advertObjInXML[model.Content[i]]?.ToString() ?? String.Empty;
                                data = data?.Replace("&amp;", "&")?.Replace("&quot;", "\"")?.Replace("&#39;", "'")?.Replace("&lt;", "<")?.Replace("&gt;", ">");
                                contentTemp += br + (Regex.Replace(data, @"<[^>]+>|&nbsp;", "")?.Trim() ?? String.Empty);
                            }
                            //boyutu 5000den büyükse ilk 14950yi al.
                            if (contentTemp.Length >= 5000)
                            {
                                contentTemp = contentTemp.Substring(0, 4950);
                            }
                            advertEntity.Content = contentTemp;
                            #endregion
                            #region Title Arrangement
                            string titleTemp = "";
                            for (int i = 0; i < model.Title.Length; i++)
                            {
                                var data = advertObjInXML[model.Title[i]]?.ToString() ?? String.Empty;
                                titleTemp += data + (i == (model.Title.Length - 1) ? String.Empty : " ");
                            }
                            //boyutu 100den büyükse ilk 100ü al.
                            if (titleTemp.Length >= 500)
                            {
                                titleTemp = titleTemp.Substring(0, 490);
                            }
                            titleTemp = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(titleTemp.ToLower());
                            //titleTemp = titleTemp.Modify();
                            titleTemp = titleTemp?.Replace("&", " ");
                            advertEntity.Title = titleTemp;
                            #endregion

                            DateTime now = DateTime.Now;
                            //eğer update edilecekse, oluşturduğumuz nesneye id'sini atıyoruz.
                            if (advertId != -1)
                            {
                                advertEntity.ID = advertId;
                                advertEntity.LastUpdateDate = now;
                            }
                            else
                            {
                                advertEntity.CreatedDate = now;
                                advertEntity.PublishDate = now;
                            }

                            advertList.Add(advertEntity);
                        }
                        catch (Exception e)
                        {
                            //using (var service = new BaseService())
                            //{
                            //    service.Log(new Log
                            //    {
                            //        Function = "AdvertService.Insert(Before Insert/Update)",
                            //        CreatedDate = DateTime.Now,
                            //        Message = e.Message,
                            //        Detail = e.InnerException?.Message,
                            //        IsError = true,
                            //        Params = new string[] { }
                            //    });
                            //}
                        }
                    }
                }
                #endregion
                string productIds = "";
                int increaseCount = 50;
                bool isFirstUpdate = true;
                int insertCounter = 0;
                foreach (var advert in advertList)
                {
                    List<string> photoIdList = new List<string>();
                    using (var advertService = new AdvertService())
                    {
                        #region Get Photo Links From XML File 
                        //Http ile başlayıp, jpg ile biten linkleri list'e atma kodu
                        List<string> urlList = new List<string>();
                        string photoUrlinXML = advert.CoverPhoto;
                        if (photoUrlinXML.IndexOf("http") > -1)
                        {
                            string tempUrl = photoUrlinXML;
                            try
                            {
                                List<string> photoUrlList = photoUrlinXML.Split("http")?.ToList();
                                if (photoUrlList != null)
                                {
                                    foreach (string url in photoUrlList)
                                    {
                                        if (!string.IsNullOrEmpty(url))
                                        {
                                            string tempLink = url.Trim();
                                            int jpgIndex = tempLink.IndexOf(".jpg");
                                            int jpegIndex = tempLink.IndexOf(".jpeg");
                                            int pngIndex = tempLink.IndexOf(".png");

                                            if (jpgIndex > -1)
                                            {
                                                tempLink = tempLink.Substring(0, (jpgIndex + 4));
                                            }
                                            else if (jpegIndex > -1)
                                            {
                                                tempLink = tempLink.Substring(0, (jpegIndex + 5));
                                            }
                                            else if (pngIndex > -1)
                                            {
                                                tempLink = tempLink.Substring(0, (pngIndex + 4));
                                            }

                                            if (!string.IsNullOrEmpty(tempLink))
                                            {
                                                if (tempLink.Length != 1)
                                                {
                                                    urlList.Add("http" + tempLink);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                urlList = new List<string>();
                                using (var service = new BaseService())
                                {
                                    service.Log(new Log
                                    {
                                        Function = "ParseController.Insert(PhotoLinkParse)",
                                        CreatedDate = DateTime.Now,
                                        Message = e.Message,
                                        Detail = e.InnerException?.Message,
                                        IsError = true
                                    });
                                }
                            }
                        }

                        try
                        {
                            if (advert.CoverPhoto?.Length > 499)
                            {
                                advert.CoverPhoto = advert.CoverPhoto.Substring(0, 499);
                            }
                        }
                        catch (Exception e)
                        {
                        }

                        #endregion
                        #region Upload Photo To Server



                        for (int i = 0; i < urlList.Count; i++)
                        {
                            try
                            {
                                string photoNameInXML = Path.GetFileName(urlList[i]);
                                Stream photoStream = GetStreamFromUrl(urlList[i], @"wwwroot\Upload\Sales\" + photoNameInXML); //new FileStream(filePath, FileMode.Open, FileAccess.Read);


                                ParsePhotoModel result = UploadPhoto(0, photoStream);
                                if (result != null)
                                {
                                    photoIdList.Add(result.id.ToString());
                                }
                                photoStream.Close();
                                string photoPath = @"wwwroot\Upload\Sales\" + photoNameInXML;
                                if (System.IO.File.Exists(photoPath))
                                {
                                    System.IO.File.Delete(photoPath);
                                }
                            }
                            catch (Exception e)
                            {
                                using (var service = new BaseService())
                                {
                                    service.Log(new Log
                                    {
                                        Function = "ParseController.Insert(Photo)",
                                        CreatedDate = DateTime.Now,
                                        Message = e.Message,
                                        Detail = e.ToString(),
                                        IsError = true
                                    });
                                }
                            }
                        }



                        #endregion
                        #region Insert Or Update Advert Object
                        bool isOk = false;
                        bool isExistAdvert = false;
                        if (advert.ID != 0)
                        {
                            isOk = advertService.Update(advert);
                            isExistAdvert = true;
                        }
                        else
                        {
                            isOk = advertService.Insert(advert);
                        }
                        #endregion
                        #region Add Info To Db If Insert/Update Successed
                        if (isOk)
                        {
                            insertCounter++;
                            productIds += advert.ID + ",,,";
                            #region Add Photos Info To Advert
                            if (photoIdList != null)
                            {
                                string SelectedPhotos = string.Join(",,,", photoIdList);

                                //burada o advert'a ait olan photolar delete edilecek.
                                // deleteAdvertPhotos(int advertId){service.delete(where advertId == advert.ID)}

                                if (isExistAdvert)
                                {
                                    var photos = advertService.GetPhotos(advert.ID);
                                   
                                    foreach (var photo in photos)
                                    {
                                        using (var srv = new AdvertPhotoService())
                                        {
                                            srv.DeleteAdvertOnlyPhysicalPhotos(photo);// Var olan ürün bir daha xmlden yüklendiği için eski ürün resimleri siliniyor.

                                        }
                                        advertService.DeletePhoto(photo);
                                    }

                                  
                                }

                                var imageIds = UpdateNewPhotos(advert.ID, SelectedPhotos);
                                if (imageIds?.Length > 0)
                                {
                                    var mainPhoto = advertService.GetPhoto(imageIds[0]);
                                    if (mainPhoto != null)
                                    {
                                        advert.CoverPhoto = mainPhoto.Source;
                                        advert.Thumbnail = mainPhoto.Thumbnail;
                                        advertService.Update(advert);
                                    }
                                }
                            }
                            #endregion
                        }
                        if ((insertCounter % increaseCount == 0) && (insertCounter != 0))
                        {
                            await AddParseFile(model, insertCounter, filePath, productIds, userId, XMLFileName, xmlElementsFromFile, deletedAdvertList, increaseCount, false, isFirstUpdate);
                            isFirstUpdate = false;
                        }


                        #endregion
                    }
                }

                //Add User Parse File Info To Parse File Table
                await AddParseFile(model, insertCounter, filePath, productIds, userId, XMLFileName, xmlElementsFromFile, deletedAdvertList, increaseCount, true, isFirstUpdate);
                DeleteOtherPArses(userId);

            }
            catch (Exception e)
            {
                using (var service = new BaseService())
                {
                    service.Log(new Log
                    {
                        Function = "AdvertService.Insert",
                        CreatedDate = DateTime.Now,
                        Message = e.Message,
                        Detail = e.InnerException?.Message,
                        IsError = true
                    });
                }
            }

            return RedirectToAction("Ilanlarim", "Hesabim");
        }

        [HttpPost]
        public async Task<JsonResult> ReadRootName()
        {
            try
            {
                _cache.Clear();

                string path = HttpContext.Session.GetString("UploadedFilePath");
                IFormFile file = null;
                if (Request.Form.Files?.Count > 0)
                {
                    file = Request.Form.Files[0];
                }

                Stream fileStream = null;

                if (file != null) //dosya indirilecek.
                {
                    fileStream = file.OpenReadStream();
                    string uploadedFilePath = await UploadFile(fileStream);
                    HttpContext.Session.SetString("UploadedFilePath", uploadedFilePath);
                    HttpContext.Session.SetString("XMLFileName", file.FileName);
                }
                else if (path != null) //link
                {
                    //linkin sadece baş kısmı
                    var linkFileName = HttpContext.Session.GetString("XMLFileLink");
                    int lastIndex = linkFileName.LastIndexOf("/");
                    string linkSite = lastIndex > -1 ? linkFileName.Substring(0, lastIndex) : "";

                    //linkin son kısmı
                    //string linkFileName = HttpContext.Session.GetString("XMLFileLink")?.Split("/")?.Last();
                    fileStream = await DownloadFile(path);
                    string fileName = new FileInfo(path).Name;
                    HttpContext.Session.SetString("XMLFileName", linkSite);
                }

                if (fileStream != null)
                {
                    fileStream.Position = 0;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(fileStream);

                    string tempNodeName = "";

                    List<string> rootNames = new List<string>();
                    tempNodeName = doc.DocumentElement.FirstChild?.Name ?? "";
                    if (!string.IsNullOrEmpty(tempNodeName))
                    {
                        rootNames.Add(tempNodeName);
                    }

                    foreach (XmlNode node in doc.DocumentElement)
                    {
                        if (rootNames.Any(s => s != node.Name))
                        {
                            rootNames.Add(node.Name);
                        }
                    }

                    return Json(rootNames);
                    //  return Json(rootNamesTemp);
                }
                else
                {
                    return Json(null);
                }
            }
            catch (Exception e)
            {
                return Json(null);
            }
        }
        List<string> rootNamesTemp = new List<string>();

        /// <summary>
        /// ilhan:
        /// </summary>
        /// <param name="_XmlNodeList"></param>
        private void LoadRootNames(XmlNodeList _XmlNodeList)
        {

            foreach (XmlNode item in _XmlNodeList)
            {
                if (item.ChildNodes.OfType<XmlElement>().Any())
                {
                    if (rootNamesTemp.IndexOf(item.Name) < 0)
                    {
                        rootNamesTemp.Add(item.Name);
                    }


                }
                LoadRootNames(item.ChildNodes);
            }
        }
        /// <summary>
        /// Root comboboxtan seçim yapılınca.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ReadChildName()
        {
            string path = HttpContext.Session.GetString("UploadedFilePath");
            string fileName = new FileInfo(path).Name;
            Stream fileStream = await DownloadFile(path);
            var selectedRootName = Request.Form["rootName"];
            XmlDocument doc = new XmlDocument();
            doc.Load(fileStream);
            if (doc != null)
            {
                HttpContext.Session.SetString("SelectedRootName", selectedRootName);
                List<List<string>> childNameList = GetFileChildNames(doc, selectedRootName);
                List<string> childNames = childNameList[0];
                HttpContext.Session.SetObject("ChildNames", childNames);

                return Json(childNameList);
            }
            else
            {
                return RedirectToAction("Ilanlarim", "Hesabim");
            }
        }

        /// <summary>
        ///  URL yapıştırılıp yükle ye tıklanınca .
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> UploadFileFromLink(string link)
        {
            try
            {
                string filePath = CreateXMLFileName(); //token ile bir path oluşturuldu (upload/parses/filename.)
                var filestream = GetStreamFromUrl(link, filePath);
                if (filestream != null)
                {
                    string uploadedFilePath = await UploadFile(filestream);
                    HttpContext.Session.SetString("UploadedFilePath", uploadedFilePath);
                    HttpContext.Session.SetString("XMLFileLink", link);

                    JsonResult rootNames = await ReadRootName();
                    return rootNames;
                }
            }
            catch (Exception e)
            {
                using (var service = new BaseService())
                {
                    service.Log(new Log
                    {
                        Function = "ParseController.UploadFileFromLink",
                        CreatedDate = DateTime.Now,
                        Message = e.Message,
                        Detail = e.ToString(),
                        IsError = true
                    });
                }
            }
            return Json(null);
        }

        public List<List<string>> GetFileChildNames(XmlDocument doc, string selectedRootName)
        {
            List<List<string>> nameValueList = new List<List<string>>();
            List<string> childNames = new List<string>();
            List<string> childValues = new List<string>();

            foreach (XmlNode node in doc.DocumentElement)
            {
                if (node.Name == selectedRootName)
                {
                    void loopFunc()
                    {
                        //outerLoop
                        for (int i = 0; i < node.ChildNodes.Count; i++)
                        {
                            XmlNode currentNode = node.ChildNodes[i];
                            var innerChildNodes = currentNode.ChildNodes;
                            int innerChildNodeCount = innerChildNodes.Count;

                            //aslında buraya bütün name'ler aynı ve her bir elemanın altında 6dan fazla eleman varsa şartı gerekiyor.
                            //şu durumda, else'e girip, if'e girdiği durumlarda childname'e eklenecek. (atıyorum ürünün özellik kısmı)
                            if (innerChildNodeCount > 10)
                            {
                                //innerLoop
                                for (int k = 0; k < innerChildNodes.Count; k++)
                                {
                                    string tagName = innerChildNodes[k].Name;
                                    string tagValue = innerChildNodes[k]?.InnerText?.Trim();
                                    if (!string.IsNullOrEmpty(tagName))
                                    {
                                        childNames.Add(tagName);
                                        childValues.Add(tagValue);
                                    }
                                }

                            }
                            else
                            {
                                string tagName = currentNode.Name;
                                string tagValue = currentNode?.InnerText?.Trim();
                                if (!string.IsNullOrEmpty(tagName))
                                {
                                    childNames.Add(tagName);
                                    childValues.Add(tagValue);
                                }
                            }
                        }
                    }
                    //outerloop break;
                    loopFunc();
                    //}
                    break;
                }
            }
            nameValueList.Add(childNames);
            nameValueList.Add(childValues);
            return nameValueList;
        }


        /// <summary>
        /// Resmi ve thumbnail /Upload/Sales/ dizinine indiriyor. Ve AdvertPhoto tablosuna insert.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fileimage"></param>
        /// <returns></returns>
        public ParsePhotoModel UploadPhoto(int id, Stream fileimage)
        {
            //Server içerisine streamdeki fotoğrafı indirip, AdvertPhotos tablosuna kayıt atan metod.
            using (var fileService = new FileService())
            using (var imageService = new ImageService())
            using (var advertService = new AdvertService())
            {
                string path = "";

                var token = Common.Encryptor.GenerateToken();
                try
                {
                    var directory = "/Upload/Sales/";
                    if ((((fileimage.Length / 8) / 1024) / 1024) < 10) //10mb
                    {

                        path = UploadPhoto(fileimage, "/Upload/Sales/", token + "_" + 1 + ".jpg");

                        var thumbName = token + "_" + 1 + "_thumb" + ".jpg";
                        imageService.CreateThumbnail(GetLoginID(), path, directory, thumbName);

                        var advertPhoto = new AdvertPhoto
                        {
                            AdvertID = 0,
                            Source = path,
                            Thumbnail = directory + thumbName,
                            CreatedDate = DateTime.Now
                        };

                        bool isInserted = advertService.InsertAdvertPhoto(advertPhoto);
                        if (isInserted)
                        {
                            return new ParsePhotoModel
                            {
                                isSuccess = true,
                                source = path,
                                thumbnail = directory + thumbName,
                                id = advertPhoto.ID
                            };
                        }

                    }
                }
                catch (Exception e)
                {
                }
                return null;
            }
        }

        public string UploadPhoto(Stream fileStream, string path, string fileName, bool jpegToJpg = true)
        {
            try
            {
                if (fileName.ToLower().Contains(".jpeg") && jpegToJpg)
                {
                    fileName = fileName.ToLower().Replace(".jpeg", ".jpg");
                }
                string imageUrl;
                var pth = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path, fileName);

                using (var newImage = Image.Load(fileStream))
                {
                    WatermarkService.WaterMarkNew(newImage);
                    newImage.Save(pth);
                }

                imageUrl = path + fileName;
                return imageUrl;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public async Task<string> UploadFile(Stream filestream)
        {
            string filePath = "";
            try
            {
                if (filestream.Length > 0)
                {
                    filestream.Position = 0;
                    filePath = CreateXMLFileName();
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await filestream.CopyToAsync(fileStream);
                    }

                }
            }
            catch (Exception ex)
            {
                using (var service = new BaseService())
                {
                    service.Log(new Log
                    {
                        Function = "ParseController.UploadFile",
                        CreatedDate = DateTime.Now,
                        Message = ex.Message,
                        Detail = ex.ToString(),
                        IsError = true
                    });
                }
                return null;
            }
            return filePath;
        }

        string AllInstallments(int max = 1)
        {
            var ins = new int[] { 1, 2, 3, 6, 9, 12 };
            var result = "";
            List<int> maxIns = ins.Where(i => i <= max).ToList();

            for (int i = 0; i < maxIns.Count; i++)
            {
                string subfix = ",";
                if ((maxIns.Count - 1) == i)
                {
                    subfix = "";
                }

                result += maxIns[i] + subfix;
            }

            return result;
        }

        int[] UpdateNewPhotos(int advertId, string selectedPhotos)
        {
            if (!string.IsNullOrEmpty(selectedPhotos))
            {
                var _ids = selectedPhotos.Split(',');
                var ids = new List<int>();
                foreach (var id in _ids)
                {
                    if (int.TryParse(id, out int intId))
                        ids.Add(intId);
                }

                if (ids.Count > 0)
                {
                    using (var advertService = new AdvertService())
                    {
                        var update = advertService.UpdateAdvertPhotos(advertId, ids.ToArray());
                        if (update)
                            return ids.ToArray();
                    }
                }
            }
            return null;
        }

        public async Task<MemoryStream> DownloadFile(string path)
        {
            try
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return memory;
            }
            catch (Exception e)
            {
                using (var service = new BaseService())
                {
                    service.Log(new Log
                    {
                        Function = "ParseController.DownloadFile",
                        CreatedDate = DateTime.Now,
                        Message = e.Message,
                        Detail = e.ToString(),
                        IsError = true
                    });
                }
                return null;
            }
        }

        public XmlDocument GetXmlDoc(Stream stream)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(stream);
                return doc;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Stream GetStreamFromUrl(string uri, string filePath)
        {
            // HttpClientHandler clientHandler = new HttpClientHandler();
            //clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };


            using (WebClient client2 = new WebClient())
            {
                // Add TLS 1.2

                client2.DownloadFile(new Uri(uri), filePath);
            }

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public string CreateXMLFileName()
        {
            var token = Common.Encryptor.GenerateToken();
            var directory = "\\Upload\\Parses\\";
            string fileName = token + "_" + GetLoginID();
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + directory, fileName + ".xml");
        }

        public bool IsXMLHour(int xmlStart, int xmlStop)
        {
            if (xmlStart < 0 || xmlStart > 24 || xmlStop < 0 || xmlStop > 24)
            {
                return false;
            }

            DateTime now = DateTime.Now;
            DateTime dateStart = new DateTime(now.Year, now.Month, now.Day, xmlStart, 0, 0);
            DateTime dateStop = (xmlStop == 24 || xmlStop == 0) ? new DateTime(now.Year, now.Month, (now.Day + 1), 0, 0, 0) : new DateTime(now.Year, now.Month, now.Day, xmlStop, 0, 0);
            if (xmlStart > xmlStop)
            {
                if ((now.Hour < xmlStart && now.Hour < xmlStop)) //after hour 00.00 a.m.
                {
                    dateStart = dateStart.AddDays(-1);
                }
                else
                {
                    dateStop = dateStop.AddDays(1); //before hour 00.00 a.m.
                }
            }
            else if (xmlStart == xmlStop)
            {
                return true;
            }

            if (!(now > dateStart && now < dateStop))
            {
                return false;
            }

            return true;
        }

        private async Task AddParseFile(ParseModel model, int insertCounter, string filePath, string productIds, int userId, string XMLFileName, List<string> contextChildNames, List<string> deletedAdvertList, int increaseCount, bool isLastUpdate, bool isFirstUpdate)
        {
           
            #region Add User Parse File Info To Parse File Table
            if (insertCounter > 0 || model.FileStream != null)
            {
                if (filePath != null)
                {
                    try
                    {
                        string seperator = ",,,";
                        int seperatorLength = seperator.Length;
                        productIds = productIds.Length > seperatorLength ? productIds.Substring(0, productIds.Length - seperatorLength) : "";

                        using (var parseService = new ParseService())
                        {
                            if (model.FileStream == null)//file create ediliyorsa
                            {
                                string fileName = Path.GetFileName(filePath);
                                var parseFile = parseService.GetUserFilesByFileName(userId, fileName);
                                if (parseFile != null && fileName != null)
                                {
                                    //update
                                    string productIdLast = string.IsNullOrEmpty(parseFile.ProductIDs) ? productIds : parseFile.ProductIDs + ",,," + productIds;
                                    productIdLast = string.Join(",,,", productIdLast.Split(",,,").ToList().Distinct());
                                    parseFile.ProductIDs = productIdLast;
                                    parseFile.AdvertCount = isLastUpdate ? (parseFile.AdvertCount + (insertCounter % increaseCount)) : (parseFile.AdvertCount + increaseCount);
                                    parseFile.IsDeleted = false;
                                    parseFile.CategoryMatches = String.Join(",", model.XMLCategoryMatches);
                                    bool isUpdated = parseService.Update(parseFile);
                                    if (!isUpdated)
                                    {
                                        await Task.Delay(1000);
                                        using (var newService = new ParseService())
                                        {
                                            isUpdated = newService.Update(parseFile);
                                        }
                                        if (!isUpdated)
                                        {
                                            await Task.Delay(1000);
                                            parseService.Update(parseFile);
                                        }
                                    }
                                }
                                else
                                {
                                    //insert
                                    string matchedFields = $"ProductID---{model.ProductID}*,* Title---{string.Join(".._..", model.Title)}*,* Content---{string.Join(".._..", model.Content)}*,* Price---{model.Price}*,* NewProductPrice---{model.NewProductPrice}*,* Category---{string.Join(".._..", model.Category)}*,* Photos---{string.Join(".._..", model.Photos)}*,* StockAmount---{model.StockAmount}*,* ShippingPrice---{model.ShippingPrice}*,* IsActive---{model.IsActive}";
                                    Parse parse = new Parse
                                    {
                                        UserFileName = XMLFileName,
                                        File = filePath,
                                        PriceFilter = model.PriceFilter,
                                        StockFilter = model.StockFilter,
                                        Discount = model.PriceDiscount,
                                        NewPriceDiscount = model.NewPriceDiscount,
                                        MaxInstallment = model.MaxInstallment,
                                        UserID = userId,
                                        Fields = matchedFields,
                                        CargoAreaID = Convert.ToInt32(model.CargoAreaID),
                                        RootName = model.RootName,
                                        ProductIDs = productIds,
                                        CreateDate = DateTime.Now,
                                        Supplier = model.Supplier,
                                        FreeShipping = model.FreeShipping,
                                        ChildNames = string.Join("_._", contextChildNames),
                                        AdvertCount = isLastUpdate ? (insertCounter % increaseCount) : increaseCount,
                                        CategoryMatches = String.Join(",", model.XMLCategoryMatches),
                                        IsDeleted = false
                                    };
                                    //for timeout
                                    bool isInserted = parseService.Insert(parse);
                                    if (!isInserted)
                                    {
                                        await Task.Delay(3000);
                                        using (var newService = new ParseService())
                                        {
                                            isInserted = newService.Insert(parse);
                                        }
                                        if (!isInserted)
                                        {
                                            await Task.Delay(3000);
                                            parseService.Insert(parse);
                                        }
                                    }
                                }
                            }
                            else if (model.FileStream != null) //file update ediliyorsa
                            {
                                //ilk buraya uğramasında, delete file sayısı var olandan çıkarılacak (1 kere)
                                //son değilse, 
                                //son ise, var olana, modlu sayıdan artan eklenecek.

                                //model.FileName;
                                string fileName = Path.GetFileName(model.FilePath);
                                var parseFile = parseService.GetUserFilesByFileName(userId, fileName);
                                if (parseFile != null)
                                {
                                    //dosya güncellemede eklenen ürünlerin id'leri, diğer ürünlere ekleniyor.
                                    string productIdLast = isFirstUpdate ? "" : parseFile.ProductIDs;
                                    productIdLast = string.IsNullOrEmpty(productIdLast) ? productIds : productIdLast + ",,," + productIds;
                                    productIdLast = string.Join(",,,", productIdLast.Split(",,,").ToList().Distinct());

                                    int adverCount = isFirstUpdate ? 0 : parseFile.AdvertCount;
                                    parseFile.AdvertCount = (isLastUpdate ? (adverCount + (insertCounter % increaseCount)) : (adverCount + increaseCount));
                                    //parseFile.CargoAreaID = model.CargoAreaID;
                                    parseFile.Supplier = model.Supplier;
                                    parseFile.Discount = model.PriceDiscount;
                                    parseFile.NewPriceDiscount = model.NewPriceDiscount;
                                    parseFile.PriceFilter = model.PriceFilter;
                                    parseFile.StockFilter = model.StockFilter;
                                    parseFile.MaxInstallment = model.MaxInstallment;
                                    parseFile.FreeShipping = model.FreeShipping;
                                    parseFile.ProductIDs = productIdLast;
                                    parseFile.UpdateDate = DateTime.Now;
                                    parseFile.IsDeleted = false;

                                    //for timeout
                                    bool isUpdated = parseService.Update(parseFile);
                                    if (!isUpdated)
                                    {
                                        await Task.Delay(3000);
                                        using (var newService = new ParseService())
                                        {
                                            isUpdated = newService.Update(parseFile);
                                        }
                                        if (!isUpdated)
                                        {
                                            await Task.Delay(3000);
                                            parseService.Update(parseFile);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        using (var service = new BaseService())
                        {
                            service.Log(new Log
                            {
                                Function = "ParseController.Insert(ParseFile)",
                                CreatedDate = DateTime.Now,
                                Message = e.Message,
                                Detail = e.InnerException?.Message,
                                IsError = true
                            });
                        }
                    }
                }
            }
            #endregion
          
        }

        private List<Dictionary<string, object>> parseXMLFile(XmlDocument doc, string rootName, List<string> contextChildNames)
        {
            List<Dictionary<string, object>> listOfAdvertsFromXML = new List<Dictionary<string, object>>();
            foreach (XmlNode node in doc.DocumentElement)
            {
                if (node.Name == rootName)
                {
                    //urun
                    XmlNodeList innerNodes = node.ChildNodes;
                    var innerNodesCount = node.ChildNodes.Count;
                    if (innerNodesCount > 10)
                    {
                        //elemana ulaştık, dışarıdaki döngü kullanılacak.
                        //burada bütün itemlar aynı isimde mi ona bakılacak, hepsi aynı isimdeyse tip 2 devreye girecek. ()
                        //ilk ve son item karşılaştırılacak, eğer name'leri farklıysa içerdeyiz, değilse dışardayız.

                        if (innerNodes[0].Name != innerNodes[innerNodesCount - 1].Name)
                        {
                            Dictionary<string, object> itemDictionary = new Dictionary<string, object>();
                            //buradaki data, almamız gereken data.
                            for (int i = 0; i < contextChildNames.Count; i++)
                            {
                                //if (!itemDictionary.ContainsKey(contextChildNames[i]))
                                //{
                                if (contextChildNames[i].ToLower() == "stoklar")
                                {
                                    if (node[contextChildNames[i]].HasChildNodes)
                                    {
                                        foreach (XmlNode childNode in node[contextChildNames[i]].ChildNodes)
                                        {
                                            if (childNode.Name.ToLower() == "stok")
                                            {
                                                if (childNode.HasChildNodes)
                                                {
                                                    foreach (XmlNode child in childNode.ChildNodes)
                                                    {
                                                        if (child.Name.ToLower() == "miktar")
                                                        {
                                                            string innerTextStock = childNode[child.Name]?.InnerText?.Trim();
                                                            itemDictionary = TryAddDictionary(itemDictionary, contextChildNames[i], innerTextStock);
                                                            break;
                                                        }
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                        continue;
                                    }
                                }
                                string innerText = node[contextChildNames[i]]?.InnerText?.Trim();
                                itemDictionary = TryAddDictionary(itemDictionary, contextChildNames[i], innerText);
                                //}
                            }
                            listOfAdvertsFromXML.Add(itemDictionary);
                        }
                        else
                        {
                            //burası, iç içe aynı olana kadar ineceğimiz yer
                            foreach (XmlNode innerNode in innerNodes)
                            {
                                XmlNodeList innerNodeChildren = innerNode.ChildNodes;
                                int innerNodeCount = innerNodeChildren.Count;
                                if (innerNodeCount > 1)
                                {
                                    if (innerNodeChildren[0].Name != innerNodeChildren[innerNodeCount - 1].Name)
                                    {
                                        Dictionary<string, object> itemDictionary = new Dictionary<string, object>();
                                        //buradaki data, almamız gereken data.
                                        for (int i = 0; i < contextChildNames.Count; i++)
                                        {
                                            string innerText = innerNode[contextChildNames[i]]?.InnerText?.Trim();
                                            itemDictionary = TryAddDictionary(itemDictionary, contextChildNames[i], innerText);
                                        }
                                        listOfAdvertsFromXML.Add(itemDictionary);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //rows - bayev
                        //eleman içerde, burada bir döngü daha kullanılacak.
                        foreach (XmlNode innerNode in innerNodes)
                        {
                            //ıtem
                            XmlNodeList innerNodeChildren = innerNode.ChildNodes;
                            int innerNodeCount = innerNodeChildren.Count;
                            if (innerNodeCount > 6)
                            {
                                Dictionary<string, object> itemDictionary = new Dictionary<string, object>();
                                for (int i = 0; i < contextChildNames.Count; i++)
                                {
                                    string innerText = innerNode[contextChildNames[i]]?.InnerText?.Trim();
                                    itemDictionary = TryAddDictionary(itemDictionary, contextChildNames[i], innerText);
                                }
                                listOfAdvertsFromXML.Add(itemDictionary);
                            }
                            else
                            {
                                foreach (XmlNode innerNodeChild in innerNodeChildren)
                                {
                                    XmlNodeList innerInnerNodeChildren = innerNodeChild.ChildNodes;
                                    int innerNodeChildrenCount = innerInnerNodeChildren.Count;
                                    if (innerNodeChildrenCount > 1)
                                    {
                                        if (innerInnerNodeChildren[0].Name != innerInnerNodeChildren[innerNodeChildrenCount - 1].Name)
                                        {
                                            Dictionary<string, object> itemDictionary = new Dictionary<string, object>();
                                            //buradaki data, almamız gereken data.
                                            for (int i = 0; i < contextChildNames.Count; i++)
                                            {
                                                string innerText = innerNodeChild[contextChildNames[i]]?.InnerText?.Trim();
                                                itemDictionary = TryAddDictionary(itemDictionary, contextChildNames[i], innerText);
                                            }
                                            listOfAdvertsFromXML.Add(itemDictionary);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            HttpContext.Session.SetObject("XMLDictionaryList", listOfAdvertsFromXML);
            return listOfAdvertsFromXML;
        }

        public Dictionary<string, object> TryAddDictionary(Dictionary<string, object> itemDictionary, string key, string value)
        {
            string editedValue = string.IsNullOrEmpty(value) ? null : value;
            if (!itemDictionary.TryAdd(key, editedValue))
            {
                int counter = 0;
                for (; ; )
                {
                    counter++;
                    if (!itemDictionary.TryAdd((key + counter), editedValue))
                    {
                        if (counter == 10)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

            }

            return itemDictionary;
        }


        [HttpPost]
        public JsonResult DeleteUnnecessaryPhotos(int pareseID)
        {
            float totalFileSize = 0;

            List<AdvertPhoto> DbPhotos = new List<AdvertPhoto>();
            using (var srv = new AdvertPhotoService())
            {
                //DbPhotos = srv.GetAdvertPhotos(); 
                DbPhotos = srv.GetAdvertPhotosNotInAdverts();

            }
            // DB    :          /Upload/Sales/xOOUPE0ij06THgBRiQlB0wQSWDq95MUqPHEd66LWQ_1.jpg
            // Dizin (item) :   wwwroot\Upload\Sales\0oO7GHUUSJTwFp6zsyHQFMPf55AJYUqDuC0ENH9WjQ_1.jpg
            //fi orgi:          wwwroot\Upload\Sales\173nwwvNrUOE7Wa3C0UPdgG2LvysyalEmK4Af1KJvg_1.jpg

            var filesInFolder = Directory.GetFiles(@"wwwroot\Upload\Sales\");
            System.IO.File.WriteAllText("WriteLines.txt", String.Empty);
            string tempFilename;
            List<string> unmatchedFilesNames = new List<string>();
            int counter = 1;
            foreach (var fileName in filesInFolder)
            {
                if (string.IsNullOrEmpty(fileName))
                    continue;

                tempFilename = fileName.Replace(@"\", "/");
                if (!string.IsNullOrEmpty(tempFilename))
                    tempFilename = tempFilename.Trim().Replace("wwwroot", string.Empty).Replace("_thumb.", ".");

                int indexx = DbPhotos.FindIndex(x => x.Source == tempFilename);
                if (indexx > -1)
                {
                    FileInfo fi = new FileInfo(fileName);
                    if (fi != null)
                    {
                        totalFileSize += (fi.Length / 1024f) / 1024f;// byte ı MB a çevirdik.
                                                                     // unmatchedFilesNames.Add(item);

                        using (StreamWriter outputFile = new StreamWriter("WriteLines.txt", true))
                        {
                            outputFile.WriteLine(counter + " - " + fileName);
                            counter++;
                        }

                        try
                        {
                            System.IO.File.Move(fileName, fileName.Replace(@"\Sales\", @"\TempSales\"));
                        }
                        catch (Exception ex)
                        {
                            throw ex;

                        }

                    }


                }

            }



            System.IO.File.WriteAllText(@"Totals.txt", "Total Files : " + filesInFolder.Length + " TotalFileSize =" + totalFileSize);
            return Json(new { TotalFiles = filesInFolder.Length, TotalFileSize = totalFileSize });
        }

    }
}