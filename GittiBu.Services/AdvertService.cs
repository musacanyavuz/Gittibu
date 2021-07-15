using Dapper;
using Dapper.FastCrud;
using GittiBu.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
// AdvertService.cs
namespace GittiBu.Services
{
    public class AdvertService : BaseService
    {
        #region Dopingli_Listelemeler_Algoritmik

        public List<Advert> GetHomePageAdverts(int lang = 1, int userId = 0)
        {
            try
            {
                //var sql_ = "SELECT\n  Adverts.*,\n  " +
                //          "( SELECT count(*) AS count FROM AdvertLikes WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n  " +
                //          " ( SELECT IsILiked(Adverts.ID, @userId ) ) as IsILiked,\n  " +
                //          "GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n  " +
                //          "GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn, " +
                //          "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                //          "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                //          "Users.Name AS UserName" +
                //          " FROM Adverts, AdvertCategories, Users\n  WHERE Adverts.IsActive = true\n  " +
                //          "AND Adverts.CategoryID = AdvertCategories.ID\n " +
                //          "AND Adverts.UserID = Users.ID\n " +
                //          " AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n  " +
                //          "ORDER BY Adverts.ID DESC limit 37;";
                var sql = @"SELECT
                              Adverts.*,
                              (SELECT count(*) AS count FROM AdvertLikes WHERE(AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,
                               ( SELECT IsILiked(Adverts.ID, @userId) ) as IsILiked,
                              GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,
                              GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn, GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, Users.Name AS UserName FROM Adverts, AdvertCategories, Users
                              WHERE Adverts.IsActive = true
                              AND Adverts.CategoryID = AdvertCategories.ID
                             AND Adverts.UserID = Users.ID
                              AND((SELECT Users.IsActive  FROM Users WHERE(Users.ID = Adverts.UserID)) = true)                                                             
                              limit 37; ";
                // // ORDER BY Adverts.ID DESC
                var list = GetConnection().Query<Advert>(sql, new { userId }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                if (list.Count > 0)
                {
                    list = list.OrderByDescending(x => x.ID).ToList();
                }
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetHomePageAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true
                });
            }
            return null;
        }

        public List<Advert> GetHomePageAdverts(int lang, int offset, int userId = 0, int limit = 10)
        {
            try
            {
                var sql = "SELECT\n  Adverts.*,\n  " +
                          "( SELECT count(*) AS count FROM AdvertLikes WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n  " +
                          " ( SELECT IsILiked(Adverts.ID, @userId ) ) as IsILiked,\n  " +
                          "GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n  " +
                          "GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn, " +
                          "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                          " GetText((AdvertCategories.NameID), (2)) AS CategoryNameEn " +
                          " FROM Adverts, AdvertCategories\n  WHERE Adverts.IsActive = true\n  " +
                          "AND Adverts.CategoryID = AdvertCategories.ID\n " +
                          " AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n  " +
                          "ORDER BY Adverts.ID DESC limit @limit OFFSET @offset;";

                var list = GetConnection().Query<Advert>(sql, new { offset, userId, limit }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "GetAdvertCategories",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true
                });
            }
            return null;
        }

