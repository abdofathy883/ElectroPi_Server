using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Domain.Enums;
using ElectroPi.Tests.Common;

namespace ElectroPi.Tests.Tickets
{
    [Collection(TicketSqlServerCollection.Name)]
    public class TicketService_CreateTests : TicketServiceTestBase
    {
        public TicketService_CreateTests(SqlServerTestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task CreateAsync_Customer_IgnoresSubmittedCustomerId_AndUsesSelf()
        {
            var customer = AddUser("customer-1", UserRole.Customer);
            var otherCustomer = AddUser("customer-2", UserRole.Customer);

            var request = new CreateUpdateTicket
            {
                Title = "Printer not working",
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                CustomerId = otherCustomer.Id, // Attempting to create a ticket "for" someone else.
            };

            var result = await Sut.CreateAsync(request, customer.Id);

            Assert.Equal(customer.Id, result.CustomerId);
        }

        [Fact]
        public async Task CreateAsync_Admin_CreatesTicketForSpecifiedCustomer()
        {
            var admin = AddUser("admin-1", UserRole.Admin);
            var customer = AddUser("customer-1", UserRole.Customer);

            var request = new CreateUpdateTicket
            {
                Title = "New workstation request",
                Priority = TicketPriority.Low,
                Status = TicketStatus.Open,
                CustomerId = customer.Id,
            };

            var result = await Sut.CreateAsync(request, admin.Id);

            Assert.Equal(customer.Id, result.CustomerId);
        }

        [Fact]
        public async Task CreateAsync_NewTicket_StartsOpenAndLogsCreationActivity()
        {
            var customer = AddUser("customer-1", UserRole.Customer);

            var request = new CreateUpdateTicket
            {
                Title = "VPN access issue",
                Priority = TicketPriority.High,
                Status = TicketStatus.InProgress, // Should be ignored; new tickets always start Open.
                CustomerId = customer.Id,
            };

            var result = await Sut.CreateAsync(request, customer.Id);

            Assert.Equal(TicketStatus.Open, result.Status);

            var activity = Assert.Single(DbContext.TicketActivities.Where(a => a.TicketId == result.Id));
            Assert.Equal(TicketActivityType.TicketCreation, activity.Type);
        }
    }
}
