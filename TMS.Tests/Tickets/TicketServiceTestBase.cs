using AutoMapper;
using ElectroPi.Application.Dtos.Auth;
using ElectroPi.Application.Interfaces;
using ElectroPi.Application.MappingProfiles;
using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Enums;
using ElectroPi.Infrastructure.Persistance;
using ElectroPi.Infrastructure.Services.Tickets;
using ElectroPi.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ElectroPi.Tests.Tickets
{
    // Schema for the shared test database is created once by SqlServerTestDatabaseFixture;
    // each test instance wipes the data (not the schema) so it starts from a clean slate.
    // All ticket test classes share TicketSqlServerCollection.Name so xUnit runs them
    // sequentially against the one database instead of racing each other.
    public abstract class TicketServiceTestBase : IDisposable
    {
        protected readonly AppDbContext DbContext;
        protected readonly Mock<IAuthService> AuthServiceMock = new();
        protected readonly TicketService Sut;

        protected TicketServiceTestBase(SqlServerTestDatabaseFixture fixture)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(fixture.ConnectionString)
                .Options;

            DbContext = new AppDbContext(options);

            // Tickets cascade-delete their comments/activities/time entries.
            DbContext.Database.ExecuteSqlRaw("DELETE FROM Tickets; DELETE FROM AspNetUsers;");

            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<TicketProfile>(), NullLoggerFactory.Instance);
            var mapper = mapperConfig.CreateMapper();

            var helperService = new TicketHelperService(DbContext, NullLogger<TicketHelperService>.Instance);

            Sut = new TicketService(
                DbContext,
                AuthServiceMock.Object,
                helperService,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<TicketService>.Instance,
                mapper);
        }

        protected AppUser AddUser(string id, UserRole role, string? fullName = null)
        {
            var user = new AppUser
            {
                Id = id,
                UserName = $"{id}@test.local",
                Email = $"{id}@test.local",
                FullName = fullName ?? id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            DbContext.Users.Add(user);
            DbContext.SaveChanges();

            AuthServiceMock
                .Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = role.ToString(),
                });

            return user;
        }

        protected Ticket AddTicket(string customerId, string? agentId = null, TicketStatus status = TicketStatus.Open,
            TicketPriority priority = TicketPriority.Medium, string title = "Sample ticket")
        {
            var ticket = new Ticket
            {
                Title = title,
                Priority = priority,
                Status = status,
                CustomerId = customerId,
                AgentId = agentId,
                CreatedAt = DateTime.UtcNow,
            };

            DbContext.Tickets.Add(ticket);
            DbContext.SaveChanges();

            return ticket;
        }

        public void Dispose()
        {
            DbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
