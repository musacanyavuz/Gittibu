using System;
using System.Collections.Generic;
using System.Linq;
using GittiBu.Common;
using GittiBu.Models;
using Dapper;
using Dapper.FastCrud;
using Npgsql;
using System.Data;
using Microsoft.AspNetCore.Hosting;

namespace GittiBu.Services
{
    public class UserService : BaseService
    {
        #region UserBase
        public UserService(IHostingEnvironment _env):base(_env)
        {

        }
        public UserService()
        {

        }
        public List<User> GetAll()
        {
            return GetConnection().Find<User>().ToList();
        }

        public List<User> GetList(string search, int count, int offset)
        {
            var sql = $"select * from Users order by ID desc limit @count offset @offset";
            if (!string.IsNullOrEmpty(search))
            {
                sql = $"select * from Users " +
                      $"where UserName like @search or Email like @search or MobilePhone like @search\nor Name like @search or cast(ID as CHAR(10)) like @search  " +
                      $"order by ID desc limit @count offset @offset";
            }
            return GetConnection().Query<User>(sql, new { search = "%" + search + "%", count, offset }).ToList();
        }

        public int GetUsersCount(string search = null)
        {
            if (string.IsNullOrEmpty(search))
            {
                return GetConnection().Query<int>($"select count(*) from Users ").First();
            }

            var sql = $"select count(*) from Users " +
                      $"where UserName like @search or Email like @search or MobilePhone like @search\nor Name like @search or cast(ID as CHAR(10)) like @search  ";
            var count = GetConnection().Query<int>(sql, new { search = "%" + search + "%" }).First();
            return count;
        }

