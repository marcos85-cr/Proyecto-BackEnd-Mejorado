using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class ProveedorServicioConfig : IEntityTypeConfiguration<ProveedorServicio>
    {
        public void Configure(EntityTypeBuilder<ProveedorServicio> builder)
        {
            builder.ToTable("ProveedoresServicios");

            builder.HasKey(ps => ps.Id);

            builder.Property(ps => ps.Nombre)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(ps => ps.Nombre)
                .IsUnique();

            builder.Property(ps => ps.ReglaValidacionContrato)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(ps => ps.FormatoContrato)
                .HasColumnName("Formato_Contrato")
                .HasMaxLength(100);

            // Relación con Usuario (Admin que lo creó)
            builder.HasOne(ps => ps.CreadoPor)
                .WithMany()
                .HasForeignKey(ps => ps.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}