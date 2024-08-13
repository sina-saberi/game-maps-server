using game_maps.Application.DTOs.MapGenie;
using game_maps.Application.Interfaces;
using game_maps.Domain.Base;
using game_maps.Domain.Entities.Category;
using game_maps.Domain.Entities.Game;
using game_maps.Domain.Entities.Location;
using game_maps.Domain.Entities.Map;
using Microsoft.AspNetCore.Http;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using game_maps.Domain.Entities.User;
using static System.Text.RegularExpressions.Regex;

#pragma warning disable CA1416 // Validate platform compatibility


namespace game_maps.Application.Services
{
    class DownloadDto(string url, string savePath)
    {
        public string Url { get; set; } = url;
        public string SavePath { get; set; } = savePath;
    }

    public class MapGenieService(
        IRepository<Map, int> mapRepository,
        IRepository<Game, int> gameRepository,
        IRepository<Category, int> categoryRepository,
        IRepository<Location, int> locationRepository,
        IFileStorageService fileStorageService)
        : IMapGenieService
    {
        private readonly HttpClient _httpClient = new();

        public async Task ToggleMapToGame(string slug, IList<IFormFile> files, string? userId)
        {
            var game = await gameRepository.GetAsync(x => x.Slug == slug);
            if (game is null) return;
            foreach (var file in files)
            {
                var data = await ConvertJsonFileToMapGenieData(file);
                var map = await AddOrUpdateMapInGame(game.Id, data.map, data.mapConfig);
                var categories = await AddOrUpdateCategories(map.Id, data.categories, data.groups);
                await AddLocations(categories, data.locations, userId);
            }
        }

        private async Task<Map> AddOrUpdateMapInGame(int gameId, MapGenieMapDto dto, MapGenieMapConfig configDto)
        {
            var tileSets = configDto.tile_sets.Select(c => new TileSet()
            {
                Name = c.name,
                Extension = c.extension,
                Pattern = c.pattern,
                MaxZoom = c.max_zoom,
                MinZoom = c.min_zoom
            });

            return await mapRepository.AddOrUpdateAsync(x => x.Slug == dto.slug, x =>
            {
                x.Slug = string.IsNullOrEmpty(x.Slug) ? dto.slug : x.Slug;
                x.GameId = gameId;
                x.Name = dto.title;
                x.MapConfig.StartLng = configDto.start_lng;
                x.MapConfig.StartLat = configDto.start_lat;
                x.MapConfig.InitialZoom = configDto.initial_zoom;
                x.MapConfig.TileSets = tileSets.ToArray();
            });
        }

        private async Task<IList<Category>> AddOrUpdateCategories(int mapId,
            IDictionary<int, MapGenieCategory> categories, ICollection<MapGenieGroup> groups)
        {
            var existingCategories = await categoryRepository.ToListAsync(x => x.MapId == mapId);
            await categoryRepository.DeleteAsync(existingCategories.ToList());
            var items = categories.Select(x => new Category()
            {
                BaseId = x.Value.id,
                MapId = mapId,
                Group = groups.FirstOrDefault(c => c.id == x.Value.group_id)?.title ?? "",
                Title = x.Value.title,
                Icon = x.Value.icon,
                Info = x.Value.info,
                Template = x.Value.template,
                Order = x.Value.order,
                HasHeatmap = x.Value.has_heatmap,
                FeaturesEnabled = x.Value.features_enabled,
                DisplayType = x.Value.display_type,
                IgnEnabled = x.Value.ign_enabled,
                IgnVisible = x.Value.ign_visible,
                Visible = x.Value.visible,
                Description = x.Value.description ?? ""
            }).ToList();
            await categoryRepository.AddAsync(items);
            return items;
        }

        private async Task AddLocations(IList<Category> categories,
            ICollection<MapGenieLocation> locations, string? userId)
        {
            var range = locations.Select(l =>
            {
                var category = categories.FirstOrDefault(x => x.BaseId == l.category_id);
                if (category is null) throw new Exception($"no category found for this location {l.title}");
                var loc = new Location()
                {
                    BaseId = l.id,
                    Title = l.title,
                    Description = l.description ?? "",
                    Latitude = l.latitude,
                    Longitude = l.longitude,
                    CategoryId = category.Id,
                    Medias = l.media?.Select(m => new Media()
                    {
                        Title = m.title,
                        Type = m.type,
                        FileName = m.file_name,
                        MimeType = m.mime_type
                    }).ToList() ?? []
                };
                if (l.Checked.HasValue && userId is not null)
                {
                    loc.UserLocations.Add(new UserLocation()
                    {
                        Checked = l.Checked.Value,
                        UserId = userId
                    });
                }

                return loc;
            }).ToList();

            await locationRepository.AddAsync(range);
            const string oldIdPattern = @"locationIds=(\d+)";
            foreach (var location in range)
            {
                if (string.IsNullOrEmpty(location.Description)) continue;
                var match = Match(location.Description, oldIdPattern);
                if (!match.Success) continue;
                if (!int.TryParse(match.Groups[1].Value, out var oldId)) continue;
                var targetLocation = range.FirstOrDefault(x => x.BaseId == oldId);
                if (targetLocation != null)
                {
                    location.Description = Replace(location.Description,
                        oldIdPattern, $"locationIds={targetLocation.Id}");
                }
            }

            await locationRepository.UpdateAsync(range);
        }
        public async Task Scrap(string slug, IList<IFormFile> files)
        {
            var game = await gameRepository.GetAsync(x => x.Slug == slug);
            if (game is null) return;
            List<DownloadDto> downloadList = [];
            foreach (var file in files)
            {
                var data = await ConvertJsonFileToMapGenieData(file);
                await ScrapMarker(slug);
                downloadList.AddRange(CreateDownloadListFromMedias(data));
                downloadList.AddRange(CreateDownloadListFromMapTiles(data));
            }

            DownloadFromList(downloadList);
        }
        private static IList<DownloadDto> CreateDownloadListFromMedias(MapGenieData data)
        {
            IList<DownloadDto> list = [];
            foreach (var location in data.locations)
            {
                if (location.media == null) continue;
                foreach (var media in location.media)
                {
                    list.Add(new DownloadDto(media.url, Path.Combine("storage", "media", media.file_name)));
                }
            }

            return list;
        }
        private static IList<DownloadDto> CreateDownloadListFromMapTiles(MapGenieData data)
        {
            IList<DownloadDto> list = [];
            var firstTileSetBound = data.mapConfig.tile_sets.First().bounds;
            foreach (var mapConfigTileSet in data.mapConfig.tile_sets)
            {
                for (var i = mapConfigTileSet.min_zoom; i <= mapConfigTileSet.max_zoom; i++)
                {
                    var bounds = mapConfigTileSet.bounds ?? firstTileSetBound;
                    if (!bounds.TryGetValue(i, value: out var bound)) continue;
                    for (var x = bound.x.min; x <= bound.x.max; x++)
                    {
                        for (var y = bound.y.min; y <= bound.y.max; y++)
                        {
                            var patternFixed = mapConfigTileSet.pattern
                                .Replace("{z}", i.ToString())
                                .Replace("{x}", x.ToString())
                                .Replace("{y}", y.ToString());
                            list.Add(new DownloadDto($"https://tiles.mapgenie.io/games/{patternFixed}",
                                Path.Combine("tiles", patternFixed)));
                        }
                    }
                }
            }

            return list;
        }
        private async void DownloadFromList(IList<DownloadDto> list)
        {
            using var semaphore = new SemaphoreSlim(3, 3);
            var tasks = new List<Task>();
            foreach (var item in list)
            {
                var semaphoreCopy = semaphore;
                await semaphoreCopy.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (fileStorageService.Exist(item.SavePath)) return;
                        var tileData = await _httpClient.GetByteArrayAsync(item.Url);
                        await fileStorageService.SaveFileAsync(tileData, item.SavePath);
                    }
                    finally
                    {
                        semaphoreCopy.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks);
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
        public async Task ScrapMarker(string slug)
        {
            var imgBytes = await DownloadAndReadImageBytes($"https://cdn.mapgenie.io/images/games/{slug}/markers.png");
            var jsonString =
                await DownloadAndReadJsonFileString($"https://cdn.mapgenie.io/images/games/{slug}/markers.json");

            if (jsonString is not null)
            {
                var json = JsonSerializer.Deserialize<IDictionary<string, MapGenieMarketDto>>(jsonString);
                if (json is not null)
                {
                    using var ms = new MemoryStream(imgBytes);
                    using var originalImage = Image.FromStream(ms);
                    foreach (var (key, model) in json)
                    {
                        var cropRect = new Rectangle(
                            (int)(model.x * model.pixelRatio),
                            (int)(model.y * model.pixelRatio),
                            (int)(model.width * model.pixelRatio),
                            (int)(model.height * model.pixelRatio)
                        );

                        using var croppedImage = new Bitmap(cropRect.Width, cropRect.Height);
                        using (var graphics = Graphics.FromImage(croppedImage))
                        {
                            graphics.DrawImage(
                                originalImage,
                                new Rectangle(0, 0, croppedImage.Width, croppedImage.Height),
                                cropRect,
                                GraphicsUnit.Pixel
                            );
                        }

                        var savePath = Path.Combine(fileStorageService.GetSavePath(), "images", slug, $"{key}.png");
                        var dir = Path.GetDirectoryName(savePath);
                        if (dir is null) continue;
                        Directory.CreateDirectory(dir);
                        croppedImage.Save(savePath, ImageFormat.Png);
                    }
                }
            }
        }
        private async Task<string?> DownloadAndReadJsonFileString(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        private async Task<byte[]> DownloadAndReadImageBytes(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            var response = await _httpClient.SendAsync(request);
            var imgBytes = await response.Content.ReadAsByteArrayAsync();
            return imgBytes;
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility