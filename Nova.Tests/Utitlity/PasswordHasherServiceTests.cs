using Nova.Web.Utitlity;
using Xunit;

namespace Nova.Tests.Utitlity
{
    public class PasswordHasherServiceTests
    {
        private readonly PasswordHasherService _sut = new();

        [Fact]
        public void HashPassword_ReturnsNonEmptyHash_DifferentFromPlainText()
        {
            var hash = _sut.HashPassword("Sup3rSecret!");

            Assert.False(string.IsNullOrEmpty(hash));
            Assert.NotEqual("Sup3rSecret!", hash);
        }

        [Fact]
        public void HashPassword_ProducesDifferentHashesForSamePassword_DueToRandomSalt()
        {
            var hash1 = _sut.HashPassword("Sup3rSecret!");
            var hash2 = _sut.HashPassword("Sup3rSecret!");

            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void HashPassword_ThrowsArgumentException_ForNullOrEmptyInput(string? plainPassword)
        {
            Assert.Throws<ArgumentException>(() => _sut.HashPassword(plainPassword!));
        }

        [Fact]
        public void VerifyPassword_ReturnsTrue_ForCorrectPassword()
        {
            var hash = _sut.HashPassword("Sup3rSecret!");

            var result = _sut.VerifyPassword(hash, "Sup3rSecret!", out var needsRehash);

            Assert.True(result);
            Assert.False(needsRehash);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_ForIncorrectPassword()
        {
            var hash = _sut.HashPassword("Sup3rSecret!");

            var result = _sut.VerifyPassword(hash, "WrongPassword", out _);

            Assert.False(result);
        }

        [Theory]
        [InlineData(null, "password")]
        [InlineData("", "password")]
        [InlineData("somehash", null)]
        [InlineData("somehash", "")]
        public void VerifyPassword_ReturnsFalse_WhenHashOrPasswordMissing(string? storedHash, string? providedPassword)
        {
            var result = _sut.VerifyPassword(storedHash!, providedPassword!, out var needsRehash);

            Assert.False(result);
            Assert.False(needsRehash);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_ForUnrecognizedHashFormat()
        {
            // Valid Base64 but with a version marker byte the hasher doesn't recognize.
            var unrecognizedFormatHash = Convert.ToBase64String(new byte[] { 0xFF, 1, 2, 3 });

            var result = _sut.VerifyPassword(unrecognizedFormatHash, "Sup3rSecret!", out var needsRehash);

            Assert.False(result);
            Assert.False(needsRehash);
        }
    }
}
