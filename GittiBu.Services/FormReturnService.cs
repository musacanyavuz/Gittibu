using System;
using GittiBu.Models;
using Dapper.FastCrud;

namespace GittiBu.Services
{
    public class FormReturnService : BaseService
    {
        public bool Insert(FormReturn formReturn)
        {
            try
            {
                GetConnection().Insert(formReturn);
                return formReturn.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}