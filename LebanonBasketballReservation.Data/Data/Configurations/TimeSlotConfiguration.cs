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
    public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
    {
        public void Configure(EntityTypeBuilder<TimeSlot> builder)
        {
            builder.HasKey(t => t.Id);

            builder.HasOne(t => t.Court)
                .WithMany(c => c.TimeSlots)
                .HasForeignKey(t => t.CourtId)
                .OnDelete(DeleteBehavior.Cascade);

            // This is the critical constraint that prevents double-booking at DB level
            builder.HasIndex(t => new { t.CourtId, t.Date, t.StartTime })
                .IsUnique()
                .HasDatabaseName("IX_TimeSlot_Court_Date_Start_Unique");

            builder.HasIndex(t => new { t.CourtId, t.Date });
            builder.HasIndex(t => t.IsAvailable);
        }
    }
}
