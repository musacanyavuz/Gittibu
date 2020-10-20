using GittiBu.Common;
using GittiBu.Common.Iyzico;
using GittiBu.Models;
using GittiBu.Services;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GittiBu.BackgroundJob
{
    public class AutoTransferJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            var workingJob = "AutoTransferJob Çalışıyor. - " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            WriteToFile(workingJob);

            using (var setService = new SystemSettingService())
            using (var service = new PaymentRequestService())
            {
                var settings = setService.GetSystemSettings();
                var days = int.Parse(settings.Single(s => s.Name == Enums.SystemSettingName.OtomatikOdemeOnayi).Value);
                //var days = 7;
                var admins = settings.SingleOrDefault(s => s.Name == Enums.SystemSettingName.YoneticiMailleri)?.Value;

                var notifyOrders = service.GetNotificationAutoTransferOrders(days);
                Before_AutoPaymentTransfer(notifyOrders);

                var orders = service.GetAutoTransferOrders(days);
                AutoPaymentTransfer(orders, admins);
            }

            var endWorkingJob = "AutoTransferJob Tamamlandı. - " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            WriteToFile(endWorkingJob);

            return Task.CompletedTask;
        }

        void Before_AutoPaymentTransfer(List<PaymentRequest> notifyOrders)
        {
            using (var notifyService = new NotificationService())
            {
                if (notifyOrders == null || !notifyOrders.Any()) return;
                foreach (var notifyOrder in notifyOrders)
                {
                    var buyer = notifyOrder.Buyer.Name;
                    var orderDate = notifyOrder.CreatedDate.ToString("dd.MM.yyyy");
                    var autoTransferDate = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");

                    var notify = new Notification
                    {
                        Message = new Localization().Get(
                            $"Merhaba {buyer}, {orderDate} tarihli #{notifyOrder.ID} sipariş kodlu alışverişiniz yarın({autoTransferDate}) otomatik olarak onaylanacak. Ürün henüz size ulaşmadıysa lütfen bizimle iletişime geçiniz.",
                            $"Hello {buyer}, your purchase of #{notifyOrder.ID} code dated {orderDate} will be approved automatically ({autoTransferDate}) tomorrow. If the product has not yet arrived, please contact us.",
                            notifyOrder.Buyer.LanguageID),
                        CreatedDate = DateTime.Now,
                        UserID = notifyOrder.Buyer.ID,
                        Url = Constants.GetURL(Enums.Routing.Alislarim, notifyOrder.Buyer.LanguageID),
                        TypeID = Enums.NotificationType.SistemMesaji
                    };
                    var notifyInsert = notifyService.Insert(notify);

                    Console.WriteLine(!notifyInsert
                        ? $"#{notifyOrder.ID} ID'li sipariş için otomatik onay öncesi alıcıya OTOMATİK ONAY ÖNCESİ HABER VERME maili gönderilemedi."
                        : $"Otomatik onaylama öncesi kullanıcıya bilgi maili gönderildi. İşlem: #{notifyOrder.ID} Kullanıcı: #{notifyOrder.Buyer.ID} {buyer} {notifyOrder.Buyer.Email}");
                }
            }
        }

        void AutoPaymentTransfer(List<PaymentRequest> orders, string admins)
        {
            using (var mailing = new MailingService())
            using (var service = new PaymentRequestService())
            {
                if (orders == null || !orders.Any()) return;
                Console.WriteLine($"OTOMATİK TRANSFERİ YAPILACAK İŞLEM SAYISI: {orders.Count}");

                foreach (var pr in orders)
                {
                    var approval = IyzicoService.PaymentApproval(pr.ID, pr.PaymentTransactionID);
                    if (approval.Status == "success")
                    {
                        Console.WriteLine("OTOMATİK TRANSFER YAPILDI | ID: " + pr.ID + " | PaymentTransactionID: " + pr.PaymentTransactionID);
                        pr.Status = Enums.PaymentRequestStatus.OtomatikOnaylandi;
                        pr.IsSuccess = true;
                        pr.UpdatedUserID = -1;
                        var update = service.Update(pr);
                        if (!update)
                            Console.WriteLine("VERİ TABANI GÜNCELLEME HATASI");
                        mailing.SendMail2Admins(admins, pr.ID + "ID'li sipariş için OTOMATİK Onay VERİLDİ",
                            $"Sipariş No: #<a href=\"https://www.gittibu.com/AdminPanel/PaymentRequests/Details/{pr.ID}\" target=\"_blank\">{pr.ID}</a>  ID'li sipariş için otomatik onay verildi. İşlem Tarihi: {DateTime.Now:g}  <br /> ");
                    }
                    else
                    {
                        Console.WriteLine("OTOMATİK ONAY HATASI | ID: " + pr.ID + " | HATA: " + approval.ErrorMessage);
                        mailing.SendMail2Admins(admins, "OTOMATİK ÖDEME BAŞARISIZ",
                            $"{pr.ID} ID'li sipariş için otomatik ödeme işlemi başarısız oldu: " + approval.ErrorMessage);
                    }
                }
            }
        }

        #region Methods
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\JobLogs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\JobLogs\\AutoTransferJob_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
        #endregion
    }
}