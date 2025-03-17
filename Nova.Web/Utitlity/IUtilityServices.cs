using Nova.Web.Models;
using System.Runtime.CompilerServices;

namespace Nova.Web.Utitlity
{
    public interface IUtilityServices
    {
        public void SetSessionValue(string sKey, object sValue);
        public object GetSessionValue(string sKey);
        public void LogOut();

        public Task<string> Encrypt(string clearText);
        public Task<string> Decrypt(string cipherText);
        public Task<string> GetCookies(string cipherText);
        public Task SetCookies(string key, string value, int? expireTime);
        public Task RemoveCookies(string key);

        public Task<bool> SendEmailAsync(string subject, string email, string htmlMessage, String name, Dictionary<string, string> objDict);
    }
}
