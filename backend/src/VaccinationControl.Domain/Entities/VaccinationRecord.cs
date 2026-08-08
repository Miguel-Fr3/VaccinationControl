using VaccinationControl.Domain.Enums;
namespace VaccinationControl.Domain.Entities
{
    public class VaccinationRecord : EntityBase
    {
        public Guid PersonId { get; set; }
        public Person Person { get; set; } = null!;
        public Guid VaccineId { get; set; }
        public Vaccine Vaccine { get; set; } = null!;
        public VaccinationTypeEnum VaccinationType { get; set; }
        public int DoseNumber { get; set; }
        public DateOnly VaccinationDate { get; set; }

    }
}
