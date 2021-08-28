using Dapper;
using Dapper.FastCrud;
using GittiBu.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GittiBu.Services
{
    public class AdvertPhotoService : BaseService
    {
        public List<AdvertPhoto> GetAllAdvertPhotos()
        {
            string sql = @"  select * from ""AdvertPhotos"" ";
            var result = GetConnection().Query<AdvertPhoto>(sql);
            return result.ToList();
        }
        public List<AdvertPhoto> GetAllAdvertPhotosExistInAdverts()
        {
            string sql = @"select * from ""Adverts"" a join ""AdvertPhotos"" ap on a.""ID"" = ap.""AdvertID"" ";
            var result = GetConnection().Query<AdvertPhoto>(sql);
          
          return result.ToList();
        }
        public List<AdvertPhoto> GetAdvertPhotosByAdvertID(int advertId)
        {

            try
            {
                return GetConnection().Find<AdvertPhoto>(s => s
                                   .Where($"{nameof(AdvertPhoto.AdvertID):C}=@advertId ")
                                   .WithParameters(new { advertId })).ToList();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertPhotoService.GetAdvertPhotosByAdvertID",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                });
            }
            return null;
           
        }
        public void DeleteAdvertPhotosByAdvertId(int advertId)
        {
            var listOfPhotos = GetAdvertPhotosByAdvertID(advertId);
            string pthSource, pthThumbnail;
            foreach (var item in listOfPhotos)
            {

                 pthSource = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + item.Source);
                pthThumbnail = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + item.Thumbnail);
                if (File.Exists(pthSource))
                    File.Delete(pthSource);
                if (File.Exists(pthThumbnail))
                    File.Delete(pthThumbnail);

                GetConnection().Delete(item);


            }

        
        }
        public void UpdateAdvertPhotosByAdvertId(int advertId)
        {
            var listOfPhotos = GetAdvertPhotosByAdvertID(advertId);
            try
            {
                foreach (var item in listOfPhotos)
                {
                    item.Source = "deleted_" + item.Source;
                    item.Thumbnail = "deleted_" + item.Thumbnail;
                    GetConnection().Update(item);


                }
            }
            catch (Exception)
            {

               
            }
            

        }
        public void DeleteAdvertOnlyPhysicalPhotos(AdvertPhoto photo)
        {
          
            string pthSource, pthThumbnail;
                pthSource = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + photo.Source);
                pthThumbnail = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + photo.Thumbnail);
                if (File.Exists(pthSource))
                    File.Delete(pthSource);
                if (File.Exists(pthThumbnail))
                    File.Delete(pthThumbnail);

                


            

        }
        /// <summary>
        /// AdvertPhotos da olup  Adverts da olmayan resimleri bulur.
        /// </summary>
        /// <returns></returns>
        public List<AdvertPhoto> GetAdvertPhotosNotInAdverts()
        {
            string sql = @" select ""AdvertPhotos"".* from ""AdvertPhotos"" left join ""Adverts"" on ""AdvertPhotos"".""AdvertID"" = ""Adverts"".""ID""  where ""Adverts"".""ID"" is NULL ";
            var result = GetConnection().Query<AdvertPhoto>(sql);
            return result.ToList();
        }
    }
}
