using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

                // Cache fresh mı kontrol et (5 dakika cache)
                if (_gamesCache != null && DateTime.Now.Subtract(_lastCacheUpdate).TotalMinutes < 5)
                {
                    System.Diagnostics.Debug.WriteLine("Cache'den veri döndürülüyor");
                    return _gamesCache;
                }

                // JSON'dan yükle
                var games = await LoadGamesFromJsonAsync();

                // Eğer JSON'dan oyun gelmezse, GamesIcons klasöründen otomatik oluştur
                if (games == null || games.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("JSON'dan oyun yüklenemedi, GamesIcons klasöründen oluşturuluyor...");
                    games = await GenerateGamesFromGamesIconsAsync();

                    // Oluşturulan oyunları JSON'a kaydet
                    if (games != null && games.Count > 0)
                    {
                        await SaveGamesToJsonAsync(games);
                        System.Diagnostics.Debug.WriteLine($"{games.Count} oyun otomatik oluşturuldu ve JSON'a kaydedildi");
                    }
                }

                // Cache'e kaydet
                _gamesCache = games ?? new List<Yafes.Models.GameData>();
                _lastCacheUpdate = DateTime.Now;

                // Event fırlat
                GamesDataLoaded?.Invoke(_gamesCache);

                System.Diagnostics.Debug.WriteLine($"Toplam {_gamesCache.Count} oyun döndürülüyor");
                return _gamesCache;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllGamesAsync Hatası: {ex.Message}");
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
        /// 🚀 COMPLETELY REWRITTEN - GamesIcons klasöründeki gerçek dosyalardan oyun listesi oluşturur
        /// ESKİ GenerateGamesFromEmbeddedResourcesAsync TAMAMEN DEĞİŞTİRİLDİ
        /// </summary>
        public static async Task<List<Yafes.Models.GameData>> GenerateGamesFromGamesIconsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== GENERATING GAMES FROM GAMESICONS FOLDER ===");

                var games = new List<Yafes.Models.GameData>();

                // ImageManager'dan GamesIcons path'ini al
                var cacheStats = Yafes.Managers.ImageManager.GetCacheStats();
                var gamesIconsPath = cacheStats.gamesPath;

                if (string.IsNullOrEmpty(gamesIconsPath) || gamesIconsPath == "Not Found")
                {
                    System.Diagnostics.Debug.WriteLine("❌ GamesIcons path not found, using fallback");

                    // Force ImageManager to initialize paths
                    Yafes.Managers.ImageManager.GetGameImage("test");
                    cacheStats = Yafes.Managers.ImageManager.GetCacheStats();
                    gamesIconsPath = cacheStats.gamesPath;

                    if (string.IsNullOrEmpty(gamesIconsPath) || gamesIconsPath == "Not Found")
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Still no GamesIcons path, returning empty list");
                        return new List<Yafes.Models.GameData>();
                    }
                }

                System.Diagnostics.Debug.WriteLine($"📁 Using GamesIcons path: {gamesIconsPath}");

                if (!Directory.Exists(gamesIconsPath))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Directory does not exist: {gamesIconsPath}");
                    return new List<Yafes.Models.GameData>();
                }

                // Supported image extensions
                var supportedExtensions = new[] { ".png", ".jpg", ".jpeg" };

                // GamesIcons klasöründeki tüm image dosyalarını al
                var imageFiles = Directory.GetFiles(gamesIconsPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToArray();

                System.Diagnostics.Debug.WriteLine($"📊 Found {imageFiles.Length} image files");

                if (imageFiles.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ No image files found in GamesIcons folder");
                    return new List<Yafes.Models.GameData>();
                }

                // İlk 10 dosyayı debug için göster
                System.Diagnostics.Debug.WriteLine("📋 Sample files:");
                foreach (var file in imageFiles.Take(10))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(file)}");
                }

                foreach (var filePath in imageFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var fullFileName = Path.GetFileName(filePath);

                        // Dosya adından oyun bilgilerini parse et
                        var gameInfo = ParseGameInfoFromFileName(fileName);

                        var game = new Yafes.Models.GameData
                        {
                            Id = GenerateUniqueGameId(fileName),
                            Name = gameInfo.Name,
                            ImageName = fullFileName, // Tam dosya adı (extension ile)
                            SetupPath = $"{gameInfo.Name}\\setup.exe",
                            Category = gameInfo.Category,
                            Size = gameInfo.Size,
                            IsInstalled = false,
                            LastPlayed = DateTime.MinValue,
                            Description = GenerateGameDescription(gameInfo.Name, gameInfo.Category)
                        };

                        games.Add(game);
                        System.Diagnostics.Debug.WriteLine($"✅ Created game: {gameInfo.Name} -> {fullFileName}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error processing file {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                // Alfabetik sırala
                games = games.OrderBy(g => g.Name).ToList();

                System.Diagnostics.Debug.WriteLine($"=== TOPLAM {games.Count} OYUN OLUŞTURULDU (FROM REAL FILES) ===");
                return games;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GenerateGamesFromGamesIconsAsync Hatası: {ex.Message}");
                ErrorOccurred?.Invoke($"GamesIcons'tan oyun listesi oluşturulurken hata: {ex.Message}");
                return new List<Yafes.Models.GameData>();
            }
        }

        /// <summary>
        /// 🎯 SMART PARSING - Dosya adından oyun bilgilerini çıkarır
        /// Örnek: "age_of_darkness_final_stand_FG_5.1GB.png" -> Name, Size, Category
        /// </summary>
        private static (string Name, string Size, string Category) ParseGameInfoFromFileName(string fileName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Parsing: {fileName}");

                // Size extraction - _FG_X.XGB or _X.XGB pattern
                string extractedSize = "Unknown";
                string cleanFileName = fileName;

                var sizeMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"_(?:FG_)?(\d+(?:\.\d+)?GB)(?:\.png)?$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (sizeMatch.Success)
                {
                    extractedSize = sizeMatch.Groups[1].Value;
                    // Remove size part from filename
                    cleanFileName = fileName.Substring(0, sizeMatch.Index);
                    System.Diagnostics.Debug.WriteLine($"📏 Extracted size: {extractedSize}");
                }

                // Clean game name
                var gameName = ParseGameNameFromCleanFileName(cleanFileName);

                // Guess category
                var category = GuessGameCategoryAdvanced(gameName, cleanFileName);

                System.Diagnostics.Debug.WriteLine($"✅ Parsed: '{gameName}' | Size: {extractedSize} | Category: {category}");

                return (gameName, extractedSize, category);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ParseGameInfoFromFileName error: {ex.Message}");
                return (fileName.Replace("_", " "), "Unknown", "General");
            }
        }

        /// <summary>
        /// 🎯 IMPROVED NAME PARSING - Temizlenmiş dosya adından oyun ismini oluşturur
        /// </summary>
        private static string ParseGameNameFromCleanFileName(string cleanFileName)
        {
            if (string.IsNullOrWhiteSpace(cleanFileName))
                return "Unknown Game";

            try
            {
                // Özel durumlar için dictionary - REAL FILE NAMES BASED
                var specialCases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Age of Empires series
                    { "age_of_empires_ıı", "Age of Empires II" },
                    { "age_of_empires_ııı", "Age of Empires III" },
                    { "age_of_empires_ıv", "Age of Empires IV" },
                    { "age_of_darkness_final_stand", "Age of Darkness: Final Stand" },
                    
                    // Assassin's Creed series
                    { "assassin_s_creed_mirage", "Assassin's Creed Mirage" },
                    { "assassin_s_creed_odyssey", "Assassin's Creed Odyssey" },
                    { "assassin_s_creed_valhalla", "Assassin's Creed Valhalla" },
                    
                    // Call of Duty series
                    { "call_of_duty_ghosts", "Call of Duty: Ghosts" },
                    { "call_of_duty_wwıı", "Call of Duty: WWII" },
                    { "call_of_duty_modern_warfare", "Call of Duty: Modern Warfare" },
                    
                    // Grand Theft Auto series
                    { "grand_theft_auto_v", "Grand Theft Auto V" },
                    { "gta_trilogy", "GTA: Trilogy" },
                    { "gta_vice_city", "GTA: Vice City" },
                    
                    // Other popular games
                    { "red_dead_redemption_2", "Red Dead Redemption 2" },
                    { "cyberpunk_2077", "Cyberpunk 2077" },
                    { "the_witcher_3", "The Witcher 3" },
                    { "god_of_war", "God of War" },
                    { "spider_man", "Spider-Man" },
                    { "horizon_forbidden_west", "Horizon Forbidden West" },
                    { "elden_ring", "Elden Ring" }
                };

                // Özel durumları kontrol et
                if (specialCases.ContainsKey(cleanFileName))
                {
                    return specialCases[cleanFileName];
                }

                // Genel parsing - underscore'ları space'e çevir ve title case yap
                var words = cleanFileName.Split('_', StringSplitOptions.RemoveEmptyEntries);
                var result = new List<string>();

                foreach (var word in words)
                {
                    if (word.Length > 0)
                    {
                        // Küçük bağlaçları küçük bırak
                        if (word.ToLower() == "of" || word.ToLower() == "the" ||
                            word.ToLower() == "and" || word.ToLower() == "in" ||
                            word.ToLower() == "a" || word.ToLower() == "an")
                        {
                            result.Add(word.ToLower());
                        }
                        else
                        {
                            // Roman rakamları özel olarak işle
                            if (word.ToLower() == "ıı") result.Add("II");
                            else if (word.ToLower() == "ııı") result.Add("III");
                            else if (word.ToLower() == "ıv") result.Add("IV");
                            else if (word.ToLower() == "v") result.Add("V");
                            else if (word.ToLower() == "vı") result.Add("VI");
                            else
                            {
                                // Normal kelime - ilk harf büyük
                                result.Add(char.ToUpper(word[0]) + word.Substring(1).ToLower());
                            }
                        }
                    }
                }

                var finalResult = string.Join(" ", result);

                // İlk kelime her zaman büyük harfle başlamalı
                if (finalResult.Length > 0 && result.Count > 0)
                {
                    result[0] = char.ToUpper(result[0][0]) + result[0].Substring(1);
                    finalResult = string.Join(" ", result);
                }

                return finalResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ParseGameNameFromCleanFileName Hatası: {ex.Message}");
                return cleanFileName.Replace("_", " ");
            }
        }

        /// <summary>
        /// 🆔 UNIQUE ID GENERATION - Dosya adından benzersiz ID oluşturur
        /// </summary>
        private static string GenerateUniqueGameId(string fileName)
        {
            try
            {
                // Basit: dosya adını ID olarak kullan, özel karakterleri temizle
                var cleanId = fileName.ToLower()
                    .Replace(" ", "_")
                    .Replace(":", "")
                    .Replace("'", "")
                    .Replace("-", "_");

                // Maksimum uzunluk sınırı
                if (cleanId.Length > 50)
                {
                    cleanId = cleanId.Substring(0, 50);
                }

                return cleanId;
            }
            catch
            {
                return Guid.NewGuid().ToString("N")[..16]; // Fallback unique ID
            }
        }

        /// <summary>
        /// 🎯 GELİŞTİRİLMİŞ - Oyun adından kategori tahmin et
        /// </summary>
        private static string GuessGameCategoryAdvanced(string gameName, string fileName)
        {
            var name = gameName.ToLower();
            var file = fileName.ToLower();

            // FPS oyunları
            if (name.Contains("call of duty") || name.Contains("battlefield") ||
                name.Contains("counter strike") || name.Contains("valorant") ||
                name.Contains("doom") || name.Contains("halo") ||
                name.Contains("overwatch") || name.Contains("apex") ||
                name.Contains("titanfall") || name.Contains("far cry"))
                return "FPS";

            // RPG oyunları  
            if (name.Contains("witcher") || name.Contains("elder scrolls") ||
                name.Contains("fallout") || name.Contains("mass effect") ||
                name.Contains("cyberpunk") || name.Contains("dragon age") ||
                name.Contains("baldur") || name.Contains("elden ring") ||
                name.Contains("sekiro") || name.Contains("final fantasy") ||
                name.Contains("persona") || name.Contains("skyrim"))
                return "RPG";

            // Racing oyunları
            if (name.Contains("forza") || name.Contains("need for speed") ||
                name.Contains("gran turismo") || name.Contains("dirt") ||
                name.Contains("f1") || name.Contains("racing") ||
                name.Contains("crew") || name.Contains("burnout"))
                return "Racing";

            // Strategy oyunları
            if (name.Contains("civilization") || name.Contains("age of empires") ||
                name.Contains("total war") || name.Contains("starcraft") ||
                name.Contains("crusader kings") || name.Contains("europa universalis") ||
                name.Contains("hearts of iron") || name.Contains("stellaris"))
                return "Strategy";

            // Action/Adventure oyunları
            if (name.Contains("assassin") || name.Contains("gta") ||
                name.Contains("red dead") || name.Contains("saints row") ||
                name.Contains("spider") || name.Contains("batman") ||
                name.Contains("god of war") || name.Contains("uncharted") ||
                name.Contains("tomb raider") || name.Contains("horizon"))
                return "Action";

            // Sports oyunları
            if (name.Contains("fifa") || name.Contains("nba") ||
                name.Contains("nfl") || name.Contains("sports") ||
                name.Contains("football") || name.Contains("basketball"))
                return "Sports";

            // Horror oyunları
            if (name.Contains("resident evil") || name.Contains("silent hill") ||
                name.Contains("dead space") || name.Contains("outlast") ||
                name.Contains("until dawn") || name.Contains("evil within"))
                return "Horror";

            // Simulation oyunları
            if (name.Contains("cities skylines") || name.Contains("sims") ||
                name.Contains("farming simulator") || name.Contains("truck simulator") ||
                name.Contains("planet") || name.Contains("two point"))
                return "Simulation";

            // Puzzle/Indie oyunları
            if (name.Contains("portal") || name.Contains("tetris") ||
                name.Contains("baba is you") || name.Contains("witness") ||
                name.Contains("ori and") || name.Contains("celeste"))
                return "Puzzle";

            return "General";
        }

        /// <summary>
        /// 🎯 YENİ - Oyun açıklaması oluştur
        /// </summary>
        private static string GenerateGameDescription(string gameName, string category)
        {
            var descriptions = new Dictionary<string, string>
            {
                { "FPS", $"{gameName} - Aksiyon dolu birinci şahıs nişancı oyunu" },
                { "RPG", $"{gameName} - Epik rol yapma macerası" },
                { "Racing", $"{gameName} - Heyecan verici yarış deneyimi" },
                { "Strategy", $"{gameName} - Stratejik düşünce oyunu" },
                { "Action", $"{gameName} - Aksiyon ve macera dolu oyun" },
                { "Sports", $"{gameName} - Gerçekçi spor simülasyonu" },
                { "Horror", $"{gameName} - Korku ve gerilim oyunu" },
                { "Simulation", $"{gameName} - Detaylı simülasyon deneyimi" },
                { "Puzzle", $"{gameName} - Zeka ve bulmaca oyunu" }
            };

            return descriptions.ContainsKey(category)
                ? descriptions[category]
                : $"{gameName} - {category} kategorisinde popüler oyun";
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
                    System.Diagnostics.Debug.WriteLine($"External JSON'dan {gamesDatabase?.Games?.Count ?? 0} oyun yüklendi");
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
                    System.Diagnostics.Debug.WriteLine($"Embedded JSON'dan {gamesDatabase?.Games?.Count ?? 0} oyun yüklendi");
                    return gamesDatabase?.Games ?? new List<Yafes.Models.GameData>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Embedded JSON yüklenemedi: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine("JSON yüklenemedi, null döndürülüyor");
            return null; // null döndür ki otomatik oluşturma tetiklensin
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

                System.Diagnostics.Debug.WriteLine($"JSON kaydedildi: {games.Count} oyun");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON kaydetme hatası: {ex.Message}");
                ErrorOccurred?.Invoke($"JSON kaydedilirken hata: {ex.Message}");
                return false;
            }
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