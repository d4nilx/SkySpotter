using SkySpotter.Core.Models;

namespace SkySpotter.Core.Interfaces;

public interface IMetadataService
{
    PhotoMetadata Extract(string imagePath);
}