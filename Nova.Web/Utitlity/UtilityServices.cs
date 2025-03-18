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

    public class UtilityServices : IUtilityServices
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public UtilityServices(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public void SetSessionValue(string sKey, object sValue)
        {
            if (_contextAccessor.HttpContext?.Session != null && sValue != null)
            {
                _contextAccessor.HttpContext.Session.SetString(sKey.ToLower(), sValue.ToString() ?? string.Empty);
            }
        }

        public object GetSessionValue(string sKey)
        {
            return GetSessionValue(sKey, null);
        }

        public object GetSessionValue(string sKey, object? oReturnValue = null)
        {
            try
            {
                if (_contextAccessor.HttpContext?.Session == null || _contextAccessor.HttpContext.Session.GetString(sKey.ToLower()) == null)
                {
                    return oReturnValue ?? string.Empty;
                }
                else
                {
                    return _contextAccessor.HttpContext.Session.GetString(sKey.ToLower()) ?? string.Empty;
                }
            }
            catch
            {
                return oReturnValue ?? string.Empty;
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
            CookieOptions option = new CookieOptions();
            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddDays(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMinutes(30);

            _contextAccessor.HttpContext?.Response.Cookies.Append(key, value, option);
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
            var to_email = new EmailAddress(email);
            var plainTextContent = " ";
            var htmlContent = ReadHtmlFile(objDict, htmlMessage);
            var msg = MailHelper.CreateSingleEmail(from_email, to_email, subject, plainTextContent, htmlContent);
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

            }

            return content;
        }

        public void LogOut()
        {
            try
            {
                if (_contextAccessor.HttpContext?.Session != null)
                {
                    _contextAccessor.HttpContext.Session.Clear();
                }
            }
            catch
            {
            }
        }
    }

}
