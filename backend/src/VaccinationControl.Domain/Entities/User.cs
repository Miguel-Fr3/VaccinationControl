namespace VaccinationControl.Domain.Entities
{
    /// <summary>
    /// Credencial de acesso à API. Não tem relação com <see cref="Person"/>: pessoa é o
    /// titular de um cartão de vacinação, usuário é quem opera o sistema.
    /// </summary>
    public class User : EntityBase
    {
        public required string Email { get; set; }

        // Nunca a senha em claro — só o hash produzido pelo IPasswordHasher.
        public required string PasswordHash { get; set; }
    }
}
