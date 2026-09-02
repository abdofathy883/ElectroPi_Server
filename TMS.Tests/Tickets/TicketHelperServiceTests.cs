using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Enums;
using ElectroPi.Infrastructure.Persistance;
using ElectroPi.Infrastructure.Services.Tickets;
using ElectroPi.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElectroPi.Tests.Tickets
{
    // FilterTasks operates purely on an IQueryable<Ticket> and never touches the DbContext,
    // so tests run against an in-memory list; the DbContext instance is only needed to satisfy
    // TicketHelperService's constructor.
    [Collection(TicketSqlServerCollection.Name)]
    public class TicketHelperServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly TicketHelperService _sut;

        public TicketHelperServiceTests(SqlServerTestDatabaseFixture fixture)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(fixture.ConnectionString)
                .Options;

            _dbContext = new AppDbContext(options);
            _sut = new TicketHelperService(_dbContext, NullLogger<TicketHelperService>.Instance);
        }

        private static IQueryable<Ticket> SampleTickets() => new List<Ticket>
        {
            new() { Id = 1, Title = "A", CustomerId = "customer-1", AgentId = "agent-1", Status = TicketStatus.Open, Priority = TicketPriority.Low, CreatedAt = new DateTime(2026, 1, 1) },
            new() { Id = 2, Title = "B", CustomerId = "customer-2", AgentId = "agent-2", Status = TicketStatus.Closed, Priority = TicketPriority.High, CreatedAt = new DateTime(2026, 2, 1) },
            new() { Id = 3, Title = "C", CustomerId = "customer-1", AgentId = null, Status = TicketStatus.Open, Priority = TicketPriority.High, CreatedAt = new DateTime(2026, 3, 1) },
        }.AsQueryable();

        [Fact]
        public void FilterTasks_Admin_ReturnsAllTickets()
        {
            var result = _sut.FilterTasks(SampleTickets(), new TicketFilterDto(), UserRole.Admin.ToString(), "admin-1").ToList();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void FilterTasks_Agent_OnlyReturnsAssignedTickets()
        {
            var result = _sut.FilterTasks(SampleTickets(), new TicketFilterDto(), UserRole.Agent.ToString(), "agent-1").ToList();

            var item = Assert.Single(result);
            Assert.Equal(1, item.Id);
        }

        [Fact]
        public void FilterTasks_Customer_OnlyReturnsOwnTickets()
        {
            var result = _sut.FilterTasks(SampleTickets(), new TicketFilterDto(), UserRole.Customer.ToString(), "customer-1").ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, t => Assert.Equal("customer-1", t.CustomerId));
        }

        [Fact]
        public void FilterTasks_FiltersByStatus()
        {
            var filter = new TicketFilterDto { Status = TicketStatus.Closed };

            var result = _sut.FilterTasks(SampleTickets(), filter, UserRole.Admin.ToString(), "admin-1").ToList();

            var item = Assert.Single(result);
            Assert.Equal(2, item.Id);
        }

        [Fact]
        public void FilterTasks_FiltersByPriority()
        {
            var filter = new TicketFilterDto { Priority = TicketPriority.High };

            var result = _sut.FilterTasks(SampleTickets(), filter, UserRole.Admin.ToString(), "admin-1").ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, t => Assert.Equal(TicketPriority.High, t.Priority));
        }

        [Fact]
        public void FilterTasks_FiltersByAgentId()
        {
            var filter = new TicketFilterDto { AgentId = "agent-2" };

            var result = _sut.FilterTasks(SampleTickets(), filter, UserRole.Admin.ToString(), "admin-1").ToList();

            var item = Assert.Single(result);
            Assert.Equal(2, item.Id);
        }

        [Fact]
        public void FilterTasks_FiltersByDateRange()
        {
            var filter = new TicketFilterDto
            {
                FromDate = new DateTime(2026, 2, 1),
                ToDate = new DateTime(2026, 2, 28),
            };

            var result = _sut.FilterTasks(SampleTickets(), filter, UserRole.Admin.ToString(), "admin-1").ToList();

            var item = Assert.Single(result);
            Assert.Equal(2, item.Id);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
