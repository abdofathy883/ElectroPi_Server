using ElectroPi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectroPi.Infrastructure.Persistance.EntityConfig
{
    public class TicketActivityConfig : IEntityTypeConfiguration<TicketActivity>
    {
        public void Configure(EntityTypeBuilder<TicketActivity> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .UseIdentityColumn(1, 1);

            builder.Property(a => a.UserId)
                .IsRequired();

            builder.Property(a => a.UserName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(a => a.OldValue)
                .HasMaxLength(1000);

            builder.Property(a => a.NewValue)
                .HasMaxLength(1000);

            builder.Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.TicketId);
            builder.HasIndex(a => a.UserId);
        }
    }
}
