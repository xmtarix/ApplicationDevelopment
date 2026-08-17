using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LebanonBasketballReservation.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LebanonBasketballReservation.Data.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.TotalPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(r => r.Notes)
                .HasMaxLength(500);

            builder.Property(r => r.CancellationReason)
                .HasMaxLength(500);

            builder.Property(r => r.Status)
                .HasConversion<string>();

            builder.HasOne(r => r.Customer)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-one: each TimeSlot can only have one Reservation
            builder.HasOne(r => r.TimeSlot)
                .WithOne(t => t.Reservation)
                .HasForeignKey<Reservation>(r => r.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.CustomerId);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => r.TimeSlotId).IsUnique();
        }
    }
}
