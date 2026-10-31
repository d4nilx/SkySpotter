using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SkySpotter.Core.Interfaces;
using SkySpotter.Core.Models;

namespace SkySpotter.Core.Services;

public class ExifMetadataService : IMetadataService
{
    public PhotoMetadata Extract(string imagePath)
    {
        var directories = ImageMetadataReader.ReadMetadata(imagePath);

        var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        var gps = directories.OfType<GpsDirectory>().FirstOrDefault();

        DateTime? takenAt = subIfd != null &&
                            subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dt)
            ? dt : null;

        var geo = gps?.GetGeoLocation();

        double? heading = gps != null &&
                          gps.TryGetDouble(GpsDirectory.TagImgDirection, out var dir)
            ? dir : null;

        string? camera = ifd0?.GetDescription(ExifDirectoryBase.TagModel);

        return new PhotoMetadata(
            FileName: Path.GetFileName(imagePath),
            TakenAt: takenAt,
            Latitude: geo?.Latitude,
            Longitude: geo?.Longitude,
            CompassHeading: heading,
            CameraModel: camera
        );
    }
}