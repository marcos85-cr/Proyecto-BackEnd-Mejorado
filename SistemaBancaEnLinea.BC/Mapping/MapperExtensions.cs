using AutoMapper;

namespace SistemaBancaEnLinea.BC.Mapping
{
    /// <summary>
    /// Extensiones para facilitar el uso de AutoMapper
    /// </summary>
    public static class MapperExtensions
    {
        /// <summary>
        /// Mapea una colección de origen a una lista del tipo destino
        /// </summary>
        public static List<TDestination> MapToList<TSource, TDestination>(
            this IMapper mapper, 
            IEnumerable<TSource> source)
        {
            return mapper.Map<List<TDestination>>(source);
        }

        /// <summary>
        /// Mapea una colección ordenada por un selector
        /// </summary>
        public static List<TDestination> MapToListOrdered<TSource, TDestination, TKey>(
            this IMapper mapper,
            IEnumerable<TSource> source,
            Func<TDestination, TKey> orderBySelector,
            bool descending = false)
        {
            var mapped = mapper.Map<List<TDestination>>(source);
            return descending 
                ? mapped.OrderByDescending(orderBySelector).ToList()
                : mapped.OrderBy(orderBySelector).ToList();
        }
    }
}
