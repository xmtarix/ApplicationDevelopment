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
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Rating)
                .IsRequired();

            builder.Property(r => r.Comment)
                .HasMaxLength(1000);

            builder.HasOne(r => r.Customer)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Stadium)
                .WithMany(s => s.Reviews)
                .HasForeignKey(r => r.StadiumId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Reservation)
                .WithOne(res => res.Review)
                .HasForeignKey<Review>(r => r.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            // One review per customer per reservation
            builder.HasIndex(r => r.ReservationId).IsUnique();
        }
    }
}
