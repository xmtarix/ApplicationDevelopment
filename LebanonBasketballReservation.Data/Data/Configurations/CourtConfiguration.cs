
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
    public class CourtConfiguration : IEntityTypeConfiguration<Court>
    {
        public void Configure(EntityTypeBuilder<Court> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasMaxLength(1000);

            builder.Property(c => c.HourlyPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(c => c.Status)
                .HasConversion<string>();

            builder.HasOne(c => c.Stadium)
                .WithMany(s => s.Courts)
                .HasForeignKey(c => c.StadiumId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => c.StadiumId);
            builder.HasIndex(c => c.Status);
        }
    }
}
