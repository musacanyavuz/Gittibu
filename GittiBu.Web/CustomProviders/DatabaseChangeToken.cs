using Microsoft.Extensions.Primitives;
using System;
using System.Data.SqlClient;

namespace GittiBu.Web.CustomProviders
{
    public class DatabaseChangeToken : IChangeToken
    {
        
        private string _viewPath;

        public DatabaseChangeToken(string viewPath)
        {
            
            _viewPath = viewPath;
        }

        public bool ActiveChangeCallbacks => false;

        public bool HasChanged
        {
            get
            {
                return true;
               
               
            }
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => EmptyDisposable.Instance;
    }

    internal class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new EmptyDisposable();
        private EmptyDisposable() { }
        public void Dispose() { }
    }
}
