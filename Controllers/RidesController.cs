using Drive_Mate_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Drive_Mate_Server.Data;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Drive_Mate_Server.Controllers
{
    [ApiController]
    [Route("api/rides")]
    public class RidesController : ControllerBase
    {
        private readonly MyDbContext _db;
        private readonly IMemoryCache _memoryCache;

        public RidesController(MyDbContext db, IMemoryCache memoryCache)
        {
            _db = db;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentRides()
        {
            try
            {
                if (!_memoryCache.TryGetValue("recentRides", out List<Ride>? rides))
                {
                    rides = await _db.Rides
                                .Include(r => r.Driver)
                                .Include(r => r.Passengers)
                                .OrderByDescending(r => r.CreatedAt)
                                .Take(10)
                                .ToListAsync();
                    _memoryCache.Set("recentRides", rides, TimeSpan.FromMinutes(5));
                }
                return Ok(rides);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRide(int id)
        {
            try
            {

                Ride? ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == id);
                if (ride == null)
                {
                    return NotFound();
                }
                return Ok(ride);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> SearchRides(string from, string to, DateTime startDate)
        {
            try
            {
                // Extracting the date part from startDate
                DateTime startDateDay = startDate.Date;
                // Extracting the date part from the next day
                DateTime nextDay = startDateDay.AddDays(1);

                // Fetching rides where StartDate falls within the specific day
                var rides = await _db.Rides
                    .Include(r => r.Driver)
                    .Include(r => r.Passengers)
                    .Where(r => r.From == from && r.To == to && r.StartDate >= startDateDay && r.StartDate < nextDay)
                    .ToListAsync();

                return Ok(rides);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateRide(RideCreationModel data)
        {
            // Creates a test ride with the current user as the driver
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return BadRequest("User not found");
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => u.ClerkId == userId);
                if (user == null)
                {
                    return BadRequest("User not found");
                }

                var ride = new Ride
                {
                    From = data.From,
                    To = data.To,
                    UserId = user.Id,
                    StartDate = data.StartDate,
                    EndDate = data.EndDate,
                    Seats = data.Seats,
                    Price = data.Price,
                    Car = data.Car,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _db.Rides.AddAsync(ride);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRide), new { id = ride.Id }, ride);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> AddPassanger(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return BadRequest("User not found");
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => u.ClerkId == userId);
                if (user == null)
                {
                    return BadRequest("User not found");
                }

                var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == id);
                if (ride == null)
                {
                    return NotFound();
                }

                var ridePassenger = new Passenger
                {
                    UserId = user.Id,
                    RideId = ride.Id
                };

                await _db.Passengers.AddAsync(ridePassenger);
                await _db.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("{id}/passengers")]
        public async Task<IActionResult> GetRidePassengers(int id)
        {
            try
            {
                var ride = await _db.Rides.Include(r => r.Passengers).FirstOrDefaultAsync(r => r.Id == id);
                if (ride == null)
                {
                    return NotFound();
                }
                return Ok(ride.Passengers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}