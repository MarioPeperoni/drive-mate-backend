namespace Drive_Mate_Server.Models
{
    public class Passenger
    {
        public int UserId { get; set; }
        public int RideId { get; set; }

        public User User { get; set; }
        public Ride Ride { get; set; }
    }
}
