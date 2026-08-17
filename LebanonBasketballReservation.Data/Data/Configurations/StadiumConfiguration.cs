using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LebanonBasketballReservation.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Data.Configurations
{
    public class StadiumConfiguration : IEntityTypeConfiguration<Stadium>
    {
        public void Configure(EntityTypeBuilder<Stadium> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(s => s.Description)
                .HasMaxLength(2000);

            builder.Property(s => s.Address)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(s => s.Phone)
                .HasMaxLength(20);

            builder.Property(s => s.Email)
                .HasMaxLength(100);

            builder.Property(s => s.Status)
                .HasConversion<string>();

            builder.HasOne(s => s.Area)
                .WithMany(a => a.Stadiums)
                .HasForeignKey(s => s.AreaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Manager)
                .WithMany(u => u.ManagedStadiums)
                .HasForeignKey(s => s.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.ManagerId);
            builder.HasIndex(s => s.AreaId);
            builder.HasIndex(s => s.Status);
        }
    }
}
