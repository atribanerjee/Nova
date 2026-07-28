using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nova.DB.Utitlity;
using Nova.Tests.TestHelpers;
using Xunit;

namespace Nova.Tests.Utitlity
{
    public class UtilityServicesTests
    {
        private readonly Mock<IHttpContextAccessor> _contextAccessorMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();
        private readonly DefaultHttpContext _httpContext = new();
        private readonly FakeSession _session = new();

        public UtilityServicesTests()
        {
            _httpContext.Features.Set<ISessionFeature>(new SessionFeatureStub(_session));
            _contextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);
        }

        private UtilityServices CreateSut() =>
            new(_contextAccessorMock.Object, _configurationMock.Object, NullLogger<UtilityServices>.Instance);

        private sealed class SessionFeatureStub : ISessionFeature
        {
            public SessionFeatureStub(ISession session) => Session = session;
            public ISession Session { get; set; }
        }

        // ---------- Session ----------

        [Fact]
        public void SetSessionValue_ThenGetSessionValue_RoundTrips()
        {
            var sut = CreateSut();

            sut.SetSessionValue("MyKey", "MyValue");
            var result = sut.GetSessionValue("MyKey");

            Assert.Equal("MyValue", result);
        }

        [Fact]
        public void GetSessionValue_ReturnsEmptyString_WhenKeyMissing()
        {
            var result = CreateSut().GetSessionValue("Missing");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetSessionValue_ReturnsFallback_WhenKeyMissingAndFallbackProvided()
        {
            var result = CreateSut().GetSessionValue("Missing", "fallback");

            Assert.Equal("fallback", result);
        }

        [Fact]
        public void LogOut_ClearsSession()
        {
            var sut = CreateSut();
            sut.SetSessionValue("MyKey", "MyValue");

            sut.LogOut();

            Assert.Equal(string.Empty, sut.GetSessionValue("MyKey"));
        }

        // ---------- Cookies ----------

        [Fact]
        public async Task GetCookies_ReturnsEmptyString_WhenCookieNotPresent()
        {
            var result = await CreateSut().GetCookies("NovaLogin");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetCookies_ReturnsValue_WhenCookiePresent()
        {
            _httpContext.Request.Headers["Cookie"] = "NovaLogin=someuser";

            var result = await CreateSut().GetCookies("NovaLogin");

            Assert.Equal("someuser", result);
        }

        // ---------- GetIPAddress ----------

        [Fact]
        public async Task GetIPAddress_ReturnsRemoteIpAddress()
        {
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.5");

            var result = await CreateSut().GetIPAddress();

            Assert.Equal("203.0.113.5", result);
        }

        // ---------- SendEmailAsync ----------

        [Fact]
        public async Task SendEmailAsync_ReturnsFalse_WhenApiKeyIsMissing()
        {
            _configurationMock.Setup(x => x["EmailSettings:ApiKey"]).Returns((string?)null);
            _configurationMock.Setup(x => x["EmailSettings:SenderEmail"]).Returns("sender@example.com");

            var result = await CreateSut().SendEmailAsync("Subject", "to@example.com", "Template.html", new Dictionary<string, string>());

            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailAsync_ReturnsFalse_WhenSenderEmailIsMissing()
        {
            _configurationMock.Setup(x => x["EmailSettings:ApiKey"]).Returns("some-key");
            _configurationMock.Setup(x => x["EmailSettings:SenderEmail"]).Returns((string?)null);

            var result = await CreateSut().SendEmailAsync("Subject", "to@example.com", "Template.html", new Dictionary<string, string>());

            Assert.False(result);
        }
    }
}
