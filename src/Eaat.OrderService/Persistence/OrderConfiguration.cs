using Eaat.OrderService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eaat.OrderService.Persistence;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.RestaurantId).IsRequired();

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
        builder.HasIndex(x => x.CustomerId);
    }
}
