using ElectroPi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ElectroPi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("fixed")]
    public class TicketReportingController : ControllerBase
    {
        private readonly ITicketReportingService _ticketReportingService;

        public TicketReportingController(ITicketReportingService ticketReportingService)
        {
            _ticketReportingService = ticketReportingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReport()
        {
            var result = await _ticketReportingService.GetReportAsync();
            return Ok(result);
        }
    }
}
