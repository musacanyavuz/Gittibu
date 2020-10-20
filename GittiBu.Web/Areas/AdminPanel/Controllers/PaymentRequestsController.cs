using GittiBu.Common;
using GittiBu.Common.Iyzico;
using GittiBu.Services;
using Microsoft.AspNetCore.Mvc;

namespace GittiBu.Web.Areas.AdminPanel.Controllers
{
    public class PaymentRequestsController : AdminBaseController
    {
        public IActionResult Index()
        {
            using (var service = new PaymentRequestService())
            {
                var list = service.GetAll();
                return View(list);
            }
        }

        public IActionResult Details(int id)
        {
            using (var service = new PaymentRequestService())
            {
                var pr = service.GetOrder(id);
                return View(pr);
            }
        }
        [HttpPost]
        public JsonResult PaymentRefund(int id)
        {
            using (var service = new PaymentRequestService())
            {
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new {isSuccess = false, message = "Ödeme kaydı bulunamadı."});

                if (string.IsNullOrEmpty(pr.PaymentTransactionID))
                    return Json(new {isSuccess = false, 
                        message = "Iyzico transaction ID geçersiz. Bu online ödeme tamamlanmamış."});
                
                var refund = IyzicoService.PaymentRefund(pr.ID, pr.PaymentTransactionID, GetIpAddress(),
                    pr.Price.ToString());
                if (refund.Status != "success")
                {
                    return Json(new {isSuccess = false, 
                        message = "Ödeme iadesi yapılamadı. "+refund.ErrorMessage});
                }

                pr.Status = Enums.PaymentRequestStatus.Iptal;
                service.Update(pr);
                
                return Json(new {isSuccess = true, 
                    message = "İade işlemi yapıldı. Ödeme kaydı iptal edildi."});
            }
        }
    }
}