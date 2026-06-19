namespace Nova.Web.Utitlity
{
    public interface IUtilityServices
    {
        void SetSessionValue(string sKey, object sValue);
        object GetSessionValue(string sKey);
        void LogOut();

        // NOTE: Encrypt/Decrypt were removed. Passwords are now one-way hashed
        // via IPasswordHasherService. No part of the app needs reversible
        // "encryption" of credentials.

        Task<string> GetCookies(string key);
        Task SetCookies(string key, string value, int? expireTime);
        Task RemoveCookies(string key);

        Task<bool> SendEmailAsync(string subject, string email, string htmlMessage, Dictionary<string, string> objDict);
        Task<string> GetIPAddress();
    }
}