        public List<Advert> GetCategoryPageAdverts(int categoryId, int lang, int offset = 0, int userId = 0, int limit = 40)
        {
            try
            {
                var sql = "SELECT\n  Adverts.*,\n  " +
                          "( SELECT count(*) AS count FROM AdvertLikes WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n  " +
                          "( SELECT GetLabelDoping((Adverts.ID)) AS GetLabelDoping) AS LabelDoping,\n  " +
                          " ( SELECT IsILiked(Adverts.ID, @userId ) ) as IsILiked,\n  " +
                          "GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n  " +
                          "GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn, " +
                          "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                          " GetText((AdvertCategories.NameID), (2)) AS CategoryNameEn, " +
                          "Users.Name AS UserName " +
                          " FROM Adverts,Users,AdvertCategories\n  WHERE Adverts.IsActive = true\n  " +
                          "AND Adverts.CategoryID = @categoryId " +
                          "AND Adverts.UserID = Users.ID" +
                          "AND Adverts.CategoryID = AdvertCategories.ID\n " +
                          " AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n  " +
                          "ORDER BY Adverts.ID DESC limit @limit OFFSET @offset;";

                var list = GetConnection().Query<Advert>(sql, new { categoryId, userId, offset, limit }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    //aktif dile göre ilanlaraın kategori slug'larını günceller.
                    list.ForEach(x =>
                    {
                        //x.CategorySlug = (lang==1)?x.CategorySlug:x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        //x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetCategoryPageAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true
                });
            }
            return null;
        }


        public List<Advert> GetSimilarAdverts(int categoryId, int limit = 4, int lang = 1)
        {
            try
            {
                var sql = "select " +
                          "Adverts.*, " +
                          "(GetText( (select SlugID from AdvertCategories where AdvertCategories.ID= (SELECT ParentCategoryID from AdvertCategories where AdvertCategories.ID=Adverts.CategoryID) ) ,@lang)) as CategorySlug, " +
                          "(GetText( (select SlugID from AdvertCategories where AdvertCategories.ID=Adverts.CategoryID) ,@lang)) as SubCategorySlug, " +
                          "AdvertCategories.*,Users.*,ParentCategories.* " +
                          "from " +
                          "Adverts, AdvertCategories, Users, AdvertCategories as ParentCategories " +
                          "where " +
                          "Adverts.UserID=Users.ID " +
                          "and Adverts.CategoryID = AdvertCategories.ID " +
                          "and AdvertCategories.ParentCategoryID = ParentCategories.ID " +
                          "and Adverts.IsActive = true " +
                          "and Users.IsActive = true " +
                          "and AdvertCategories.ID=@categoryId " +
                          "order by Adverts.CreatedDate desc " +
                          "LIMIT " + limit;
                var result = GetConnection().Query<Advert, AdvertCategory, User, AdvertCategory, Advert>(sql,
                    (advert, category, user, parentCategory) =>
                    {
                        advert.Category = category;
                        advert.Category.ParentCategory = parentCategory;
                        advert.User = user;
                        return advert;
                    }, splitOn: "ID", param: new { categoryId, lang }).ToList();

                return result;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetSimilarAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true
                });
                return null;
            }
        }

        #endregion

        #region Base

        public Advert Get(int id)
        {
            try
            {
                var sql = "select Adverts.*,( SELECT count(*) AS count           FROM AdvertLikes       " +
                            "   WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n     " +
                            "(GetText(AdvertCategories.SlugID, 1)) as SubCategorySlugTr,\n  " +
                            "     (GetText(AdvertCategories.SlugID, 2)) as SubCategorySlugEn,\n\n   " +
                            "    AdvertCategories.*,Users.* from\n  Adverts, AdvertCategories, Users\nwhere\n      Adverts.UserID=Users.ID\n  and Adverts.CategoryID = AdvertCategories.ID\n  and Adverts.IsActive = true\n  and Users.IsActive = true\n  and Adverts.ID = @id; ";


                var adv = GetConnection().Query<Advert, AdvertCategory, User, Advert>(sql,
                   (advert, category, user) =>
                   {
                       advert.Category = category;
                       //advert.Category.ParentCategory = parentCategory;
                       advert.User = user;
                       return advert;
                   }, new { id }
                   ).FirstOrDefault();
                if (adv == null)
                    throw new Exception("adv is NULL");
                string photosSql = $"select * from AdvertPhotos where AdvertID = @id;";
                var addPhotos = GetConnection().Query<AdvertPhoto>(photosSql, new { id });
                if (addPhotos != null)
                    adv.Photos = addPhotos;
                return adv;

                //using (var multi = GetConnection().QueryMultiple(sql, new { id }))
                //{
                //    var ad = multi.Read<Advert, AdvertCategory, User, Advert>(
                //        (advert, category, user) =>
                //        {
                //            advert.Category = category;
                //            //advert.Category.ParentCategory = parentCategory;
                //            advert.User = user;
                //            return advert;
                //        }, splitOn: "ID").SingleOrDefault();
                //    if (ad == null)
                //        return null;

                //    ad.Photos = multi.Read<AdvertPhoto>()?.OrderBy(p => p.OrderNumber).ToList();
                //    return ad;
                //}



            }
            catch (Exception e)
            {
                if (e.Message == "adv is NULL")
                    return null;



                Log(new Log
                {
                    Function = "AdvertService.Get",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                });
                return null;
            }
        }

