using GittiBu.Models;
using GittiBu.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Reflection;

namespace GittiBu.Web.CustomProviders
{
    public class DatabaseFileProvider : IFileProvider
    {
        IHttpContextAccessor _httpContext;
        public DatabaseFileProvider(IHttpContextAccessor httpContext)
        {
            
            this._httpContext = httpContext;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return null;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
           
            string ln = "tr";
            try
            {
                if (_httpContext.HttpContext != null)
                {
                    ln = _httpContext.HttpContext.Session.GetString("lang");
                }

                var result = new DatabaseFileInfo(subpath, ln);
                
                return result.Exists ? result as IFileInfo : new NotFoundFileInfo(subpath);
            }
            catch (Exception ex)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        IsError = true,
                        Function = "DatabaseFileProvider.GetFileInfo",
                        Message =ex.Message,
                       Detail = ex.ToString(),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }
                throw;
            }
           
        }

        public IChangeToken Watch(string filter)
        {
            return new DatabaseChangeToken( filter);
        }
    }
}
