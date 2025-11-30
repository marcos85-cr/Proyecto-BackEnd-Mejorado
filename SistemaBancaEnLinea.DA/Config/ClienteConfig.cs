using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class ClienteConfig : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");

            builder.HasKey(c => c.Id);

            // Atributos únicos del cliente
            builder.Property(c => c.Direccion)
                .HasMaxLength(500);

            builder.Property(c => c.FechaNacimiento);

            builder.Property(c => c.Estado)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Activo");

            // Relación 1:N Gestor -> Clientes
            builder.HasOne(c => c.GestorAsignado)
                .WithMany(u => u.ClientesAsignados)
                .HasForeignKey(c => c.GestorAsignadoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}