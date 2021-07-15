using Dapper;
using Dapper.FastCrud;
using GittiBu.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GittiBu.Services
{
   public class ParsesCategoryMatchService : BaseService
    {
        public List<ParsesCategoryMatch> GetList(int parsesID)
        {
            try
            {
                return GetConnection().Find<ParsesCategoryMatch>(s => s
                   .Where($"{nameof(ParsesCategoryMatch.ParsesID):C}=@parsesID")
                   .WithParameters(new { parsesID })
               ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }

          //  return GetConnection().Query<ParsesCategoryMatch>(sql, new { search = "%" + search + "%", count, offset }).ToList();
        }
        public bool Insert(ParsesCategoryMatch parseCategoryMatch)
        {
            try
            {
                parseCategoryMatch.CreateDate = DateTime.Now;
                GetConnection().Insert(parseCategoryMatch);
                return parseCategoryMatch.ID > 0;
            }
            catch (Exception e)
            {
                string jsonString;
                jsonString = JsonConvert.SerializeObject(parseCategoryMatch);
                Log(new Log
                {
                    Function = "ParsesCategoryMatchService.Insert",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString() + " \n \n" + jsonString,
                    IsError = true
                });
                return false;
            }
        }
        public int DeleteByParsesID(int ParsesID)
        {
            //  var listToDelete = GetList(id);
            var sql = "delete from ParsesCategoryMatches where ParsesID=@ParsesID ";
            return GetConnection().Execute(sql, new { ParsesID });
           // int rval=  GetConnection().BulkDelete<ParsesCategoryMatch>(statement => statement.Where($"ParsesID=@ParsesID").WithParameters(ParsesID));
           
           // return rval;
        }
    }
}
