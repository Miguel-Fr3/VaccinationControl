namespace VaccinationControl.Domain.Entities
{
    public class Person : EntityBase
    {
        public required string Name { get; set; }
        public required string Document { get; set; }
        public ICollection<VaccinationRecord> VaccinationRecords { get; set; } = [];
    }
}
