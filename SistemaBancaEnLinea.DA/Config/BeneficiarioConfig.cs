using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class BeneficiarioConfig : IEntityTypeConfiguration<Beneficiario>
    {
        public void Configure(EntityTypeBuilder<Beneficiario> builder)
        {
            builder.ToTable("Beneficiarios");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Alias)
                .IsRequired()
                .HasMaxLength(30);

            // Índice compuesto único: Alias + ClienteId
            builder.HasIndex(b => new { b.ClienteId, b.Alias })
                .IsUnique();

            builder.Property(b => b.Banco)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.Moneda)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(b => b.NumeroCuentaDestino)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(b => b.Pais)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.Estado)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Inactivo");

            // Relación 1:N Cliente -> Beneficiarios
            builder.HasOne(b => b.Cliente)
                .WithMany(c => c.Beneficiarios)
                .HasForeignKey(b => b.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}