namespace Nova.Web.ViewModels
{
    public class UserRoleViewModel
    {
        public int Id { get; set; }
        public string Rolename { get; set; }
        public bool IsDeleted { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int TotalRecords { get; set; }
    }
}
