using GittiBu.Common;
using GittiBu.Common.Iyzico;
using GittiBu.Models;
using GittiBu.Services;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GittiBu.BackgroundJob
{
    public class PayoutTransactionJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\JobLogs";
                string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\JobLogs\\PayoutTransactionJob_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
                var workingJob = "PayoutTransactionJob Çalışıyor. - " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                WriteToFile(path, filepath, workingJob);
                using (var systemService = new SystemSettingService())
                using (var paymentRequestService = new PaymentRequestService())
                {
                    var settings = systemService.GetSystemSettings();
                    var adminMailSetting = settings.FirstOrDefault(s => s.Name == Enums.SystemSettingName.YoneticiMailleri);
                    var adminMail = "";
                    if (adminMailSetting != null)
                    {
                        adminMail = adminMailSetting.Value;
                    }
                    var paymentRequests = paymentRequestService.GetPayoutTransactionOrders();
                    PayoutTransactions(paymentRequests, adminMail);
                }
                var endWorkingJob = "PayoutTransactionJob Tamamlandı. - " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                WriteToFile(path, filepath, endWorkingJob);

            }
            catch (Exception ex)
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\JobErrorLogs";
                string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\JobErrorLogs\\ErrorPayoutTransactionJob_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
                WriteToFile(path, filepath, JsonConvert.SerializeObject(ex));
                WriteToFile(path, filepath, "-----------------------------------------");
            }
            return Task.CompletedTask;
        }

        #region Methods
        public void WriteToFile(string path, string filepath, string Message, string subPath = null, string conversationPath = null)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!string.IsNullOrWhiteSpace(subPath))
            {
                if (!Directory.Exists(subPath))
                {
                    Directory.CreateDirectory(subPath);
                }
            }

            if (!string.IsNullOrWhiteSpace(conversationPath))
            {
                if (!Directory.Exists(conversationPath))
                {
                    Directory.CreateDirectory(conversationPath);
                }
            }

            if (!System.IO.File.Exists(filepath))
            {
                using (StreamWriter sw = System.IO.File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = System.IO.File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        public void PayoutTransactions(IList<PaymentRequest> paymentRequests, string adminMail)
        {
            if (paymentRequests.Any())
            {
                using (var mailing = new MailingService())
                using (var service = new PaymentRequestService())
                using (var textService = new TextService())
                {
                    foreach (var paymentRequest in paymentRequests)
                    {
                        var conversationId = paymentRequest.ID.ToString();
                        string path = AppDomain.CurrentDomain.BaseDirectory + "\\Iyzico";
                        string subPath = path + "\\PaymentRetrieve";
                        string conversationPath = subPath + "\\" + conversationId + "_Idli_PaymentRequest";
                        string filepath = conversationPath + "\\" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
                        WriteToFile(path, filepath, "----------Request------------", subPath: subPath, conversationPath: conversationPath);
                        var request = new
                        {
                            conversationId,
                            date = DateTime.Now.ToString()
                        };
                        WriteToFile(path, filepath, JsonConvert.SerializeObject(request), subPath: subPath, conversationPath: conversationPath);
                        var response = IyzicoService.PaymentRetrieve(conversationId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        if (response.Status == "success")
                        {
                            paymentRequest.Status = Enums.PaymentRequestStatus.OdemeAktarildi;
                            paymentRequest.UpdatedUserID = -1;
                            var updatePaymentRequest = service.Update(paymentRequest);
                            if (!updatePaymentRequest)
                                WriteToFile(path, filepath, "Payment Request Guncellenemedi", subPath: subPath, conversationPath: conversationPath);
                            mailing.SendMail2Admins(adminMail, conversationId + " ID'li siparisin ödemesi aktarıldı.",
                           $"Sipariş No: #<a href=\"https://www.gittibu.com/AdminPanel/PaymentRequests/Details/{conversationId}\" target=\"_blank\">{conversationId}</a>  ID'li sipariş için  {paymentRequest.Price} TL ödeme satıcıya aktarıldı. İşlem Tarihi: {DateTime.Now:g}  <br /> ");
                            var mailContent = textService.GetContent(Enums.Texts.OdemeAktarildiMail, paymentRequest.Seller.LanguageID);
                            var body = mailContent.TextContent.Replace("@adSoyad", paymentRequest.Seller.Name).Replace("@urunAdi ", paymentRequest.Advert.Title).Replace("@urunAdet", paymentRequest.Amount.ToString());
                            var sellerMailResult = mailing.Send(body, paymentRequest.Seller.Email, paymentRequest.Seller.Name, mailContent.Name);
                            if (!sellerMailResult)
                                WriteToFile(path, filepath, "Para Aktarildi ancak satıcıya mail gonderilemedi!", subPath: subPath, conversationPath: conversationPath);
                        }
                        WriteToFile(path, filepath, "----------Response------------", subPath: subPath, conversationPath: conversationPath);
                        WriteToFile(path, filepath, JsonConvert.SerializeObject(response), subPath: subPath, conversationPath: conversationPath);
                        WriteToFile(path, filepath, "-----------------------------", subPath: subPath, conversationPath: conversationPath);
                    }
                }
            }
        }
        #endregion
    }
}
