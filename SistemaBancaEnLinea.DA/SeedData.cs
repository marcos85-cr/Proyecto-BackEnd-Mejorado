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
                return;

            // Crear usuario administrador
            var adminUser = new Usuario
            {
                Email = "admin@banco.com",
                PasswordHash = HashPassword("Admin123!"),
                Rol = "Administrador",
                Nombre = "Administrador del Sistema",
                IntentosFallidos = 0,
                EstaBloqueado = false,
                FechaCreacion = DateTime.UtcNow
            };
            context.Usuarios.Add(adminUser);

            // Crear usuario cliente de prueba
            var clienteUser = new Usuario
            {
                Email = "cliente@banco.com",
                PasswordHash = HashPassword("Cliente123!"),
                Rol = "Cliente",
                Nombre = "Cliente de Prueba",
                Identificacion = "1-1234-5678",
                Telefono = "8888-9999",
                IntentosFallidos = 0,
                EstaBloqueado = false,
                FechaCreacion = DateTime.UtcNow
            };
            context.Usuarios.Add(clienteUser);

            await context.SaveChangesAsync();

            // Crear cliente asociado al usuario
            var cliente = new Cliente
            {
                Identificacion = "1-1234-5678",
                NombreCompleto = "Cliente de Prueba",
                Telefono = "8888-9999",
                Correo = "cliente@banco.com",
                Estado = "Activo",
                FechaRegistro = DateTime.UtcNow,
                UsuarioAsociado = clienteUser
            };
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            // Actualizar usuario con referencia al cliente
            clienteUser.ClienteId = cliente.Id;
            await context.SaveChangesAsync();

            // Crear cuentas de prueba
            var cuentaCRC = new Cuenta
            {
                Numero = "100000000001",
                Tipo = "Ahorros",
                Moneda = "CRC",
                Saldo = 500000,
                Estado = "Activa",
                ClienteId = cliente.Id,
                FechaApertura = DateTime.UtcNow
            };
            context.Cuentas.Add(cuentaCRC);

            var cuentaUSD = new Cuenta
            {
                Numero = "200000000001",
                Tipo = "Corriente",
                Moneda = "USD",
                Saldo = 2500,
                Estado = "Activa",
                ClienteId = cliente.Id,
                FechaApertura = DateTime.UtcNow
            };
            context.Cuentas.Add(cuentaUSD);

            await context.SaveChangesAsync();

            // Crear proveedores de servicio
            var proveedores = new List<ProveedorServicio>
            {
                new() { Nombre = "ICE - Electricidad", ReglaValidacionContrato = @"^\d{8,12}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "AyA - Agua", ReglaValidacionContrato = @"^\d{10}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "Kolbi - Teléfono", ReglaValidacionContrato = @"^\d{8}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "Tigo - Internet", ReglaValidacionContrato = @"^\d{8,10}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "Cabletica", ReglaValidacionContrato = @"^\d{12}$", CreadoPorUsuarioId = adminUser.Id },
                new() { Nombre = "INS - Seguros", ReglaValidacionContrato = @"^\d{9}$", CreadoPorUsuarioId = adminUser.Id }
            };
            context.ProveedoresServicios.AddRange(proveedores);

            await context.SaveChangesAsync();

            // Crear beneficiarios de prueba
            var beneficiarios = new List<Beneficiario>
            {
                new()
                {
                    Alias = "Mamá",
                    Banco = "Banco Nacional",
                    Moneda = "CRC",
                    NumeroCuentaDestino = "100200300400",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-30)
                },
                new()
                {
                    Alias = "Hermano Juan",
                    Banco = "BAC San José",
                    Moneda = "USD",
                    NumeroCuentaDestino = "200300400500",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-20)
                },
                new()
                {
                    Alias = "Proveedor XYZ",
                    Banco = "Banco de Costa Rica",
                    Moneda = "CRC",
                    NumeroCuentaDestino = "300400500600",
                    Pais = "Costa Rica",
                    Estado = "Confirmado",
                    ClienteId = cliente.Id,
                    FechaCreacion = DateTime.UtcNow.AddDays(-10)
                }
            };
            context.Beneficiarios.AddRange(beneficiarios);

            await context.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
