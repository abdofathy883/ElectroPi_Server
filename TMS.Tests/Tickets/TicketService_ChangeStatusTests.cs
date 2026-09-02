using ElectroPi.Domain.Enums;
using ElectroPi.Domain.Exceptions;
using ElectroPi.Tests.Common;

namespace ElectroPi.Tests.Tickets
{
    [Collection(TicketSqlServerCollection.Name)]
    public class TicketService_ChangeStatusTests : TicketServiceTestBase
    {
        public TicketService_ChangeStatusTests(SqlServerTestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ChangeStatusAsync_TicketDoesNotExist_ThrowsNotFoundException()
        {
            AddUser("admin-1", UserRole.Admin);

            await Assert.ThrowsAsync<NotFoundException>(() => Sut.ChangeStatusAsync(999, TicketStatus.Closed, "admin-1"));
        }

        [Fact]
        public async Task ChangeStatusAsync_SameStatus_ReturnsTrueWithoutLoggingActivity()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var ticket = AddTicket(customer.Id, status: TicketStatus.Open);

            var result = await Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Open, customer.Id);

            Assert.True(result);
            Assert.Empty(DbContext.TicketActivities.Where(a => a.TicketId == ticket.Id));
        }

        [Fact]
        public async Task ChangeStatusAsync_Agent_AllowedTransition_Succeeds()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id, status: TicketStatus.Open);

            var result = await Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Acknowledged, agent.Id);

            Assert.True(result);
            Assert.Equal(TicketStatus.Acknowledged, DbContext.Tickets.Single(t => t.Id == ticket.Id).Status);
        }

        [Fact]
        public async Task ChangeStatusAsync_Agent_DisallowedTransition_ThrowsInvalidOperationException()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id, status: TicketStatus.Open);

            // Open -> Resolved is not an allowed direct transition for non-admins.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved, agent.Id));
        }

        [Fact]
        public async Task ChangeStatusAsync_Agent_NotAssignedToTicket_ThrowsForbidden()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var assignedAgent = AddUser("agent-1", UserRole.Agent);
            var otherAgent = AddUser("agent-2", UserRole.Agent);
            var ticket = AddTicket(customer.Id, assignedAgent.Id, status: TicketStatus.Open);

            await Assert.ThrowsAsync<ForbiddenException>(
                () => Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Acknowledged, otherAgent.Id));
        }

        [Fact]
        public async Task ChangeStatusAsync_Customer_CanCloseResolvedTicket()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var ticket = AddTicket(customer.Id, status: TicketStatus.Resolved);

            var result = await Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Closed, customer.Id);

            Assert.True(result);
            Assert.Equal(TicketStatus.Closed, DbContext.Tickets.Single(t => t.Id == ticket.Id).Status);
        }

        [Fact]
        public async Task ChangeStatusAsync_Customer_CannotMakeOtherTransitions_ThrowsForbidden()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var ticket = AddTicket(customer.Id, status: TicketStatus.Open);

            await Assert.ThrowsAsync<ForbiddenException>(
                () => Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Acknowledged, customer.Id));
        }

        [Fact]
        public async Task ChangeStatusAsync_Admin_CanForceAnyTransition()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var admin = AddUser("admin-1", UserRole.Admin);
            var ticket = AddTicket(customer.Id, status: TicketStatus.Open);

            var result = await Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Closed, admin.Id);

            Assert.True(result);
            Assert.Equal(TicketStatus.Closed, DbContext.Tickets.Single(t => t.Id == ticket.Id).Status);
        }

        [Fact]
        public async Task ChangeStatusAsync_ToResolved_SetsResolvedAtTimestamp()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id, status: TicketStatus.InProgress);

            await Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved, agent.Id);

            var updated = DbContext.Tickets.Single(t => t.Id == ticket.Id);
            Assert.NotNull(updated.ResolvedAt);
        }

        [Fact]
        public async Task ChangeStatusAsync_ToClosed_SetsClosedAtTimestamp()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var ticket = AddTicket(customer.Id, status: TicketStatus.Resolved);

            await Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Closed, customer.Id);

            var updated = DbContext.Tickets.Single(t => t.Id == ticket.Id);
            Assert.NotNull(updated.ClosedAt);
        }

        [Fact]
        public async Task ChangeStatusAsync_ValidTransition_LogsStatusChangedActivity()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var ticket = AddTicket(customer.Id, agent.Id, status: TicketStatus.Open);

            await Sut.ChangeStatusAsync(ticket.Id, TicketStatus.Acknowledged, agent.Id);

            var activity = Assert.Single(DbContext.TicketActivities.Where(a => a.TicketId == ticket.Id));
            Assert.Equal(TicketActivityType.StatusChanged, activity.Type);
            Assert.Equal(nameof(TicketStatus.Open), activity.OldValue);
            Assert.Equal(nameof(TicketStatus.Acknowledged), activity.NewValue);
        }
    }
}
