using Microsoft.Data.SqlClient;
using SIMS.Models;

namespace SIMS.Data
{
    public class ApplicationDbContext
    {
        private readonly string _connectionString;

        public ApplicationDbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public User ValidateUser(string username, string password)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "SELECT * FROM Users WHERE Username=@u AND PasswordHash=@p";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", password);
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    return new User
                    {
                        UserID = (int)rdr["UserID"],
                        Username = rdr["Username"].ToString(),
                        RoleID = (int)rdr["RoleID"]
                    };
                }
            }
            return null;
        }
        public bool CreateIncident(Incident inc)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "INSERT INTO Incidents (Title, Description, Category, Priority, Status, ReportedBy, Location, FilePath, CreatedAt) " +
                             "VALUES (@t, @d, @cat, @p, @s, @r, @loc, @file, GETDATE())";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@t", inc.Title);
                cmd.Parameters.AddWithValue("@d", inc.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@cat", inc.Category);
                cmd.Parameters.AddWithValue("@p", inc.Priority);
                cmd.Parameters.AddWithValue("@s", "Pending");
                cmd.Parameters.AddWithValue("@r", inc.ReportedBy);
                cmd.Parameters.AddWithValue("@loc", (object)inc.Location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@file", (object)inc.FilePath ?? DBNull.Value);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public List<Incident> GetIncidentsByReporter(int reporterId)
        {
            var list = new List<Incident>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "SELECT * FROM Incidents WHERE ReportedBy = @r ORDER BY CreatedAt DESC";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@r", reporterId);
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var inc = MapIncident(rdr);
                    if (inc.Status == "Archived")
                        inc.Status = (inc.Priority == "Critical") ? "Denied" : "Resolved";
                    list.Add(inc);
                }
            }
            return list;
        }

        public List<Incident> GetAllIncidents()
        {
            var list = new List<Incident>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT i.*, u1.Username as ReporterName, u2.Username as InvestigatorName 
                               FROM Incidents i 
                               LEFT JOIN Users u1 ON i.ReportedBy = u1.UserID 
                               LEFT JOIN Users u2 ON i.AssignedTo = u2.UserID 
                               ORDER BY i.CreatedAt DESC";
                SqlCommand cmd = new SqlCommand(sql, conn);
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read()) { list.Add(MapIncident(rdr)); }
            }
            return list;
        }
        public bool UpdateIncidentStatus(int id, string status)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE Incidents SET Status = @s WHERE IncidentID = @id";
                if (status == "Denied")
                {
                    sql = "UPDATE Incidents SET Status = 'Denied', Priority = 'Critical' WHERE IncidentID = @id";
                }
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
        public void AssignIncident(int incId, int invId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE Incidents SET AssignedTo=@inv, Status='Assigned' WHERE IncidentID=@inc";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@inv", invId);
                cmd.Parameters.AddWithValue("@inc", incId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public bool CancelAssignment(int incId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE Incidents SET AssignedTo = NULL, Status = 'Pending' WHERE IncidentID = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", incId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
        public List<Incident> GetAssignedIncidents(int investigatorId)
        {
            var list = new List<Incident>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT i.*, u1.Username as ReporterName 
                               FROM Incidents i 
                               LEFT JOIN Users u1 ON i.ReportedBy = u1.UserID 
                               WHERE i.AssignedTo = @id AND i.Status != 'Archived' 
                               ORDER BY i.CreatedAt DESC";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", investigatorId);
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read()) { list.Add(MapIncident(rdr)); }
            }
            return list;
        }
        public void SendMessage(InternalMessage msg)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "INSERT INTO InternalMessages (IncidentID, SenderID, MessageBody, SentAt) VALUES (@i, @s, @m, GETDATE())";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@i", msg.IncidentID);
                cmd.Parameters.AddWithValue("@s", msg.SenderID);
                cmd.Parameters.AddWithValue("@m", msg.MessageBody);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<InternalMessage> GetMessages(int incidentId)
        {
            var list = new List<InternalMessage>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "SELECT * FROM InternalMessages WHERE IncidentID=@i ORDER BY SentAt ASC";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@i", incidentId);
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new InternalMessage
                    {
                        MessageID = (int)rdr["MessageID"],
                        IncidentID = (int)rdr["IncidentID"],
                        SenderID = (int)rdr["SenderID"],
                        MessageBody = rdr["MessageBody"].ToString(),
                        SentAt = (DateTime)rdr["SentAt"]
                    });
                }
            }
            return list;
        }
        public List<User> GetAllInvestigators()
        {
            var list = new List<User>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT UserID, Username FROM Users WHERE RoleID=3", conn);
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read()) { list.Add(new User { UserID = (int)rdr["UserID"], Username = rdr["Username"].ToString() }); }
            }
            return list;
        }

        private Incident MapIncident(SqlDataReader rdr)
        {
            var incident = new Incident
            {
                IncidentID = (int)rdr["IncidentID"],
                Title = rdr["Title"].ToString(),
                Status = rdr["Status"].ToString(),
                Category = rdr["Category"].ToString(),
                Priority = rdr["Priority"].ToString(),
                AssignedTo = rdr["AssignedTo"] == DBNull.Value ? (int?)null : (int)rdr["AssignedTo"],
                CreatedAt = (DateTime)rdr["CreatedAt"]
            };
            if (ColumnExists(rdr, "Location"))
                incident.Location = rdr["Location"] == DBNull.Value ? "Not Specified" : rdr["Location"].ToString();

            if (ColumnExists(rdr, "FilePath"))
                incident.FilePath = rdr["FilePath"] == DBNull.Value ? null : rdr["FilePath"].ToString();

            if (ColumnExists(rdr, "Description"))
                incident.Description = rdr["Description"] == DBNull.Value ? "" : rdr["Description"].ToString();

            if (ColumnExists(rdr, "ReporterName"))
                incident.ReporterName = rdr["ReporterName"].ToString();

            if (ColumnExists(rdr, "InvestigatorName"))
                incident.InvestigatorName = rdr["InvestigatorName"] == DBNull.Value ? "Not Assigned" : rdr["InvestigatorName"].ToString();

            return incident;
        }
        private bool ColumnExists(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }
    }
}