using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GittiBu.Models
{
    [Table("Districts")]
    public class District
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Name { get; set; }
        [ForeignKey("City")]
        public int CityID { get; set; }
        public int Order { get; set; }
        public City City { get; set; }
    }
}