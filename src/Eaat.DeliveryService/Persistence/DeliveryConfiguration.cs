using Eaat.DeliveryService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eaat.DeliveryService.Persistence;

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("deliveries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.RestaurantId).IsRequired();

        builder.Property(x => x.PickupAddress)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DeliveryArea)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DeliveryArea);
        builder.HasIndex(x => x.OrderId);
    }
}
