using ElectroPi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectroPi.Infrastructure.Persistance.EntityConfig
{
    public class TimeEntryConfig : IEntityTypeConfiguration<TimeEntry>
    {
        public void Configure(EntityTypeBuilder<TimeEntry> builder)
        {
            builder.ToTable("TimeEntries", t => t.HasCheckConstraint("CK_TimeEntries_DurationMinutes", "[DurationMinutes] > 0"));

            builder.HasKey(te => te.Id);

            builder.Property(te => te.AgentId)
                .IsRequired();

            builder.Property(te => te.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(te => te.WorkDate)
                .IsRequired();

            builder.Property(te => te.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(te => te.Agent)
                .WithMany()
                .HasForeignKey(te => te.AgentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(te => te.TicketId);
            builder.HasIndex(te => te.AgentId);
        }
    }
}
