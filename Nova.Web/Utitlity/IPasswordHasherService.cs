using Microsoft.AspNetCore.Identity;

namespace Nova.Web.Utitlity
{
    /// <summary>
    /// Handles one-way password hashing and verification.
    /// Passwords are never stored or transmitted in a reversible form.
    /// </summary>
    public interface IPasswordHasherService
    {
        /// <summary>Produces a salted, one-way hash of a plaintext password for storage.</summary>
        string HashPassword(string plainPassword);

        /// <summary>
        /// Verifies a plaintext password against a stored hash.
        /// Returns true on match. Also signals when the stored hash should be
        /// re-hashed with updated parameters (handled transparently by the caller).
        /// </summary>
        bool VerifyPassword(string storedHash, string providedPassword, out bool needsRehash);
    }
}
