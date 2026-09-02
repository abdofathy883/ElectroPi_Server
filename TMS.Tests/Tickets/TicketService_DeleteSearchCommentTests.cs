using ElectroPi.Application.Dtos.Tickets.Comments;
using ElectroPi.Domain.Enums;
using ElectroPi.Domain.Exceptions;
using ElectroPi.Tests.Common;

namespace ElectroPi.Tests.Tickets
{
    [Collection(TicketSqlServerCollection.Name)]
    public class TicketService_DeleteSearchCommentTests : TicketServiceTestBase
    {
        public TicketService_DeleteSearchCommentTests(SqlServerTestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task DeleteAsync_RemovesTicket_ReturnsTrue()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var ticket = AddTicket(customer.Id);

            var result = await Sut.DeleteAsync(ticket.Id);

            Assert.True(result);
            Assert.Null(DbContext.Tickets.SingleOrDefault(t => t.Id == ticket.Id));
        }

        [Fact]
        public async Task SearchAsync_Admin_SeesTicketsFromAnyCustomer()
        {
            var admin = AddUser("admin-1", UserRole.Admin);
            var customer1 = AddUser("customer-1", UserRole.Customer);
            var customer2 = AddUser("customer-2", UserRole.Customer);
            AddTicket(customer1.Id, title: "Network outage");
            AddTicket(customer2.Id, title: "Network slowdown");

            var result = await Sut.SearchAsync("Network", admin.Id);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SearchAsync_Agent_OnlySeesAssignedTickets()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var agent = AddUser("agent-1", UserRole.Agent);
            var otherAgent = AddUser("agent-2", UserRole.Agent);
            AddTicket(customer.Id, agent.Id, title: "Network outage - mine");
            AddTicket(customer.Id, otherAgent.Id, title: "Network outage - not mine");

            var result = await Sut.SearchAsync("Network", agent.Id);

            var item = Assert.Single(result);
            Assert.Equal("Network outage - mine", item.Title);
        }

        [Fact]
        public async Task SearchAsync_Customer_OnlySeesOwnTickets()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var otherCustomer = AddUser("customer-2", UserRole.Customer);
            AddTicket(customer.Id, title: "Network outage - mine");
            AddTicket(otherCustomer.Id, title: "Network outage - not mine");

            var result = await Sut.SearchAsync("Network", customer.Id);

            var item = Assert.Single(result);
            Assert.Equal("Network outage - mine", item.Title);
        }

        [Fact]
        public async Task SearchAsync_NonMatchingQuery_ReturnsEmpty()
        {
            var admin = AddUser("admin-1", UserRole.Admin);
            var customer = AddUser("customer-1", UserRole.Customer);
            AddTicket(customer.Id, title: "Network outage");

            var result = await Sut.SearchAsync("Printer", admin.Id);

            Assert.Empty(result);
        }

        [Fact]
        public async Task CreateCommentAsync_TicketDoesNotExist_ThrowsNotFoundException()
        {
            var customer = AddUser("customer-1", UserRole.Customer);

            var request = new CreateTicketCommentDto
            {
                TicketId = 999,
                AuthorId = customer.Id,
                Content = "Hello",
            };

            await Assert.ThrowsAsync<NotFoundException>(() => Sut.CreateCommentAsync(request, customer.Id));
        }

        [Fact]
        public async Task CreateCommentAsync_UserWithoutAccess_ThrowsForbidden()
        {
            var owner = AddUser("customer-1", UserRole.Customer);
            var intruder = AddUser("customer-2", UserRole.Customer);
            var ticket = AddTicket(owner.Id);

            var request = new CreateTicketCommentDto
            {
                TicketId = ticket.Id,
                AuthorId = intruder.Id,
                Content = "Hello",
            };

            await Assert.ThrowsAsync<ForbiddenException>(() => Sut.CreateCommentAsync(request, intruder.Id));
        }

        [Fact]
        public async Task CreateCommentAsync_AuthorIdAlwaysComesFromCurrentUser_NotRequestBody()
        {
            var owner = AddUser("customer-1", UserRole.Customer);
            var impersonated = AddUser("customer-2", UserRole.Customer);
            var ticket = AddTicket(owner.Id);

            var request = new CreateTicketCommentDto
            {
                TicketId = ticket.Id,
                AuthorId = impersonated.Id, // Attempt to post as someone else.
                Content = "Spoofed comment",
            };

            var result = await Sut.CreateCommentAsync(request, owner.Id);

            Assert.Equal(owner.Id, result.AuthorId);
        }

        [Fact]
        public async Task CreateCommentAsync_Success_PersistsCommentLinkedToTicket()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var ticket = AddTicket(customer.Id);

            var request = new CreateTicketCommentDto
            {
                TicketId = ticket.Id,
                AuthorId = customer.Id,
                Content = "Still happening",
            };

            var result = await Sut.CreateCommentAsync(request, customer.Id);

            Assert.Equal("Still happening", result.Content);
            var stored = Assert.Single(DbContext.TicketComments.Where(c => c.TicketId == ticket.Id));
            Assert.Equal(customer.Id, stored.AuthorId);
        }
    }
}
