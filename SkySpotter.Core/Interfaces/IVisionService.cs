using SkySpotter.Core.Models;

namespace SkySpotter.Core.Interfaces;

public interface IVisionService
{
    Task<VisionScanResult?> AnalyzeImageAsync(string imagePath);
}