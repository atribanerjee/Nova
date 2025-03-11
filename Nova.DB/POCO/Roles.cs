using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.DB.POCO
{
    [Table("Roles")]
    public class Roles
    {
        [Key]
        public int Id { get; set; }
        public string Rolename { get; set; }
        public bool IsDeleted { get; set; }
    }
}
