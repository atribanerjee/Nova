using Nova.Web.Models;
using System.Runtime.CompilerServices;

namespace Nova.Web.Utitlity
{
    public interface IUtilityService
    {
        public Task SetSessionValue(string sKey, object sValue);
        public Task<object> GetSessionValue(string sKey, object oReturnValue);
        public Task<string> Encrypt(string clearText);
        public Task<string> Decrypt(string cipherText);
        public Task<string> GetCookies(string cipherText);
        public Task SetCookies(string key, string value, int? expireTime);
        public Task RemoveCookies(string key);

        //  public Task<string> sha256encription(string pass);


    }
}
