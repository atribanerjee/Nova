namespace Nova.DB.Utitlity
{
    using Azure.Core;
    using Azure;
    using Microsoft.AspNetCore.Http;
    using Nova.Web.Utitlity;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.AspNetCore.Authentication.Cookies;

    public class UtilityHelper : IUtilityService
    {
        //private readonly IHttpContextAccessor _contextAccessor;

        //public UtilityHelper(IHttpContextAccessor contextAccessor)
        //{
        //    _contextAccessor = contextAccessor;
        //}


        public async void SetSessionValue(string sKey, object sValue)
        {
            Microsoft.AspNetCore.Http.IHttpContextAccessor _contextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
            _contextAccessor.HttpContext.Response.Cookies.Delete(sKey);

            CookieOptions cookieOptions = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(30)
            };

            var CookieVal = await Encrypt(sValue.ToString());
            _contextAccessor.HttpContext.Response.Cookies.Append(sKey, CookieVal, cookieOptions);
        }

       
        public  object GetSessionValue(string sKey, object oReturnValue)
        {
            try
            {
                Microsoft.AspNetCore.Http.IHttpContextAccessor _contextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
                if (_contextAccessor.HttpContext != null)
                {

                    if (_contextAccessor.HttpContext.Request.Cookies[sKey] == null)
                    {
                        return oReturnValue;
                    }
                    else
                    {
                        return Decrypt(_contextAccessor.HttpContext.Request.Cookies[sKey]);
                    }
                }
                else
                {
                    return oReturnValue;
                }
                //if (_contextAccessor != null && _contextAccessor.HttpContext.Request.Cookies[sKey] == null)
                //{
                //    return oReturnValue;
                //}
                //else
                //{
                //    return Decrypt(_contextAccessor.HttpContext.Request.Cookies[sKey]);
                //}
                //else return Decrypt(_contextAccessor.HttpContext.Request.Cookies[sKey]);

                //if (_contextAccessor.HttpContext.Session.GetString(sKey.ToLower()) == null) return oReturnValue;
                //else return _contextAccessor.HttpContext.Session.GetString(sKey.ToLower());
            }
            catch
            {
                return oReturnValue;
            }
        }


        public async Task <string> Encrypt(string clearText)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes("true", new byte[] { 0x65, 0x3d, 0x54, 0x9d, 0x76, 0x49, 0x76, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x61 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        public async Task<string> Decrypt(string cipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes("true", new byte[] { 0x65, 0x3d, 0x54, 0x9d, 0x76, 0x49, 0x76, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x61 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

        public async Task<string> sha256encription(string pass)
        {
            // We have created an instance of the MD5CryptoServiceProvider class.
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            //We converted the data as a parameter to a byte array.
            byte[] array = Encoding.UTF8.GetBytes(pass);
            //We have calculated the hash of the array.
            array = md5.ComputeHash(array);
            //We created a StringBuilder object to store hashed data.
            StringBuilder sb = new StringBuilder();
            //We have converted each byte from string into string type.

            foreach (byte ba in array)
            {
                sb.Append(ba.ToString("x2").ToLower());
            }

            //We returned the hexadecimal string.
            return sb.ToString();
        }

        


    }

}
