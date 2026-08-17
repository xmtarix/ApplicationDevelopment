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
    public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.HasKey(f => f.Id);

            builder.HasOne(f => f.Customer)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Stadium)
                .WithMany(s => s.Favorites)
                .HasForeignKey(f => f.StadiumId)
                .OnDelete(DeleteBehavior.Cascade);

            // One favorite per customer per stadium
            builder.HasIndex(f => new { f.CustomerId, f.StadiumId }).IsUnique();
        }
    }
}
