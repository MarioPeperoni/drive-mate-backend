using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drive_Mate_Server.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string ClerkId { get; set; }

        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Username { get; set; }
        public required string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Ride> RidesAsDriver { get; set; }
        public ICollection<Passenger> RidesAsPassenger { get; set; }

    }
}