using GittiBu.Models;
using GittiBu.Services;
using GittiBu.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GittiBu.Web.Areas.AdminPanel.Controllers
{
    public class ParseController : AdminBaseController
    {
        public IActionResult Index()
        {
            List<Parse> parseList = new List<Parse>();
            using (var service = new ParseService())
            {
                parseList = service.GetAll();
            }

            return View(parseList);
        }

        [HttpPost]
        public async Task<JsonResult> UpdateStatus(int id, bool status)
        {
            using (var service = new ParseService())
            {
                var parse = service.Get(id);
                if (parse == null)
                {
                    return Json(new { isSuccess = false, message = "XML dosyası bulunamadı" });
                }
                bool isOk = false;
                if (status)
                {
                    GittiBu.Web.Controllers.ParseController parseController = new GittiBu.Web.Controllers.ParseController(new MyMemoryCache());
                    await parseController.Edit(parse);
                    isOk = true;
                }
                else
                {
                    Parse parseFile = service.DeleteParseFileWithAdverts(id);
                    isOk = parseFile != null;
                }

                if (!isOk)
                    return Json(new { isSuccess = false, message = "Güncelleme başarısız" });

                return Json(new { isSuccess = true, message = "XML dosyasının aktiflik durumu güncellendi." });
            }
        }
    }
}