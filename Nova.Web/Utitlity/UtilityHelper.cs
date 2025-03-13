namespace Nova.DB.Utitlity
{
    using Azure.Core;
    using Azure;
    using Microsoft.AspNetCore.Http;
    using Nova.Web.Utitlity;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.IdentityModel.Tokens;
    using Nova.Web.Models;

    public class UtilityHelper : IUtilityService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public UtilityHelper(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task SetSessionValue(string sKey, object sValue)
        {
            _contextAccessor.HttpContext?.Response.Cookies.Delete(sKey);

            CookieOptions cookieOptions = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(30)
            };

            var CookieVal = await Encrypt(sValue.ToString());
            _contextAccessor.HttpContext?.Response.Cookies.Append(sKey, CookieVal, cookieOptions);
        }

        public async Task<object> GetSessionValue(string sKey, object oReturnValue)
        {
            try
            {
                if (_contextAccessor.HttpContext != null)
                {
                    var cookieValue = _contextAccessor.HttpContext.Request.Cookies[sKey];
                    if (cookieValue == null)
                    {
                        return oReturnValue;
                    }
                    else
                    {
                        return await Decrypt(cookieValue);
                    }
                }
                else
                {
                    return oReturnValue;
                }
            }
            catch
            {
                return oReturnValue;
            }
        }

        public async Task<string> Encrypt(string plainText)
        {
            // Implement your encryption logic here
            // This is a placeholder implementation
            await Task.Delay(1); // Simulate async work
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        public async Task<string> Decrypt(string encryptedText)
        {
            // Implement your decryption logic here
            // This is a placeholder implementation
            return await Task.FromResult(Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText)));
        }

        public async Task<string> GetCookies(string key)
        {
            return await Task.FromResult(_contextAccessor.HttpContext?.Request.Cookies[key] ?? string.Empty);
        }

        public async Task SetCookies(string key, string value, int? expireTime)
        {
            await Task.Run(() =>
            {
                CookieOptions option = new CookieOptions();
                if (expireTime.HasValue)
                    option.Expires = DateTime.Now.AddDays(expireTime.Value);
                else
                    option.Expires = DateTime.Now.AddMilliseconds(10);
                _contextAccessor.HttpContext?.Response.Cookies.Append(key, value, option);
            });
        }

        public async Task RemoveCookies(string key)
        {
            await Task.Run(() => _contextAccessor.HttpContext?.Response.Cookies.Delete(key));
        }

        
    }

}
