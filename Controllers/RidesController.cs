using Drive_Mate_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Drive_Mate_Server.Data;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Newtonsoft.Json.Linq;

namespace Drive_Mate_Server.Controllers
{
    [ApiController]
    [Route("api/rides")]
    public class RidesController : ControllerBase
    {
        private readonly MyDbContext _db;
        private readonly IMemoryCache _memoryCache;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public RidesController(MyDbContext db, IMemoryCache memoryCache, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _db = db;
            _memoryCache = memoryCache;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
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
                        .Where(r => r.Seats - r.Passengers.Count > 0)
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(10)
                        .Where(r => r.StartDate >= DateTime.Now)
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
                Ride? ride = await _db.Rides
                    .Include(r => r.Driver)
                    .Include(r => r.Passengers).ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

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
                    EndDate = GetDriveTime(data.From, data.To, data.StartDate).Result,
                    Seats = data.Seats,
                    Price = data.Price,
                    Car = data.Car,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _db.Rides.AddAsync(ride);
                await _db.SaveChangesAsync();

                await SendEmail(user.Email,
                                $"Your ride to {ride.To} is now listed",
                                $"You have successfully created a ride from {ride.From} to {ride.To} on {ride.StartDate}. Your ride will be available until {ride.StartDate}. If someone reserves a seat, you will be notified.",
                                $"<strong>You have successfully created a ride from {ride.From} to {ride.To} on {ride.StartDate}.</strong><br>Your ride will be available until {ride.StartDate}. If someone reserves a seat, you will be notified.");

                return CreatedAtAction(nameof(GetRide), new { id = ride.Id }, ride);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }

        }


        private async Task<DateTime> GetDriveTime(string from, string to, DateTime startDate)
        {
            var requestParameters = new
            {
                destinations = from,
                origins = to,
                units = "metric",
                key = _configuration["GoogleApi:Key"]
            };

            var requestUrl = "https://maps.googleapis.com/maps/api/distancematrix/json?" +
                $"destinations={Uri.EscapeDataString(requestParameters.destinations)}" +
                $"&origins={Uri.EscapeDataString(requestParameters.origins)}" +
                $"&units={Uri.EscapeDataString(requestParameters.units)}" +
                $"&key={Uri.EscapeDataString(requestParameters.key!)}";

            var response = await _httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                // Parese the JSON response
                var json = JObject.Parse(content);

                // Extract the duration value from the JSON response
                int durationValue = (int)json["rows"]![0]!["elements"]![0]!["duration"]!["value"]!;

                // Return time added to the startDate
                return startDate.AddSeconds(durationValue);
            }
            else
            {
                return startDate;
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Reserve(int id)
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
                var ride = await _db.Rides
                    .Include(r => r.Passengers)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (ride == null)
                {
                    return NotFound();
                }

                if (ride.UserId == user.Id)
                {
                    return BadRequest("You cannot reserve your own ride");
                }

                if (ride.Passengers.Count >= ride.Seats)
                {
                    return BadRequest("No seats available");
                }

                if (ride.Passengers.Any(p => p.UserId == user.Id))
                {
                    return BadRequest("You have already reserved this ride");
                }

                if (ride.StartDate < DateTime.Now)
                {
                    return BadRequest("Ride is in the past");
                }

                var ridePassenger = new Passenger
                {
                    UserId = user.Id,
                    RideId = ride.Id
                };
                var driver = await _db.Users.FirstOrDefaultAsync(u => u.Id == ride.UserId);
                if (driver == null)
                {
                    return BadRequest("Driver not found");
                }

                await SendEmail(
                    driver.Email,
                    $"New passenger in your ride to {ride.To}",
                    $"A new passenger has reserved your ride from {ride.From} to {ride.To} on {ride.StartDate}. {ridePassenger.User.FirstName} {ridePassenger.User.LastName}",
                    $"<strong>A new passenger has reserved your ride from {ride.From} to {ride.To} on {ride.StartDate}.</strong><br>Passenger: {ridePassenger.User.FirstName} {ridePassenger.User.LastName}");

                // Send an email to the passenger
                var passenger = await _db.Users.FirstOrDefaultAsync(u => u.Id == ridePassenger.UserId);
                if (passenger == null)
                {
                    return BadRequest("Passenger not found");
                }

                await SendEmail(
                    passenger.Email,
                    $"Your ride to {ride.To} has been reserved",
                    $"You have successfully reserved a ride from {ride.From} to {ride.To} on {ride.StartDate}. Your driver is {driver.FirstName} {driver.LastName}.",
                    $"<strong>You have successfully reserved a ride from {ride.From} to {ride.To} on {ride.StartDate}.</strong><br>Your driver is {driver.FirstName} {driver.LastName}.");

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

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteRide(int id)
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
                var ride = await _db.Rides
                    .Include(r => r.Passengers)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (ride == null)
                {
                    return NotFound();
                }
                if (ride.UserId != user.Id)
                {
                    return BadRequest("You are not the driver of this ride");
                }
                _db.Rides.Remove(ride);
                await _db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private async Task<IActionResult> SendEmail(string to, string subject, string plainTextContent, string htmlContent)
        {
            try
            {
                var apiKey = _configuration["SendGrid:ApiKey"];
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("embers.podium_0g@icloud.com", "Drive Mate");
                var toEmail = new EmailAddress(to);
                var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}