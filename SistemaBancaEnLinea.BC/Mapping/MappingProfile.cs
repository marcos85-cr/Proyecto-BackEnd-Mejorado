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
                .ConstructUsing(s => new BeneficiarioListaDto(
                    s.Id,
                    s.Alias,
                    s.Banco,
                    s.Moneda,
                    s.NumeroCuentaDestino,
                    s.Pais ?? "Costa Rica",
                    s.Estado,
                    s.FechaCreacion
                ));

            CreateMap<Beneficiario, BeneficiarioDetalleDto>()
                .ConstructUsing(s => new BeneficiarioDetalleDto(
                    s.Id,
                    s.Alias,
                    s.Banco,
                    s.Moneda,
                    s.NumeroCuentaDestino,
                    s.Pais ?? "Costa Rica",
                    s.Estado,
                    s.FechaCreacion,
                    false
                ));

            CreateMap<Beneficiario, BeneficiarioCreacionDto>()
                .ConstructUsing(s => new BeneficiarioCreacionDto(
                    s.Id,
                    s.Alias,
                    s.Banco,
                    s.NumeroCuentaDestino,
                    s.Estado
                ));

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
                .ConstructUsing(s => new ClienteListaDto(
                    s.Id,
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Id : 0,
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Identificacion ?? "" : "",
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Nombre ?? "" : "",
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Telefono : null,
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Email ?? "" : "",
                    s.Direccion,
                    s.FechaNacimiento,
                    s.Estado,
                    s.FechaRegistro,
                    s.Cuentas != null ? s.Cuentas.Count(c => c.Estado == ConstantesGenerales.ESTADO_CUENTA_ACTIVA) : 0,
                    s.GestorAsignadoId,
                    s.GestorAsignado != null ? s.GestorAsignado.Nombre ?? s.GestorAsignado.Email : null
                ));

            CreateMap<Cliente, ClienteActualizacionDto>()
                .ConstructUsing(s => new ClienteActualizacionDto(
                    s.Id,
                    s.Direccion,
                    s.FechaNacimiento,
                    s.GestorAsignadoId,
                    s.GestorAsignado != null ? s.GestorAsignado.Nombre ?? s.GestorAsignado.Email : null
                ));

            CreateMap<Cliente, ClienteCreacionDto>()
                .ConstructUsing((s, ctx) => new ClienteCreacionDto(
                    s.Id,
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Id : 0,
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Identificacion ?? "" : "",
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Nombre ?? "" : "",
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Telefono : null,
                    s.UsuarioAsociado != null ? s.UsuarioAsociado.Email ?? "" : "",
                    s.Direccion,
                    s.FechaNacimiento,
                    s.Estado,
                    s.FechaRegistro,
                    s.GestorAsignadoId,
                    s.Cuentas != null 
                        ? s.Cuentas.Select(c => new CuentaCreadaDto(c.Id, c.Numero, c.Tipo, c.Moneda, c.Saldo, c.Estado)).ToList() 
                        : new List<CuentaCreadaDto>()
                ));

            CreateMap<Usuario, UsuarioVinculadoDto>();
            
            CreateMap<Usuario, GestorAsignadoDto>();

            // ==================== USUARIO MAPPINGS ====================

            CreateMap<Usuario, UsuarioListaDto>()
                .ConstructUsing(s => new UsuarioListaDto(
                    s.Id,
                    s.Email,
                    s.Rol,
                    s.Nombre ?? "",
                    s.Identificacion,
                    s.Telefono,
                    s.EstaBloqueado,
                    s.IntentosFallidos,
                    s.FechaCreacion,
                    0
                ));

            CreateMap<Usuario, UsuarioDetalleDto>()
                .ConstructUsing(s => new UsuarioDetalleDto(
                    s.Id.ToString(),
                    s.Email,
                    s.Rol,
                    s.Nombre ?? "",
                    s.Identificacion,
                    s.Telefono,
                    s.EstaBloqueado,
                    s.IntentosFallidos,
                    s.FechaCreacion,
                    s.FechaBloqueo
                ));

            CreateMap<Usuario, UsuarioCreacionDto>()
                .ConstructUsing(s => new UsuarioCreacionDto(
                    s.Id.ToString(),
                    s.Email,
                    s.Rol,
                    s.Nombre
                ));

            CreateMap<Usuario, UsuarioActualizacionDto>()
                .ConstructUsing(s => new UsuarioActualizacionDto(
                    s.Id.ToString(),
                    s.Email,
                    s.Rol,
                    s.Nombre,
                    s.Identificacion,
                    s.Telefono
                ));

            CreateMap<Usuario, UsuarioBloqueoDto>()
                .ConstructUsing(s => new UsuarioBloqueoDto(
                    s.Id.ToString(),
                    s.EstaBloqueado,
                    s.FechaBloqueo
                ));

            CreateMap<Usuario, CambioContrasenaDto>()
                .ConstructUsing(s => new CambioContrasenaDto(
                    s.Id.ToString(),
                    s.Email,
                    DateTime.UtcNow
                ));

            CreateMap<Usuario, RegistroDto>()
                .ConstructUsing(s => new RegistroDto(
                    s.Id.ToString(),
                    s.Email,
                    s.Rol,
                    s.Nombre,
                    s.Identificacion,
                    s.Telefono
                ));

            // ==================== PROVEEDOR SERVICIO MAPPINGS ====================
            
            CreateMap<ProveedorServicio, ProveedorServicioDto>();

            CreateMap<ProveedorServicio, ProveedorListaDto>()
                .ConstructUsing(s => new ProveedorListaDto(
                    s.Id.ToString(),
                    s.Nombre,
                    ProveedorServicioReglas.ExtraerTipo(s.Nombre),
                    ProveedorServicioReglas.ObtenerIcono(ProveedorServicioReglas.ExtraerTipo(s.Nombre)),
                    s.ReglaValidacionContrato,
                    s.FormatoContrato,
                    true,
                    s.CreadoPor != null ? s.CreadoPor.Nombre ?? "Sistema" : "Sistema"
                ));

            CreateMap<ProveedorServicio, ProveedorDetalleDto>()
                .ConstructUsing(s => new ProveedorDetalleDto(
                    s.Id.ToString(),
                    s.Nombre,
                    ProveedorServicioReglas.ExtraerTipo(s.Nombre),
                    ProveedorServicioReglas.ObtenerIcono(ProveedorServicioReglas.ExtraerTipo(s.Nombre)),
                    s.ReglaValidacionContrato,
                    s.FormatoContrato,
                    true
                ));

            CreateMap<ProveedorServicio, ProveedorCreacionDto>()
                .ConstructUsing(s => new ProveedorCreacionDto(
                    s.Id.ToString(),
                    s.Nombre,
                    s.ReglaValidacionContrato,
                    s.FormatoContrato
                ));

            // ==================== TRANSACCION MAPPINGS ====================
            
            CreateMap<Transaccion, TransferenciaListaDto>()
                .ConstructUsing(s => new TransferenciaListaDto(
                    s.Id,
                    s.Tipo,
                    s.Estado,
                    s.Monto,
                    s.Moneda,
                    s.Comision,
                    s.FechaCreacion,
                    s.FechaEjecucion,
                    s.ComprobanteReferencia,
                    s.Descripcion,
                    s.CuentaOrigen != null ? s.CuentaOrigen.Numero : "",
                    s.CuentaDestino != null ? s.CuentaDestino.Numero : null,
                    s.Beneficiario != null ? s.Beneficiario.Alias : null
                ));

            CreateMap<Transaccion, PagoRealizadoDto>()
                .ConstructUsing(s => new PagoRealizadoDto(
                    s.Id,
                    s.ComprobanteReferencia ?? "",
                    s.Monto,
                    s.Comision,
                    s.Monto + s.Comision,
                    s.Estado,
                    s.FechaEjecucion,
                    s.ProveedorServicio != null ? s.ProveedorServicio.Nombre : null,
                    s.NumeroContrato ?? ""
                ));

            CreateMap<Transaccion, PagoDetalleDto>()
                .ConstructUsing(s => new PagoDetalleDto(
                    s.Id,
                    s.ProveedorServicio != null ? s.ProveedorServicio.Nombre : null,
                    s.NumeroContrato ?? "",
                    s.Monto,
                    s.Moneda,
                    s.Comision,
                    s.Estado,
                    s.FechaCreacion,
                    s.FechaEjecucion,
                    s.ComprobanteReferencia ?? "",
                    s.Descripcion
                ));

            CreateMap<Transaccion, PagoListaDto>()
                .ConstructUsing(s => new PagoListaDto(
                    s.Id,
                    s.ProveedorServicio != null ? s.ProveedorServicio.Nombre : null,
                    s.NumeroContrato ?? "",
                    s.Monto,
                    s.Moneda,
                    s.Comision,
                    s.Estado,
                    s.FechaCreacion,
                    s.FechaEjecucion,
                    s.ComprobanteReferencia ?? ""
                ));

            CreateMap<Transaccion, PagoResumenDto>()
                .ConstructUsing(s => new PagoResumenDto(
                    s.Id,
                    s.ProveedorServicio != null ? s.ProveedorServicio.Nombre : null,
                    s.Monto,
                    s.Estado,
                    s.FechaCreacion
                ));

            // ==================== PROGRAMACION MAPPINGS ====================
            
            CreateMap<Programacion, ProgramacionListaDto>()
                .ConstructUsing(s => new ProgramacionListaDto(
                    s.TransaccionId,
                    s.Transaccion != null ? s.Transaccion.Tipo : null,
                    s.Transaccion != null ? s.Transaccion.Monto : null,
                    s.Transaccion != null ? s.Transaccion.Moneda : null,
                    s.Transaccion != null ? s.Transaccion.Descripcion : null,
                    s.FechaProgramada,
                    s.FechaLimiteCancelacion,
                    s.EstadoJob,
                    s.EstadoJob == "Pendiente" && DateTime.UtcNow < s.FechaLimiteCancelacion
                ));

            CreateMap<Programacion, ProgramacionResumenDto>()
                .ConstructUsing(s => new ProgramacionResumenDto(
                    s.TransaccionId,
                    s.Transaccion != null ? s.Transaccion.Tipo : null,
                    s.Transaccion != null ? s.Transaccion.Monto : null,
                    s.Transaccion != null ? s.Transaccion.Moneda : null,
                    s.FechaProgramada,
                    s.EstadoJob
                ));

            CreateMap<Programacion, ProgramacionDetalleDto>()
                .ConstructUsing(s => new ProgramacionDetalleDto(
                    s.TransaccionId,
                    s.TransaccionId,
                    s.Transaccion != null ? s.Transaccion.Tipo : null,
                    s.Transaccion != null ? s.Transaccion.Monto : null,
                    s.Transaccion != null ? s.Transaccion.Moneda : null,
                    s.Transaccion != null ? s.Transaccion.Descripcion : null,
                    s.FechaProgramada,
                    s.FechaLimiteCancelacion,
                    s.EstadoJob,
                    s.EstadoJob == "Pendiente" && DateTime.UtcNow < s.FechaLimiteCancelacion,
                    s.Transaccion != null && s.Transaccion.CuentaOrigen != null ? s.Transaccion.CuentaOrigen.Numero : null,
                    s.Transaccion != null && s.Transaccion.CuentaDestino != null ? s.Transaccion.CuentaDestino.Numero : null
                ));
        }
    }
}
