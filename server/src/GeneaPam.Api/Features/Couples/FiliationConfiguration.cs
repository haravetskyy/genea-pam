using GeneaPam.Api.Features.Persons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneaPam.Api.Features.Couples;

public sealed class FiliationConfiguration : IEntityTypeConfiguration<Filiation>
{
    public void Configure(EntityTypeBuilder<Filiation> builder)
    {
        builder.HasKey(f => f.Id);
        builder.ToTable("filiations");

        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.CoupleId).HasColumnName("couple_id");
        builder.Property(f => f.ChildPersonId).HasColumnName("child_person_id");
        builder.Property(f => f.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(f => f.CoupleId);

        builder
            .HasOne<Couple>()
            .WithMany()
            .HasForeignKey(f => f.CoupleId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(f => f.ChildPersonId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
