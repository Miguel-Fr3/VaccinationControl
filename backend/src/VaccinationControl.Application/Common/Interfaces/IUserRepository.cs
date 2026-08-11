using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        void Add(User user);
    }
}
