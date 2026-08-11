using Microsoft.EntityFrameworkCore;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Repositories
{
    public class UserRepository(AppDbContext context) : IUserRepository
    {
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return context.Users.AnyAsync(user => user.Email == email, cancellationToken);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
        }

        public void Add(User user)
        {
            context.Users.Add(user);
        }
    }
}
