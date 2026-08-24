using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectroPi.Infrastructure.Persistance
{
    /// <summary>
    /// Seeds demo data (2 admins, 4 customers, 4 agents and 30 tickets with comments,
    /// activity timelines and time entries) so the app is usable right after first run.
    /// All seeded users share the password pattern "{Role}@123" (e.g. "Admin@123").
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            if (await context.Tickets.AnyAsync())
                return;

            var admin1 = await EnsureUserAsync(userManager, "Abdo Fathy", "01028128912", "abdofathy883@gmail.com", "Aa123#", UserRole.Admin);
            var admin2 = await EnsureUserAsync(userManager, "Mona Hassan", "01000000002", "mona.admin@electropi.test", "Admin@123", UserRole.Admin);

            var customers = new List<AppUser>
            {
                await EnsureUserAsync(userManager, "Youssef Ahmed", "01000000011", "youssef.customer@electropi.test", "Customer@123", UserRole.Customer),
                await EnsureUserAsync(userManager, "Nourhan Sami", "01000000012", "nourhan.customer@electropi.test", "Customer@123", UserRole.Customer),
                await EnsureUserAsync(userManager, "Karim Adel", "01000000013", "karim.customer@electropi.test", "Customer@123", UserRole.Customer),
                await EnsureUserAsync(userManager, "Laila Mostafa", "01000000014", "laila.customer@electropi.test", "Customer@123", UserRole.Customer),
            };

            var agents = new List<AppUser>
            {
                await EnsureUserAsync(userManager, "Omar Khaled", "01000000021", "omar.agent@electropi.test", "Agent@123", UserRole.Agent),
                await EnsureUserAsync(userManager, "Salma Tarek", "01000000022", "salma.agent@electropi.test", "Agent@123", UserRole.Agent),
                await EnsureUserAsync(userManager, "Hassan Ali", "01000000023", "hassan.agent@electropi.test", "Agent@123", UserRole.Agent),
                await EnsureUserAsync(userManager, "Dina Fouad", "01000000024", "dina.agent@electropi.test", "Agent@123", UserRole.Agent),
            };

            var admins = new List<AppUser> { admin1, admin2 };

            await SeedTicketsAsync(context, customers, agents, admins);
        }

        private static async Task<AppUser> EnsureUserAsync(
            UserManager<AppUser> userManager,
            string fullName,
            string phoneNumber,
            string email,
            string password,
            UserRole role)
        {
            var existing = await userManager.FindByNameAsync(phoneNumber);
            if (existing != null)
                return existing;

            var user = new AppUser
            {
                FullName = fullName,
                UserName = phoneNumber,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = phoneNumber,
                PhoneNumberConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception($"Failed to seed user '{fullName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");

            await userManager.AddToRoleAsync(user, role.ToString());
            return user;
        }

        private static async Task SeedTicketsAsync(
            AppDbContext context,
            List<AppUser> customers,
            List<AppUser> agents,
            List<AppUser> admins)
        {
            var titles = new[]
            {
                "Cannot access company email",
                "VPN connection keeps dropping",
                "Laptop screen flickering",
                "Invoice discrepancy for March",
                "Password reset not working",
                "Printer not detected on network",
                "Application crashes on startup",
                "Slow internet connection in office",
                "Unable to install software update",
                "Account locked after failed logins",
                "Monitor no display signal",
                "Billing charged twice this month",
                "New employee laptop setup request",
                "Wi-Fi disconnects intermittently",
                "Software license expired",
                "Data sync error in CRM",
                "Request for additional storage",
                "Two-factor authentication issue",
                "Website checkout page not loading",
                "Mobile app login failure",
                "Backup job failed last night",
                "Request to reset database credentials",
                "Keyboard not responding",
                "Email attachments not sending",
                "Server downtime alert",
                "Report export returns empty file",
                "Access request for shared drive",
                "Antivirus blocking legitimate app",
                "Refund request for cancelled service",
                "Feature request: dark mode support"
            };

            // Oldest tickets are Closed, newest are still sitting untriaged in Open.
            var statusGroups = new[]
            {
                TicketStatus.Closed,
                TicketStatus.Resolved,
                TicketStatus.InProgress,
                TicketStatus.Acknowledged,
                TicketStatus.Open
            };

            var now = DateTime.UtcNow;

            for (int i = 0; i < titles.Length; i++)
            {
                var status = statusGroups[i / 6];
                var priority = (TicketPriority)(i % 4);
                var customer = customers[i % customers.Count];
                var createdAt = now.AddDays(-(60 - i * 2));

                var ticket = new Ticket
                {
                    Title = titles[i],
                    Description = $"{titles[i]}. Reported by {customer.FullName}. Please investigate and resolve as soon as possible.",
                    Priority = priority,
                    Status = TicketStatus.Open,
                    CustomerId = customer.Id,
                    Customer = customer,
                    CreatedAt = createdAt
                };

                context.Tickets.Add(ticket);

                void AddActivity(AppUser actor, TicketActivityType type, string oldValue, string newValue, DateTime at)
                {
                    ticket.Activities.Add(new TicketActivity
                    {
                        Ticket = ticket,
                        UserId = actor.Id,
                        UserName = actor.FullName,
                        Type = type,
                        OldValue = oldValue,
                        NewValue = newValue,
                        CreatedAt = at
                    });
                }

                AddActivity(customer, TicketActivityType.TicketCreation, "N/A", "N/A", createdAt);

                ticket.Comments.Add(new TicketComment
                {
                    Ticket = ticket,
                    AuthorId = customer.Id,
                    Content = $"Hi team, I'm running into this issue: {titles[i]}. Could someone take a look?",
                    CreatedAt = createdAt.AddMinutes(5)
                });

                if (status == TicketStatus.Open)
                    continue; // stays unassigned in the triage queue

                var agent = agents[i % agents.Count];
                var admin = admins[i % admins.Count];
                var assignedAt = createdAt.AddHours(3);

                AddActivity(admin, TicketActivityType.AgentAssigned, "N/A", agent.Id, assignedAt);
                ticket.AgentId = agent.Id;
                ticket.Agent = agent;

                ticket.Comments.Add(new TicketComment
                {
                    Ticket = ticket,
                    AuthorId = agent.Id,
                    Content = "Thanks for the report - I've been assigned to this ticket and I'm looking into it now.",
                    CreatedAt = assignedAt.AddMinutes(20)
                });

                var acknowledgedAt = assignedAt.AddMinutes(30);
                AddActivity(agent, TicketActivityType.StatusChanged, TicketStatus.Open.ToString(), TicketStatus.Acknowledged.ToString(), acknowledgedAt);
                ticket.Status = TicketStatus.Acknowledged;
                ticket.UpdatedAt = acknowledgedAt;

                if (status == TicketStatus.Acknowledged)
                    continue;

                var inProgressAt = acknowledgedAt.AddDays(1);
                AddActivity(agent, TicketActivityType.StatusChanged, TicketStatus.Acknowledged.ToString(), TicketStatus.InProgress.ToString(), inProgressAt);
                ticket.Status = TicketStatus.InProgress;
                ticket.UpdatedAt = inProgressAt;

                var entryCount = 1 + (i % 3);
                for (int e = 0; e < entryCount; e++)
                {
                    var workAt = inProgressAt.AddDays(e);
                    ticket.TimeEntries.Add(new TimeEntry
                    {
                        Ticket = ticket,
                        AgentId = agent.Id,
                        Agent = agent,
                        WorkDate = DateOnly.FromDateTime(workAt),
                        DurationMinutes = 30 + (e * 25) + (i % 4) * 10,
                        Description = $"Investigated and worked on: {titles[i]}.",
                        CreatedAt = workAt
                    });
                }

                ticket.Comments.Add(new TicketComment
                {
                    Ticket = ticket,
                    AuthorId = agent.Id,
                    Content = "Update: I've identified the root cause and I'm applying a fix.",
                    CreatedAt = inProgressAt.AddHours(4)
                });

                if (status == TicketStatus.InProgress)
                    continue;

                var resolvedAt = inProgressAt.AddDays(2);
                AddActivity(agent, TicketActivityType.StatusChanged, TicketStatus.InProgress.ToString(), TicketStatus.Resolved.ToString(), resolvedAt);
                ticket.Status = TicketStatus.Resolved;
                ticket.ResolvedAt = resolvedAt;
                ticket.UpdatedAt = resolvedAt;

                ticket.Comments.Add(new TicketComment
                {
                    Ticket = ticket,
                    AuthorId = agent.Id,
                    Content = "This has been resolved. Please confirm and let us know if you need anything else.",
                    CreatedAt = resolvedAt.AddMinutes(10)
                });

                if (status == TicketStatus.Resolved)
                    continue;

                var closedAt = resolvedAt.AddDays(1);
                AddActivity(customer, TicketActivityType.StatusChanged, TicketStatus.Resolved.ToString(), TicketStatus.Closed.ToString(), closedAt);
                ticket.Status = TicketStatus.Closed;
                ticket.ClosedAt = closedAt;
                ticket.UpdatedAt = closedAt;

                ticket.Comments.Add(new TicketComment
                {
                    Ticket = ticket,
                    AuthorId = customer.Id,
                    Content = "Confirmed working now, thank you! Closing this ticket.",
                    CreatedAt = closedAt.AddMinutes(5)
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
