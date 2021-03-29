using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GittiBu.Models
{
    [Table("ParsesCategoryMatches")]
    public class ParsesCategoryMatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int ParsesID { get; set; }
        public string XmlCategory { get; set; }
        public int GittibuCategory { get; set; }
        public DateTime CreateDate { get; set; }
       
    }
}
