using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drive_Mate_Server.Models
{

    public enum RideStatus
    {
        Active,
        Completed,
        Cancelled
    }
    public class Ride
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public RideStatus Status { get; set; }

        public required string From { get; set; }
        public required string To { get; set; }

        public int UserId { get; set; }
        public User Driver { get; set; }

        public ICollection<Passenger> Passengers { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public int Seats { get; set; }
        public required string Car { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RideCreationModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public DateTime StartDate { get; set; }
        public int Seats { get; set; }
        public decimal Price { get; set; }
        public string Car { get; set; }
    }
}