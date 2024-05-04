using Drive_Mate_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Drive_Mate_Server.Data;
using MongoDB.Bson;

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
                    rides = await _db.Rides.Take(10).ToListAsync();
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
        public async Task<IActionResult> GetRide(string id)
        {
            try
            {
                // Convert the string id to an ObjectId
                if (!ObjectId.TryParse(id, out ObjectId objectId))
                {
                    return BadRequest("Invalid ride id");
                }

                Ride? ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == objectId);
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

        [HttpPost]
        public async Task<IActionResult> CreateRide(Ride ride)
        {
            try
            {
                ride.Id = ObjectId.GenerateNewId();
                ride.CreatedAt = DateTime.UtcNow;
                ride.UpdatedAt = DateTime.UtcNow;
                await _db.Rides.AddAsync(ride);
                await _db.SaveChangesAsync();
                return CreatedAtAction(nameof(GetRide), new { id = ride.Id }, ride);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}