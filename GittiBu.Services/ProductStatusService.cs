using Dapper.FastCrud;
using GittiBu.Models;
using System.Collections.Generic;
using System.Linq;

namespace GittiBu.Services
{
    public class ProductStatusService : BaseService
    {
        public List<ProductStatus> GetAll()
        {
            return GetConnection().Find<ProductStatus>().ToList();
        }
    }
}
