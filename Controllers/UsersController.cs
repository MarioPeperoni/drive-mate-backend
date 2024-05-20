using Drive_Mate_Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Drive_Mate_Server.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly MyDbContext _db;
        public UsersController(MyDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("{id}/driver")]
        public async Task<IActionResult> GetRidesAsDriver(string id)
        {
            try
            {
                var user = await _db.Users
                    .Include(u => u.RidesAsDriver)
                    .FirstOrDefaultAsync(u => u.ClerkId == id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found.");
                }

                var orderedRides = user.RidesAsDriver
                    .OrderByDescending(r => r.StartDate)
                    .ToList();

                return Ok(orderedRides);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }

        }

        [HttpGet]
        [Route("{id}/passenger")]
        public async Task<IActionResult> GetRidesAsPassenger(string id)
        {
            try
            {
                var user = await _db.Users
                    .Include(u => u.RidesAsPassenger)
                    .ThenInclude(p => p.Ride)
                    .FirstOrDefaultAsync(u => u.ClerkId == id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found.");
                }

                var orderedRides = user.RidesAsPassenger
                    .Select(p => p.Ride)
                    .OrderByDescending(r => r.StartDate)
                    .ToList();

                return Ok(orderedRides);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }

        }
    }
}