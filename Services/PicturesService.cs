using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    /// <summary>
    /// Servicio encargado de guardar, consultar y eliminar (soft delete) las fotografías
    /// asociadas a una orden o a un detalle de orden.
    /// </summary>
    /// <remarks>
    /// Los archivos se almacenan dentro de la carpeta <c>Pictures</c> de <c>wwwroot</c>,
    /// creando, cuando es necesario, una subcarpeta con el folio de la orden.
    /// Toda la manipulación de rutas se realiza con <see cref="Path"/> y
    /// <see cref="Directory"/> para garantizar compatibilidad con Windows y Linux.
    /// </remarks>
    public class PicturesService(
        IDbContextFactory<LavanderiaDbContext> contextFactory,
        IWebHostEnvironment environment,
        ILogger<PicturesService> logger)
    {
        private const string PicturesFolderName = "Pictures";

        /// <summary>
        /// Obtiene la ruta física absoluta de la carpeta raíz <c>Pictures</c> dentro de
        /// <c>wwwroot</c>. Si la carpeta no existe, se crea.
        /// </summary>
        public string GetPicturesRootPath()
        {
            var webRoot = environment.WebRootPath
                ?? Path.Combine(environment.ContentRootPath, "wwwroot");

            var picturesRoot = Path.Combine(webRoot, PicturesFolderName);
            Directory.CreateDirectory(picturesRoot);
            return picturesRoot;
        }

        /// <summary>
        /// Guarda una fotografía en disco y registra el <see cref="Picture"/> en la base de datos.
        /// Se crea la carpeta <c>Pictures</c> y la subcarpeta con el folio de la orden si no existen.
        /// </summary>
        /// <param name="orderId">Identificador de la orden a la que pertenece la foto.</param>
        /// <param name="content">Flujo con el contenido binario de la imagen.</param>
        /// <param name="originalFileName">Nombre original del archivo (se usa solo para la extensión).</param>
        /// <param name="orderDetailId">Identificador opcional del detalle de orden asociado.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        public async Task<Picture> SavePictureAsync(
            int orderId,
            Stream content,
            string originalFileName,
            int? orderDetailId = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            if (!content.CanRead)
                throw new ArgumentException("El contenido de la imagen no es legible.", nameof(content));

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var order = await context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
                ?? throw new InvalidOperationException($"No se encontró la orden con Id {orderId}.");

            if (orderDetailId.HasValue)
            {
                var detailExists = await context.OrderDetails
                    .AsNoTracking()
                    .AnyAsync(od => od.Id == orderDetailId.Value && od.OrderId == orderId, cancellationToken);

                if (!detailExists)
                    throw new InvalidOperationException(
                        $"No se encontró el detalle con Id {orderDetailId.Value} para la orden {orderId}.");
            }

            var folderName = SanitizeFolderName(order.Folio);
            var orderFolder = Path.Combine(GetPicturesRootPath(), folderName);
            Directory.CreateDirectory(orderFolder);

            var extension = Path.GetExtension(originalFileName);
            var storedFileName = $"{Guid.NewGuid():N}{extension}".ToLowerInvariant();
            var fullPath = Path.Combine(orderFolder, storedFileName);

            await using (var fileStream = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            // Se almacena una ruta relativa a wwwroot con separador '/' para que sea
            // válida como URL y funcione igual en Windows y Linux.
            var relativeRute = $"{PicturesFolderName}/{folderName}/{storedFileName}";

            var picture = new Picture
            {
                Id = 0,
                Rute = relativeRute,
                OrderId = orderId,
                OrderDetailId = orderDetailId,
                SavedAt = DateTimeOffset.UtcNow
            };

            context.Pictures.Add(picture);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Foto {PictureId} guardada para la orden {OrderId} en {Rute}.",
                picture.Id, orderId, relativeRute);

            return picture;
        }

        /// <summary>
        /// Obtiene las fotografías asociadas a una orden.
        /// </summary>
        /// <param name="orderId">Identificador de la orden.</param>
        /// <param name="includeDeleted">Si es <c>true</c> incluye las fotografías eliminadas lógicamente.</param>
        public async Task<List<Picture>> GetPicturesByOrderAsync(
            int orderId,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var query = context.Pictures.Where(picture => picture.OrderId == orderId);

            if (!includeDeleted)
                query = query.Where(picture => picture.DeletedAt == null);

            return await query
                .OrderByDescending(picture => picture.SavedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Obtiene las fotografías asociadas a un detalle de orden.
        /// </summary>
        /// <param name="orderDetailId">Identificador del detalle de orden.</param>
        /// <param name="includeDeleted">Si es <c>true</c> incluye las fotografías eliminadas lógicamente.</param>
        public async Task<List<Picture>> GetPicturesByOrderDetailAsync(
            int orderDetailId,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var query = context.Pictures.Where(picture => picture.OrderDetailId == orderDetailId);

            if (!includeDeleted)
                query = query.Where(picture => picture.DeletedAt == null);

            return await query
                .OrderByDescending(picture => picture.SavedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Obtiene una fotografía por su identificador.
        /// </summary>
        public async Task<Picture?> GetPictureByIdAsync(
            int pictureId,
            CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Pictures
                .FirstOrDefaultAsync(picture => picture.Id == pictureId, cancellationToken);
        }

        /// <summary>
        /// Realiza un borrado lógico de una fotografía, estableciendo <see cref="Picture.DeletedAt"/>.
        /// El archivo físico se conserva.
        /// </summary>
        /// <returns>La fotografía actualizada o <c>null</c> si no existe.</returns>
        public async Task<Picture?> SoftDeleteAsync(
            int pictureId,
            CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var picture = await context.Pictures
                .FirstOrDefaultAsync(p => p.Id == pictureId, cancellationToken);

            if (picture is null)
                return null;

            if (picture.DeletedAt is null)
            {
                picture.DeletedAt = DateTimeOffset.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Foto {PictureId} eliminada lógicamente.", pictureId);
            }

            return picture;
        }

        /// <summary>
        /// Realiza un borrado lógico de todas las fotografías activas de una orden.
        /// </summary>
        /// <returns>Cantidad de fotografías afectadas.</returns>
        public async Task<int> SoftDeleteByOrderAsync(
            int orderId,
            CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var pictures = await context.Pictures
                .Where(picture => picture.OrderId == orderId && picture.DeletedAt == null)
                .ToListAsync(cancellationToken);

            if (pictures.Count == 0)
                return 0;

            var now = DateTimeOffset.UtcNow;
            foreach (var picture in pictures)
                picture.DeletedAt = now;

            await context.SaveChangesAsync(cancellationToken);
            return pictures.Count;
        }

        /// <summary>
        /// Restaura una fotografía eliminada lógicamente.
        /// </summary>
        /// <returns>La fotografía actualizada o <c>null</c> si no existe.</returns>
        public async Task<Picture?> RestoreAsync(
            int pictureId,
            CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var picture = await context.Pictures
                .FirstOrDefaultAsync(p => p.Id == pictureId, cancellationToken);

            if (picture is null)
                return null;

            if (picture.DeletedAt is not null)
            {
                picture.DeletedAt = null;
                await context.SaveChangesAsync(cancellationToken);
            }

            return picture;
        }

        /// <summary>
        /// Convierte la ruta relativa almacenada en <see cref="Picture.Rute"/> en una ruta
        /// física absoluta válida en el sistema operativo actual.
        /// </summary>
        public string GetPhysicalPath(Picture picture)
        {
            ArgumentNullException.ThrowIfNull(picture);

            var webRoot = environment.WebRootPath
                ?? Path.Combine(environment.ContentRootPath, "wwwroot");

            var relativePath = picture.Rute
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            return Path.Combine(webRoot, relativePath);
        }

        private static string SanitizeFolderName(string? folio)
        {
            var value = string.IsNullOrWhiteSpace(folio) ? "SinFolio" : folio.Trim();

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');

            // Evita separadores de ruta residuales.
            value = value
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_');

            return string.IsNullOrWhiteSpace(value) ? "SinFolio" : value;
        }
    }
}
