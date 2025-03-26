namespace Nova.Web.Interfaces
{
    public interface IUserActivities
    {
        public Task<bool> SaveActivity(int UserId, string Description);
    }
}
