using MongoDB.Bson;

namespace Drive_Mate_Server.Models
{
    public class Ride
    {
        public required ObjectId Id { get; set; }

        public required string From { get; set; }
        public required string To { get; set; }

        public DateTime RideDate { get; set; }
        public decimal Price { get; set; }

        public required ObjectId DriverId { get; set; }
        public required User Driver { get; set; }

        public List<ObjectId>? PassengersIds { get; set; }
        public List<User>? Passengers { get; set; }

        public int AvailableSeats { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}