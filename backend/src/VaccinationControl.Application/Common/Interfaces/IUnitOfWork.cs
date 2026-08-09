namespace VaccinationControl.Application.Common.Interfaces
{
    /// <summary>
    /// Confirma as alterações acumuladas pelos repositórios. Separado das interfaces de
    /// repositório para que um handler possa compor várias escritas numa única transação.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
