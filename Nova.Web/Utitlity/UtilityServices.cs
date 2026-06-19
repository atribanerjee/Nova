using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nova.Web.Utitlity;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text;

namespace Nova.DB.Utitlity
{
    /// <summary>
    /// Cross-cutting helpers: session, cookies, email, request info.
    ///
    /// Key changes from the original:
    /// - IConfiguration and ILogger are injected (no more `new ConfigurationBuilder()`
    ///   rebuilt inside methods — that re-reads the file on every call and ignores
    ///   environment-specific config and secrets).
    /// - The reversible Base64 "Encrypt/Decrypt" methods are gone. Passwords are now
    ///   hashed via IPasswordHasherService; nothing in this app needs reversible
    ///   "encryption" of credentials.
    /// - Exceptions are logged instead of being silently swallowed.
    /// - Email sender details come from configuration, not hardcoded strings.
    /// </summary>
    public class UtilityServices : IUtilityServices
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UtilityServices> _logger;

        public UtilityServices(
            IHttpContextAccessor contextAccessor,
            IConfiguration configuration,
            ILogger<UtilityServices> logger)
        {
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _logger = logger;
        }

        public void SetSessionValue(string sKey, object sValue)
        {
            var session = _contextAccessor.HttpContext?.Session;
            if (session != null && sValue != null)
            {
                session.SetString(sKey.ToLower(), sValue.ToString() ?? string.Empty);
            }
        }

        public object GetSessionValue(string sKey) => GetSessionValue(sKey, null);

        public object GetSessionValue(string sKey, object? oReturnValue = null)
        {
            var session = _contextAccessor.HttpContext?.Session;
            var value = session?.GetString(sKey.ToLower());
            return value ?? oReturnValue ?? string.Empty;
        }

        public Task<string> GetCookies(string key)
        {
            var value = _contextAccessor.HttpContext?.Request.Cookies[key] ?? string.Empty;
            return Task.FromResult(value);
        }

        public Task SetCookies(string key, string value, int? expireTime)
        {
            var option = new CookieOptions
            {
                Expires = expireTime.HasValue
                    ? DateTimeOffset.UtcNow.AddDays(expireTime.Value)
                    : DateTimeOffset.UtcNow.AddMinutes(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };

            _contextAccessor.HttpContext?.Response.Cookies.Append(key, value, option);
            return Task.CompletedTask;
        }

        public Task RemoveCookies(string key)
        {
            _contextAccessor.HttpContext?.Response.Cookies.Delete(key);
            return Task.CompletedTask;
        }

        public async Task<bool> SendEmailAsync(string subject, string email, string htmlMessage, Dictionary<string, string> objDict)
        {
            try
            {
                var apiKey = _configuration["EmailSettings:ApiKey"];
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "Support";

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail))
                {
                    _logger.LogError("Email settings are not configured (ApiKey or SenderEmail missing).");
                    return false;
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(senderEmail, senderName);
                var to = new EmailAddress(email);
                var htmlContent = ReadHtmlFile(objDict, htmlMessage);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: " ", htmlContent);
                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var body = await response.Body.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("Failed to send email. StatusCode: {StatusCode}, Body: {Body}", response.StatusCode, body);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {Email}.", email);
                return false;
            }
        }

        private string ReadHtmlFile(Dictionary<string, string> tokens, string templateFileName)
        {
            try
            {
                var mailTemplatePath = _configuration["EmailSettings:MailTemplatePath"];
                if (string.IsNullOrEmpty(mailTemplatePath))
                {
                    _logger.LogError("Mail template path is not configured.");
                    return string.Empty;
                }

                var fullPath = Path.Combine(mailTemplatePath, templateFileName);
                string content = File.ReadAllText(fullPath, Encoding.UTF8);

                // Organization-level placeholders should come from configuration,
                // not be hardcoded. Pulled from config with safe fallbacks.
                tokens["OrganizationMainSite"] = _configuration["Organization:MainSite"] ?? string.Empty;
                tokens["OrganizationName"] = _configuration["Organization:Name"] ?? string.Empty;
                tokens["OrganizationLogo"] = _configuration["Organization:Logo"] ?? string.Empty;
                tokens["OrgSupportEmail"] = _configuration["Organization:SupportEmail"] ?? string.Empty;
                tokens["OrgAddress"] = _configuration["Organization:Address"] ?? string.Empty;

                foreach (var kv in tokens)
                {
                    content = content.Replace("@@" + kv.Key + "@@", kv.Value);
                }

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read or render email template {Template}.", templateFileName);
                return string.Empty;
            }
        }

        public void LogOut()
        {
            _contextAccessor.HttpContext?.Session.Clear();
        }

        public Task<string> GetIPAddress()
        {
            var ip = _contextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;
            return Task.FromResult(ip);
        }
    }
}
