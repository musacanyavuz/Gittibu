using System.Collections.Generic;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;

namespace GittiBu.Common.Iyzico
{
    public class IyzicoService
    {

        #region Test
        //private const string ApiKey = "sandbox-ryXNeF5U5PXyqe4BLKvqbJHeib9hjjB8";
        //private const string SecretKey = "sandbox-aAx3agYWV1HgHUGNqs2wPfPCxP6hO4j2";
        //private const string BaseUrl = "https://sandbox-api.iyzipay.com/";
        //private const string SiteUrl = "https://localhost:44390/";
        //private const string SiteUrl = "https://test.gittibu.com/";
        #endregion

        #region Live
        private const string ApiKey = "Tgtul24I78EtSfGR0OZSDRvYKvRef3UQ";
        private const string SecretKey = "bBR5ZaUMsxW7jMMYefyoPD8uZkne6DUT";
        private const string BaseUrl = "https://api.iyzipay.com";
        private const string SiteUrl = "https://www.gittibu.com/";
        #endregion
        public const string callbackName = "Callback";
        public static IyzicoResult<InstallmentInfo> GetInstallmentInfoAllBanks(double price)
        {
            var request = new RetrieveInstallmentInfoRequest
            {
                Locale = Locale.TR.ToString(),
                Price = price.ToString("0.##").Replace(",", "."),
            };

            var installmentInfoResult = InstallmentInfo.Retrieve(request, GetOptions());
            return installmentInfoResult.Status == "success" ? new IyzicoResult<InstallmentInfo> { IsSuccess = true, Message = string.Empty, Data = installmentInfoResult } : new IyzicoResult<InstallmentInfo> { IsSuccess = false, Message = installmentInfoResult.ErrorMessage };
        }

        public static IyzicoResult<InstallmentInfo> GetInstallmentInfoAllBanks(string binNumber, string totalPrice)
        {
            var request = new RetrieveInstallmentInfoRequest
            {
                Price = totalPrice,
                Locale = Locale.TR.ToString(),
                BinNumber = binNumber
            };
            var installmentInfoResult = InstallmentInfo.Retrieve(request, GetOptions());
            return installmentInfoResult.Status == "success" ? new IyzicoResult<InstallmentInfo> { IsSuccess = true, Message = string.Empty, Data = installmentInfoResult } : new IyzicoResult<InstallmentInfo> { IsSuccess = false, Message = installmentInfoResult.ErrorMessage };
        }

        public static Options GetOptions()
        {
            var d = new Options
            {
                ApiKey = ApiKey,
                SecretKey = SecretKey,
                BaseUrl = BaseUrl,
            };
            return d;
        }

        public static IyzicoResult<SubMerchant> CreateSubMerchant(SubMerchant subMerchant)
        {
            var request = new CreateSubMerchantRequest
            {
                Locale = subMerchant.Locale,
                ConversationId = subMerchant.ConversationId,
                SubMerchantExternalId = subMerchant.SubMerchantExternalId,
                SubMerchantType = subMerchant.SubMerchantType,
                Address = subMerchant.Address,
                Email = subMerchant.Email,
                GsmNumber = subMerchant.GsmNumber,
                Name = subMerchant.Name,
                Iban = subMerchant.Iban,
                Currency = "TRY",
                IdentityNumber = subMerchant.IdentityNumber
            };

            if (subMerchant.SubMerchantType == SubMerchantType.PERSONAL.ToString())
            {
                request.ContactName = subMerchant.ContactName;
                request.ContactSurname = subMerchant.ContactSurname;
                request.IdentityNumber = subMerchant.IdentityNumber;
            }
            else if (subMerchant.SubMerchantType == SubMerchantType.PRIVATE_COMPANY.ToString())
            {
                request.TaxOffice = subMerchant.TaxOffice;
                request.LegalCompanyTitle = subMerchant.LegalCompanyTitle;
                request.IdentityNumber = subMerchant.IdentityNumber;
            }
            else if (subMerchant.SubMerchantType == SubMerchantType.LIMITED_OR_JOINT_STOCK_COMPANY.ToString())
            {
                request.TaxOffice = subMerchant.TaxOffice;
                request.TaxNumber = subMerchant.TaxNumber;
                request.LegalCompanyTitle = subMerchant.LegalCompanyTitle;
            }
            var subMerchantResult = SubMerchant.Create(request, GetOptions());
            return subMerchantResult.Status == "success" ? new IyzicoResult<SubMerchant> { IsSuccess = true, Message = "Alt üye iş yeri başarıyla oluşturuldu!", Data = subMerchantResult } : new IyzicoResult<SubMerchant> { IsSuccess = false, Message = subMerchantResult.ErrorMessage, ErrorCode = subMerchantResult.ErrorCode };
        }

