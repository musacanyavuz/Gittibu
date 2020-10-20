using Dapper.FastCrud;
using GittiBu.Models;
using System.Collections.Generic;
using System.Linq;

namespace GittiBu.Services
{
    public class CargoService : BaseService
    {
        public List<CargoArea> GetAll()
        {
            return GetConnection().Find<CargoArea>().OrderBy(s => s.Order).ToList();
        }
    }
}
