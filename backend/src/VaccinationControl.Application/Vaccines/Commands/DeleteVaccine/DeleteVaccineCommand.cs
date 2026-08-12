using MediatR;

namespace VaccinationControl.Application.Vaccines.Commands.DeleteVaccine
{
    /// <summary>
    /// Remove uma vacina do catálogo. Vacina com dose registrada não é removida: os
    /// registros de vacinação são o histórico da pessoa e não podem perder a referência.
    /// </summary>
    public record DeleteVaccineCommand(Guid Id) : IRequest;
}
