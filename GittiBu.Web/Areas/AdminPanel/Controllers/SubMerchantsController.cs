using System.Collections.Generic;
using GittiBu.Models;
using GittiBu.Services;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace GittiBu.Web.Areas.AdminPanel.Controllers
{
    public class SubMerchantsController : AdminBaseController
    {
        public IActionResult Index()
        {
            using (var service = new UserService())
            {
                var list = service.GetSubMerchants();
                return View(list);
            }
        }
        
        [HttpPost]
        public JsonResult Delete(int id)
        {
            using (var service = new UserService())
            {
                var delete = service.DeleteSubMerchant(id);
                if (!delete)
                    return Json(new {isSuccess = false, message = "Silme işlemi başarısız"});
                
                return Json(new {isSuccess = true, message = "Alt üye işyeri silindi."});
            }
        }
        public IActionResult ExcelExport(){
            string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            using(var package=new ExcelPackage())
            using(var service=new UserService()){
                var subMerchant=service.GetSubMerchants();
                var worksheet = package.Workbook.Worksheets.Add("SubMerchanExpert");
                var export=new List<UserSecurePaymentDetail>();
                foreach(var user in subMerchant){
                    export.Add(user);
                }
                worksheet.Cells["A1"].LoadFromCollection(export, true, TableStyles.Light8);
                for (var col = 1; col < 50 + 1; col++)
                {
                    worksheet.Column(col).AutoFit();
                }           
                return File(package.GetAsByteArray(), XlsxContentType, 
                    "altuye.xlsx");
            }
        }
        public IActionResult Details(int id)
        {
            using (var service = new UserService())
            {
                var subMerchant = service.GetSubMerchant(id);
                if (subMerchant == null)
                    return RedirectToAction("Index");

                return View(subMerchant);
            }
        }
    }
}