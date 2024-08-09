using game_maps.Application.DTOs.MapGenie;
using game_maps.Application.Interfaces;
using game_maps.Domain.Base;
using game_maps.Domain.Entities.Category;
using game_maps.Domain.Entities.Game;
using game_maps.Domain.Entities.Location;
using game_maps.Domain.Entities.Map;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;

#pragma warning disable CA1416 // Validate platform compatibility



namespace game_maps.Application.Services
{
    public class MapGenieService(IRepository<Map, int> _mapRepository, IRepository<Game, int> _gameRepository) : IMapGenieService
    {
        private readonly HttpClient _httpClient = new();

        public async Task Scrap(MapGenieSetting setting, IFormFile mapDataFile)
        {
            MapGenieData data = await ConvertJsonFileToMapGenieData(mapDataFile);
            Game game = CreateEntityFromMapGenieData(data, setting);
            await InsertMapGenieDataToDb(game);
            await ScrapMarker(setting.GameSlug);
            _ = Task.Run(async () =>
            {
                await ScrapMedias(data);
                await ScrapMapTiles(data, setting.GameSlug);
            });
        }

        private async Task ScrapMapTiles(MapGenieData data, string slug)
        {
            string basePath = Path.Combine("wwwroot", "tiles", slug, data.map.slug);
            using var semaphore = new SemaphoreSlim(3);
            var tasks = new List<Task>();
            var firstTileSetBound = data.mapConfig.tile_sets.First()?.bounds;
            if (firstTileSetBound is null) return;

            foreach (var tileSet in data.mapConfig.tile_sets)
            {
                foreach (var b in tileSet.bounds ?? firstTileSetBound)
                {
                    var z = b.Key;
                    var bound = b.Value;

                    if (z > tileSet.max_zoom) break;

                    for (int x = bound.x.min; x <= bound.x.max; x++)
                    {
                        for (int y = bound.y.min; y <= bound.y.max; y++)
                        {
                            var patternFixed = tileSet.pattern
                                .Replace("{z}", z.ToString())
                                .Replace("{x}", x.ToString())
                                .Replace("{y}", y.ToString());
                            await semaphore.WaitAsync();
                            tasks.Add(Task.Run(() => DownloadTile(semaphore, patternFixed)));
                        }
                    }
                }
            }
            await Task.WhenAll(tasks);
        }
        private async Task ScrapMedias(MapGenieData data)
        {
            string basePath = Path.Combine("wwwroot", "storage", "media");
            Directory.CreateDirectory(basePath);
            using var semaphore = new SemaphoreSlim(3);
            var tasks = new List<Task>();

            foreach (var item in data.locations)
            {
                if (item.media is not null)
                {
                    foreach (var item1 in item.media)
                    {
                        var savePath = Path.Combine(basePath, item1.file_name);
                        if (!File.Exists(savePath))
                        {
                            await semaphore.WaitAsync();
                            tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                Console.WriteLine($"downloading media {savePath}");
                                byte[] image = await _httpClient.GetByteArrayAsync(item1.url);
                                await File.WriteAllBytesAsync(savePath, image);
                            }
                            catch
                            {
                                Console.WriteLine($"downloading media faild {savePath}");
                                semaphore.Release();
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                        };
                    }
                }
            }
            await Task.WhenAll(tasks);
        }
        private async Task InsertMapGenieDataToDb(Game game)
        {
            var currentMap = game.Maps.FirstOrDefault() ?? throw new Exception("no map to update");
            var qurey = _gameRepository.AsQueryable()
                .Include(x => x.Maps.Where(c => c.Slug == currentMap.Slug))
                .ThenInclude(m => m.TileSets)
                .Include(x => x.Maps)
                .ThenInclude(m => m.Categories)
                .ThenInclude(c => c.Locations)
                .ThenInclude(l => l.Medias)
                .Where(x => x.Slug == game.Slug);

            var existingGame = await qurey.FirstOrDefaultAsync();
            if (existingGame == null) await _gameRepository.AddAsync(game);
            else
            {
                if (existingGame.Maps.Any(x => x.Slug == currentMap.Slug))
                    await _mapRepository.AsQueryable().Where(x => x.Slug == currentMap.Slug)
                        .ExecuteDeleteAsync();

                existingGame.Description = game.Description;
                existingGame.Name = game.Name;
                var newMap = new Map()
                {
                    Name = currentMap.Name,
                    Slug = currentMap.Slug,
                    Categories = currentMap.Categories,
                    Description = game.Description,
                    GameId = existingGame.Id,
                    MapConfig = new()
                    {
                        StartLng = currentMap.MapConfig.StartLng,
                        StartLat = currentMap.MapConfig.StartLat,
                        InitialZoom = currentMap.MapConfig.InitialZoom,
                    },
                };

                foreach (var tileSet in currentMap.TileSets)
                    newMap.TileSets.Add(tileSet);

                existingGame.Maps.Add(newMap);
                await _gameRepository.UpdateAsync(existingGame);
            }
        }
        private static Game CreateEntityFromMapGenieData(MapGenieData data, MapGenieSetting setting)
        {
            var game = new Game
            {
                Slug = data.slug ?? setting.GameSlug,
                Name = data.name ?? setting.GameName,
                Description = data.description
            };

            var map = game.Maps.FirstOrDefault(x => x.Slug == data.map.slug);
            if (map == null)
            {
                map = new Map
                {
                    GameId = game.Id,
                    MapConfig = new MapConfig()
                };
                game.Maps.Add(map);
            }

            map.Name = data.map.title;
            map.Slug = data.map.slug;
            map.MapConfig.StartLng = data.mapConfig.start_lng;
            map.MapConfig.StartLat = data.mapConfig.start_lat;
            map.MapConfig.InitialZoom = data.mapConfig.initial_zoom;
            map.TileSets.Clear();
            map.Categories.Clear();

            foreach (var tile_set in data.mapConfig.tile_sets)
            {
                map.TileSets.Add(new()
                {
                    Extension = tile_set.extension,
                    Name = tile_set.name,
                    MaxZoom = tile_set.max_zoom,
                    MinZoom = tile_set.min_zoom,
                    Pattern = tile_set.pattern,
                });
            }

            foreach (var group in data.groups)
            {
                foreach (var category in group.categories)
                {
                    Category category1 = new Category()
                    {
                        Title = category.title,
                        Description = category.description ?? string.Empty,
                        DisplayType = category.display_type,
                        FeaturesEnabled = category.features_enabled,
                        HasHeatmap = category.has_heatmap,
                        Icon = category.icon,
                        IgnEnabled = category.ign_enabled,
                        IgnVisible = category.ign_visible,
                        Visible = category.visible,
                        Template = category.template,
                        Order = category.order,
                        Info = category.info,
                        Group = group.title,
                    };

                    foreach (var location in data.locations.Where(x => x.category_id == category.id))
                    {
                        category1.Locations.Add(new()
                        {
                            Title = location.title,
                            Description = location.description ?? string.Empty,
                            Latitude = location.latitude,
                            Longitude = location.longitude,
                            Medias = location.media.Select(x => new Media()
                            {
                                FileName = x.file_name,
                                MimeType = x.mime_type,
                                Title = x.title,
                                Type = x.type,
                            }).ToList()
                        });

                    }
                    map.Categories.Add(category1);
                }
            }

            return game;
        }
        private static async Task<MapGenieData> ConvertJsonFileToMapGenieData(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var jsonString = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<MapGenieData>(jsonString)!;
        }
        private async Task ScrapMarker(string slug)
        {
            var imgBytes = await DownloadAndReadImageBytes‌($"https://cdn.mapgenie.io/images/games/{slug}/markers.png");
            var jsonString = await DownloadAndReadJsonFileString‌($"https://cdn.mapgenie.io/images/games/{slug}/markers.json");

            if (jsonString is not null)
            {
                var json = JsonSerializer.Deserialize<IDictionary<string, MapGenieMarketDto>>(jsonString);
                if (json is not null)
                {
                    using var ms = new MemoryStream(imgBytes);
                    using var originalImage = Image.FromStream(ms);
                    foreach (var kvp in json)
                    {
                        var key = kvp.Key;
                        var model = kvp.Value;

                        var cropRect = new Rectangle(
                            (int)(model.x * model.pixelRatio),
                            (int)(model.y * model.pixelRatio),
                            (int)(model.width * model.pixelRatio),
                            (int)(model.height * model.pixelRatio)
                        );

                        using (var croppedImage = new Bitmap(cropRect.Width, cropRect.Height))
                        {
                            using (var graphics = Graphics.FromImage(croppedImage))
                            {
                                graphics.DrawImage(
                                    originalImage,
                                    new Rectangle(0, 0, croppedImage.Width, croppedImage.Height),
                                    cropRect,
                                    GraphicsUnit.Pixel
                                );
                            }

                            string savePath = Path.Combine("wwwroot", "images", slug, $"{key}.png");
                            var dir = Path.GetDirectoryName(savePath);
                            if (dir is not null)
                            {
                                Directory.CreateDirectory(dir);
                                croppedImage.Save(savePath, ImageFormat.Png);
                            }
                        }
                    }
                }
            }
        }
        private async Task<string> DownloadAndReadJsonFileString(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        private async Task<byte[]> DownloadAndReadImageBytes(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            var response = await _httpClient.SendAsync(request);
            var imgBytes = await response.Content.ReadAsByteArrayAsync();
            return imgBytes;
        }
        private async Task DownloadTile(SemaphoreSlim semaphore, string pattern, int attempt = 1, int maxAttempts = 5)
        {
            string url = $"https://tiles.mapgenie.io/games/{pattern}";
            try
            {
                var savePath = Path.Combine("wwwroot", "tiles", pattern);
                var directoryPath = Path.GetDirectoryName(savePath);
                Directory.CreateDirectory(directoryPath!);
                if (File.Exists(savePath)) return;
                byte[] tileData = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(savePath, tileData);
                Console.WriteLine($"Tile successfully downloaded and saved: {savePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download or save tile from {url} on attempt {attempt}: {ex.Message}");

                if (attempt < maxAttempts)
                {
                    Console.WriteLine($"Retrying... (Attempt {attempt + 1} of {maxAttempts})");
                    await DownloadTile(semaphore, pattern, attempt + 1, maxAttempts);
                }
                else Console.WriteLine($"Max retries reached. Could not download the tile from {url}");
            }
            finally
            {
                semaphore.Release();
            }
        }
        private static string GetSavePath(string basePath, string tileSetName, int zoom, int x, int y, string extension)
        {
            string directoryPath = Path.Combine(basePath, tileSetName, zoom.ToString(), x.ToString());
            Directory.CreateDirectory(directoryPath);
            return Path.Combine(directoryPath, $"{y}.{extension}");
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
