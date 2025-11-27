using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA
{
    /// <summary>
    /// Seed data adicional para proveedores de servicios
    /// </summary>
    public static class SeedDataProveedores
    {
        public static async Task InitializeProveedoresAsync(BancaContext context)
        {
            // Verificar si ya existen proveedores
            if (await context.ProveedoresServicios.AnyAsync())
            {
                Console.WriteLine("Los proveedores ya existen. Saltando seed de proveedores...");
                return;
            }

            Console.WriteLine("Creando proveedores de servicios...");

            // Obtener un admin para asociar
            var admin = await context.Usuarios
                .FirstOrDefaultAsync(u => u.Rol == "Administrador");

            if (admin == null)
            {
                Console.WriteLine("No se encontró un administrador. Creando proveedores sin admin.");
                return;
            }

            var proveedores = new List<ProveedorServicio>
            {
                // Electricidad
                new() {
                    Nombre = "ICE - Electricidad",
                    ReglaValidacionContrato = @"^\d{8,12}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "CNFL - Compañía Nacional de Fuerza y Luz",
                    ReglaValidacionContrato = @"^\d{10,14}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "ESPH - Empresa de Servicios Públicos de Heredia",
                    ReglaValidacionContrato = @"^\d{8,10}$",
                    CreadoPorUsuarioId = admin.Id
                },

                // Agua
                new() {
                    Nombre = "AyA - Acueductos y Alcantarillados",
                    ReglaValidacionContrato = @"^\d{10}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "ASADA - Asociaciones Administradoras de Acueductos",
                    ReglaValidacionContrato = @"^\d{8,12}$",
                    CreadoPorUsuarioId = admin.Id
                },

                // Telefonía
                new() {
                    Nombre = "Kolbi - ICE",
                    ReglaValidacionContrato = @"^\d{8}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Movistar Costa Rica",
                    ReglaValidacionContrato = @"^\d{8}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Claro Costa Rica",
                    ReglaValidacionContrato = @"^\d{8}$",
                    CreadoPorUsuarioId = admin.Id
                },

                // Internet y Cable
                new() {
                    Nombre = "Tigo - Internet",
                    ReglaValidacionContrato = @"^\d{8,10}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Cabletica - Cable e Internet",
                    ReglaValidacionContrato = @"^\d{12}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Liberty - Internet y Cable",
                    ReglaValidacionContrato = @"^\d{7,10}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Telecable - Televisión por Cable",
                    ReglaValidacionContrato = @"^\d{9,12}$",
                    CreadoPorUsuarioId = admin.Id
                },

                // Seguros
                new() {
                    Nombre = "INS - Instituto Nacional de Seguros",
                    ReglaValidacionContrato = @"^\d{9}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Seguros del Magisterio",
                    ReglaValidacionContrato = @"^\d{10,12}$",
                    CreadoPorUsuarioId = admin.Id
                },

                // Seguridad Social
                new() {
                    Nombre = "CCSS - Caja Costarricense del Seguro Social",
                    ReglaValidacionContrato = @"^\d{9,12}$",
                    CreadoPorUsuarioId = admin.Id
                },

                // Municipalidades
                new() {
                    Nombre = "Municipalidad de San José",
                    ReglaValidacionContrato = @"^\d{8,15}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Municipalidad de Alajuela",
                    ReglaValidacionContrato = @"^\d{8,15}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Municipalidad de Cartago",
                    ReglaValidacionContrato = @"^\d{8,15}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Municipalidad de Heredia",
                    ReglaValidacionContrato = @"^\d{8,15}$",
                    CreadoPorUsuarioId = admin.Id
                },

                // Otros Servicios
                new() {
                    Nombre = "Educación - Colegios Privados",
                    ReglaValidacionContrato = @"^\d{6,10}$",
                    CreadoPorUsuarioId = admin.Id
                },
                new() {
                    Nombre = "Condominio - Cuotas de Mantenimiento",
                    ReglaValidacionContrato = @"^\d{5,12}$",
                    CreadoPorUsuarioId = admin.Id
                }
            };

            context.ProveedoresServicios.AddRange(proveedores);
            await context.SaveChangesAsync();

            Console.WriteLine($" {proveedores.Count} proveedores de servicios creados exitosamente");
        }
    }
}