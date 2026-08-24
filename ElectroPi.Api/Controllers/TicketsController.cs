using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Application.Dtos.Tickets.Comments;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectroPi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet]
        [Route("tickets")]
        public async Task<IActionResult> GetAll([FromQuery] TicketFilterDto request)
        {
            var userId = GetUserId();
            var result = await _ticketService.GetAllAsync(request, userId);
            return Ok(result);
        }

        [HttpGet]
        [Route("ticket/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _ticketService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [Route("ticket")]
        public async Task<IActionResult> Create(CreateUpdateTicket request)
        {
            try
            {
                var result = await _ticketService.CreateAsync(request);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update(CreateUpdateTicket request)
        {
            try
            {
                var result = await _ticketService.UpdateAsync(request);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch]
        [Route("{id}/{status}")]
        public async Task<IActionResult> ChangeStatus(int id, TicketStatus status)
        {
            var userId = GetUserId();
            try
            {
                var result = await _ticketService.ChangeStatusAsync(id, status, userId);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("search/{query}")]
        public async Task<IActionResult> Search(string query)
        {
            var userId = GetUserId();
            var result = await _ticketService.SearchAsync(query, userId);
            return Ok(result);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _ticketService.DeleteAsync(id);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("comment")]
        public async Task<IActionResult> CreateComment(CreateTicketCommentDto request)
        {
            try
            {
                var result = await _ticketService.CreateCommentAsync(request);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
}