        public Advert GetAdvert(int id) //admin panelden tüm ilanlara erişebilmek için
        {
            try
            {
                var sql = "select\n  Adverts.*,\n  GetText(AdvertCategories.NameID, 1) as CategorySlug,\n  Users.*\nfrom Adverts, AdvertCategories, Users\nwhere Adverts.CategoryID = AdvertCategories.ID\nand Adverts.UserID = Users.ID\nand Adverts.ID = @id; " +
                          $"select * from AdvertPhotos where AdvertID = @id;" +
                          $" select * from AdvertDopings, DopingTypes where AdvertID = @id and TypeID = DopingTypes.ID;";

                using (var multi = GetConnection().QueryMultiple(sql, new { id }))
                {
                    var ad = multi.Read<Advert, User, Advert>(
                        (advert, user) =>
                        {
                            advert.User = user;
                            return advert;
                        }, splitOn: "ID").SingleOrDefault();
                    if (ad == null)
                        return null;

                    ad.Photos = multi.Read<AdvertPhoto>().ToList();

                    ad.AdvertDopings = multi.Read<AdvertDoping, DopingType, AdvertDoping>(
                        (doping, type) =>
                        {
                            doping.DopingType = type;
                            return doping;
                        }, splitOn: "ID")
                    .ToList();

                    return ad;
                }
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetAdvert",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true
                });
                return null;
            }
        }

        public bool Insert(Advert advert)
        {
            try
            {
                GetConnection().Insert(advert);
                return advert.ID > 0;
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
                        Detail = e.ToString(),
                        IsError = true
                    });
                }
                return false;
            }
        }

        public bool Update(Advert advert)
        {
            try
            {
                return GetConnection().Update(advert);
            }
            catch (Exception e)
            {
                using (var service = new BaseService())
                {
                    service.Log(new Log
                    {
                        Function = "AdvertService.Update",
                        CreatedDate = DateTime.Now,
                        Message = e.Message,
                        Detail = e.ToString(),
                        IsError = true
                    });
                }
                return false;
            }
        }

        public bool Delete(int id)
        {
            using (var service = new AdvertPhotoService())
            {
                service.DeleteAdvertPhotosByAdvertId(id);
            }
            return GetConnection().Delete(new Advert() { ID = id });
        }

        /// <summary>
        /// Silmeden Önce  PaymentRequest Kontrolü Papar .
        /// </summary>
        /// <param name="advert"></param>
        /// <returns></returns>
        public bool Delete(Advert advert)
        {
            IList<PaymentRequest> blackList = new List<PaymentRequest>();
            using (var paymentRequestSrv = new PaymentRequestService())
            using (var service = new AdvertPhotoService())
            {
                blackList = paymentRequestSrv.GetPaymentRequestsOfAdvertToDelete(advert.ID);
                if (blackList == null || blackList.Count == 0)
                {
                    service.DeleteAdvertPhotosByAdvertId(advert.ID);
                    return GetConnection().Delete(new Advert() { ID = advert.ID });
                }
                else
                {
                    return false;
                }
            }

        }


        public Advert GetMyAdvert(int id, int userId)
        {
            var ad = GetConnection().Find<Advert>(s => s
                    .Include<AdvertCategory>().Include<AdvertPhoto>()
                    .Where($"{nameof(Advert.ID):C}=@id and {nameof(Advert.UserID):C}=@userId")
                    .WithParameters(new { id, userId }))
                .SingleOrDefault();
            if (ad == null)
            {
                ad = GetConnection().Find<Advert>(s => s
                         .Include<AdvertCategory>()
                         .Where($"{nameof(Advert.ID):C}=@id and {nameof(Advert.UserID):C}=@userId")
                         .WithParameters(new { id, userId }))
                    .SingleOrDefault();
            }
            if (ad != null)
            {
                var request = GetConnection().Find<AdvertPublishRequest>(s => s
                    .Where($"{nameof(AdvertPublishRequest.AdvertID):C}=@id and {nameof(AdvertPublishRequest.IsActive):C}=true")
                    .OrderBy($"{nameof(AdvertPublishRequest.ID):C} DESC")
                    .WithParameters(new { id })
                ).FirstOrDefault();
                ad.IsPendingApproval = request != null; //request null değil ise yayınlanma onayı bekliyor demektir.
            }
            return ad;
        }

        public List<Advert> GetUserAdverts(int userId)
        {
            try
            {
                var sql = "select Adverts.*, " +
                          "GetIsPendingApproval(Adverts.ID) as IsPendingApproval, Users.* " +
                          "from Adverts, Users where Adverts.UserID=@userId and Adverts.UserID=Users.ID " +
                          " " +
                          " " +
                          " ";
                return GetConnection().Query<Advert, User, Advert>(sql, (advert, user) =>
                {
                    advert.User = user;
                    return advert;
                }, new { userId }, splitOn: "ID").ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public List<Advert> GetUserAdvertsByXml(int userId)
        {
            try
            {
                var sql = "select Adverts.*, " +
                          "GetIsPendingApproval(Adverts.ID) as IsPendingApproval, Users.* " +
                          "from Adverts, Users where Adverts.UserID=@userId and Adverts.UserID=Users.ID " +
                          " " +
                          " " +
                          " ";
                return GetConnection().Query<Advert, User, Advert>(sql, (advert, user) =>
                {
                    advert.User = user;
                    return advert;
                }, new { userId }, splitOn: "ID").ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public List<Advert> GetAdverts()
        {
            var sql = "select\n    Adverts.*,\n    GetText(AdvertCategories.NameID,1) as CategorySlug,\n    Users.*\nfrom Adverts, Users, AdvertCategories\nwhere Adverts.UserID = Users.ID\nand Adverts.CategoryID = AdvertCategories.ID";
            var query = GetConnection().Query<Advert, User, Advert>(sql, (advert, user) =>
            {
                advert.User = user;
                return advert;
            }, splitOn: "ID").ToList();
            return query;
        }

        #endregion

        #region Filter


        public List<Advert> FilterAdverts(int categoryId, string content, string brand,
            string model, int advertId, int minPrice, int maxPrice, int lang, int userId)
        {
            try
            {
                var list = GetCategoryPageAdverts(categoryId, lang, 0, userId, 200);
                if (!string.IsNullOrEmpty(content))
                {
                    content = content.ToLower();
                    list = list.Where(x => x.Content.ToLower().Contains(content) || x.Title.ToLower().Contains(content)).ToList();
                }
                if (!string.IsNullOrEmpty(brand))
                {
                    brand = brand.ToLower();
                    list = list.Where(x => x.Brand.ToLower().Contains(brand)).ToList();
                }
                if (!string.IsNullOrEmpty(model))
                {
                    model = model.ToLower();
                    list = list.Where(x => x.Model.ToLower().Contains(model)).ToList();
                }
                if (advertId > 0)
                {
                    list = list.Where(x => x.ID.ToString().Contains(advertId.ToString())).ToList();
                }
                if (minPrice > 0)
                {
                    list = list.Where(x => x.Price >= minPrice).ToList();
                }
                if (maxPrice > 0)
                {
                    list = list.Where(x => x.Price <= maxPrice).ToList();
                }
                return list;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        #endregion

        #region PublishRequest

        public void DeletePublishRequests(int advertId)
        {
            try
            {
                var sql = $"DELETE FROM AdvertPublishRequests where AdvertID=@advertId ";
                GetConnection().Execute(sql, new { advertId });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public AdvertPublishRequest GetPublishRequest(int id)
        {
            try
            {
                return GetConnection().Find<AdvertPublishRequest>(s => s
                    .Where($"{nameof(AdvertPublishRequest.ID):C}=@id")
                    .WithParameters(new { id })
                    .Include<Advert>()
                    .Include<User>()
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public List<AdvertPublishRequest> GetPublishRequests()
        {
            var sql = "select\n   AdvertPublishRequests.*,\n   Adverts.*,\n   GetText(AdvertCategories.NameID, 1) as CategorySlug,\n   Users.*\nfrom\n  AdvertPublishRequests, Adverts, AdvertCategories, Users\nwhere\n    AdvertPublishRequests.AdvertID = Adverts.ID\nand Adverts.UserID = Users.ID\nand Adverts.CategoryID = AdvertCategories.ID\nand AdvertPublishRequests.IsActive = true";

            var list = GetConnection().Query<AdvertPublishRequest, Advert, User, AdvertPublishRequest>(sql,
                (request, advert, user) =>
                {
                    request.Advert = advert;
                    request.Advert.User = user;
                    return request;
                }, splitOn: "ID").ToList();

            return list;
        }

        public bool InsertPublishRequest(AdvertPublishRequest publishRequest)
        {
            try
            {
                GetConnection().Insert(publishRequest);
                return publishRequest.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Update(AdvertPublishRequest publishRequest)
        {
            try
            {
                return GetConnection().Update(publishRequest);
            }
            catch (Exception e)
            {
                return false;
            }
        }
        #endregion

        public List<Advert> GetList(string search, int count, int offset)
        {
            var sql = $"select Adverts.*, " +
                      $"(select GetText((select NameID from AdvertCategories where ID = Adverts.CategoryID), 1 )) as CategorySlug," +
                      $" Users.* " +
                      $"from Adverts, Users " +
                      $"where UserID=Users.ID " +
                      $"order by Adverts.ID desc limit @count offset @offset";
            if (!string.IsNullOrEmpty(search))
            {
                sql = $"select Adverts.*, " +
                      $"(select GetText((select NameID from AdvertCategories where ID = Adverts.CategoryID), 1 )) as CategorySlug," +
                      $" Users.* " +
                      $" from Adverts, Users " +
                      $"where UserID=Users.ID and (" +
                      $"Title ilike @search or Content ilike @search or Brand ilike @search\nor " +
                      $" (select GetText((select NameID from AdvertCategories where ID = Adverts.CategoryID), 1 )) ilike @search  or " +
                      $" Users.Name ilike @search or " +
                      $"cast(Adverts.ID as varchar) ilike @search ) " +
                      $"order by Adverts.ID desc limit @count offset @offset";
            }
            return GetConnection().Query<Advert, User, Advert>(sql, (advert, user) =>
            {
                advert.User = user;
                return advert;
            }, new { search = "%" + search + "%", count, offset }, splitOn: "ID")
            .ToList();
        }

        public int GetAdvertsCount(string search = null)
        {
            if (string.IsNullOrEmpty(search))
            {
                return GetConnection().Query<int>($"select count(*) from Adverts, Users where UserID=Users.ID").First();
            }

            var sql = $"select count(Adverts.ID)\nfrom Adverts\nwhere\n  Title ilike @search\n  or Content ilike @search\n  or Brand ilike @search\n  or  (select GetText((select NameID from AdvertCategories where ID = Adverts.CategoryID), 1 )) ilike @search\n  or (select Name from Users where Users.ID=UserID) ilike @search\n  or cast(Adverts.ID as CHAR(10)) ilike @search";
            var count = GetConnection().Query<int>(sql, new { search = "%" + search + "%" }).First();
            return count;
        }

        public List<Advert> Search(string query, int userId, int lang = 1)
        {
            try
            {
                var sql = $"SELECT ( SELECT GetOrderFromDopingInHomepage((Adverts.ID)) AS GetOrderFromDopingInHomepage) AS AdvertOrder,\n       Adverts.*,    ( SELECT count(*) AS count           FROM AdvertLikes          WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n       ( SELECT GetLabelDoping((Adverts.ID)) AS GetLabelDoping) AS LabelDoping,\n       ( SELECT GetYellowFrameDoping((Adverts.ID)) AS GetYellowFrameDoping) AS YellowFrameDoping,\n       ( SELECT IsILiked(Adverts.ID, @userId ) ) as IsILiked,\n       GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n       GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n       GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr,\n       GetText((AdvertCategories.NameID), (2)) AS CategoryNameEn\nFROM Adverts,    AdvertCategories,Users\nWHERE (Adverts.IsActive = true) AND Adverts.CategoryID = AdvertCategories.ID\n  AND (Adverts.UserID=Users.ID)\n AND (Users.IsActive=true)\n AND (Adverts.Title ilike @query or Adverts.Content ilike @query or Adverts.ProductDefects ilike @query or Adverts.ID::text ilike @query or Users.Name ilike @query)\nORDER BY AdvertOrder, CreatedDate DESC limit 200;";
                var list = GetConnection().Query<Advert>(sql, new { query = "%" + query + "%", userId, lang }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                //aktif dile göre ilanlaraın kategori slug'larını günceller.
                return list;
            }
            catch (Exception e)
            {
                return null;
            }

        }
        public User GetUser(int id)
        {
            try
            {
                var sql = $"select * from Users\nleft outer join Countries on CountryID = Countries.ID\nleft outer join Cities on CityID = Cities.ID\nleft outer join Districts on DistrictID = Districts.ID\nwhere Users.ID = @id";
                return GetConnection().Query<User, Country, City, District, User>(sql,
                        (user, country, city, district) =>
                        {
                            user.Country = country;
                            user.City = city;
                            user.District = district;
                            return user;
                        }, new { id }, splitOn: "ID")
                    .SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }


        public void UpdateViewCount(int id)
        {
            try
            {
                GetConnection().Execute("update Adverts set ViewCount = ViewCount + 1 where ID = @id",
                    new { id });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public bool BulkInsert(List<Advert> list)
        {
            try
            {
                var sql = "INSERT INTO Adverts ( " +
                          " ID,Title,Content,NewProductPrice,Price,IsAvailableForSwap,IsAvailableBargain,PaymentMethodID,FreeShipping,ShippingTypeID, " +
                          " OriginalBox,TermsOfUse,ProductStatus,WebSite,CategoryID,CreatedDate, LastUpdateDate,UserID,ViewCount,MoneyTypeID, " +
                          " StockAmount,IsActive,ShippingPrice,UseSecurePayment,ProductDefects,IsDeleted,Brand,AvailableInstallments,Model " +
                          ") " +
                          " values ( " +
                          "@ID, @Title, @Content, @NewProductPrice, @Price, @IsAvailableForSwap, @IsAvailableBargain, @PaymentMethodID, @FreeShipping, @ShippingTypeID, @OriginalBox, @TermsOfUse, @ProductStatus, @WebSite, " +
                          "@CategoryID, @CreatedDate, @LastUpdateDate, @UserID, @ViewCount, @MoneyTypeID, @StockAmount, @IsActive, @ShippingPrice, @UseSecurePayment, @ProductDefects, @IsDeleted,   " +
                          "@Brand, @AvailableInstallments, @Model" +
                          ") ";
                GetConnection().Open();
                IDbTransaction trans = GetConnection().BeginTransaction();
                GetConnection().Execute(sql, list, trans);

                trans.Commit();
                GetConnection().Close();
                return true;
            }
            catch (Exception e)
            {
                throw e;
                return false;
            }
        }

        public int GetUserAdvertCount(int userId)
        {
            const string sql = "SELECT COUNT(*) FROM Adverts where IsActive =true and UserID =@userId";
            var count = GetConnection().Query<int>(sql, new { userId }).FirstOrDefault();
            return count;
        }

        //satıcının diğer ilanları sayfası için
        // userId = satıcıID , loginId = sayfaya giren kullanıcını id
        public List<Advert> GetUserAdverts(int userId, int loginId, int lang)
        {
            try
            {
                var sql = $"SELECT Adverts.*,\n    ( SELECT count(*) AS count FROM AdvertLikes WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n ( SELECT IsILiked(Adverts.ID, @loginId ) ) as IsILiked,\n  GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n  GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn\n   FROM Adverts, AdvertCategories\n  WHERE Adverts.IsActive = true AND Adverts.IsDeleted=false AND Adverts.CategoryID = AdvertCategories.ID\n    AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n AND Adverts.UserID = @userId  ORDER BY CreatedDate DESC;";
                var list = GetConnection().Query<Advert>(sql, new { userId, loginId }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                //aktif dile göre ilanlaraın kategori slug'larını günceller.

                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetHomePageAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                });
            }
            return null;
        }

        #region AdvertPhotos

        public bool UpdatePhotoOrder(int id, int order)
        {
            try
            {
                var sql = $"UPDATE AdvertPhotos set OrderNumber=@order where ID=@id";
                GetConnection().Execute(sql, new { order, id });
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public bool DeletePhoto(AdvertPhoto photo)
        {
            try
            {
                return GetConnection().Delete(photo);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool InsertAdvertPhoto(AdvertPhoto advertPhoto)
        {
            try
            {
                GetConnection().Insert(advertPhoto);
                return advertPhoto.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public List<AdvertPhoto> GetPhotos(int advertId)
        {
            try
            {
                return GetConnection().Find<AdvertPhoto>(s => s
                    .Where($"{nameof(AdvertPhoto.AdvertID):C}=@advertId")
                    .WithParameters(new { advertId })
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public AdvertPhoto GetPhoto(int id)
        {
            try
            {
                return GetConnection().Find<AdvertPhoto>(s => s
                    .Where($"{nameof(AdvertPhoto.ID):C}=@id")
                    .WithParameters(new { id })
                    .Include<Advert>()
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool UpdateAdvertPhotos(int advertId, int[] advertPhotos)
        {
            if (advertPhotos != null && advertPhotos.Length > 0)
            {
                var ids = "";
                foreach (var id in advertPhotos)
                {
                    ids += id + ",";
                }
                ids = ids.Substring(0, ids.Length - 1);
                var sql = $"UPDATE AdvertPhotos set AdvertID=@advertId where ID in (" + ids + ") ";
                return GetConnection().Execute(sql, new { advertId }) > 0;
            }
            return false;
        }

        public bool BulkInsertAdvertPhoto(List<AdvertPhoto> list)
        {
            try
            {
                var sql = "INSERT INTO AdvertPhotos ( " +
                          " ID,AdvertID,Source,Thumbnail,CreatedDate " +
                          ") " +
                          " values ( " +
                          "@ID, @AdvertID, @Source, @Thumbnail, @CreatedDate ) ";

                GetConnection().Open();
                IDbTransaction trans = GetConnection().BeginTransaction();
                GetConnection().Execute(sql, list, trans);

                trans.Commit();
                GetConnection().Close();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion

        #region AdvertDopings

        public bool InsertAdvertDoping(AdvertDoping advertDoping)
        {
            try
            {
                GetConnection().Insert(advertDoping);
                return advertDoping.ID > 0;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "InsertAdvertDoping",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true
                });
                return false;
            }
        }

        public AdvertDoping GetAdvertDoping(int id)
        {
            return GetConnection().Get(new AdvertDoping { ID = id });
        }

        public bool UpdateAdvertDoping(AdvertDoping advertDoping)
        {
            return GetConnection().Update(advertDoping);
        }

        public int ActivateAllPendingDopings(int advertId, int paymentId)
        {
            var sql = $"UPDATE AdvertDopings set IsActive=true, IsPendingApproval=false " +
                      $"where IsPendingApproval=true and PaymentID=@paymentId " +
                      $"and AdvertID=@advertId ";

            return GetConnection().Execute(sql, new { advertId, paymentId });
        }

        public int PassiveAllPendingDopings(int advertId, int paymentId)
        {
            var sql = $"UPDATE AdvertDopings set IsPendingApproval=false " +
                      $"where IsPendingApproval=true and PaymentID=@paymentId " +
                      $"and AdvertID=@advertId ";

            return GetConnection().Execute(sql, new { advertId, paymentId });
        }

        public void UpdateDopingPaymentId(int id, int paymentId)
        {
            try
            {
                var sql = $"update AdvertDopings set PaymentID=@paymentId where ID = @id";
                GetConnection().Query(sql, new { paymentId, id });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        #endregion

        #region AdvertLikes

        public AdvertLike GetAdvertLike(int advertId, int userId)
        {
            try
            {
                return GetConnection().Find<AdvertLike>(s => s
                    .Where($"{nameof(AdvertLike.AdvertID):C}=@advertId and {nameof(AdvertLike.UserID):C}=@userId")
                    .WithParameters(new { advertId, userId })
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool InsertAdvertLike(AdvertLike advertLike)
        {
            try
            {
                GetConnection().Insert(advertLike);
                return advertLike.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool DeleteAdvertLike(AdvertLike advertLike)
        {
            try
            {
                return GetConnection().Delete(advertLike);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion

        public List<Advert> GetAllActiveAdverts(int lang = 1, int userId = 0)
        {
            try
            {
                var sql = "SELECT\n  Adverts.*,\n  " +
                          "GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n  " +
                          "GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn, " +
                          "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                          " GetText((AdvertCategories.NameID), (2)) AS CategoryNameEn " +
                          " FROM Adverts, AdvertCategories\n  WHERE Adverts.IsActive = true\n  " +
                          "AND Adverts.CategoryID = AdvertCategories.ID\n " +
                          " AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n  " +
                          "ORDER BY Adverts.ID DESC;";

                var list = GetConnection().Query<Advert>(sql).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                    });
                }

                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetAllActiveAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                });
            }
            return null;
        }

        public Advert GetAdvertByXMLProductId(int userId, int XMLProductId)
        {
            try
            {
                return GetConnection().Find<Advert>(s => s
                    .Where($"{nameof(Advert.UserID):C}=@userId and {nameof(Advert.XMLProductID):C}=@XMLProductId")
                    .WithParameters(new { userId, XMLProductId }))
                    .FirstOrDefault();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetAdvertByXMLProductId",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params = userId.ToString() + " " + XMLProductId.ToString()
                });

                return null;
            }
        }

    }
}