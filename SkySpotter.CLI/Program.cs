using SkySpotter.Core.Services;

Console.Write("📸 Drop aircraft photo path here: ");
var path = Console.ReadLine()?.Trim().Trim('\'', '\"');

if (string.IsNullOrEmpty(path) || !File.Exists(path))
{
    Console.WriteLine("❌ File not found!");
    return;
}

var metaService = new ExifMetadataService();
var data = metaService.Extract(path);

Console.WriteLine("\n✅ Metadata extracted successfully:");
Console.WriteLine($"📄 File:     {data.FileName}");
Console.WriteLine($"📱 Camera:   {data.CameraModel ?? "Unknown"}");
Console.WriteLine($"🕒 Time:     {data.TakenAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");
Console.WriteLine($"📍 GPS:      {data.Latitude}, {data.Longitude}");
Console.WriteLine($"🧭 Heading:  {(data.CompassHeading.HasValue ? $"{data.CompassHeading:F1}°" : "N/A")}");

Console.WriteLine("\n🤖 Analyzing aircraft with AI...");
var visionService = new OllamaVisionService();
var scan = await visionService.AnalyzeImageAsync(path);

if (scan == null || !scan.IsAircraft)
{
    Console.WriteLine("❌ No aircraft detected in the frame.");
    return;
}

Console.WriteLine("\n✈️ Visual Spot Result:");
Console.WriteLine($"🏢 Airline:      {scan.Airline}");
Console.WriteLine($"🛩️ Model Guess:  {scan.AircraftModel}");
Console.WriteLine($"🔢 Registration: {scan.Registration ?? "Not readable"}");
Console.WriteLine($"🎨 Livery Note:  {scan.LiveryDetails}");