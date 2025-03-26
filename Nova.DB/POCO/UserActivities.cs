using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nova.DB.POCO
{
    [Table("UserActivities")]
    public class UserActivities
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public Users User { get; set; } = null!; 

        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
