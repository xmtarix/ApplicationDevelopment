namespace LebanonBasketballReservation.Business.Interfaces;

public interface IFileService
{
    /// <summary>Saves an image under wwwroot/images/{folder} and returns its web-relative path.</summary>
    Task<string> SaveImageAsync(Stream fileStream, string fileName, string folder, CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously saved image. Ignores paths that no longer exist.</summary>
    void DeleteImage(string? relativePath);

    /// <summary>True when the extension is allowed and the file is within the size limit.</summary>
    bool IsValidImage(string fileName, long fileSizeBytes);

    /// <summary>Human-readable reason the file was rejected, or null when it is acceptable.</summary>
    string? GetValidationError(string fileName, long fileSizeBytes);
}
