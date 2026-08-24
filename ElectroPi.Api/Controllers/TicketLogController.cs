using ElectroPi.Application.Dtos.Tickets.Time;
using ElectroPi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ElectroPi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, Agent")]
    [EnableRateLimiting("fixed")]
    public class TicketLogController : ControllerBase
    {
        private readonly ITicketLogService _ticketLogService;

        public TicketLogController(ITicketLogService ticketLogService)
        {
            _ticketLogService = ticketLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var result = await _ticketLogService.GetAllByEmpIdAsync(userId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Log(LogTimeEntryDto request)
        {
            var userId = GetUserId();
            var result = await _ticketLogService.LogAsync(request, userId);
            return Ok(result);
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
}
