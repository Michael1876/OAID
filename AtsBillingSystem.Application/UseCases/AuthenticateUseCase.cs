using System;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Interfaces.Services;
using AtsBillingSystem.Domain.Interfaces.UseCases;

namespace AtsBillingSystem.Application.UseCases
{
    public class AuthenticateUseCase : IAuthenticateUseCase
    {
        private readonly IAuthService _authService;

        public AuthenticateUseCase(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public async Task<bool> ExecuteAsync(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            return await _authService.AuthenticateAsync(login, password);
        }
    }
}