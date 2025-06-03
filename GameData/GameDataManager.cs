using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
// using Yafes.Data; ← KALDIR
// using Yafes.GameData; ← KALDIR

namespace Yafes.Managers
{
    internal static class GameDataManager
    {
        // JSON dosya yolu
        private const string GAMES_JSON_FILE = "games_data.json";
        private const string EMBEDDED_JSON_PATH = "Yafes.Resources.games_data.json";

        // Cache - EXPLICIT QUALIFIER
        private static List<Yafes.Models.GameData> _gamesCache;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;

        // Events - EXPLICIT QUALIFIER
        public static event Action<List<Yafes.Models.GameData>> GamesDataLoaded;
        public static event Action<string> ErrorOccurred;

        /// <summary>
        /// Tüm oyun verilerini döndürür (cache'den veya dosyadan)
        /// </summary>
        public static async Task<List<Yafes.Models.GameData>> GetAllGamesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== GAMEDATA MANAGER DEBUG ===");

                var assembly = Assembly.GetExecutingAssembly();
                var allResourceNames = assembly.GetManifestResourceNames();

                System.Diagnostics.Debug.WriteLine($"Toplam Embedded Resource: {allResourceNames.Length}");
                foreach (var resource in allResourceNames)
                {
                    System.Diagnostics.Debug.WriteLine($"Resource: {resource}");
                }

                var pngResources = allResourceNames.Where(r => r.EndsWith(".png")).ToList();
                System.Diagnostics.Debug.WriteLine($"PNG Resource Count: {pngResources.Count}");

                // ❗ DEBUG KODU SON

                // Cache fresh mı kontrol et (5 dakika cache)
                if (_gamesCache != null && DateTime.Now.Subtract(_lastCacheUpdate).TotalMinutes < 5)
                {
                    System.Diagnostics.Debug.WriteLine("Cache'den veri döndürülüyor");
                    return _gamesCache;
                }
                // Cache fresh mı kontrol et (5 dakika cache)
                if (_gamesCache != null && DateTime.Now.Subtract(_lastCacheUpdate).TotalMinutes < 5)
                {
                    return _gamesCache;
                }

                // JSON'dan yükle
                var games = await LoadGamesFromJsonAsync();

                // Cache'e kaydet
                _gamesCache = games;
                _lastCacheUpdate = DateTime.Now;

                // Event fırlat
                GamesDataLoaded?.Invoke(games);

