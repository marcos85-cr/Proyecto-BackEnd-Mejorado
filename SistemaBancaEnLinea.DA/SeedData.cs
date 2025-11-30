using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;
using System.Security.Cryptography;
using System.Text;

namespace SistemaBancaEnLinea.DA
{
    public static class SeedData
    {
        public static async Task InitializeAsync(BancaContext context)
        {
            // Verificar si ya hay datos
            if (await context.Usuarios.AnyAsync())
            {
                Console.WriteLine("La base de datos ya contiene datos. Saltando seed...");
                return;
            }

            Console.WriteLine("Iniciando seed de datos...");

            // ========== 1. CREAR USUARIOS ==========
            // Los datos personales están en Usuario
            var adminUser = new Usuario
            {
                Email = "admin@banco.com",
                PasswordHash = HashPassword("Admin123!"),
                Rol = "Administrador",
                Nombre = "Admin Sistema",
                IntentosFallidos = 0,
                EstaBloqueado = false,
                FechaCreacion = DateTime.UtcNow
            };
            context.Usuarios.Add(adminUser);

            var gestorUser = new Usuario
            {
                Email = "gestor@banco.com",
                PasswordHash = HashPassword("Gestor123!"),
                Rol = "Gestor",
                Nombre = "Gestor Principal",
                IntentosFallidos = 0,
                EstaBloqueado = false,
                FechaCreacion = DateTime.UtcNow
            };
            context.Usuarios.Add(gestorUser);

            var clienteUser = new Usuario
            {
                Email = "cliente@banco.com",
                PasswordHash = HashPassword("Cliente123!"),
                Rol = "Cliente",
                Nombre = "Marcos Vargas",
                Identificacion = "1-1234-5678",
                Telefono = "8888-9999",
                IntentosFallidos = 0,
                EstaBloqueado = false,
                FechaCreacion = DateTime.UtcNow
            };
            context.Usuarios.Add(clienteUser);

            // Usuario cliente adicional
            var clienteUser2 = new Usuario
            {
                Email = "maria@banco.com",
                PasswordHash = HashPassword("Maria123!"),
                Rol = "Cliente",
                Nombre = "María López",
                Identificacion = "2-5678-1234",
                Telefono = "7777-8888",
                IntentosFallidos = 0,
                EstaBloqueado = false,
                FechaCreacion = DateTime.UtcNow
            };
            context.Usuarios.Add(clienteUser2);

            await context.SaveChangesAsync();
            Console.WriteLine("Usuarios creados.");

            // ========== 2. CREAR CLIENTES ==========
            // Cliente solo tiene datos únicos (Direccion, FechaNacimiento)
            var cliente1 = new Cliente
            {
                Direccion = "San José, Costa Rica",
                FechaNacimiento = new DateTime(1990, 5, 15),
                Estado = "Activo",
                FechaRegistro = DateTime.UtcNow.AddMonths(-6),
                UsuarioAsociado = clienteUser,
                GestorAsignadoId = gestorUser.Id
            };
            context.Clientes.Add(cliente1);

            var cliente2 = new Cliente
            {
                Direccion = "Heredia, Costa Rica",
                FechaNacimiento = new DateTime(1985, 8, 20),
                Estado = "Activo",
                FechaRegistro = DateTime.UtcNow.AddMonths(-3),
                UsuarioAsociado = clienteUser2,
                GestorAsignadoId = gestorUser.Id
            };
            context.Clientes.Add(cliente2);

            await context.SaveChangesAsync();

            // Actualizar usuarios con referencia al cliente
            clienteUser.ClienteId = cliente1.Id;
            clienteUser2.ClienteId = cliente2.Id;
            await context.SaveChangesAsync();
            Console.WriteLine("Clientes creados y asociados.");

            // ========== 3. CREAR CUENTAS ==========
            // Cuentas del Cliente 1
            var cuenta1CRC = new Cuenta
            {
                Numero = "100000000001",
                Tipo = "Ahorros",
                Moneda = "CRC",
                Saldo = 500000,
                Estado = "Activa",
                ClienteId = cliente1.Id,
                FechaApertura = DateTime.UtcNow.AddMonths(-6)
            };
            context.Cuentas.Add(cuenta1CRC);

            var cuenta1USD = new Cuenta
            {
                Numero = "200000000001",
                Tipo = "Corriente",
                Moneda = "USD",
                Saldo = 2500,
                Estado = "Activa",
                ClienteId = cliente1.Id,
                FechaApertura = DateTime.UtcNow.AddMonths(-5)
            };
            context.Cuentas.Add(cuenta1USD);

            var cuenta1Inversion = new Cuenta
            {
                Numero = "300000000001",
                Tipo = "Inversión",
                Moneda = "CRC",
                Saldo = 1500000,
                Estado = "Activa",
                ClienteId = cliente1.Id,
                FechaApertura = DateTime.UtcNow.AddMonths(-2)
            };
            context.Cuentas.Add(cuenta1Inversion);

            // Cuentas del Cliente 2
            var cuenta2CRC = new Cuenta
            {
                Numero = "100000000002",
                Tipo = "Ahorros",
                Moneda = "CRC",
                Saldo = 350000,
                Estado = "Activa",
                ClienteId = cliente2.Id,
                FechaApertura = DateTime.UtcNow.AddMonths(-3)
            };
            context.Cuentas.Add(cuenta2CRC);

            var cuenta2USD = new Cuenta
            {
                Numero = "200000000002",
                Tipo = "Ahorros",
                Moneda = "USD",
                Saldo = 1800,
                Estado = "Activa",
                ClienteId = cliente2.Id,
                FechaApertura = DateTime.UtcNow.AddMonths(-2)
            };
            context.Cuentas.Add(cuenta2USD);

            await context.SaveChangesAsync();
            Console.WriteLine("Cuentas creadas.");

            // ========== 4. CREAR PROVEEDORES DE SERVICIO ==========
            var proveedores = new List<ProveedorServicio>
            {
                new() { Nombre = "ICE - Electricidad", ReglaValidacionContrato = @"^\d{8,12}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "AyA - Agua", ReglaValidacionContrato = @"^\d{10}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "Kolbi - Teléfono", ReglaValidacionContrato = @"^\d{8}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "Tigo - Internet", ReglaValidacionContrato = @"^\d{8,10}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "Cabletica - Cable", ReglaValidacionContrato = @"^\d{12}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "INS - Seguros", ReglaValidacionContrato = @"^\d{9}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "Liberty - Internet", ReglaValidacionContrato = @"^\d{7,10}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "CCSS - Seguro Social", ReglaValidacionContrato = @"^\d{9,12}$", CreadoPorUsuarioId = adminUser.Id }
            };
            context.ProveedoresServicios.AddRange(proveedores);
            await context.SaveChangesAsync();
            Console.WriteLine("Proveedores de servicio creados.");

            // ========== 5. CREAR BENEFICIARIOS ==========
            var beneficiarios = new List<Beneficiario>
            {
                // Beneficiarios del Cliente 1
                new()
                {
                    Alias = "Mamá",
                    Banco = "Banco Nacional",
                    Moneda = "CRC",
                    NumeroCuentaDestino = "100200300400",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente1.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-60)
                },
                new()
                {
                    Alias = "Hermano Juan",
                    Banco = "BAC San José",
                    Moneda = "USD",
                    NumeroCuentaDestino = "200300400500",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente1.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-45)
                },
                new()
                {
                    Alias = "Proveedor XYZ",
                    Banco = "Banco de Costa Rica",
                    Moneda = "CRC",
                    NumeroCuentaDestino = "300400500600",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente1.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-30)
                },
                new()
                {
                    Alias = "Tía María",
                    Banco = "Banco Popular",
                    Moneda = "CRC",
                    NumeroCuentaDestino = "400500600700",
                    Pais = "Costa Rica",
                    Estado = "Inactivo", // Pendiente de confirmación
                    ClienteId = cliente1.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-5)
                },
                new()
                {
                    Alias = "Socio Comercial",
                    Banco = "Scotiabank",
                    Moneda = "USD",
                    NumeroCuentaDestino = "500600700800",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente1.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-90)
                },
                // Beneficiarios del Cliente 2
                new()
                {
                    Alias = "Papá",
                    Banco = "Banco Nacional",
                    Moneda = "CRC",
                    NumeroCuentaDestino = "111222333444",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente2.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-40)
                },
                new()
                {
                    Alias = "Esposo",
                    Banco = "BAC San José",
                    Moneda = "CRC",
                    NumeroCuentaDestino = "222333444555",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente2.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-35)
                }
            };
            context.Beneficiarios.AddRange(beneficiarios);
            await context.SaveChangesAsync();
            Console.WriteLine("Beneficiarios creados.");

            // ========== 6. CREAR TRANSACCIONES DE EJEMPLO ==========
            var transacciones = new List<Transaccion>
            {
                // Transferencias del Cliente 1
                new()
                {
                    Tipo = "Transferencia",
                    Estado = "Exitosa",
                    Monto = 50000,
                    Moneda = "CRC",
                    Comision = 500,
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    FechaCreacion = DateTime.UtcNow.AddDays(-10),
                    FechaEjecucion = DateTime.UtcNow.AddDays(-10),
                    ComprobanteReferencia = $"TRF-{DateTime.UtcNow.AddDays(-10):yyyyMMdd}-ABC12345",
                    Descripcion = "Pago de préstamo",
                    SaldoAnterior = 550000,
                    SaldoPosterior = 499500,
                    CuentaOrigenId = cuenta1CRC.Id,
                    ClienteId = cliente1.Id
                },
                new()
                {
                    Tipo = "PagoServicio",
                    Estado = "Exitosa",
                    Monto = 35000,
                    Moneda = "CRC",
                    Comision = 1000,
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    FechaCreacion = DateTime.UtcNow.AddDays(-8),
                    FechaEjecucion = DateTime.UtcNow.AddDays(-8),
                    ComprobanteReferencia = $"PAG-{DateTime.UtcNow.AddDays(-8):yyyyMMdd}-DEF67890",
                    Descripcion = "Pago ICE - Electricidad",
                    SaldoAnterior = 499500,
                    SaldoPosterior = 463500,
                    CuentaOrigenId = cuenta1CRC.Id,
                    ProveedorServicioId = 1, // ICE
                    NumeroContrato = "12345678",
                    ClienteId = cliente1.Id
                },
                new()
                {
                    Tipo = "Transferencia",
                    Estado = "Exitosa",
                    Monto = 75000,
                    Moneda = "CRC",
                    Comision = 500,
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    FechaCreacion = DateTime.UtcNow.AddDays(-5),
                    FechaEjecucion = DateTime.UtcNow.AddDays(-5),
                    ComprobanteReferencia = $"TRF-{DateTime.UtcNow.AddDays(-5):yyyyMMdd}-GHI11111",
                    Descripcion = "Transferencia a Mamá",
                    SaldoAnterior = 463500,
                    SaldoPosterior = 388000,
                    CuentaOrigenId = cuenta1CRC.Id,
                    ClienteId = cliente1.Id
                },
                new()
                {
                    Tipo = "PagoServicio",
                    Estado = "Exitosa",
                    Monto = 18000,
                    Moneda = "CRC",
                    Comision = 1000,
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    FechaCreacion = DateTime.UtcNow.AddDays(-3),
                    FechaEjecucion = DateTime.UtcNow.AddDays(-3),
                    ComprobanteReferencia = $"PAG-{DateTime.UtcNow.AddDays(-3):yyyyMMdd}-JKL22222",
                    Descripcion = "Pago AyA - Agua",
                    SaldoAnterior = 388000,
                    SaldoPosterior = 369000,
                    CuentaOrigenId = cuenta1CRC.Id,
                    ProveedorServicioId = 2, // AyA
                    NumeroContrato = "1234567890",
                    ClienteId = cliente1.Id
                },
                new()
                {
                    Tipo = "Transferencia",
                    Estado = "Exitosa",
                    Monto = 500,
                    Moneda = "USD",
                    Comision = 0,
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    FechaCreacion = DateTime.UtcNow.AddDays(-2),
                    FechaEjecucion = DateTime.UtcNow.AddDays(-2),
                    ComprobanteReferencia = $"TRF-{DateTime.UtcNow.AddDays(-2):yyyyMMdd}-MNO33333",
                    Descripcion = "Transferencia entre cuentas propias",
                    SaldoAnterior = 3000,
                    SaldoPosterior = 2500,
                    CuentaOrigenId = cuenta1USD.Id,
                    CuentaDestinoId = cuenta1CRC.Id, // Transferencia propia
                    ClienteId = cliente1.Id
                }
            };
            context.Transacciones.AddRange(transacciones);
            await context.SaveChangesAsync();
            Console.WriteLine("Transacciones de ejemplo creadas.");

            // ========== 7. CREAR REGISTROS DE AUDITORÍA ==========
            var auditorias = new List<RegistroAuditoria>
            {
                new()
                {
                    FechaHora = DateTime.UtcNow.AddMonths(-6),
                    TipoOperacion = "RegistroUsuario",
                    Descripcion = "Usuario cliente@banco.com registrado",
                    UsuarioId = clienteUser.Id
                },
                new()
                {
                    FechaHora = DateTime.UtcNow.AddMonths(-6),
                    TipoOperacion = "AperturaCuenta",
                    Descripcion = "Cuenta 100000000001 abierta. Tipo: Ahorros, Moneda: CRC",
                    UsuarioId = clienteUser.Id
                },
                new()
                {
                    FechaHora = DateTime.UtcNow.AddDays(-10),
                    TipoOperacion = "Transferencia",
                    Descripcion = "Transferencia de 50000 CRC desde cuenta 100000000001",
                    UsuarioId = clienteUser.Id
                },
                new()
                {
                    FechaHora = DateTime.UtcNow.AddDays(-8),
                    TipoOperacion = "PagoServicio",
                    Descripcion = "Pago de 35000 a ICE - Electricidad. Contrato: 12345678",
                    UsuarioId = clienteUser.Id
                }
            };
            context.RegistrosAuditoria.AddRange(auditorias);
            await context.SaveChangesAsync();
            Console.WriteLine("Registros de auditoría creados.");

            Console.WriteLine("========================================");
            Console.WriteLine("SEED DATA COMPLETADO EXITOSAMENTE");
            Console.WriteLine("========================================");
            Console.WriteLine("Credenciales de prueba:");
            Console.WriteLine("  Admin:   admin@banco.com / Admin123!");
            Console.WriteLine("  Gestor:  gestor@banco.com / Gestor123!");
            Console.WriteLine("  Cliente: cliente@banco.com / Cliente123!");
            Console.WriteLine("  Cliente: maria@banco.com / Maria123!");
            Console.WriteLine("========================================");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}