using ElectroPi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectroPi.Infrastructure.Persistance.EntityConfig
{
    public class TicketConfig : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .UseIdentityColumn(1, 1);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .HasMaxLength(4000);

            builder.Property(t => t.CustomerId)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Agent)
                .WithMany()
                .HasForeignKey(t => t.AgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.Comments)
                .WithOne(c => c.Ticket)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Activities)
                .WithOne(a => a.Ticket)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.TimeEntries)
                .WithOne(te => te.Ticket)
                .HasForeignKey(te => te.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => t.CustomerId);
            builder.HasIndex(t => t.Title);
            builder.HasIndex(t => t.AgentId);
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => t.Priority);
        }
    }
}
