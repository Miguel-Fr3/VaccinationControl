using MediatR;

namespace VaccinationControl.Application.Vaccinations.Commands.DeleteVaccinationRecord
{
    /// <summary>
    /// Remove um registro específico do cartão de vacinação de uma pessoa.
    /// </summary>
    public record DeleteVaccinationRecordCommand(Guid PersonId, Guid RecordId) : IRequest;
}
