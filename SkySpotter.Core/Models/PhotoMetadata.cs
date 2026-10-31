namespace SkySpotter.Core.Models;

public record PhotoMetadata(
    string FileName,
    DateTime? TakenAt,
    double? Latitude,
    double? Longitude,
    double? CompassHeading,
    string? CameraModel
);