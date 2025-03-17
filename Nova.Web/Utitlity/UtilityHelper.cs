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
    using System.Net.Mail;
    using SendGrid;
    using SendGrid.Helpers.Mail;

    public class UtilityHelper : IUtilityService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public UtilityHelper(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        #region Handle Session Data

        public void SetSessionValue(string sKey, object sValue)
        {
            _contextAccessor.HttpContext.Session.SetString(sKey.ToLower(), sValue.ToString());
        }
        public object GetSessionValue(string sKey)
        {
            return GetSessionValue(sKey, null);
        }
        public object GetSessionValue(string sKey, object oReturnValue)
        {
            try
            {
                if (_contextAccessor.HttpContext.Session.GetString(sKey.ToLower()) == null) return oReturnValue;
                else return _contextAccessor.HttpContext.Session.GetString(sKey.ToLower());
            }
            catch
            {
                return oReturnValue;
            }
        }

        #endregion

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


        public async Task<bool> SendEmailAsync(string subject, string email, string htmlMessage, String name, Dictionary<string, string> objDict)
        {
            bool Result = false;
            //  var apiKey = _Configuration["EmailSettings:ApiKey"];

            Microsoft.Extensions.Configuration.IConfiguration _Configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var client = new SendGridClient(_Configuration["EmailSettings:ApiKey"]);
            // var from_email = new EmailAddress("amit.chakraborty@baseclass.co.in", "Example User");
            var from_email = new EmailAddress(_Configuration["EmailSettings:SenderEmail"], "Support@novaassetmanagement.net");
            var subject1 = "Reset Your Password";
            var to_email = new EmailAddress(email);
            var plainTextContent = " ";
            var htmlContent = ReadHtmlFile(objDict, htmlMessage);
            var msg = MailHelper.CreateSingleEmail(from_email, to_email, subject1, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                Result = true;
            }
            else
            {
                // Log the response for debugging
                var responseBody = await response.Body.ReadAsStringAsync();
                Console.WriteLine($"Failed to send email. StatusCode: {response.StatusCode}, ResponseBody: {responseBody}");
            }

            return Result;
        }

        public String ReadHtmlFile(Dictionary<String, String> obj, string htmlMessage)
        {
            String content = String.Empty;
            String TemplatePath = htmlMessage;
            try
            {
                Microsoft.Extensions.Configuration.IConfiguration _Configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

                var mailTemplatePath = _Configuration["EmailSettings:MailTemplatePath"];
                if (string.IsNullOrEmpty(mailTemplatePath))
                {
                    throw new ArgumentNullException(nameof(mailTemplatePath), "Mail template path is not configured.");
                }

                var fileStream = new FileStream(Path.Combine(mailTemplatePath, TemplatePath), FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    content = streamReader.ReadToEnd();
                }

                obj.Add("OrganizationMainSite", "Test");
                obj.Add("OrganizationName", "Test1");
                obj.Add("OrganizationLogo", "Test2");
                obj.Add("OrgSupportEmail", "Test3");
                obj.Add("OrgAddress", "Test4");

                foreach (KeyValuePair<String, String> kv in obj)
                {
                    content = content.Replace("@@" + kv.Key + "@@", kv.Value);
                }
            }
            catch (Exception Ex)
            {
                throw Ex;
            }

            return content;
        }

      



    }

}
