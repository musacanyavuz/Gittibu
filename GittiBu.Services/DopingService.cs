using System;
using System.Collections.Generic;
using System.Linq;
using GittiBu.Models;
using Dapper.FastCrud;

namespace GittiBu.Services
{
    public class DopingService : BaseService
    {
        public List<AdvertDoping> GetDopingHistory(int userId)
        {
            try
            {
                return GetConnection().Find<AdvertDoping>(s => s
                    .Include<Advert>(j => j.Where($"{nameof(Advert.UserID):C}=@userId"))
                    .Include<DopingType>()
                    .WithParameters(new {userId})
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public DopingType Get(int id)
        {
            return GetConnection().Get(new DopingType() {ID = id});
        }

        public bool Update(DopingType doping)
        {
            return GetConnection().Update(doping);
        }
    }
}