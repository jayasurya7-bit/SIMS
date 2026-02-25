namespace SIMS.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public int RoleID { get; set; }
    }



    public class Incident
    {
        public int IncidentID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public string FilePath { get; set; }
        public int ReportedBy { get; set; }
        public int? AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReporterName { get; set; }
        public string InvestigatorName { get; set; }
    }

    public class InternalMessage
    {
        public int MessageID { get; set; }
        public int IncidentID { get; set; }
        public int SenderID { get; set; }
        public string MessageBody { get; set; }
        public DateTime SentAt { get; set; }
    }
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}