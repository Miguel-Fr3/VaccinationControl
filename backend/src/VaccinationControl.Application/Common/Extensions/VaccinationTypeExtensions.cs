using VaccinationControl.Domain.Enums;

namespace VaccinationControl.Application.Common.Extensions
{
    public static class VaccinationTypeExtensions
    {
        /// <summary>
        /// Nome do tipo em português, para compor mensagens legíveis ao usuário da API.
        /// Registro e remoção descrevem as mesmas doses nas suas mensagens de conflito, então
        /// a tradução vive num lugar só — mudar o texto aqui reflete nos dois.
        /// </summary>
        public static string Describe(this VaccinationTypeEnum vaccinationType)
        {
            return vaccinationType switch
            {
                VaccinationTypeEnum.BoosterDose => "dose de reforço",
                _ => "dose"
            };
        }
    }
}
