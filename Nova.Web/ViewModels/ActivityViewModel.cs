using Nova.DB.POCO;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nova.Web.ViewModels
{
    public class ActivityViewModel
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
