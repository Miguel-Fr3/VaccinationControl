namespace VaccinationControl.Application.Common.Interfaces
{
    /// <summary>
    /// Abstrai o algoritmo de hash da senha. A Application decide *quando* verificar; a
    /// Infrastructure decide *como* — trocar o algoritmo não toca em caso de uso nenhum.
    /// </summary>
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string password, string passwordHash);
    }
}
