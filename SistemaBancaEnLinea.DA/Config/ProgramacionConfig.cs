using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class ProgramacionConfig : IEntityTypeConfiguration<Programacion>
    {
        public void Configure(EntityTypeBuilder<Programacion> builder)
        {
            builder.ToTable("Programaciones");

            builder.HasKey(p => p.TransaccionId);

            builder.Property(p => p.FechaProgramada)
                .IsRequired();

            builder.Property(p => p.FechaLimiteCancelacion)
                .IsRequired();

            builder.Property(p => p.EstadoJob)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente");

            // Relación 1:1 con Transaccion
            builder.HasOne(p => p.Transaccion)
                .WithOne(t => t.Programacion)
                .HasForeignKey<Programacion>(p => p.TransaccionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice para búsqueda de programaciones pendientes
            builder.HasIndex(p => p.FechaProgramada);
            builder.HasIndex(p => p.EstadoJob);
        }
    }
}