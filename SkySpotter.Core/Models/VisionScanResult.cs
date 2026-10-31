namespace SkySpotter.Core.Models;

public record VisionScanResult(
    bool IsAircraft,
    string? Airline,
    string? AircraftModel,
    string? Registration,
    string? LiveryDetails
);