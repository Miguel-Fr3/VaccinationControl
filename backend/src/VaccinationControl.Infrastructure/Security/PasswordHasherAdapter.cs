using Microsoft.AspNetCore.Identity;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Security
{
    /// <summary>
    /// Usa o <see cref="PasswordHasher{TUser}"/> do ASP.NET Core, que aplica PBKDF2 com salt
    /// aleatório por senha e embute o algoritmo no próprio hash — trocar de parâmetros no
    /// futuro não invalida os hashes já gravados.
    /// </summary>
    public class PasswordHasherAdapter : IPasswordHasher
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        public string Hash(string password)
        {
            // O hasher recebe o usuário só por assinatura; o algoritmo não usa o objeto.
            return _passwordHasher.HashPassword(null!, password);
        }

        public bool Verify(string password, string passwordHash)
        {
            var result = _passwordHasher.VerifyHashedPassword(null!, passwordHash, password);

            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
