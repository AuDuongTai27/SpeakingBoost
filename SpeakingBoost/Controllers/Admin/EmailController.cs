using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Services.Interfaces.Email;
using SpeakingBoost.Services.Implementations.Email;


namespace SpeakingBoost.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail()
        {
            await _emailService.SendEmailAsync(
                "phat.duongtan.cit22@eiu.edu.vn",
                "TEST EMAIL",
                "Hello from SpeakingBoost"
            );

            return Ok("Sent");
        }
    }
}
