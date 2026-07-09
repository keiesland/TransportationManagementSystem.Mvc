namespace TransportationManagementSystem.Mvc.Data.DTOs
{
    public class ImportResult
    {
        public bool IsValid { get; set; }
        public int ImportedCount { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
    }
}
