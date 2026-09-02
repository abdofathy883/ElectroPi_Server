using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Domain.Enums;
using ElectroPi.Domain.Exceptions;
using ElectroPi.Tests.Common;

namespace ElectroPi.Tests.Tickets
{
    [Collection(TicketSqlServerCollection.Name)]
    public class TicketService_ReadTests : TicketServiceTestBase
    {
        public TicketService_ReadTests(SqlServerTestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task GetByIdAsync_TicketDoesNotExist_ThrowsNotFoundException()
        {
            AddUser("admin-1", UserRole.Admin);

            await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(999, "admin-1"));
        }

        [Fact]
        public async Task GetByIdAsync_CustomerAccessingOwnTicket_ReturnsTicket()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var ticket = AddTicket(customer.Id);

            var result = await Sut.GetByIdAsync(ticket.Id, customer.Id);

            Assert.Equal(ticket.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_CustomerAccessingAnotherCustomersTicket_ThrowsForbidden()
        {
            var owner = AddUser("customer-1", UserRole.Customer);
            var intruder = AddUser("customer-2", UserRole.Customer);
            var ticket = AddTicket(owner.Id);

            await Assert.ThrowsAsync<ForbiddenException>(() => Sut.GetByIdAsync(ticket.Id, intruder.Id));
        }

        [Fact]
        public async Task GetByIdAsync_AgentAssignedToTicket_ReturnsTicket()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id);

            var result = await Sut.GetByIdAsync(ticket.Id, agent.Id);

            Assert.Equal(ticket.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_AgentNotAssignedToTicket_ThrowsForbidden()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var assignedAgent = AddUser("agent-1", UserRole.Agent);
            var otherAgent = AddUser("agent-2", UserRole.Agent);
            var ticket = AddTicket(customer.Id, assignedAgent.Id);

            await Assert.ThrowsAsync<ForbiddenException>(() => Sut.GetByIdAsync(ticket.Id, otherAgent.Id));
        }

        [Fact]
        public async Task GetByIdAsync_Admin_CanAccessAnyTicket()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var admin = AddUser("admin-1", UserRole.Admin);
            var ticket = AddTicket(customer.Id);

            var result = await Sut.GetByIdAsync(ticket.Id, admin.Id);

            Assert.Equal(ticket.Id, result.Id);
        }

        [Fact]
        public async Task GetAllAsync_Customer_OnlySeesOwnTickets()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var otherCustomer = AddUser("customer-2", UserRole.Customer);
            AddTicket(customer.Id, title: "Mine");
            AddTicket(otherCustomer.Id, title: "Not mine");

            var result = await Sut.GetAllAsync(new TicketFilterDto(), customer.Id);

            var item = Assert.Single(result.Items);
            Assert.Equal("Mine", item.Title);
        }

        [Fact]
        public async Task GetAllAsync_FiltersByStatus()
        {
            var admin = AddUser("admin-1", UserRole.Admin);
            var customer = AddUser("customer-1", UserRole.Customer);
            AddTicket(customer.Id, status: TicketStatus.Open, title: "Open one");
            AddTicket(customer.Id, status: TicketStatus.Closed, title: "Closed one");

            var result = await Sut.GetAllAsync(new TicketFilterDto { Status = TicketStatus.Closed }, admin.Id);

            var item = Assert.Single(result.Items);
            Assert.Equal("Closed one", item.Title);
        }

        [Fact]
        public async Task GetAllAsync_Paginates_UsingPageNumberAndPageSize()
        {
            var admin = AddUser("admin-1", UserRole.Admin);
            var customer = AddUser("customer-1", UserRole.Customer);
            for (var i = 0; i < 5; i++)
                AddTicket(customer.Id, title: $"Ticket {i}");

            var result = await Sut.GetAllAsync(new TicketFilterDto { PageNumber = 2, PageSize = 2 }, admin.Id);

            Assert.Equal(2, result.Items.Count);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Page);
        }
    }
}
