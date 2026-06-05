using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Infrastructure.Json.Repositories
{
    public class JsonAuthRepository : IAuthRepository
    {
        public Task<DomainAdminUser> GetByLoginAsync(string login)
        {
            if (string.Equals(login, "admin", StringComparison.OrdinalIgnoreCase))
            {
                // Возвращаем объект, соответствующий жестко зашитому SHA-256 хэшу "admin"
                return Task.FromResult(new DomainAdminUser
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Login = "admin",
                    PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918"
                });
            }

            throw new KeyNotFoundException($"Администратор с логином {login} не найден в конфигурации.");
        }
    }
}