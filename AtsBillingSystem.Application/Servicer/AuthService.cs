using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Interfaces.Services;

namespace AtsBillingSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;

        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
        }

        public async Task<bool> AuthenticateAsync(string login, string password)
        {
            try
            {
                var user = await _authRepository.GetByLoginAsync(login);
                if (user == null)
                {
                    return false;
                }

                var inputHash = HashPassword(password);
                return string.Equals(user.PasswordHash, inputHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public string HashPassword(string raw)
        {
            if (raw == null) throw new ArgumentNullException(nameof(raw));

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(raw);
            var hashBytes = sha256.ComputeHash(bytes);

            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}