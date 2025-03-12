using Nova.Web.Models;
using System.Runtime.CompilerServices;

namespace Nova.Web.Utitlity
{
    public interface IUtilityService
    {
        Task<string> Encrypt(string clearText);
        Task<string> Decrypt(string cipherText);
        Task<string> sha256encription(string pass);
        Task SetSessionValue(string sKey, object sValue);
        public object GetSessionValue(string sKey, object oReturnValue);
    }
}
