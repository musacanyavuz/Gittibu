using Dapper.FastCrud;
using GittiBu.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GittiBu.Services
{
    public class ParseService : BaseService
    {
        public bool Insert(Parse parse)
        {
            try
            {
                GetConnection().Insert(parse);
                return parse.ID > 0;
            }
            catch (Exception e)
            {
                string jsonString;
                jsonString = JsonConvert.SerializeObject(parse);
                Log(new Log
                {
                    Function = "ParseService.Insert",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail =e.ToString() + " \n \n" + jsonString,
                    IsError = true
                });
                return false;
            }
        }
       
        public bool Update(Parse parse)
        {
            try
            {
                return GetConnection().Update(parse);
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "ParseService.Update",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                });
                return false;
            }
        }

        public bool Delete(int id)
        {
            return GetConnection().Delete(new Parse() { ID = id });
        }

        public List<Content> GetCategoryContents(List<AdvertCategory> categories)
        {
            string categoriesNamesId = categories.Select(s => s.NameID.ToString()).Aggregate((current, next) => current + ", " + next);
            var data = GetConnection().Find<Content>(s => s
                     .Where($"{nameof(Content.Key):C} IN ({categoriesNamesId}) and {nameof(Content.LanguageID):C}=1")).ToList();
            return data;
        }

        public Parse Get(int id)
        {
            return GetConnection().Get(new Parse { ID = id });
        }

        public List<Parse> GetAll()
        {
            return GetConnection().Find<Parse>().ToList();
        }

        public List<Parse> GetUserFilesById(int userId)
        {
            return GetConnection().Find<Parse>(s => s
            .Where($"{nameof(Parse.UserID):C} = @userId and {nameof(Parse.IsDeleted):C} = false")
            .WithParameters(new { userId }))
            .ToList();
        }
       


        public Parse GetUserFilesByFileName(int userId, string fileName)
        {
            try
            {
                return GetConnection().Find<Parse>(s => s
                    .Where($"{nameof(Parse.UserID):C}=@userId and {nameof(Parse.File):C} LIKE @fileNameLike")
                    .WithParameters(new { userId, fileNameLike = "%" + fileName + "%" }))
                    .FirstOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public List<Parse> GetAllParsesByUserFileName(int userId, string userFileName)
        {
            try
            {
                return GetConnection().Find<Parse>(s => s
                    .Where($"{nameof(Parse.UserID):C}=@userId and {nameof(Parse.UserFileName):C} LIKE @userFileNameLike  and \"IsDeleted\" = false ORDER BY \"ID\" desc ")                   
                    .WithParameters(new { userId, userFileNameLike = "%" + userFileName + "%" })).ToList();
                   
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool DeleteOrUpdateAdvert(List<PaymentRequest> paymentRequests, int advertId)
        {
            using (var service = new AdvertService())
            {
                bool isOk = false;
                bool isSold = paymentRequests.Any(s => s.AdvertID == advertId);
                if (isSold)
                {
                    Advert currentAdvert = service.GetAdvert(advertId);
                    if (currentAdvert != null)
                    {
                        currentAdvert.IsActive = false;
                        currentAdvert.IsDeleted = true;
                        currentAdvert.LastUpdateDate = DateTime.Now;
                        isOk = service.Update(currentAdvert);
                    }
                }
                else
                {
                    isOk = service.Delete(advertId);
                }
                return isOk;
            }
        }

        public Parse DeleteParseFileWithAdverts(int parseId)
        {
            string[] advertIdArray;
            try
            {
                using (var parseService = new ParseService())
                using (var paymentService = new PaymentRequestService())
                {
                    var parseToDelete = parseService.Get(parseId);
                    if (parseToDelete != null)
                    {
                        List<PaymentRequest> paymentRequests = paymentService.GetAll();
                        DateTime now = DateTime.Now;
                        string productIds = parseToDelete.ProductIDs;
                        advertIdArray = productIds.Split(",,,");
                      
                        foreach (string advId in advertIdArray)
                        {
                            if (!string.IsNullOrEmpty(advId))
                            {
                                int advertId = Convert.ToInt32(advId.Trim());
                                DeleteOrUpdateAdvert(paymentRequests, advertId);
                            }
                        }
                        parseToDelete.IsDeleted = true;
                        parseService.Update(parseToDelete);
                        return parseToDelete;
                    }
                }

                

            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "ParseService.DeleteParseFileWithAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true
                });
            }
            return null;
        }

    }
}
