using Eaat.RestaurantService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eaat.RestaurantService.Persistence;

public class RestaurantOrderConfiguration : IEntityTypeConfiguration<RestaurantOrder>
{
    public void Configure(EntityTypeBuilder<RestaurantOrder> builder)
    {
        builder.ToTable("restaurant_orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RestaurantId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();

        builder.Property(x => x.DeliveryArea)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RestaurantId);
    }
}
