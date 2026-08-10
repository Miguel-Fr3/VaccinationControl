using Microsoft.EntityFrameworkCore;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Repositories
{
    public class VaccinationRecordRepository : IVaccinationRecordRepository
    {
        private readonly AppDbContext _context;

        public VaccinationRecordRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<VaccinationRecord>> GetDosesAsync(
            Guid personId,
            Guid vaccineId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VaccinationRecords
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
            return _context.VaccinationRecords
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
            return await _context.VaccinationRecords
                .AsNoTracking()
                // A vacina vem junto porque o cartão exibe o nome dela em cada grupo.
                .Include(record => record.Vaccine)
                .Where(record => record.PersonId == personId)
                .OrderBy(record => record.Vaccine.Name)
                .ThenBy(record => record.VaccinationType)
                .ThenBy(record => record.DoseNumber)
                .ToListAsync(cancellationToken);
        }

        public void Add(VaccinationRecord vaccinationRecord)
        {
            _context.VaccinationRecords.Add(vaccinationRecord);
        }

        public void Remove(VaccinationRecord vaccinationRecord)
        {
            _context.VaccinationRecords.Remove(vaccinationRecord);
        }
    }
}
