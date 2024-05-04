using MongoDB.Bson;

namespace Drive_Mate_Server.Models
{
    public class User
    {
        public required ObjectId Id { get; set; }

        public required string ClerkId { get; set; }
    }
}