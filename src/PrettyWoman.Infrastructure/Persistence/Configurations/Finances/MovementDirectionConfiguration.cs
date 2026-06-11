using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Finances;

public class MovementDirectionConfiguration : IEntityTypeConfiguration<MovementDirection>
{
    public void Configure(EntityTypeBuilder<MovementDirection> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new MovementDirection
            {
                Id = (int)MovementDirectionOptions.In,
                Name = nameof(MovementDirectionOptions.In)
            },
            new MovementDirection
            {
                Id = (int)MovementDirectionOptions.Out,
                Name = nameof(MovementDirectionOptions.Out)
            }
        );
    }
}
