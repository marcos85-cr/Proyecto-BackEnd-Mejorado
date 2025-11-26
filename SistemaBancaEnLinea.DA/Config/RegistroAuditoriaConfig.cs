using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class RegistroAuditoriaConfig : IEntityTypeConfiguration<RegistroAuditoria>
    {
        public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
        {
            builder.ToTable("RegistrosAuditoria");

            builder.HasKey(ra => ra.Id);

            builder.Property(ra => ra.FechaHora)
                .IsRequired();

            builder.Property(ra => ra.TipoOperacion)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ra => ra.Descripcion)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(ra => ra.DetalleJson)
                .HasColumnType("nvarchar(max)");

            // Relación con Usuario
            builder.HasOne(ra => ra.Usuario)
                .WithMany()
                .HasForeignKey(ra => ra.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices para búsquedas
            builder.HasIndex(ra => ra.FechaHora);
            builder.HasIndex(ra => ra.TipoOperacion);
            builder.HasIndex(ra => ra.UsuarioId);
        }
    }
}