        public bool DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }
          return  GetConnection().Delete(Get(Convert.ToInt32(userId)));
        }

        public void DeleteSecurePaymentDetail(string userId)
        {
            GetConnection().Delete(Get(id: Convert.ToInt32(userId)));
        }
        public bool Insert(User user)
        {
            try
            {
                GetConnection().Insert(user);
                return user.ID > 0;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "UserService.Insert",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params = user.Email 
                });
                return false;
            }
        }

        public User Get(int id)
        {
            try
            {
                var sql = $"select * from Users\nleft outer join Countries on CountryID = Countries.ID\nleft outer join Cities on CityID = Cities.ID\nleft outer join Districts on DistrictID = Districts.ID\nwhere Users.ID = @id";
                return GetConnection().Query<User, Country, City, District, User>(sql,
                    (user, country, city, district) =>
                    {
                        user.Country = country;
                        user.City = city;
                        user.District = district;
                        return user;
                    }, new { id }, splitOn: "ID")
                .SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public User Get(string userName, string password)
        {
            try
            {
                string sql = $"select * from Users\nwhere (lower(Email) = lower(@userName) or lower(UserName) = lower(@userName))\nand Password = @password";
                if(isDev)
                     sql = $"select * from Users\nwhere (lower(Email) = lower(@userName) or lower(UserName) = lower(@userName)) " ;
                return GetConnection().Query<User>(sql, new
                {
                    userName,
                    password
                }).SingleOrDefault();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "UserService.Get1",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params = userName + " " + password
                });
                return null;
            }
        }

        public User Get(string userNameOrEmail)
        {
            const string sql = "select * from Users where lower(UserName) = lower(@userNameOrEmail) " +
                               "or lower(Email) = lower(@userNameOrEmail) ";
            try
            {
                return GetConnection().Query<User>(sql, new
                {
                    userNameOrEmail
                }).FirstOrDefault();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "UserService.Get2",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params = userNameOrEmail
                });
                return null;
            }
        }

        public User GetFromToken(string token)
        {
            try
            {
                return GetConnection()
                    .Find<User>(s => s.Where($"{nameof(User.Token):C}=@token").WithParameters(new { token }))
                    .SingleOrDefault();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "UserService.GetFromToken",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params = token 
                });
                return null;
            }
        }

        public User GetFromTC(string tc)
        {
            try
            {
                return GetConnection()
                    .Find<User>(s => s.Where($"{nameof(User.TC):C}=@tc").WithParameters(new { tc }))
                    .SingleOrDefault();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "UserService.GetFromTC",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params = tc 
                });
                return null;
            }
        }


        public User GetFromExternalLoginId(short oauth_provider, string oauth_id)
        {
            try
            {
                var sql = $"select * from Users where OAuth_Provider = @oauth_provider and OAuth_Uid = @oauth_id";
                return GetConnection().Query<User>(sql, new
                {
                    oauth_provider,
                    oauth_id
                }).FirstOrDefault();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "UserService.GetFromExternalLoginId",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params =  oauth_provider.ToString() + " " + oauth_id 
                });
                return null;

            }
        }

        public bool Update(User user)
        {
            try
            {
                return GetConnection().Update(user);
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "UserService.Update",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params = user.ID.ToString()
                });
                return false;
            }
        }

        public bool EmailIsUsable(string email, int userId)
        {
            if (string.IsNullOrEmpty(email))
                return false;
            const string sql = "select * from Users where lower(Email)= lower(@email) and ID<>@userId ";
            return GetConnection().Query<User>(sql, new { email, userId }).ToList().Count == 0;
        }

        public bool UsernameIsUsable(string username, int userId)
        {
            if (string.IsNullOrEmpty(username))
                return false;
            const string sql = "select * from Users where lower(UserName)= lower(@username) and ID<>@userId ";
            return GetConnection().Query<User>(sql, new { username, userId }).ToList().Count == 0;
        }

        #endregion


        #region Adresler

        public List<UserAddress> GetUserAddresses(int userId)
        {
            try
            {
                var sql = $"select * from UserAddresses\nleft outer join Countries on UserAddresses.CountryID=Countries.ID\nleft outer join Cities on UserAddresses.CityID=Cities.ID\nleft outer join Districts on UserAddresses.DistrictID=Districts.ID\nwhere UserAddresses.UserID=@userId";
                return GetConnection().Query<UserAddress, Country, City, District, UserAddress>(sql,
                    (address, country, city, district) =>
                {
                    address.Country = country;
                    address.City = city;
                    address.District = district;
                    return address;
                }, new { userId }, splitOn: "ID").ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public UserAddress GetUserAddress(int userId, int addressId)
        {
            try
            {
                var sql = $"select * from UserAddresses\nleft outer join Countries on UserAddresses.CountryID=Countries.ID\nleft outer join Cities on UserAddresses.CityID=Cities.ID\nleft outer join Districts on UserAddresses.DistrictID=Districts.ID\nwhere UserAddresses.ID=@addressId";
                return GetConnection().Query<UserAddress, Country, City, District, UserAddress>(sql,
                    (address, country, city, district) =>
                {
                    address.Country = country;
                    address.City = city;
                    address.District = district;
                    return address;
                }, new { addressId }, splitOn: "ID").SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool InsertAddress(UserAddress address)
        {
            try
            {
                GetConnection().Insert(address);
                return address.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool UpdateAddress(UserAddress address)
        {
            try
            {
                return GetConnection().Update(address);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool DeleteAddress(int addressId, int userId)
        {
            try
            {
                var sql = "delete from UserAddresses where UserID=@userId and ID=@addressId ";
                return GetConnection().Execute(sql, new { userId, addressId }) > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion

        #region GuvenliOdemeBilgileri

        public UserSecurePaymentDetail GetSecurePaymentDetail(int userId, bool IsActive = true)
        {
            try
            {
                var sql = "select * from UserSecurePaymentDetails where UserID=@userId AND IsActive=@IsActive";
                return GetConnection().Query<UserSecurePaymentDetail>(sql, new { userId, IsActive }).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool InsertSecurePaymentDetail(UserSecurePaymentDetail userSecurePaymentDetail)
        {
            try
            {
                GetConnection().Insert(userSecurePaymentDetail);
                return userSecurePaymentDetail.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool UpdateSecurePaymentDetail(UserSecurePaymentDetail userSecurePaymentDetail)
        {
            try
            {
                return GetConnection().Update(userSecurePaymentDetail);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion

        #region MyLikes

        public List<AdvertLike> GetMyLikes(int userId)
        {
            try
            {
                return GetConnection().Find<AdvertLike>(s => s
                    .Where($"{nameof(AdvertLike.UserID):C}=@userId")
                    .Include<Advert>(j => j.Where($"{nameof(Advert.IsActive):C}=true"))
                    .WithParameters(new { userId })
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public AdvertLike GetAdvertLike(int id)
        {
            try
            {
                return GetConnection().Get(new AdvertLike() { ID = id });
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool DeleteAdvertLike(AdvertLike advertLike)
        {
            try
            {
                return GetConnection().Delete(advertLike);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion

        public List<PaymentRequest> GetBuys(int userId)
        {
            try
            {
                var sql = "select * from PaymentRequests\n  left outer join Adverts on PaymentRequests.AdvertID=Adverts.ID\n                left outer join Users on PaymentRequests.SellerID = Users.ID\nwhere PaymentRequests.Type=@type and IsSuccess=true\nand PaymentRequests.UserID = @userId order by PaymentRequests desc";

                var result = GetConnection().Query<PaymentRequest, Advert, User, PaymentRequest>(sql,
                    (request, advert, seller) =>
                    {
                        request.Advert = advert;
                        request.Seller = seller;
                        return request;
                    }, new { type = Enums.PaymentType.Ilan, userId }, splitOn: "ID").ToList();

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<PaymentRequest> GetSales(int userId)
        {
            try
            {
                const string sql = "select * from PaymentRequests\n  left outer join Adverts on PaymentRequests.AdvertID=Adverts.ID                left outer join Users on PaymentRequests.UserID = Users.ID where PaymentRequests.Type=@type and IsSuccess=true and PaymentRequests.SellerID = @userId order by PaymentRequests.ID desc";

                var result = GetConnection().Query<PaymentRequest, Advert, User, PaymentRequest>(sql,
                    (request, advert, buyer) =>
                    {
                        request.Advert = advert;
                        request.Buyer = buyer;
                        return request;
                    }, new { type = Enums.PaymentType.Ilan, userId }, splitOn: "ID").ToList();

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public PaymentRequest GetSale(int id, int userId)
        {
            try
            {
                var sql = "select * from PaymentRequests\n  left outer join Adverts on " +
                          "PaymentRequests.AdvertID=Adverts.ID                " +
                          "left outer join Users on PaymentRequests.UserID = Users.ID " +
                          "where  PaymentRequests.Type=@type and IsSuccess=true " +
                          "and PaymentRequests.SellerID = @userId and PaymentRequests.ID=@id ;";
                //                
                var result = GetConnection().Query<PaymentRequest, Advert, User, PaymentRequest>(sql,
                    (request, advert, buyer) =>
                    {

                        request.Advert = advert;
                        request.Buyer = buyer;
                        return request;
                    }, new { type = Enums.PaymentType.Ilan, userId, id }, splitOn: "ID").SingleOrDefault();


                var sqlSeller = "select * from Users where ID=@userId";
                result.Seller = GetConnection().Query<User>(sqlSeller, new { userId }).SingleOrDefault();
                return result;
                //                var result = GetConnection().QueryMultiple(sql, new {type = Enums.PaymentType.Ilan, userId, id});
                //
                //                var payment = result.ReadSingle<PaymentRequest>();
                //                payment.Seller = result.ReadSingle<User>();
                //                return payment;


                /*
                return GetConnection().Find<PaymentRequest>(s => s
                    .Where($"{nameof(PaymentRequest.ID):C}=@id and {nameof(PaymentRequest.SellerID):C}=@userId")
                    .WithParameters(new {id, userId})
                ).SingleOrDefault();
                */
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool BulkInsert(List<User> users)
        {
            try
            {
                GetConnection().Open();
                IDbTransaction trans = GetConnection().BeginTransaction();

                GetConnection().Execute(" insert into Users " +
                "(ID, UserName, Name, Email, GenderID, Password, IsActive, LastLoginDate, " +
                " MobilePhone, WorkPhone, BirthDate, WebSite, About, LanguageID, " +
                " CreatedDate, InMailing, ProfilePicture, TC, CityID, DistrictID," +
                " CountryID, Role " +
                ") " +
                "values( @ID, @UserName, @Name, @Email, @GenderID, @Password, @IsActive, @LastLoginDate, " +
                "@MobilePhone, @WorkPhone, @BirthDate, @WebSite, @About, @LanguageID, @CreatedDate, @InMailing, " +
                "@ProfilePicture, @TC, @CityID, @DistrictID, @CountryID, @Role )", users, transaction: trans);

                trans.Commit();
                GetConnection().Close();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public List<UserSecurePaymentDetail> GetSubMerchants()
        {
            return GetConnection().Find<UserSecurePaymentDetail>(s => s
                .Include<User>()
            ).ToList();
        }

        public UserSecurePaymentDetail GetSubMerchant(int id)
        {
            return GetConnection().Find<UserSecurePaymentDetail>(s => s
                .Where($"{nameof(UserSecurePaymentDetail.ID):C}=@id")
                .WithParameters(new { id })
                .Include<User>()
            ).SingleOrDefault();
        }

        public bool DeleteSubMerchant(int id)
        {
            return GetConnection().Delete(new UserSecurePaymentDetail() { ID = id });
        }

        public List<User> GetVerifyPendingUsers()
        {
            try
            {
                //const string sql = "select * from Users where\n\n  (IdentityPhotoFront is not null and IdentityPhotosApproved = false)\nOR ((select IBAN from UserSecurePaymentDetails where UserID = Users.ID) is not null\n    and IbanApproved = false\n    )";

                //const string sql = "select Users.*,UserSecurePaymentDetails.IBAN from Users,UserSecurePaymentDetails where (Users.IdentityPhotoFront is not null and Users.IdentityPhotosApproved = false) OR (UserSecurePaymentDetails.UserID = Users.ID and  Users.IbanApproved = false)";

                const string sql = "select u.* from Users u inner join UserSecurePaymentDetails us  on u.ID = us.UserID and IBAN is not null where (u.IdentityPhotoFront is not null and IdentityPhotosApproved = false) OR (u.IbanApproved = false) GROUP BY u.ID";
                var list = GetConnection().Query<User>(sql).ToList();
                return list;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}