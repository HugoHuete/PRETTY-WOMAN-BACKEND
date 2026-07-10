using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;


public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.InstagramUser).HasMaxLength(50);
        builder.Property(x => x.MessengerUser).HasMaxLength(50);
        builder.Property(x => x.Address).HasMaxLength(350);
        builder.Property(x => x.BlockedReason).HasMaxLength(350);
        builder.Property(x => x.Comments).HasMaxLength(500);
        builder.Property(x => x.IsBlocked).HasDefaultValue(false);
        builder.Property(x => x.IsFriend).HasDefaultValue(false);


        builder.HasIndex(x => x.IsBlocked);
        builder.HasIndex(x => x.PhoneNumber).IsUnique();
        builder.HasIndex(x => x.InstagramUser).IsUnique();
        builder.HasIndex(x => x.MessengerUser).IsUnique();
    }
}