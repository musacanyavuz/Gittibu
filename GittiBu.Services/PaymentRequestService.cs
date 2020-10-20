using System;
using System.Collections.Generic;
using System.Linq;
using GittiBu.Common;
using GittiBu.Models;
using Dapper;
using Dapper.FastCrud;

namespace GittiBu.Services
{
    public class PaymentRequestService : BaseService
    {
        public PaymentRequest Get(int id)
        {
            return GetConnection().Get(new PaymentRequest() { ID = id });
        }

        public PaymentRequest GetOrder(int id)
        {
            try
            {
              //  var sql = @"select * from ""PaymentRequests"" left outer join ""Adverts"" on ""PaymentRequests"".""AdvertID""=""Adverts"".""ID""\nleft outer join  ""Users"" as ""Buyer"" on ""PaymentRequests"".""UserID""=""Buyer"".""ID""\nleft outer join ""Users"" as ""Seller"" on ""PaymentRequests"".""SellerID""=""Seller"".""ID""\nwhere ""PaymentRequests"".""ID"" = " + id;
                var sql = $"select * from \"PaymentRequests\"\nleft outer join \"Adverts\" on \"PaymentRequests\".\"AdvertID\"=\"Adverts\".\"ID\"\nleft outer join  \"Users\" as \"Buyer\" on \"PaymentRequests\".\"UserID\"=\"Buyer\".\"ID\"\nleft outer join \"Users\" as \"Seller\" on \"PaymentRequests\".\"SellerID\"=\"Seller\".\"ID\"\nwhere \"PaymentRequests\".\"ID\" = @id";
                var paymentRequest= GetConnection().Query<PaymentRequest, Advert, User, User, PaymentRequest>(sql,
                    (pr, ad, buyer, seller) =>
                    {
                        pr.Advert = ad;
                        pr.Buyer = buyer;
                        pr.Seller = seller;
                        return pr;
                    }, new { id }, splitOn: "ID")
                .SingleOrDefault();
                if (paymentRequest.Advert == null)
                    paymentRequest.Advert = new Advert();
                return paymentRequest;
            }
            catch (Exception ex)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        IsError = true,
                        Function = "GetOrder(int id)",
                        Message = ex.Message,
                        Detail = ex.ToString(),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }
                return null;
            }
        }

        public List<PaymentRequest> GetAll()
        {
            try
            {
                var sql = $"select * from \"PaymentRequests\"\nleft outer join \"Adverts\" on \"PaymentRequests\".\"AdvertID\"=\"Adverts\".\"ID\"\nleft outer join  \"Users\" as \"Buyer\" on \"PaymentRequests\".\"UserID\"=\"Buyer\".\"ID\"\nleft outer join \"Users\" as \"Seller\" on \"PaymentRequests\".\"SellerID\"=\"Seller\".\"ID\"";
                return GetConnection().Query<PaymentRequest, Advert, User, User, PaymentRequest>(sql,
                        (pr, ad, buyer, seller) =>
                        {
                            pr.Advert = ad;
                            pr.Buyer = buyer;
                            pr.Seller = seller;
                            return pr;
                        }, splitOn: "ID")
                    .ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool Insert(PaymentRequest paymentRequest)
        {
            try
            {
                GetConnection().Insert(paymentRequest);
                return paymentRequest.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Update(PaymentRequest paymentRequest)
        {
            try
            {
                return GetConnection().Update(paymentRequest);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public List<PaymentRequest> GetWaitingPaymentRequests()
        {
            try
            {
                var sql = "select * from \"PaymentRequests\"\nLEFT OUTER JOIN \"Users\" as \"user\" on \"user\".\"ID\"=\"PaymentRequests\".\"UserID\"\nLEFT OUTER JOIN \"Users\" as \"seller\" on \"seller\".\"ID\"=\"PaymentRequests\".\"SellerID\"\n" +
                          "where " +
                          "\"PaymentRequests\".\"Status\"=" + (int)Enums.PaymentRequestStatus.Bekleniyor + " or " +
                          "\"PaymentRequests\".\"Status\"=" + (int)Enums.PaymentRequestStatus.OnlineOdemeYapildi + " or " +
                          "\"PaymentRequests\".\"Status\"=" + (int)Enums.PaymentRequestStatus.AliciIptalTalebiOlusturdu + " or " +
                          "\"PaymentRequests\".\"Status\"=" + (int)Enums.PaymentRequestStatus.KargoyaVerildi + " or " +
                          "\"PaymentRequests\".\"Status\"=" + (int)Enums.PaymentRequestStatus.Reddedildi + " or " +
                          "\"PaymentRequests\".\"Status\"=" + (int)Enums.PaymentRequestStatus.Onaylandi + " or " +
                          "\"PaymentRequests\".\"Status\"=" + (int)Enums.PaymentRequestStatus.AliciIptalEtti + " " +
                          "order by \"PaymentRequests\".\"CreatedDate\" desc";

                var query = GetConnection().Query<PaymentRequest, User, User, PaymentRequest>
                (sql, (request, user, seller) =>
                {
                    request.Buyer = user;
                    request.Seller = seller;
                    return request;
                }, splitOn: "ID")
                    .ToList();

                return query;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool Delete(PaymentRequest pr)
        {
            return GetConnection().Delete(pr);
        }

        public List<PaymentRequest> GetAutoTransferOrders(int days)
        {
            //Enums.PaymentRequestStatus.KargoyaVerildi

            var sql =
                $"select * from \"PaymentRequests\"\nwhere\n\"Status\"= {(int)Enums.PaymentRequestStatus.KargoyaVerildi}\nAND DATE_PART('day', now() - \"CargoDate\") = {days}\nAND \"IsSuccess\" = true";
            var query = GetConnection().Query<PaymentRequest>(sql).ToList();
            return query;
        }

        public List<PaymentRequest> GetNotificationAutoTransferOrders(int days)
        {
            var sql =
                $"select * from \"PaymentRequests\", \"Users\" where  \"Status\"= {(int)Enums.PaymentRequestStatus.KargoyaVerildi}\nAND DATE_PART('day', now() - \"CargoDate\") = {days}\n AND \"PaymentRequests\".\"UserID\"=\"Users\".\"ID\" AND \"IsSuccess\" = true";
            var query = GetConnection().Query<PaymentRequest, User, PaymentRequest>(sql, (request, user) =>
            {
                request.Buyer = user;
                return request;
            }).ToList();
            return query;
        }

        public IList<PaymentRequest> GetPayoutTransactionOrders()
        {
            var sql =
            $"select * from \"PaymentRequests\", \"Users\", \"Adverts\" where \"Type\" = {(int)Enums.PaymentType.Ilan}\n and (\"Status\"= {(int)Enums.PaymentRequestStatus.Onaylandi}\n or \"Status\"= {(int)Enums.PaymentRequestStatus.OtomatikOnaylandi})   AND \"PaymentRequests\".\"SellerID\"=\"Users\".\"ID\" AND \"PaymentRequests\".\"AdvertID\"=\"Adverts\".\"ID\" AND \"IsSuccess\" = true";
            var query = GetConnection().Query<PaymentRequest, User, Advert, PaymentRequest>(sql, (request, user, advert) =>
             {
                 request.Seller = user;
                 request.Advert = advert;
                 return request;
             }).ToList();
            return query;
        }
    }
}