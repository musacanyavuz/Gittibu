using System;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using System.Text;
using GittiBu.Services;
using GittiBu.Models;
using GittiBu.Common;
using Microsoft.AspNetCore.Http;

namespace GittiBu.Web.CustomProviders
{
    public class DatabaseFileInfo : IFileInfo
    {
        private string _viewPath;
        private byte[] _viewContent;
        private DateTimeOffset _lastModified;
        private bool _exists = false;
       

        public DatabaseFileInfo(string viewPath,string lang)
        {
            
            _viewPath = viewPath;
            getView(viewPath,lang);
        }
        public bool Exists => _exists;

        public bool IsDirectory => false;

        public DateTimeOffset LastModified => _lastModified;

        public long Length
        {
            get
            {
                using (var stream = new MemoryStream(_viewContent))
                {
                    return stream.Length;
                }
            }
        }

        public string Name => Path.GetFileName(_viewPath);

        public string PhysicalPath => null;

        public Stream CreateReadStream()
        {
            return new MemoryStream(_viewContent);
        }

        private void getView(string viewPath,string lang)
        {
            try
            {
                using (var service = new TextService())
                {
                   
                    int langint = Constants.GetLang(lang);
                    Content content = service.GetContentByViewPath(viewPath, langint);
                    
                    if (content != null)
                    {
                        _exists = true;
                        _viewContent = Encoding.UTF8.GetBytes(content.TextContent);
                        _lastModified = content.LastModifiedDate == default(DateTime) || content.LastModifiedDate == null ? DateTime.Now : content.LastModifiedDate;
                    }
                    else
                    {

                        _exists = false;
                    }
                   


                }
            }
            catch (Exception ex)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        IsError=true,
                        Function= "getView",
                        Message = ex.Message,
                        Detail = ex.ToString(),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }
                throw;
            }
           

        }

    }
}
