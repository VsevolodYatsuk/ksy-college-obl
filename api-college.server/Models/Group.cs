namespace api_college.server.Models
{
    public class Group
    {
        public int Id { get; set; }
        public int SpecializationId { get; set; }
        public string? Name { get; set; }
        public int NumberOfStudents { get; set; }
    }
}