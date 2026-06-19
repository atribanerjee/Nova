using Microsoft.AspNetCore.Identity;

namespace Nova.Web.Utitlity
{
    /// <summary>
    /// Password hashing backed by ASP.NET Core's <see cref="PasswordHasher{TUser}"/>,
    /// which uses PBKDF2 with a per-password random salt and a high iteration count.
    /// No third-party packages required; ships with the framework.
    ///
    /// If you prefer BCrypt or Argon2, swap this implementation for the
    /// BCrypt.Net-Next or Konscious.Security.Cryptography package — the interface
    /// (<see cref="IPasswordHasherService"/>) stays the same, so nothing else changes.
    /// </summary>
    public class PasswordHasherService : IPasswordHasherService
    {
        // The generic type argument is only a marker for the framework hasher; we
        // pass a throwaway object at call time since we don't need a user instance.
        private readonly PasswordHasher<object> _hasher = new();
        private static readonly object _marker = new();

        public string HashPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
            {
                throw new ArgumentException("Password must not be empty.", nameof(plainPassword));
            }

            return _hasher.HashPassword(_marker, plainPassword);
        }

        public bool VerifyPassword(string storedHash, string providedPassword, out bool needsRehash)
        {
            needsRehash = false;

            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(providedPassword))
            {
                return false;
            }

            var result = _hasher.VerifyHashedPassword(_marker, storedHash, providedPassword);

            switch (result)
            {
                case PasswordVerificationResult.SuccessRehashNeeded:
                    needsRehash = true;
                    return true;
                case PasswordVerificationResult.Success:
                    return true;
                default:
                    return false;
            }
        }
    }
}
