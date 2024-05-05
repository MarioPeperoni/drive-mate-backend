using System.Text;
using Microsoft.AspNetCore.Mvc;
using Svix;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Drive_Mate_Server.Data;
using Drive_Mate_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Webhooks.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly MyDbContext _db;
        private readonly IConfiguration _configuration;

        public WebhooksController(MyDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {

            // Get the webhook secret from the dotnet secrets
            var webhookSecret = _configuration["Clerk:Webhook"];

            if (string.IsNullOrEmpty(webhookSecret))
            {
                throw new InvalidOperationException("Please add WEBHOOK_SECRET from Clerk Dashboard to environment variables");
            }

            // Get the headers
            var svixId = Request.Headers["svix-id"];
            var svixTimestamp = Request.Headers["svix-timestamp"];
            var svixSignature = Request.Headers["svix-signature"];

            // If there are no headers, error out
            if (string.IsNullOrEmpty(svixId) || string.IsNullOrEmpty(svixTimestamp) || string.IsNullOrEmpty(svixSignature))
            {
                return BadRequest("Error occurred -- no svix headers");
            }

            // Get the body
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            // Create a new Svix instance with your secret.
            var wh = new Webhook(webhookSecret);

            try
            {
                // Verify the payload with the headers
                wh.Verify(body, new System.Net.WebHeaderCollection
                {
                    { "svix-id", svixId },
                    { "svix-timestamp", svixTimestamp },
                    { "svix-signature", svixSignature }
                });

                dynamic eventData = JsonConvert.DeserializeObject<dynamic>(body)!;

                // Access the necessary properties
                string eventType = eventData.type;
                JObject userData = eventData.data;

                switch (eventType)
                {
                    case "user.created":
                        await UserCreated(userData);
                        break;
                    case "user.updated":
                        await UserUpdated(userData);
                        break;
                    case "user.deleted":
                        await UserDeleted(userData);
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        private async Task<IActionResult> UserCreated(dynamic data)
        {
            var user = new User
            {
                ClerkId = data.id,
                Email = data.email_addresses[0].email_address,
                FirstName = data.first_name,
                LastName = data.last_name,
                Username = data.username,
                ImageUrl = data.image_url
            };

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            return Ok();
        }


        private async Task<IActionResult> UserUpdated(dynamic data)
        {
            if (data.id == null)
            {
                return BadRequest();
            }

            var userId = (string)data.id;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.ClerkId == userId);

            if (user == null)
            {
                return NotFound();
            }

            user.ClerkId = userId;
            user.Email = data.email_addresses[0].email_address;
            user.FirstName = data.first_name;
            user.LastName = data.last_name;
            user.Username = data.username;
            user.ImageUrl = data.image_url;

            await _db.SaveChangesAsync();

            return Ok();
        }

        private async Task<IActionResult> UserDeleted(dynamic data)
        {
            if (data.id == null)
            {
                return BadRequest();
            }

            var userId = (string)data.id;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.ClerkId == userId);

            if (user == null)
            {
                return NotFound();
            }
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
