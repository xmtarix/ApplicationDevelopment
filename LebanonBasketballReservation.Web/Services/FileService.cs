using LebanonBasketballReservation.Business.Interfaces;

namespace LebanonBasketballReservation.Web.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>Magic bytes for the formats we accept, so a renamed .exe cannot slip through.</summary>
    private static readonly byte[][] AllowedSignatures =
    [
        [0xFF, 0xD8, 0xFF],                         // JPEG
        [0x89, 0x50, 0x4E, 0x47],                   // PNG
        [0x52, 0x49, 0x46, 0x46]                    // RIFF (WebP container)
    ];

    public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> SaveImageAsync(Stream fileStream, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Only JPG, PNG, and WebP images are allowed.");

        // Reject anything whose content does not match an image header, regardless of extension.
        if (!await HasImageSignatureAsync(fileStream, cancellationToken))
            throw new InvalidOperationException("That file does not appear to be a valid image.");

        // Constrain the folder to a simple name so a caller cannot escape the images root.
        var safeFolder = Path.GetFileName(folder);
        if (string.IsNullOrWhiteSpace(safeFolder))
            throw new InvalidOperationException("Invalid upload folder.");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadFolder = Path.Combine(webRoot, "images", safeFolder);
        Directory.CreateDirectory(uploadFolder);

        // The stored name is generated, never taken from user input.
        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadFolder, uniqueName);

        await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(fs, cancellationToken);
        }

        _logger.LogInformation("Saved upload to images/{Folder}/{Name}", safeFolder, uniqueName);
        return $"/images/{safeFolder}/{uniqueName}";
    }

    private static async Task<bool> HasImageSignatureAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek) return true; // Cannot inspect; extension check already applied.

        var header = new byte[4];
        stream.Position = 0;
        var read = await stream.ReadAsync(header.AsMemory(0, 4), cancellationToken);
        stream.Position = 0;

        if (read < 3) return false;

        return AllowedSignatures.Any(sig => sig.Length <= read && header.Take(sig.Length).SequenceEqual(sig));
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        try
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath.TrimStart('/', '\\')));

            // Never delete outside wwwroot, however the stored path was constructed.
            if (!fullPath.StartsWith(Path.GetFullPath(webRoot), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Refused to delete {Path}: outside the web root", relativePath);
                return;
            }

            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            // A stale file on disk is not worth failing the user's request over.
            _logger.LogWarning(ex, "Could not delete image {Path}", relativePath);
        }
    }

    public bool IsValidImage(string fileName, long fileSizeBytes)
        => GetValidationError(fileName, fileSizeBytes) is null;

    public string? GetValidationError(string fileName, long fileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "No file was selected.";

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return "Only JPG, PNG, and WebP images are allowed.";

        if (fileSizeBytes <= 0)
            return "The selected file is empty.";

        if (fileSizeBytes > MaxSizeBytes)
            return $"Images must be smaller than {MaxSizeBytes / (1024 * 1024)} MB.";

        return null;
    }
}
