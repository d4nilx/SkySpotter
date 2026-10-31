using SkySpotter.Core.Services;

Console.Write("📸 Put photo here: ");
var path = Console.ReadLine()?.Trim().Trim('\'', '\"');

if (string.IsNullOrEmpty(path) || !File.Exists(path))
{
    Console.WriteLine("❌ No file found!");
    return;
}

var service = new ExifMetadataService();
var data = service.Extract(path);

Console.WriteLine("\n✅ Metadata extracted successfully:");
Console.WriteLine($"📄 File:     {data.FileName}");
Console.WriteLine($"📱 Camera:   {data.CameraModel ?? "Unknown"}");
Console.WriteLine($"🕒 Time:      {data.TakenAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "None"}");
Console.WriteLine($"📍 GPS:      {data.Latitude}, {data.Longitude}");
Console.WriteLine($"🧭 Heading:   {(data.CompassHeading.HasValue ? $"{data.CompassHeading:F1}°" : "None")}");
