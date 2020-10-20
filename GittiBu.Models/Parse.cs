using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GittiBu.Models
{
    [Table("Parses")]
    public class Parse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string File { get; set; }
        public int PriceFilter { get; set; }
        public int StockFilter { get; set; }
        public int Discount { get; set; }
        public int CargoAreaID { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string Fields { get; set; }
        public string UserFileName { get; set; }
        public string RootName { get; set; }
        public string ProductIDs { get; set; }
        public string ChildNames { get; set; }
        public bool IsDeleted { get; set; }
        public bool FreeShipping { get; set; }
        [NotMapped]
        public string ShippingOwner { get { return FreeShipping ? "Satıcıya Ait" : "Alıcıya Ait"; } }
        [MaxLength(50)]
        public string Supplier { get; set; }
        public string CategoryMatches { get; set; }
        public int MaxInstallment { get; set; }
        public int NewPriceDiscount { get; set; }
        public int AdvertCount { get; set; }

        //[ForeignKey("User")] //Db'de ilişkilendirildi
        public int UserID { get; set; }
    }
}
