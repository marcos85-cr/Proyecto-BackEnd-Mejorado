using AutoMapper;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.BC.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ==================== CUENTA MAPPINGS ====================
            
            CreateMap<Cuenta, CuentaListaDto>()
                .ForMember(d => d.Titular, o => o.MapFrom(s => s.Cliente != null && s.Cliente.UsuarioAsociado != null 
                    ? s.Cliente.UsuarioAsociado.Nombre : null))
                .ForMember(d => d.LimiteDiario, o => o.MapFrom(_ => CuentasReglas.LIMITE_DIARIO_DEFAULT))
                .ForMember(d => d.SaldoDisponible, o => o.MapFrom(s => s.Saldo));

            CreateMap<Cuenta, CuentaDetalleDto>()
                .ForMember(d => d.Titular, o => o.MapFrom(s => s.Cliente != null && s.Cliente.UsuarioAsociado != null 
                    ? s.Cliente.UsuarioAsociado.Nombre : "N/A"));

            CreateMap<Cuenta, CuentaCreacionDto>();
            
            CreateMap<Cuenta, CuentaBalanceDto>()
                .ForMember(d => d.Disponible, o => o.MapFrom(s => s.Saldo));

            CreateMap<Cuenta, CuentaClienteDto>();

            CreateMap<Cuenta, CuentaCreadaDto>();

            CreateMap<Cuenta, CuentaCompletaDto>()
                .ForMember(d => d.Cliente, o => o.MapFrom(s => s.Cliente))
                .ForMember(d => d.Usuario, o => o.MapFrom(s => s.Cliente != null ? s.Cliente.UsuarioAsociado : null))
                .ForMember(d => d.Gestor, o => o.MapFrom(s => s.Cliente != null ? s.Cliente.GestorAsignado : null));

            CreateMap<Cliente, CuentaRelacionClienteDto>();
            CreateMap<Usuario, CuentaRelacionUsuarioDto>();
            CreateMap<Usuario, CuentaRelacionGestorDto>();

            // ==================== BENEFICIARIO MAPPINGS ====================
            
            CreateMap<Beneficiario, BeneficiarioListaDto>()
                .ForMember(d => d.NumeroCuenta, o => o.MapFrom(s => s.NumeroCuentaDestino));

            CreateMap<Beneficiario, BeneficiarioDetalleDto>()
                .ForMember(d => d.NumeroCuenta, o => o.MapFrom(s => s.NumeroCuentaDestino))
                .ForMember(d => d.TieneOperacionesPendientes, o => o.Ignore());

            CreateMap<Beneficiario, BeneficiarioCreacionDto>()
                .ForMember(d => d.NumeroCuenta, o => o.MapFrom(s => s.NumeroCuentaDestino));

            CreateMap<Beneficiario, BeneficiarioConfirmacionDto>();

            CreateMap<Beneficiario, BeneficiarioActualizacionDto>();

            CreateMap<CrearBeneficiarioRequest, Beneficiario>()
                .ForMember(d => d.Pais, o => o.MapFrom(s => s.Pais ?? "Costa Rica"))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.ClienteId, o => o.Ignore())
                .ForMember(d => d.Estado, o => o.Ignore())
                .ForMember(d => d.FechaCreacion, o => o.Ignore())
                .ForMember(d => d.Cliente, o => o.Ignore());

            // ==================== CLIENTE MAPPINGS ====================
            
            CreateMap<Cliente, ClienteListaDto>()
                .ForCtorParam("UsuarioId", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Id : 0))
                .ForCtorParam("Identificacion", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Identificacion : ""))
                .ForCtorParam("NombreCompleto", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Nombre : ""))
                .ForCtorParam("Telefono", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Telefono : null))
                .ForCtorParam("Email", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Email : ""))
                .ForCtorParam("CuentasActivas", o => o.MapFrom(s => s.Cuentas != null 
                    ? s.Cuentas.Count(c => c.Estado == ConstantesGenerales.ESTADO_CUENTA_ACTIVA) : 0))
                .ForCtorParam("GestorNombre", o => o.MapFrom(s => s.GestorAsignado != null 
                    ? s.GestorAsignado.Nombre ?? s.GestorAsignado.Email : null));

            CreateMap<Cliente, ClienteActualizacionDto>()
                .ForCtorParam("GestorNombre", o => o.MapFrom(s => s.GestorAsignado != null 
                    ? s.GestorAsignado.Nombre ?? s.GestorAsignado.Email : null));

            CreateMap<Cliente, ClienteCreacionDto>()
                .ForCtorParam("UsuarioId", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Id : 0))
                .ForCtorParam("Identificacion", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Identificacion : ""))
                .ForCtorParam("NombreCompleto", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Nombre : ""))
                .ForCtorParam("Telefono", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Telefono : null))
                .ForCtorParam("Email", o => o.MapFrom(s => s.UsuarioAsociado != null ? s.UsuarioAsociado.Email : ""))
                .ForCtorParam("Cuentas", o => o.MapFrom(s => s.Cuentas ?? new List<Cuenta>()));

            CreateMap<Usuario, UsuarioVinculadoDto>();
            
            CreateMap<Usuario, GestorAsignadoDto>();

            // ==================== USUARIO MAPPINGS ====================

            CreateMap<Usuario, UsuarioListaDto>()
                .ForCtorParam("Bloqueado", o => o.MapFrom(s => s.EstaBloqueado))
                .ForCtorParam("Role", o => o.MapFrom(s => s.Rol));

            CreateMap<Usuario, UsuarioDetalleDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()));

            CreateMap<Usuario, UsuarioCreacionDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()));

            CreateMap<Usuario, UsuarioActualizacionDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()));

            CreateMap<Usuario, UsuarioBloqueoDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()))
                .ForCtorParam("Bloqueado", o => o.MapFrom(s => s.EstaBloqueado));

            CreateMap<Usuario, CambioContrasenaDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()))
                .ForCtorParam("FechaCambio", o => o.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Usuario, RegistroDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()));

            // ==================== PROVEEDOR SERVICIO MAPPINGS ====================
            
            CreateMap<ProveedorServicio, ProveedorServicioDto>();

            CreateMap<ProveedorServicio, ProveedorListaDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()))
                .ForCtorParam("Type", o => o.MapFrom(s => ProveedorServicioReglas.ExtraerTipo(s.Nombre)))
                .ForCtorParam("Icon", o => o.MapFrom(s => ProveedorServicioReglas.ObtenerIcono(ProveedorServicioReglas.ExtraerTipo(s.Nombre))))
                .ForCtorParam("ReglaValidacion", o => o.MapFrom(s => s.ReglaValidacionContrato))
                .ForCtorParam("Activo", o => o.MapFrom(_ => true))
                .ForCtorParam("CreadoPor", o => o.MapFrom(s => s.CreadoPor != null ? s.CreadoPor.Nombre : "Sistema"));

            CreateMap<ProveedorServicio, ProveedorDetalleDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()))
                .ForCtorParam("Type", o => o.MapFrom(s => ProveedorServicioReglas.ExtraerTipo(s.Nombre)))
                .ForCtorParam("Icon", o => o.MapFrom(s => ProveedorServicioReglas.ObtenerIcono(ProveedorServicioReglas.ExtraerTipo(s.Nombre))))
                .ForCtorParam("ReglaValidacion", o => o.MapFrom(s => s.ReglaValidacionContrato))
                .ForCtorParam("Activo", o => o.MapFrom(_ => true));

            CreateMap<ProveedorServicio, ProveedorCreacionDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()))
                .ForCtorParam("ReglaValidacion", o => o.MapFrom(s => s.ReglaValidacionContrato));

            // ==================== TRANSACCION MAPPINGS ====================
            
            CreateMap<Transaccion, TransferenciaListaDto>()
                .ForCtorParam("CuentaOrigen", o => o.MapFrom(s => s.CuentaOrigen != null ? s.CuentaOrigen.Numero : ""))
                .ForCtorParam("CuentaDestino", o => o.MapFrom(s => s.CuentaDestino != null ? s.CuentaDestino.Numero : null))
                .ForCtorParam("BeneficiarioAlias", o => o.MapFrom(s => s.Beneficiario != null ? s.Beneficiario.Alias : null));

            CreateMap<Transaccion, PagoRealizadoDto>()
                .ForCtorParam("TransaccionId", o => o.MapFrom(s => s.Id))
                .ForCtorParam("ComprobanteReferencia", o => o.MapFrom(s => s.ComprobanteReferencia ?? ""))
                .ForCtorParam("MontoTotal", o => o.MapFrom(s => s.Monto + s.Comision))
                .ForCtorParam("Proveedor", o => o.MapFrom(s => s.ProveedorServicio != null ? s.ProveedorServicio.Nombre : null))
                .ForCtorParam("NumeroContrato", o => o.MapFrom(s => s.NumeroContrato ?? ""));

            CreateMap<Transaccion, PagoDetalleDto>()
                .ForCtorParam("Proveedor", o => o.MapFrom(s => s.ProveedorServicio != null ? s.ProveedorServicio.Nombre : null))
                .ForCtorParam("NumeroContrato", o => o.MapFrom(s => s.NumeroContrato ?? ""))
                .ForCtorParam("ComprobanteReferencia", o => o.MapFrom(s => s.ComprobanteReferencia ?? ""));

            CreateMap<Transaccion, PagoListaDto>()
                .ForCtorParam("Proveedor", o => o.MapFrom(s => s.ProveedorServicio != null ? s.ProveedorServicio.Nombre : null))
                .ForCtorParam("NumeroContrato", o => o.MapFrom(s => s.NumeroContrato ?? ""))
                .ForCtorParam("ComprobanteReferencia", o => o.MapFrom(s => s.ComprobanteReferencia ?? ""));

            CreateMap<Transaccion, PagoResumenDto>()
                .ForCtorParam("Proveedor", o => o.MapFrom(s => s.ProveedorServicio != null ? s.ProveedorServicio.Nombre : null));

            // ==================== PROGRAMACION MAPPINGS ====================
            
            CreateMap<Programacion, ProgramacionListaDto>()
                .ForCtorParam("TransaccionId", o => o.MapFrom(s => s.TransaccionId))
                .ForCtorParam("Tipo", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Tipo : null))
                .ForCtorParam("Monto", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Monto : (decimal?)null))
                .ForCtorParam("Moneda", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Moneda : null))
                .ForCtorParam("Descripcion", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Descripcion : null))
                .ForCtorParam("EstadoJob", o => o.MapFrom(s => s.EstadoJob))
                .ForCtorParam("PuedeCancelarse", o => o.MapFrom(s => s.EstadoJob == "Pendiente" && DateTime.UtcNow < s.FechaLimiteCancelacion));

            CreateMap<Programacion, ProgramacionResumenDto>()
                .ForCtorParam("TransaccionId", o => o.MapFrom(s => s.TransaccionId))
                .ForCtorParam("Tipo", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Tipo : null))
                .ForCtorParam("Monto", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Monto : (decimal?)null))
                .ForCtorParam("Moneda", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Moneda : null))
                .ForCtorParam("EstadoJob", o => o.MapFrom(s => s.EstadoJob));

            CreateMap<Programacion, ProgramacionDetalleDto>()
                .ForCtorParam("Id", o => o.MapFrom(s => s.TransaccionId))
                .ForCtorParam("TransaccionId", o => o.MapFrom(s => s.TransaccionId))
                .ForCtorParam("Tipo", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Tipo : null))
                .ForCtorParam("Monto", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Monto : (decimal?)null))
                .ForCtorParam("Moneda", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Moneda : null))
                .ForCtorParam("Descripcion", o => o.MapFrom(s => s.Transaccion != null ? s.Transaccion.Descripcion : null))
                .ForCtorParam("EstadoJob", o => o.MapFrom(s => s.EstadoJob))
                .ForCtorParam("PuedeCancelarse", o => o.MapFrom(s => s.EstadoJob == "Pendiente" && DateTime.UtcNow < s.FechaLimiteCancelacion))
                .ForCtorParam("CuentaOrigen", o => o.MapFrom(s => s.Transaccion != null && s.Transaccion.CuentaOrigen != null 
                    ? s.Transaccion.CuentaOrigen.Numero : null))
                .ForCtorParam("CuentaDestino", o => o.MapFrom(s => s.Transaccion != null && s.Transaccion.CuentaDestino != null 
                    ? s.Transaccion.CuentaDestino.Numero : null));
        }
    }
}
