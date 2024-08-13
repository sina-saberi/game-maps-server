using game_maps.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace game_maps.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetController(IFileStorageService storageService) : ControllerBase
{
    [HttpGet("{gameSlug}/{mapSlug}/{tileName}/{z}/{x}/{y}.{extension}")]
    public async Task<IActionResult> Get(string gameSlug, string mapSlug, string tileName, int z, int x, int y,
        string extension)
    {
        var pattern = $"{gameSlug}/{mapSlug}/{tileName}/{z}/{x}/{y}.{extension}";
        var path = Path.Combine("tiles", pattern);
        if (storageService.Exist(path))
        {
            var img = await System.IO.File.ReadAllBytesAsync(storageService.GetSavePath(path));
            return File(img, $"image/{extension}");
        }

        var url = "https://tiles.mapgenie.io/games/" + pattern;
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(url);
        await storageService.SaveFileAsync(bytes, path);
        return File(bytes, $"image/{extension}");
    }

    [HttpGet("storage/media/{fileName}.{extension}")]
    public async Task<IActionResult> Get(string fileName, string extension)
    {
        var pattern = $"{fileName}.{extension}";
        var path = Path.Combine("storage", "media", pattern);
        if (storageService.Exist(path))
        {
            var img = await System.IO.File.ReadAllBytesAsync(storageService.GetSavePath(path));
            return File(img, $"image/{extension}");
        }

        var url = "https://media.mapgenie.io/storage/media/" + pattern;
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(url);
        await storageService.SaveFileAsync(bytes, path);
        return File(bytes, $"image/{extension}");
    }

    [HttpGet("images/{slug}/{icon}.{extension}")]
    public async Task<IActionResult> GetIcon(string slug, string icon, string extension)
    {
        var pattern = $"{slug}/{icon}.{extension}";
        var path = Path.Combine("storage", "media", pattern);
        if (storageService.Exist(path))
        {
            var img = await System.IO.File.ReadAllBytesAsync(storageService.GetSavePath(path));
            return File(img, $"image/{extension}");
        }

        var url = "https://media.mapgenie.io/storage/media/" + pattern;
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(url);
        await storageService.SaveFileAsync(bytes, path);
        return File(bytes, $"image/{extension}");
    }
}