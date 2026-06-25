using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneaPam.Api.Features.Trees;

public sealed class TreeConfiguration : IEntityTypeConfiguration<Tree>
{
    public void Configure(EntityTypeBuilder<Tree> builder)
    {
        builder.HasKey(t => t.Id);
        builder.ToTable("trees");

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.Description).HasColumnName("description");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(t => t.OwnerId);

        builder
            .HasOne<GeneaPam.Api.Infrastructure.Persistence.ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
