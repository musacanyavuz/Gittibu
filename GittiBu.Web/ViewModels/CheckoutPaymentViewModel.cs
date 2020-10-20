using System.Collections.Generic;
using GittiBu.Models;
using Iyzipay.Model;

namespace GittiBu.Web.ViewModels
{
    public class CheckoutPaymentViewModel
    {
        public Advert Advert { get; set; }
        public int Amount { get; set; }
        public int ShippingAddressID { get; set; }
        public int InvoiceAddressID { get; set; }
        public List<InstallmentDetail> InstallmentDetails { get; set; }
    }
}