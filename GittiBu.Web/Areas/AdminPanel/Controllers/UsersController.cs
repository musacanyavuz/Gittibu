using System.Collections.Generic;
using System.IO;
using GittiBu.Common;
using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.Areas.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace GittiBu.Web.Areas.AdminPanel.Controllers
{
    public class UsersController : AdminBaseController
    {
 

        public IActionResult Index()
        {
           using (var service = new UserService())
            {
                // var users = service.GetAll();
                return View(null);
            }
        }

        public JsonResult GetList(Search search, int start, int length)
        {
            using (var service = new UserService())
            {
                var users = service.GetList(search?.Value, length, start);
                var count = service.GetUsersCount();
                var filteredCount = 0;
                if (!string.IsNullOrEmpty(search?.Value))
                    filteredCount = service.GetUsersCount(search?.Value);
                
                return Json(new
                {
                    recordsTotal = count,
                    recordsFiltered = filteredCount == 0 ? count : filteredCount,
                    data = users
                });
            }
        }
        
        [HttpPost]
        public JsonResult UpdateStatus(int id, bool status)
        {
            using (var service = new UserService())
            {
                var user = service.Get(id);
                if (user == null)
                {
                    return Json(new {isSuccess = false, message = "Kullanıcı bulunamadı"});
                }

                user.IsActive = status;
                //  service.DeleteUser(user.ID.ToString());
                var update = service.Update(user);              
                if (!update)
                    return Json(new {isSuccess = false, message = "Güncelleme başarısız"});
                
                return Json(new {isSuccess = true, message = "Kullanıcı aktiflik durumu güncellendi."});
            }
        }


        public IActionResult ExcelExport(){
            string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            using(var package=new ExcelPackage())
            using(var service=new UserService()){
                var users=service.GetAll();
                var worksheet = package.Workbook.Worksheets.Add("UserExpert");
                 var export=new List<User>();
                foreach(var user in users){
                    export.Add(user);
                }
                worksheet.Cells["A1"].LoadFromCollection(export, true, TableStyles.Light8);
                for (var col = 1; col < 50 + 1; col++)
                {
                    worksheet.Column(col).AutoFit();
                }           
                return File(package.GetAsByteArray(), XlsxContentType, 
                    "personelexpert.xlsx");
            }
        }
        public IActionResult Details(int id)
        {
            using (var service = new UserService())
            {
                var user = service.Get(id);
                if (user == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Kullanıcı bulunamadı.");
                    return RedirectToAction("Index");
                }

                var securePaymentInfo = service.GetSecurePaymentDetail(id);
                ViewBag.IBAN = securePaymentInfo?.IBAN;
                return View(user);
            }
        }

        public IActionResult Edit(User user)
        {
            using (var service = new UserService())
            {
                if (user.ID == 0)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Kullanıcı bulunamadı.");
                    return RedirectToAction("Index");
                }
                var _user = service.Get(user.ID);
                _user.UserName = user.UserName;
                _user.Email = user.Email;
                _user.Name = user.Name;
                _user.MobilePhone = user.MobilePhone;
                _user.TC = user.TC;
                _user.WebSite = user.WebSite;
                _user.About = user.About;
                _user.Role = user.Role;

                var update = service.Update(_user);
                if (!update)
                    AdminNotification = new UiMessage(NotyType.error, "Güncelleme başarısız.");
                else
                    AdminNotification = new UiMessage(NotyType.success, "Kullanıcı bilgileri güncellendi.");

                return RedirectToAction("Details", new {id = user.ID});
            }
        }

        public IActionResult ApproveIdentity(int id)
        {
            using (var service = new UserService())
            {
                var user = service.Get(id);
                user.IdentityPhotosApproved = true;
                if (service.Update(user))
                    AdminNotification = new UiMessage(NotyType.success, "Kullanıcı kimlik fotoğrafları onaylandı.");
                else
                    AdminNotification = new UiMessage(NotyType.error, "Kimlik fotoğrafları onaylama sırasında bir hata oluştu.");

                return RedirectToAction("Details", new {id});
            }
        }
        
        public IActionResult RejectIdentity(int id)
        {
            using (var service = new UserService())
            {
                var user = service.Get(id);
                if (user == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, id+" id'li kullanıcı bulunamadı.");
                    return RedirectToAction("Index");
                }
                var pathBase = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var frontPath = pathBase + user.IdentityPhotoFront;
                var backPath = pathBase + user.IdentityPhotoBack;
                
                user.IdentityPhotosApproved = false;
                user.IdentityPhotoBack = null;
                user.IdentityPhotoFront = null;

                if (service.Update(user))
                {
                    AdminNotification = new UiMessage(NotyType.success, "Kullanıcı kimlik fotoğrafları reddedildi.");
                    
                    if (System.IO.File.Exists(frontPath))
                        System.IO.File.Delete(frontPath);
                    if (System.IO.File.Exists(backPath))
                        System.IO.File.Delete(backPath);
                }
                else
                    AdminNotification = new UiMessage(NotyType.error, "Kimlik fotoğrafları reddetme sırasında bir hata oluştu.");

                return RedirectToAction("Details", new {id});
            }
        }
        
        public IActionResult ApproveIban(int id)
        {
            using (var service = new UserService())
            {
                var user = service.Get(id);
                user.IbanApproved = true;
                if (service.Update(user))
                    AdminNotification = new UiMessage(NotyType.success, "Kullanıcı IBAN bilgileri onaylandı.");
                else
                    AdminNotification = new UiMessage(NotyType.error, "IBAN bilgisi onaylama sırasında bir hata oluştu.");

                return RedirectToAction("Details", new {id});
            }
        }
        
        public IActionResult RejectIban(int id)
        {
            using (var service = new UserService())
            {
                var user = service.Get(id);
                user.IbanApproved = false;
                if (service.Update(user))
                    AdminNotification = new UiMessage(NotyType.success, "Kullanıcı IBAN bilgileri reddedildi.");
                else
                    AdminNotification = new UiMessage(NotyType.error, "IBAN bilgisi reddetme sırasında bir hata oluştu.");

                return RedirectToAction("Details", new {id});
            }
        }
    }
}