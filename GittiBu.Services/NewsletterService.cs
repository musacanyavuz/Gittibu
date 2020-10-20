using GittiBu.Models;
using Dapper.FastCrud;

namespace GittiBu.Services
{
    public class NewsletterService : BaseService
    {
        public bool Insert(NewsletterSubscriber newsletterSubscriber)
        {
            try
            {
                GetConnection().Insert(newsletterSubscriber);
                return newsletterSubscriber.ID > 0;
            }
            catch (System.Exception)
            {
                return false;                
            }
        }
    }
}