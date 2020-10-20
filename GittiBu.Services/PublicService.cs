using System.Collections.Generic;
using System.Linq;
using GittiBu.Models;
using Dapper.FastCrud;

namespace GittiBu.Services
{
    public class PublicService : BaseService
    {
        public List<Country> GetCountries()
        {
            return GetConnection().Find<Country>().OrderBy(x=>x.Order).ToList();
        }
        
        public List<City> GetCities()
        {
            return GetConnection().Find<City>().OrderBy(x=>x.Order).ToList();
        }
        
        public List<District> GetDistricts()
        {
            return GetConnection().Find<District>().OrderBy(x=>x.Name).ToList();
        }

        public List<CargoArea> GetCargoAreas()
        {
            return GetConnection().Find<CargoArea>().OrderBy(x => x.Order).ToList();
        }
        public CargoArea GetCargoAreaById(int id)
        {
            return GetConnection().Find<CargoArea>().FirstOrDefault(p => p.ID == id);
        }
        public List<DopingType> GetDopingTypes() => GetConnection().Find<DopingType>().ToList();

    }
}