                return games;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Oyun verileri yüklenirken hata: {ex.Message}");
                return new List<Yafes.Models.GameData>();
            }
        }

        /// <summary>
        /// ID'ye göre oyun döndürür
        /// </summary>
        public static async Task<Yafes.Models.GameData> GetGameByIdAsync(string gameId)
        {
            var games = await GetAllGamesAsync();
            return games.FirstOrDefault(g => g.Id.Equals(gameId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Kategori'ye göre oyunları filtreler
        /// </summary>
        public static async Task<List<Yafes.Models.GameData>> GetGamesByCategoryAsync(string category)
        {
            var games = await GetAllGamesAsync();
            return games.Where(g => g.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Oyun adına göre arama yapar
        /// </summary>
        public static async Task<List<Yafes.Models.GameData>> SearchGamesAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllGamesAsync();

            var games = await GetAllGamesAsync();
            return games.Where(g =>
                g.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                g.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                g.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// Oyun kurulum durumunu günceller
        /// </summary>
        public static async Task<bool> UpdateGameInstallStatusAsync(string gameId, bool isInstalled)
        {
            try
            {
                var games = await GetAllGamesAsync();
                var game = games.FirstOrDefault(g => g.Id.Equals(gameId, StringComparison.OrdinalIgnoreCase));

                if (game != null)
                {
                    game.IsInstalled = isInstalled;
                    if (isInstalled)
                    {
                        game.LastPlayed = DateTime.Now;
                    }

                    // JSON'a kaydet
                    await SaveGamesToJsonAsync(games);

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Kurulum durumu güncellenirken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Embedded resource'ları tarayarak otomatik oyun listesi oluşturur
        /// </summary>
        public static async Task<List<Yafes.Models.GameData>> GenerateGamesFromEmbeddedResourcesAsync()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();

                // GamePosters klasöründeki PNG'leri bul
                var gamePosterResources = resourceNames
                    .Where(r => r.Contains("GamePosters") && r.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var games = new List<Yafes.Models.GameData>();

                foreach (var resource in gamePosterResources)
                {
                    var fileName = Path.GetFileName(resource);
                    var gameId = Path.GetFileNameWithoutExtension(fileName);
                    var gameName = FormatGameNameFromFileName(gameId);

                    var game = new Yafes.Models.GameData
                    {
                        Id = gameId,
                        Name = gameName,
                        ImageName = fileName,
                        SetupPath = $"{gameName}\\setup.exe", // Tahmine dayalı
                        Category = GuessGameCategory(gameName),
                        Size = "Unknown",
                        IsInstalled = false,
                        LastPlayed = DateTime.MinValue,
                        Description = $"Auto-generated entry for {gameName}"
                    };

                    games.Add(game);
                }

                // Alfabetik sırala
                games = games.OrderBy(g => g.Name).ToList();

                return games;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Otomatik oyun listesi oluşturulurken hata: {ex.Message}");
                return new List<Yafes.Models.GameData>();
            }
        }

        /// <summary>
        /// Otomatik oluşturulan oyun listesini JSON'a kaydet
        /// </summary>
        public static async Task<bool> SaveGeneratedGamesAsync()
        {
            try
            {
                var games = await GenerateGamesFromEmbeddedResourcesAsync();
                return await SaveGamesToJsonAsync(games);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Oyun listesi kaydedilirken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// JSON'dan oyun verilerini yükler
        /// </summary>
        private static async Task<List<Yafes.Models.GameData>> LoadGamesFromJsonAsync()
        {
            // 1. Önce external JSON dosyasını dene
            if (File.Exists(GAMES_JSON_FILE))
            {
                try
                {
                    var jsonContent = await File.ReadAllTextAsync(GAMES_JSON_FILE);
                    var gamesDatabase = JsonSerializer.Deserialize<Yafes.GameData.GamesDatabase>(jsonContent);
                    return gamesDatabase?.Games ?? new List<Yafes.Models.GameData>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"External JSON yüklenemedi: {ex.Message}");
                }
            }

            // 2. Embedded JSON'u dene
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(EMBEDDED_JSON_PATH);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var jsonContent = await reader.ReadToEndAsync();
                    var gamesDatabase = JsonSerializer.Deserialize<Yafes.GameData.GamesDatabase>(jsonContent);
                    return gamesDatabase?.Games ?? new List<Yafes.Models.GameData>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Embedded JSON yüklenemedi: {ex.Message}");
            }

            // 3. Hiçbiri yoksa embedded resource'lardan otomatik oluştur
            return await GenerateGamesFromEmbeddedResourcesAsync();
        }

        /// <summary>
        /// Oyun verilerini JSON'a kaydeder
        /// </summary>
        private static async Task<bool> SaveGamesToJsonAsync(List<Yafes.Models.GameData> games)
        {
            try
            {
                var gamesDatabase = new Yafes.GameData.GamesDatabase
                {
                    Games = games,
                    LastUpdated = DateTime.Now,
                    Version = "1.0"
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(gamesDatabase, options);
                await File.WriteAllTextAsync(GAMES_JSON_FILE, jsonContent);

                // Cache'i güncelle
                _gamesCache = games;
                _lastCacheUpdate = DateTime.Now;

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"JSON kaydedilirken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dosya adından game name formatla
        /// </summary>
        private static string FormatGameNameFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown Game";

            // Alt çizgileri boşluk yap
            var formatted = fileName.Replace("_", " ");

            // Kelime başlarını büyük yap
            var words = formatted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }

            return string.Join(" ", words);
        }

        /// <summary>
        /// Oyun adından kategori tahmin et
        /// </summary>
        private static string GuessGameCategory(string gameName)
        {
            var name = gameName.ToLower();

            // FPS oyunları
            if (name.Contains("call of duty") || name.Contains("battlefield") ||
                name.Contains("counter strike") || name.Contains("valorant") ||
                name.Contains("doom") || name.Contains("halo"))
                return "FPS";

            // RPG oyunları
            if (name.Contains("witcher") || name.Contains("elder scrolls") ||
                name.Contains("fallout") || name.Contains("mass effect") ||
                name.Contains("cyberpunk") || name.Contains("dragon age"))
                return "RPG";

            // Racing oyunları
            if (name.Contains("forza") || name.Contains("need for speed") ||
                name.Contains("gran turismo") || name.Contains("dirt") ||
                name.Contains("f1") || name.Contains("racing"))
                return "Racing";

            // Strategy oyunları
            if (name.Contains("civilization") || name.Contains("age of empires") ||
                name.Contains("total war") || name.Contains("starcraft"))
                return "Strategy";

            // Action oyunları
            if (name.Contains("assassin") || name.Contains("gta") ||
                name.Contains("red dead") || name.Contains("saints row"))
                return "Action";

            // Sports oyunları
            if (name.Contains("fifa") || name.Contains("nba") ||
                name.Contains("nfl") || name.Contains("sports"))
                return "Sports";

            return "General";
        }

        /// <summary>
        /// Cache'i temizler
        /// </summary>
        public static void ClearCache()
        {
            _gamesCache = null;
            _lastCacheUpdate = DateTime.MinValue;
        }

        /// <summary>
        /// Cache durumunu döndürür
        /// </summary>
        public static (bool isCached, int gameCount, DateTime lastUpdate) GetCacheStatus()
        {
            return (_gamesCache != null, _gamesCache?.Count ?? 0, _lastCacheUpdate);
        }

        /// <summary>
        /// Oyun kategorilerinin listesini döndürür
        /// </summary>
        public static async Task<List<string>> GetAvailableCategoriesAsync()
        {
            var games = await GetAllGamesAsync();
            return games.Select(g => g.Category).Distinct().OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Kurulu oyunların sayısını döndürür
        /// </summary>
        public static async Task<int> GetInstalledGamesCountAsync()
        {
            var games = await GetAllGamesAsync();
            return games.Count(g => g.IsInstalled);
        }

        /// <summary>
        /// En son oynanan oyunları döndürür
        /// </summary>
        public static async Task<List<Yafes.Models.GameData>> GetRecentlyPlayedGamesAsync(int count = 5)
        {
            var games = await GetAllGamesAsync();
            return games
                .Where(g => g.IsInstalled && g.LastPlayed > DateTime.MinValue)
                .OrderByDescending(g => g.LastPlayed)
                .Take(count)
                .ToList();
        }
    }
}