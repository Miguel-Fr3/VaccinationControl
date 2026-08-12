using Microsoft.EntityFrameworkCore;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Repositories
{
    public class VaccinationRecordRepository(AppDbContext context) : IVaccinationRecordRepository
    {
        public async Task<IReadOnlyList<VaccinationRecord>> GetDosesAsync(
            Guid personId,
            Guid vaccineId,
            CancellationToken cancellationToken = default)
        {
            return await context.VaccinationRecords
                .AsNoTracking()
                .Where(record => record.PersonId == personId && record.VaccineId == vaccineId)
                .OrderBy(record => record.DoseNumber)
                .ToListAsync(cancellationToken);
        }

        public Task<VaccinationRecord?> GetByIdAsync(
            Guid personId,
            Guid recordId,
            CancellationToken cancellationToken = default)
        {
            return context.VaccinationRecords
                .AsNoTracking()
                .Include(record => record.Vaccine)
                .FirstOrDefaultAsync(
                    record => record.Id == recordId && record.PersonId == personId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<VaccinationRecord>> GetByPersonAsync(
            Guid personId,
            CancellationToken cancellationToken = default)
        {
            return await context.VaccinationRecords
                .AsNoTracking()
                // A vacina vem junto porque o cartão exibe o nome dela em cada grupo.
                .Include(record => record.Vaccine)
                .Where(record => record.PersonId == personId)
                .OrderBy(record => record.Vaccine.Name)
                .ThenBy(record => record.VaccinationType)
                .ThenBy(record => record.DoseNumber)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> ExistsByVaccineAsync(
            Guid vaccineId,
            CancellationToken cancellationToken = default)
        {
            return context.VaccinationRecords
                .AnyAsync(record => record.VaccineId == vaccineId, cancellationToken);
        }

        public void Add(VaccinationRecord vaccinationRecord)
        {
            context.VaccinationRecords.Add(vaccinationRecord);
        }

        public void Remove(VaccinationRecord vaccinationRecord)
        {
            context.VaccinationRecords.Remove(vaccinationRecord);
        }
    }
}
