using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA
{
    public class BancaContext : DbContext
    {
        public BancaContext(DbContextOptions<BancaContext> options) : base(options)
        {
        }

        // =========================================================
        // DbSets para todas las entidades
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
        // Configuración con Fluent API
        // =========================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // === MÓDULO A: Usuarios y Clientes ===

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.ClienteAsociado)
                .WithOne(c => c.UsuarioAsociado)
                .HasForeignKey<Usuario>(u => u.ClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.GestorAsignado)
                .WithMany(u => u.ClientesAsignados)
                .HasForeignKey(c => c.GestorAsignadoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Identificacion)
                .IsUnique();

            // === MÓDULO B: Cuentas ===

            modelBuilder.Entity<Cuenta>()
                .HasIndex(c => c.Numero)
                .IsUnique();

            modelBuilder.Entity<Cuenta>()
                .HasOne(c => c.Cliente)
                .WithMany(cl => cl.Cuentas)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cuenta>()
                .Property(c => c.Saldo)
                .HasPrecision(18, 2);

            // === MÓDULO C: Beneficiarios ===

            modelBuilder.Entity<Beneficiario>()
                .HasIndex(b => new { b.ClienteId, b.Alias })
                .IsUnique();

            modelBuilder.Entity<Beneficiario>()
                .HasOne(b => b.Cliente)
                .WithMany(c => c.Beneficiarios)
                .HasForeignKey(b => b.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // === MÓDULO D y E: Transacciones ===

            modelBuilder.Entity<Transaccion>()
                .HasIndex(t => t.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.Monto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.Comision)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.SaldoAnterior)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.SaldoPosterior)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .HasOne(t => t.CuentaOrigen)
                .WithMany()
                .HasForeignKey(t => t.CuentaOrigenId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaccion>()
                .HasOne(t => t.CuentaDestino)
                .WithMany()
                .HasForeignKey(t => t.CuentaDestinoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaccion>()
                .HasOne(t => t.Beneficiario)
                .WithMany()
                .HasForeignKey(t => t.BeneficiarioId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaccion>()
                .HasOne(t => t.ProveedorServicio)
                .WithMany()
                .HasForeignKey(t => t.ProveedorServicioId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaccion>()
                .HasOne(t => t.Cliente)
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            // === Programación ===

            modelBuilder.Entity<Programacion>()
                .HasOne(p => p.Transaccion)
                .WithOne(t => t.Programacion)
                .HasForeignKey<Programacion>(p => p.TransaccionId)
                .OnDelete(DeleteBehavior.Cascade);

            // === MÓDULO E: ProveedorServicio ===

            modelBuilder.Entity<ProveedorServicio>()
                .HasIndex(ps => ps.Nombre)
                .IsUnique();

            modelBuilder.Entity<ProveedorServicio>()
                .HasOne(ps => ps.CreadoPor)
                .WithMany()
                .HasForeignKey(ps => ps.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // === MÓDULO G: Auditoría ===

            modelBuilder.Entity<RegistroAuditoria>()
                .HasOne(ra => ra.Usuario)
                .WithMany()
                .HasForeignKey(ra => ra.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegistroAuditoria>()
                .HasIndex(ra => ra.FechaHora);

            modelBuilder.Entity<RegistroAuditoria>()
                .HasIndex(ra => ra.TipoOperacion);

            base.OnModelCreating(modelBuilder);
        }
    }
}