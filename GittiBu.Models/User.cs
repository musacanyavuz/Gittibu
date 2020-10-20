﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using static GittiBu.Common.Enums;

namespace GittiBu.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int GenderID { get; set; }
        public string MobilePhone { get; set; }
        public string WorkPhone { get; set; }
        public DateTime? BirthDate { get; set; }
        public string WebSite { get; set; }
        public string About { get; set; }
        public string ProfilePicture { get; set; }
        public string TC { get; set; }
        public bool InMailing { get; set; }
        public int LanguageID { get; set; }
        [ForeignKey("City")]
        public int CityID { get; set; }
        [ForeignKey("District")]
        public int DistrictID { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        [ForeignKey("Country")]
        public int CountryID { get; set; }

        public string IdentityPhotoFront { get; set; }
        public string IdentityPhotoBack { get; set; }
        public bool IdentityPhotosApproved { get; set; }
        public bool IbanApproved { get; set; }

        [NotMapped] public IFormFile ProfilePictureFile { get; set; }
        public IEnumerable<Advert> Adverts { get; set; }
        public List<UserAddress> Addresses { get; set; }

        public City City { get; set; }
        public District District { get; set; }
        public Country Country { get; set; }

        [NotMapped] public string Surname { get; set; }

        public short? OAuth_Provider { get; set; }

        public string OAuth_Uid { get; set; }


    }
}