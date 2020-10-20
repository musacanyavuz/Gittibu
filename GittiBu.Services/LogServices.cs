using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Dapper.FastCrud;
using GittiBu.Models;

namespace GittiBu.Services
{
    public class LogServices: BaseService
    {

        public bool Insert(Log log)
        {
            try
            { 
                GetConnection().Insert(log);
                return true;
            }
            catch (Exception e)
            {
                return false; 
            }
        }

        public List<Log> GetLog500()
        {
             string sql = "Select * from \"Logs\" WHERE \"CreatedDate\" is not null  ORDER BY \"CreatedDate\" desc limit 500 ";
             return GetConnection().Query<Log>(sql).ToList();
        }
    }
}