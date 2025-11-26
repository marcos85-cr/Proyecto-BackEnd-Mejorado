using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Entidades;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA
{
    public class BancaContext : DbContext
    {
        public BancaContext(DbContextOptions<BancaContext> options) : base(options)
        {
        }

        // =========================================================
        // 1. Declaración de los DbSets para todas las entidades
        // =========================================================
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<Beneficiario> Beneficiarios { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<Programacion> Programaciones { get; set; }
        public DbSet<ProveedorServicio> ProveedoresServicios { get; set; }
        public DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; }

        // =========================================================
        // 2. Configuración de Relaciones y Restricciones (Fluent API)
        // =========================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===========================
            // Módulo A: Usuarios y Clientes
            // ===========================

            // Restricción: Email del Usuario debe ser único (RF-A1)
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Relación 1:1 Usuario <-> Cliente (RF-A3: Cliente solo tiene un Usuario)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.ClienteAsociado)
                .WithOne(c => c.UsuarioAsociado)
                .HasForeignKey<Usuario>(u => u.ClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Relación 1:N Gestor (Usuario) <-> Clientes Asignados
            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.GestorAsignado)
                .WithMany(u => u.ClientesAsignados)
                .HasForeignKey(c => c.GestorAsignadoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Restricción: Identificación del Cliente debe ser única (RF-A3)
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Identificacion)
                .IsUnique();

            // ===========================
            // Módulo B: Cuentas
            // ===========================

            // Restricción: Número de cuenta debe ser único (RF-B1)
            modelBuilder.Entity<Cuenta>()
                .HasIndex(c => c.Numero)
                .IsUnique();

            // Relación 1:N Cliente <-> Cuentas
            modelBuilder.Entity<Cuenta>()
                .HasOne(c => c.Cliente)
                .WithMany(cl => cl.Cuentas)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar precisión del decimal para Saldo
            modelBuilder.Entity<Cuenta>()
                .Property(c => c.Saldo)
                .HasPrecision(18, 2);

            // ===========================
            // Módulo C: Beneficiarios (Terceros)
            // ===========================

            // Restricción: Alias no se repita en el mismo cliente (RF-C1 - Índice compuesto único)
            modelBuilder.Entity<Beneficiario>()
                .HasIndex(b => new { b.ClienteId, b.Alias })
                .IsUnique();

            // Relación 1:N Cliente <-> Beneficiarios
            modelBuilder.Entity<Beneficiario>()
                .HasOne(b => b.Cliente)
                .WithMany(c => c.Beneficiarios)
                .HasForeignKey(b => b.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===========================
            // Módulo D y E: Transacciones y Programación
            // ===========================

            // Restricción: IdempotencyKey debe ser única (RF-D2)
            modelBuilder.Entity<Transaccion>()
                .HasIndex(t => t.IdempotencyKey)
                .IsUnique();

            // Configurar precisión de decimales
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.Monto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.Comision)
                .HasPrecision(18, 2);

            // Relación 1:N Cuenta <-> Transacciones (Origen)
            modelBuilder.Entity<Transaccion>()
                .HasOne(t => t.CuentaOrigen)
                .WithMany()
                .HasForeignKey(t => t.CuentaOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 1:N Cliente <-> Transacciones
            modelBuilder.Entity<Transaccion>()
                .HasOne(t => t.Cliente)
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 1:1 Transaccion <-> Programacion (RF-D3, RF-E3)
            modelBuilder.Entity<Programacion>()
                .HasOne(p => p.Transaccion)
                .WithOne(t => t.Programacion)
                .HasForeignKey<Programacion>(p => p.TransaccionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===========================
            // Módulo E: ProveedorServicio
            // ===========================

            // Relación 1:N Usuario (Admin) <-> ProveedorServicio
            modelBuilder.Entity<ProveedorServicio>()
                .HasOne(ps => ps.CreadoPor)
                .WithMany()
                .HasForeignKey(ps => ps.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===========================
            // Módulo G: RegistroAuditoria
            // ===========================

            // Relación 1:N Usuario <-> RegistroAuditoria
            modelBuilder.Entity<RegistroAuditoria>()
                .HasOne(ra => ra.Usuario)
                .WithMany()
                .HasForeignKey(ra => ra.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}