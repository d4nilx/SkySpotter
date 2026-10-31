using System.Net.Http.Json;
using System.Text.Json;
using ImageMagick;
using SkySpotter.Core.Interfaces;
using SkySpotter.Core.Models;

namespace SkySpotter.Core.Services;

public class OllamaVisionService : IVisionService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(4) };
    private const string OllamaUrl = "http://localhost:11434/api/generate";
    private const string ModelName = "qwen2.5vl:7b";

    public async Task<VisionScanResult?> AnalyzeImageAsync(string imagePath)
    {
        var overviewBytes = PrepareOverviewBytes(imagePath);
        var box = await DetectAircraftBoundingBoxAsync(overviewBytes);

        var ultraHdCropBytes = CropUltraHdAircraft(imagePath, box);

        await File.WriteAllBytesAsync("last_aircraft_crop.jpg", ultraHdCropBytes);

        var base64Crop = Convert.ToBase64String(ultraHdCropBytes);

        var prompt = """
            You are looking at a high-resolution zoomed-in crop of an aircraft.
            Analyze the visual details and read any text on the fuselage carefully.
            
            STRICT RULES:
            1. If the tail is plain white with no logo, say "White tail (wet-lease / charter)" and look closely at the small logo near the front cabin doors.
            2. Read the registration (tail number) near the rear fuselage or nose gear doors character by character. Do NOT guess or invent registrations (like F-HBCN or EI-FRP) if the letters are unreadable! If unreadable, return null.

            Reply ONLY with a valid JSON object:
            {
              "IsAircraft": true,
              "Airline": "Airline name written on fuselage/tail, or Unknown (White livery)",
              "AircraftModel": "Exact aircraft model (e.g. Airbus A320-200, Boeing 737-800)",
              "Registration": "Exact characters read from fuselage or null",
              "LiveryDetails": "Detailed description of fuselage text, tail, engines, and wingtips"
            }
            """;

        var aiText = await QueryOllamaAsync(prompt, base64Crop);
        if (string.IsNullOrWhiteSpace(aiText)) return null;

        var cleanJson = ExtractJson(aiText);
        if (cleanJson == null) return null;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<VisionScanResult>(cleanJson, options);
    }

    private async Task<int[]?> DetectAircraftBoundingBoxAsync(byte[] overviewBytes)
    {
        var prompt = """
            Find the main aircraft in this image.
            Reply ONLY with a JSON object containing the bounding box coordinates as percentages (0 to 100) of the image dimensions:
            { "xmin": 45, "ymin": 45, "xmax": 80, "ymax": 70 }
            """;

        var aiText = await QueryOllamaAsync(prompt, Convert.ToBase64String(overviewBytes));
        var cleanJson = ExtractJson(aiText);
        if (cleanJson == null) return null;

        try
        {
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;
            return
            [
                root.GetProperty("xmin").GetInt32(),
                root.GetProperty("ymin").GetInt32(),
                root.GetProperty("xmax").GetInt32(),
                root.GetProperty("ymax").GetInt32()
            ];
        }
        catch
        {
            return null;
        }
    }

    private static byte[] CropUltraHdAircraft(string imagePath, int[]? box)
    {
        using var image = new MagickImage(imagePath);
        image.Strip();
        image.ColorSpace = ColorSpace.sRGB;

        if (box != null && box.Length == 4)
        {
            int xmin = Math.Max(0, box[0] - 5);
            int ymin = Math.Max(0, box[1] - 5);
            int xmax = Math.Min(100, box[2] + 5);
            int ymax = Math.Min(100, box[3] + 5);

            uint cropX = (uint)(image.Width * (xmin / 100.0));
            uint cropY = (uint)(image.Height * (ymin / 100.0));
            uint cropW = (uint)(image.Width * ((xmax - xmin) / 100.0));
            uint cropH = (uint)(image.Height * ((ymax - ymin) / 100.0));

            if (cropW > 200 && cropH > 200)
            {
                image.Crop(new MagickGeometry((int)cropX, (int)cropY, cropW, cropH));
                image.ResetPage();
            }
        }

        if (image.Width > 2560 || image.Height > 2560)
            image.Resize(2560, 2560);

        image.Format = MagickFormat.Jpeg;
        image.Quality = 100;
        return image.ToByteArray();
    }

    private static byte[] PrepareOverviewBytes(string imagePath)
    {
        using var image = new MagickImage(imagePath);
        image.Strip();
        image.ColorSpace = ColorSpace.sRGB;
        image.Resize(1280, 1280);
        image.Format = MagickFormat.Jpeg;
        image.Quality = 90;
        return image.ToByteArray();
    }

    private async Task<string?> QueryOllamaAsync(string prompt, string base64Image)
    {
        var payload = new
        {
            model = ModelName,
            prompt = prompt,
            images = new[] { base64Image },
            stream = false,
            options = new { temperature = 0.0, num_ctx = 8192 }
        };

        var response = await _http.PostAsJsonAsync(OllamaUrl, payload);
        if (!response.IsSuccessStatusCode) return null;

        var rawJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        return rawJson.GetProperty("response").GetString();
    }

    private static string? ExtractJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return (start != -1 && end != -1 && end > start) ? text.Substring(start, end - start + 1) : null;
    }
}