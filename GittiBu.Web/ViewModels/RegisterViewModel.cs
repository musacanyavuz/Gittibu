using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GittiBu.Models;
using GittiBu.Web.ViewModels.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OfficeOpenXml.Utils;

namespace GittiBu.Web.ViewModels
{
    public class RegisterViewModel
    {
        public List<Country> Countries { get; set; }
        public List<City> Cities { get; set; }
        public List<District> Districts { get; set; }

        [Required(ErrorMessage = "Ad zorunludur")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Soyadı zorunludur")]
        public string Surname { get; set; }
        [Required(ErrorMessage = "Kullanıcı adı zorunludur"), UserNameValidation]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Şifre zorunludur")]
        public string Password { get; set; }
        [Compare(nameof(Password), ErrorMessage = "Şifreler uyuşmamaktadır")]
        public string Password2 { get; set; }
        [Required(ErrorMessage = "Email zorunludur"), EmailAddress(ErrorMessage = "Lütfen düzgün bir email adresi giriniz"), EmailUniqValidationAttribute]
        public string Email { get; set; }
        [Required(ErrorMessage = "Telefon zorunludur")]
        public string MobilePhone { get; set; }
        public int GenderID { get; set; }
        public string About { get; set; }
        public string WebSite { get; set; }
        public int CountryID { get; set; }
        public int CityID { get; set; }
        public int DistrictID { get; set; }
        public int LanguageID { get; set; }
        public DateTime? BirthDate { get; set; }
        public string WorkPhone { get; set; }

        public short MemberType { get; set; }
        public bool InMailing { get; set; }

        //[MinLength(11, ErrorMessage = "Tc 11 karakter olmak zorundadır")]
        //[MaxLength(11, ErrorMessage = "Tc 11 karakter olmak zorundadır")]
        //[RegularExpression("([0-9]+)", ErrorMessage = "Lütfen düzgün tc numarası giriniz")]
        public string TC { get; set; }
        //[Required(ErrorMessage = "Iban zorunludur"), MinLength(26, ErrorMessage = "Iban numarası hatalıdır"), MaxLength(33, ErrorMessage = "Iban numarası hatalıdır")]
        public string IBAN { get; set; }
        //[Required(ErrorMessage = "Gönderici adresi zorunludur"), MinLength(5, ErrorMessage = "Gönderici adresi {1} karakterden büyük olmak zorundadır")]
        public string SenderAddress { get; set; }
        //[Required(ErrorMessage = "Fatura adresi zorunludur"), MinLength(5, ErrorMessage = "Fatura adresi {1} karakterten büyük olmak zorundadır")]
        public string InvoiceAddress { get; set; }
        public Iyzipay.Model.SubMerchantType SubMerchantType { get; set; }
        public string CompanyName { get; set; }
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyFax { get; set; }
        public string CompanyEmail { get; set; }
        public IFormFile ProfilePictureFile { get; set; }
        [Range(typeof(bool), "true", "true", ErrorMessage = "Üye olmak için üyelik sözleşmesini kabul etmeniz gerekiyor.!")]
        public bool TermsAndConditions { get; set; }
    }
}