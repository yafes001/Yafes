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

                // Eğer JSON'dan oyun gelmezse, embedded resource'lardan otomatik oluştur
                if (games == null || games.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("JSON'dan oyun yüklenemedi, embedded resource'lardan oluşturuluyor...");
                    games = await GenerateGamesFromEmbeddedResourcesAsync();

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
        /// 🚀 YENİ VE GELİŞTİRİLMİŞ - Embedded resource'ları tarayarak AKILLI oyun listesi oluşturur
        /// </summary>
        public static async Task<List<Yafes.Models.GameData>> GenerateGamesFromEmbeddedResourcesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== EMBEDDED RESOURCES'DAN OYUN OLUŞTURMA ===");

                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();

                System.Diagnostics.Debug.WriteLine($"Toplam Resource: {resourceNames.Length}");

                // GamePosters klasöründeki PNG'leri bul - DÜZELTME: DOĞRU PATH
                var gamePosterResources = resourceNames
                    .Where(r => r.Contains("GamePosters") && r.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Bulunan PNG Resource: {gamePosterResources.Count}");

                if (gamePosterResources.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Hiç PNG resource bulunamadı!");
                    // Alternative pattern deneyelim
                    var allPngResources = resourceNames.Where(r => r.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToList();
                    System.Diagnostics.Debug.WriteLine($"Tüm PNG'ler: {allPngResources.Count}");
                    foreach (var png in allPngResources.Take(5))
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {png}");
                    }
                }

                var games = new List<Yafes.Models.GameData>();

                foreach (var resource in gamePosterResources)
                {
                    try
                    {
                        // Resource path'inden dosya adını çıkar
                        var fileName = Path.GetFileName(resource);
                        var gameId = Path.GetFileNameWithoutExtension(fileName);

                        // 🎯 AKILLI İSİM OLUŞTURMA
                        var gameName = ParseGameNameFromFileName(gameId);

                        // 🎯 AKILLI KATEGORİ TAHMİNİ
                        var category = GuessGameCategoryAdvanced(gameName, gameId);

                        // 🎯 AKILLI BOYUT TAHMİNİ
                        var size = EstimateGameSize(gameName, category);

                        var game = new Yafes.Models.GameData
                        {
                            Id = gameId,
                            Name = gameName,
                            ImageName = fileName,
                            SetupPath = $"{gameName}\\setup.exe",
                            Category = category,
                            Size = size,
                            IsInstalled = false,
                            LastPlayed = DateTime.MinValue,
                            Description = GenerateGameDescription(gameName, category)
                        };

                        games.Add(game);
                        System.Diagnostics.Debug.WriteLine($"✅ Oyun oluşturuldu: {gameName} ({category})");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Resource işleme hatası: {resource} - {ex.Message}");
                    }
                }

                // Alfabetik sırala
                games = games.OrderBy(g => g.Name).ToList();

                System.Diagnostics.Debug.WriteLine($"=== TOPLAM {games.Count} OYUN OLUŞTURULDU ===");
                return games;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GenerateGamesFromEmbeddedResourcesAsync Hatası: {ex.Message}");
                ErrorOccurred?.Invoke($"Otomatik oyun listesi oluşturulurken hata: {ex.Message}");
                return new List<Yafes.Models.GameData>();
            }
        }

        /// <summary>
        /// 🎯 SÜPER AKILLI - Dosya adından oyun ismini parse eder
        /// </summary>
        private static string ParseGameNameFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown Game";

            try
            {
                // Özel durumlar için dictionary
                var specialCases = new Dictionary<string, string>
                {
                    // Roman rakamları düzeltmeleri
                    { "age_of_empires_ıv", "Age of Empires IV" },
                    { "age_of_empires_ıı", "Age of Empires II" },
                    { "age_of_empires_ııı", "Age of Empires III" },
                    { "cities_skylines_ıı", "Cities: Skylines II" },
                    { "sid_meiers_civilization_vı", "Sid Meier's Civilization VI" },
                    { "total_war_warhammer_ıı", "Total War: Warhammer II" },
                    { "the_last_of_us_part_ı", "The Last of Us Part I" },
                    { "the_last_of_us_part_ıı_remastered", "The Last of Us Part II Remastered" },
                    
                    // Özel isimler
                    { "assassin_s_creed_mirage", "Assassin's Creed Mirage" },
                    { "assassin_s_creed_odyssey", "Assassin's Creed Odyssey" },
                    { "assassin_s_creed_ıv_black_flag", "Assassin's Creed IV: Black Flag" },
                    { "call_of_duty_ghosts", "Call of Duty: Ghosts" },
                    { "call_of_duty_wwıı", "Call of Duty: WWII" },
                    { "grand_theft_auto_v", "Grand Theft Auto V" },
                    { "gta_trilogy", "GTA: Trilogy" },
                    { "gta_vice_city_nextgen", "GTA: Vice City NextGen" },
                    { "red_dead_redemption_2", "Red Dead Redemption 2" },
                    { "cyberpunk", "Cyberpunk 2077" },
                    { "codex_doom_eternal", "DOOM Eternal" },
                    { "half_life_2", "Half-Life 2" },
                    { "half_life_alyx", "Half-Life: Alyx" },
                    { "left_4_dead_2", "Left 4 Dead 2" },
                    { "counter_strike_2", "Counter-Strike 2" },
                    { "the_witcher_3_ce", "The Witcher 3: Complete Edition" },
                    { "god_of_war", "God of War" },
                    { "god_of_war_ragnarok", "God of War Ragnarök" },
                    { "spider_man_remastered", "Spider-Man Remastered" },
                    { "spider_man_miles_morales", "Spider-Man: Miles Morales" },
                    { "horizon_forbidden_west_ce", "Horizon Forbidden West: Complete Edition" },
                    { "ghost_of_tsushima_dc", "Ghost of Tsushima: Director's Cut" },
                    { "death_strandıng_dırectors", "Death Stranding: Director's Cut" },
                    { "final_fantasy_vıı_rebirth", "Final Fantasy VII Rebirth" },
                    { "crısıs_core_ff7_reunıon", "Crisis Core: Final Fantasy VII Reunion" },
                    { "tekken_7", "Tekken 7" },
                    { "tekken_8", "Tekken 8" },
                    { "resident_evil_2", "Resident Evil 2" },
                    { "resident_evil_3", "Resident Evil 3" },
                    { "resident_evil_4_hd_project", "Resident Evil 4: HD Project" },
                    { "resident_evil_7_biohazard", "Resident Evil 7: Biohazard" },
                    { "resident_evil_village", "Resident Evil Village" },
                    { "silent_hill_2_remake", "Silent Hill 2 Remake" },
                    { "need_for_speed_heat", "Need for Speed: Heat" },
                    { "need_for_speed_most_wanted", "Need for Speed: Most Wanted" },
                    { "nfs_hot_pursuit_remastered", "Need for Speed: Hot Pursuit Remastered" },
                    { "forza_horizon_5", "Forza Horizon 5" },
                    { "fıfa_23", "FIFA 23" },
                    { "the_sims_4", "The Sims 4" },
                    { "star_wars_jedi_survivor", "Star Wars Jedi: Survivor" },
                    { "elden_rıng", "Elden Ring" },
                    { "baldur_s_gate_3", "Baldur's Gate 3" },
                    { "hogwarts_legacy", "Hogwarts Legacy" },
                    { "sekiro_shadows_die_twice", "Sekiro: Shadows Die Twice" },
                    { "metal_gear_solid_v_tpp", "Metal Gear Solid V: The Phantom Pain" },
                    { "monster_hunter_world_ıceborne", "Monster Hunter World: Iceborne" },
                    { "monster_hunter_rise", "Monster Hunter Rise" },
                    { "palworld", "Palworld" },
                    { "starfield", "Starfield" },
                    { "atomic_heart", "Atomic Heart" },
                    { "lies_of_p", "Lies of P" },
                    { "sıfu", "Sifu" },
                    { "control", "Control" },
                    { "prey", "Prey" },
                    { "dishonored_collection", "Dishonored Collection" },
                    { "deathloop", "Deathloop" },
                    { "it_takes_two", "It Takes Two" },
                    { "a_way_out", "A Way Out" },
                    { "uncharted_lotc", "Uncharted: Legacy of Thieves Collection" },
                    { "the_legend_of_zelda_totk", "The Legend of Zelda: Tears of the Kingdom" },
                    { "super_mario_bros_wonder", "Super Mario Bros. Wonder" }
                };

                // Özel durumları kontrol et
                var lowerFileName = fileName.ToLower();
                if (specialCases.ContainsKey(lowerFileName))
                {
                    return specialCases[lowerFileName];
                }

                // Genel parsing
                var formatted = fileName.Replace("_", " ");

                // Kelimelerin ilk harflerini büyük yap
                var words = formatted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var result = new List<string>();

                foreach (var word in words)
                {
                    if (word.Length > 0)
                    {
                        // Küçük bağlaçları küçük bırak (of, the, and, vb.)
                        if (word.ToLower() == "of" || word.ToLower() == "the" ||
                            word.ToLower() == "and" || word.ToLower() == "in" ||
                            word.ToLower() == "a" || word.ToLower() == "an")
                        {
                            result.Add(word.ToLower());
                        }
                        else
                        {
                            // Normal kelime - ilk harf büyük
                            result.Add(char.ToUpper(word[0]) + word.Substring(1).ToLower());
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
                System.Diagnostics.Debug.WriteLine($"ParseGameNameFromFileName Hatası: {ex.Message}");
                return fileName.Replace("_", " ");
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
        /// 🎯 YENİ - Oyun boyutunu tahmin et
        /// </summary>
        private static string EstimateGameSize(string gameName, string category)
        {
            var name = gameName.ToLower();

            // Büyük AAA oyunları
            if (name.Contains("call of duty") || name.Contains("battlefield") ||
                name.Contains("gta") || name.Contains("red dead") ||
                name.Contains("cyberpunk") || name.Contains("witcher 3") ||
                name.Contains("horizon") || name.Contains("god of war"))
                return "80-150 GB";

            // Orta boyut oyunları
            if (category == "FPS" || category == "Action" || category == "RPG")
                return "25-60 GB";

            // Strategy oyunları genelde küçük
            if (category == "Strategy" || category == "Simulation")
                return "5-20 GB";

            // İndie/küçük oyunlar
            if (category == "Puzzle" || name.Contains("indie"))
                return "1-5 GB";

            // Varsayılan
            return "15-40 GB";
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