        public static IyzicoResult<SubMerchant> UpdateSubMerchant(SubMerchant subMerchant)
        {
            var request = new UpdateSubMerchantRequest
            {
                Locale = subMerchant.Locale,
                ConversationId = subMerchant.ConversationId,
                Address = subMerchant.Address,
                Email = subMerchant.Email,
                GsmNumber = subMerchant.GsmNumber,
                Name = subMerchant.Name,
                Iban = subMerchant.Iban,
                Currency = "TRY",
                IdentityNumber = subMerchant.IdentityNumber,
                SubMerchantKey = subMerchant.SubMerchantKey,
                // ContactName = subMerchant.ContactName,
                // ContactSurname = subMerchant.ContactSurname,
            };

            if (subMerchant.SubMerchantType == SubMerchantType.PERSONAL.ToString())
            {
                request.ContactName = subMerchant.ContactName;
                request.ContactSurname = subMerchant.ContactSurname;
                request.IdentityNumber = subMerchant.IdentityNumber;
            }
            else if (subMerchant.SubMerchantType == SubMerchantType.PRIVATE_COMPANY.ToString())
            {
                request.TaxOffice = subMerchant.TaxOffice;
                request.LegalCompanyTitle = subMerchant.LegalCompanyTitle;
                request.IdentityNumber = subMerchant.IdentityNumber;
            }
            else if (subMerchant.SubMerchantType == SubMerchantType.LIMITED_OR_JOINT_STOCK_COMPANY.ToString())
            {
                request.TaxOffice = subMerchant.TaxOffice;
                request.TaxNumber = subMerchant.TaxNumber;
                request.LegalCompanyTitle = subMerchant.LegalCompanyTitle;
            }
            var subMerchantResult = SubMerchant.Update(request, GetOptions());
            return subMerchantResult.Status == "success" ? new IyzicoResult<SubMerchant> { IsSuccess = true, Message = "Alt üye iş yeri başarıyla güncellendi!", Data = subMerchantResult } : new IyzicoResult<SubMerchant> { IsSuccess = false, Message = subMerchantResult.ErrorMessage };
        }

        public static string GetSubMerchantKey(SubMerchant subMerchant)
        {
            var subMerchantKeyRequest = new RetrieveSubMerchantRequest()
            {
                Locale = subMerchant.Locale,
                SubMerchantExternalId = subMerchant.SubMerchantExternalId
            };
            var subKeyResult = SubMerchant.Retrieve(subMerchantKeyRequest, GetOptions());
            return subKeyResult.SubMerchantKey;
        }

        public static ThreedsInitialize PayBanner(int requestId, int price, PaymentCard paymentCard, Buyer buyer, Address shippingAddress,
            Address billingAddress, int dopingId, string dopingName, string callback)
        {//banner ödemeleri, doping ödemeleri, blog ödemeleri bu fonksiyon ile yapılıyor
            CreatePaymentRequest request = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = requestId.ToString(),
                Price = price.ToString(),
                PaidPrice = price.ToString(),
                Currency = Currency.TRY.ToString(),
                Installment = 1,
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
                CallbackUrl = SiteUrl + callback,
                PaymentCard = paymentCard,
                Buyer = buyer,
                ShippingAddress = shippingAddress,
                BillingAddress = billingAddress
            };





            var basketItems = new List<BasketItem>();
            var firstBasketItem = new BasketItem
            {
                Id = dopingId.ToString(),
                Name = dopingName,
                Category1 = dopingName,
                ItemType = BasketItemType.VIRTUAL.ToString(),
                Price = price.ToString(),
                //SubMerchantKey = "hlvmB2LSjkZALCL/Zfb9qfJhX9o=",
                //SubMerchantPrice = subMerchantPrice.ToString("F2").Replace(",",".")
            };
            basketItems.Add(firstBasketItem);
            request.BasketItems = basketItems;

            var threesInitialize = ThreedsInitialize.Create(request, GetOptions());
            return threesInitialize;
        }

        public static ThreedsInitialize SecurePayment(int requestId, double price, PaymentCard paymentCard, Buyer buyer, Address shippingAddress,
            Address billingAddress, int adId, string callback, string subMerchantKey, string subMerchantPrice, int installment, string name,
            string category1, string category2)
        {
            CreatePaymentRequest request = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = requestId.ToString(),
                Price = price.ToString("0.##"),
                PaidPrice = price.ToString("0.##"),
                Currency = Currency.TRY.ToString(),
                Installment = installment,
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = SiteUrl + callback,
                PaymentCard = paymentCard,
                Buyer = buyer,
                ShippingAddress = shippingAddress,
                BillingAddress = billingAddress
            };




            var basketItems = new List<BasketItem>();
            var firstBasketItem = new BasketItem
            {
                Id = adId.ToString(),
                Name = name,
                Category1 = category1,
                Category2 = category2,
                ItemType = BasketItemType.PHYSICAL.ToString(),
                Price = price.ToString("0.##"),
                SubMerchantKey = subMerchantKey,
                SubMerchantPrice = subMerchantPrice.Replace(",", ".")
            };
            basketItems.Add(firstBasketItem);
            request.BasketItems = basketItems;


            var threadsInitialize = ThreedsInitialize.Create(request, GetOptions());
            return threadsInitialize;
        }

        public static Approval PaymentApproval(int paymentRequestId, string paymentTransactionId)
        {//ödeme onayı, parayı satıcıya aktarır
            var request = new CreateApprovalRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = paymentRequestId.ToString(),
                PaymentTransactionId = paymentTransactionId
            };

            var approval = Approval.Create(request, GetOptions());
            return approval;
        }
        public static Cancel PaymentCancel(int paymentRequestId, string paymentId, string ipAddress)
        {//ödeme iptali, alıcının parası iade edilir
            var request = new CreateCancelRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = paymentRequestId.ToString(),
                PaymentId = paymentId,
                Ip = ipAddress
            };

            var cancel = Cancel.Create(request, GetOptions());
            return cancel;
        }

        public static Refund PaymentRefund(int paymentRequestId, string paymentTransactionId, string ipAddress, string price)
        {//ödeme iadesi, alıcının parası iade edilir
            var request = new CreateRefundRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = paymentRequestId.ToString(),
                PaymentTransactionId = paymentTransactionId,
                Ip = ipAddress,
                Price = price
            };

            var refund = Refund.Create(request, GetOptions());
            return refund;
        }

        public static PayoutCompletedTransactionList PaymentRetrieve(string conversationId,string date)
        {
            RetrieveTransactionsRequest request = new RetrieveTransactionsRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = conversationId,
                Date = date
            };

            var response = PayoutCompletedTransactionList.Retrieve(request, GetOptions());

            return response;
        }
    }
}