using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Domain.Enums;
using ElectroPi.Domain.Exceptions;
using ElectroPi.Tests.Common;
using Moq;

namespace ElectroPi.Tests.Tickets
{
    [Collection(TicketSqlServerCollection.Name)]
    public class TicketService_UpdateTests : TicketServiceTestBase
    {
        public TicketService_UpdateTests(SqlServerTestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task UpdateAsync_TicketDoesNotExist_ThrowsNotFoundException()
        {
            var admin = AddUser("admin-1", UserRole.Admin);
            var customer = AddUser("customer-1", UserRole.Customer);

            var request = new CreateUpdateTicket
            {
                Title = "Anything",
                CustomerId = customer.Id,
            };

            await Assert.ThrowsAsync<NotFoundException>(() => Sut.UpdateAsync(999, request, admin.Id));
        }

        [Fact]
        public async Task UpdateAsync_Customer_ThrowsForbidden()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var ticket = AddTicket(customer.Id);

            var request = new CreateUpdateTicket
            {
                Title = "Trying to edit",
                CustomerId = customer.Id,
            };

            await Assert.ThrowsAsync<ForbiddenException>(() => Sut.UpdateAsync(ticket.Id, request, customer.Id));
        }

        [Fact]
        public async Task UpdateAsync_Agent_UpdatesTitleAndDescription()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id, title: "Old title");

            var request = new CreateUpdateTicket
            {
                Title = "New title",
                Description = "New description",
                Priority = ticket.Priority,
                CustomerId = customer.Id,
                AgentId = agent.Id,
            };

            var result = await Sut.UpdateAsync(ticket.Id, request, agent.Id);

            Assert.Equal("New title", result.Title);
            Assert.Equal("New description", result.Description);
        }

        [Fact]
        public async Task UpdateAsync_PriorityChanged_LogsPriorityChangedActivity()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id, priority: TicketPriority.Low, title: "T");

            var request = new CreateUpdateTicket
            {
                Title = ticket.Title,
                Priority = TicketPriority.Critical,
                CustomerId = customer.Id,
                AgentId = agent.Id,
            };

            await Sut.UpdateAsync(ticket.Id, request, agent.Id);

            var activity = Assert.Single(DbContext.TicketActivities
                .Where(a => a.TicketId == ticket.Id && a.Type == TicketActivityType.PriorityChanged));
            Assert.Equal(nameof(TicketPriority.Low), activity.OldValue);
            Assert.Equal(nameof(TicketPriority.Critical), activity.NewValue);
        }

        [Fact]
        public async Task UpdateAsync_Admin_AssignsAgent_ValidatesAgentAndLogsActivity()
        {
            var admin = AddUser("admin-1", UserRole.Admin);
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, title: "T");

            var request = new CreateUpdateTicket
            {
                Title = ticket.Title,
                Priority = ticket.Priority,
                CustomerId = customer.Id,
                AgentId = agent.Id,
            };

            var result = await Sut.UpdateAsync(ticket.Id, request, admin.Id);

            Assert.Equal(agent.Id, result.AgentId);
            AuthServiceMock.Verify(x => x.GetByIdAsync(agent.Id), Times.AtLeastOnce);

            var activity = Assert.Single(DbContext.TicketActivities
                .Where(a => a.TicketId == ticket.Id && a.Type == TicketActivityType.AgentAssigned));
            Assert.Equal(agent.Id, activity.NewValue);
        }

        [Fact]
        public async Task UpdateAsync_Admin_UnassignsAgent_LogsActivity()
        {
            var admin = AddUser("admin-1", UserRole.Admin);
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id, title: "T");

            var request = new CreateUpdateTicket
            {
                Title = ticket.Title,
                Priority = ticket.Priority,
                CustomerId = customer.Id,
                AgentId = null,
            };

            var result = await Sut.UpdateAsync(ticket.Id, request, admin.Id);

            Assert.Null(result.AgentId);

            var activity = Assert.Single(DbContext.TicketActivities
                .Where(a => a.TicketId == ticket.Id && a.Type == TicketActivityType.AgentUnassigned));
            Assert.Equal(agent.Id, activity.OldValue);
        }

        [Fact]
        public async Task UpdateAsync_Agent_CannotReassignAgent()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var otherAgent = AddUser("agent-2", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id, title: "T");

            var request = new CreateUpdateTicket
            {
                Title = ticket.Title,
                Priority = ticket.Priority,
                CustomerId = customer.Id,
                AgentId = otherAgent.Id,
            };

            var result = await Sut.UpdateAsync(ticket.Id, request, agent.Id);

            // Reassignment is an Admin-only action; a non-admin's AgentId change is silently ignored.
            Assert.Equal(agent.Id, result.AgentId);
        }
    }
}
