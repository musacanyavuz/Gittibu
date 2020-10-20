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
        public List<AdvertPhoto> GetAdvertPhotosByAdvertID(int advertId)
        {
            return GetConnection().Find<AdvertPhoto>(s => s
                    .Where($"{nameof(AdvertPhoto.AdvertID):C}=@advertid ")
                    .WithParameters(new { advertId })).ToList();
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
        public List<AdvertPhoto> GetAdvertPhotosNotInAdverts()
        {
            string sql = @" select ""AdvertPhotos"".* from ""AdvertPhotos"" left join ""Adverts"" on ""AdvertPhotos"".""AdvertID"" = ""Adverts"".""ID""  where ""Adverts"".""ID"" is NULL ";
            var result = GetConnection().Query<AdvertPhoto>(sql);
            return result.ToList();
        }
    }
}
