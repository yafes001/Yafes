using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;

namespace Yafes.Managers
{
    internal static class ImageManager
    {
        // Cache sistemi - FIXED: Unique cache keys
        private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();
        private static BitmapImage _defaultGameImage;

        // Disk ve klasör yapılandırması
        private static string _gamesDiskPath = null;
        private static string _gamesIconsPath = null;
        private static bool _pathsInitialized = false;
        private static readonly object _initLock = new object();

        // Desteklenen dosya formatları
        private static readonly string[] SUPPORTED_EXTENSIONS = { ".png", ".jpg", ".jpeg" };
        private const string TARGET_DISK_NAME = "Game";
        private const string TARGET_FOLDER_NAME = "GamesIcons";

        /// <summary>
        /// 🚀 FIXED MAIN METHOD - Unique images only
        /// </summary>
        public static BitmapImage GetGameImage(string imageName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"\n=== IMAGE MANAGER DEBUG START ===");
                System.Diagnostics.Debug.WriteLine($"🔍 Requested: '{imageName}'");

                // FIXED: Unique cache key with input validation
                if (string.IsNullOrWhiteSpace(imageName))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Empty image name - returning default");
                    return GetDefaultImage();
                }

                string cacheKey = $"IMG_{imageName.Trim()}".ToLower();
                if (_imageCache.TryGetValue(cacheKey, out var cachedImage))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Found in cache: {cacheKey}");
                    return cachedImage;
                }

                // Path'lerin initialize edildiğinden emin ol
                if (!EnsurePathsInitialized())
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Paths not initialized - returning default");
                    var defaultImg = GetDefaultImage();
                    _imageCache[cacheKey] = defaultImg; // Cache to avoid re-checking
                    return defaultImg;
                }

                System.Diagnostics.Debug.WriteLine($"📁 GamesIcons Path: {_gamesIconsPath}");

                // ONLY EXACT MATCH - No fuzzy search to prevent wrong assignments
                var image = LoadImageFromLocalDiskExactOnly(imageName);
                if (image != null)
                {
                    _imageCache[cacheKey] = image;
                    System.Diagnostics.Debug.WriteLine($"✅ EXACT MATCH: Loaded and cached: {cacheKey}");
                    return image;
                }

                System.Diagnostics.Debug.WriteLine($"❌ NO EXACT MATCH: Returning default for {imageName}");
                var defaultImage = GetDefaultImage();
                // DON'T cache default images to allow future retries
                return defaultImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception in GetGameImage: {ex.Message}");
                return GetDefaultImage();
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"=== IMAGE MANAGER DEBUG END ===\n");
            }
        }

        /// <summary>
        /// 🔍 DISK SCANNING - Game diskini ve GamesIcons klasörünü bulur
        /// </summary>
        private static bool EnsurePathsInitialized()
        {
            if (_pathsInitialized)
                return _gamesDiskPath != null && _gamesIconsPath != null;

            lock (_initLock)
            {
                if (_pathsInitialized)
                    return _gamesDiskPath != null && _gamesIconsPath != null;

                try
                {
                    System.Diagnostics.Debug.WriteLine("🔍 === DISK SCANNING START ===");

                    // Tüm local drive'ları tara
                    var drives = DriveInfo.GetDrives()
                        .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                        .ToArray();

                    System.Diagnostics.Debug.WriteLine($"📀 Found {drives.Length} drives:");
                    foreach (var drive in drives)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {drive.Name} Label:'{drive.VolumeLabel}' Type:{drive.DriveType}");
                    }

                    // 1. Önce "Game" label'lı disk ara
                    foreach (var drive in drives)
                    {
                        try
                        {
                            if (string.Equals(drive.VolumeLabel, TARGET_DISK_NAME, StringComparison.OrdinalIgnoreCase))
                            {
                                var gamesIconsPath = Path.Combine(drive.RootDirectory.FullName, TARGET_FOLDER_NAME);
                                System.Diagnostics.Debug.WriteLine($"🎯 Checking Game disk: {gamesIconsPath}");

                                if (Directory.Exists(gamesIconsPath))
                                {
                                    _gamesDiskPath = drive.RootDirectory.FullName;
                                    _gamesIconsPath = gamesIconsPath;

                                    var iconFiles = Directory.GetFiles(_gamesIconsPath, "*.*", SearchOption.TopDirectoryOnly)
                                        .Where(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLower()))
                                        .ToArray();

                                    System.Diagnostics.Debug.WriteLine($"✅ SUCCESS: Game disk found!");
                                    System.Diagnostics.Debug.WriteLine($"📁 Path: {_gamesIconsPath}");
                                    System.Diagnostics.Debug.WriteLine($"📊 Files: {iconFiles.Length} images");

                                    // İlk 10 dosyayı listele
                                    System.Diagnostics.Debug.WriteLine("📋 First 10 files:");
                                    foreach (var file in iconFiles.Take(10))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(file)}");
                                    }

                                    _pathsInitialized = true;
                                    return true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Error checking drive {drive.Name}: {ex.Message}");
                        }
                    }

                    // 2. DIRECT D:\ CHECK - Force check D:\GamesIcons
                    System.Diagnostics.Debug.WriteLine("🎯 DIRECT CHECK: Looking for D:\\GamesIcons...");
                    string directPath = @"D:\GamesIcons";
                    if (Directory.Exists(directPath))
                    {
                        _gamesDiskPath = @"D:\";
                        _gamesIconsPath = directPath;

                        var iconFiles = Directory.GetFiles(_gamesIconsPath, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLower()))
                            .ToArray();

                        System.Diagnostics.Debug.WriteLine($"✅ DIRECT SUCCESS: D:\\GamesIcons found!");
                        System.Diagnostics.Debug.WriteLine($"📁 Path: {_gamesIconsPath}");
                        System.Diagnostics.Debug.WriteLine($"📊 Files: {iconFiles.Length} images");

                        // İlk 10 dosyayı listele
                        System.Diagnostics.Debug.WriteLine("📋 Sample files:");
                        foreach (var file in iconFiles.Take(10))
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(file)}");
                        }

                        _pathsInitialized = true;
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ D:\\GamesIcons does not exist!");
                    }

                    // 2. Alternatif: Tüm drive'larda GamesIcons klasörü ara
                    System.Diagnostics.Debug.WriteLine("🔍 Alternative search: Looking for GamesIcons folder...");
                    foreach (var drive in drives)
                    {
                        try
                        {
                            var possiblePaths = new[]
                            {
                                Path.Combine(drive.RootDirectory.FullName, TARGET_FOLDER_NAME),
                                Path.Combine(drive.RootDirectory.FullName, "Games", TARGET_FOLDER_NAME),
                                Path.Combine(drive.RootDirectory.FullName, "Gaming", TARGET_FOLDER_NAME)
                            };

                            foreach (var path in possiblePaths)
                            {
                                if (Directory.Exists(path))
                                {
                                    _gamesDiskPath = drive.RootDirectory.FullName;
                                    _gamesIconsPath = path;

                                    var iconFiles = Directory.GetFiles(_gamesIconsPath, "*.*", SearchOption.TopDirectoryOnly)
                                        .Where(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLower()))
                                        .ToArray();

                                    System.Diagnostics.Debug.WriteLine($"✅ ALTERNATIVE SUCCESS: Found at {path}");
                                    System.Diagnostics.Debug.WriteLine($"📊 Files: {iconFiles.Length} images");

                                    _pathsInitialized = true;
                                    return true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Error in alternative search {drive.Name}: {ex.Message}");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("❌ NO GAMESICONS FOLDER FOUND!");
                    _pathsInitialized = true;
                    return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Disk scanning failed: {ex.Message}");
                    _pathsInitialized = true;
                    return false;
                }
            }
        }

        /// <summary>
        /// 📁 EXACT MATCH ONLY - Sadece tam eşleşen dosyaları yükler
        /// </summary>
        private static BitmapImage LoadImageFromLocalDiskExactOnly(string imageName)
        {
            try
            {
                if (string.IsNullOrEmpty(_gamesIconsPath) || !Directory.Exists(_gamesIconsPath))
                {
                    System.Diagnostics.Debug.WriteLine("❌ GamesIcons path not available");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"🔍 === EXACT SEARCH FOR: '{imageName}' ===");

                // LIMITED variations - only exact naming conventions
                var imageVariations = GenerateExactImageNameVariations(imageName);

                System.Diagnostics.Debug.WriteLine($"📝 Generated {imageVariations.Count} exact variations:");
                foreach (var variation in imageVariations)
                {
                    System.Diagnostics.Debug.WriteLine($"  - '{variation}'");
                }

                // STRICT exact match only
                foreach (var variation in imageVariations)
                {
                    foreach (var extension in SUPPORTED_EXTENSIONS)
                    {
                        var fullPath = Path.Combine(_gamesIconsPath, variation + extension);

                        if (File.Exists(fullPath))
                        {
                            System.Diagnostics.Debug.WriteLine($"🎯 EXACT MATCH FOUND: {Path.GetFileName(fullPath)}");

                            var image = LoadBitmapFromFile(fullPath);
                            if (image != null)
                            {
                                return image;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("❌ No exact match found - NO FUZZY SEARCH");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadImageFromLocalDiskExactOnly error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎯 EXACT VARIATIONS ONLY - Conservative name variations
        /// </summary>
        private static List<string> GenerateExactImageNameVariations(string baseName)
        {
            var variations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(baseName))
                return variations.ToList();

            var cleanName = baseName.Trim();

            // Remove extension if present
            if (cleanName.Contains('.'))
            {
                cleanName = Path.GetFileNameWithoutExtension(cleanName);
            }

            // ONLY EXACT variations - no fuzzy matching
            variations.Add(cleanName);
            variations.Add(cleanName.ToLower());
            variations.Add(cleanName.Replace(" ", "_"));
            variations.Add(cleanName.Replace(" ", "_").ToLower());
            variations.Add(cleanName.Replace("_", " "));
            variations.Add(cleanName.Replace("_", " ").ToLower());

            // Basic punctuation handling only
            variations.Add(cleanName.Replace(":", ""));
            variations.Add(cleanName.Replace(":", "").Replace(" ", "_"));
            variations.Add(cleanName.Replace("'", ""));
            variations.Add(cleanName.Replace("'", "").Replace(" ", "_"));

            return variations.ToList();
        }

        /// <summary>
        /// 🔎 IMPROVED FUZZY SEARCH - Better similarity algorithm
        /// </summary>
        private static BitmapImage FindSimilarImageInDirectory(string targetName)
        {
            try
            {
                if (string.IsNullOrEmpty(_gamesIconsPath) || !Directory.Exists(_gamesIconsPath))
                    return null;

                var allImageFiles = Directory.GetFiles(_gamesIconsPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLower()))
                    .ToArray();

                if (allImageFiles.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ No image files found in directory");
                    return null;
                }

                var cleanTarget = CleanStringForComparison(targetName);
                System.Diagnostics.Debug.WriteLine($"🎯 Fuzzy search for: '{cleanTarget}'");

                string bestMatch = null;
                int bestScore = 0;
                const int MIN_SCORE_THRESHOLD = 3; // Minimum similarity required

                foreach (var filePath in allImageFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var cleanFileName = CleanStringForComparison(fileName);

                    int score = CalculateAdvancedSimilarityScore(cleanTarget, cleanFileName);

                    System.Diagnostics.Debug.WriteLine($"  📊 '{fileName}' -> Score: {score}");

                    if (score > bestScore && score >= MIN_SCORE_THRESHOLD)
                    {
                        bestScore = score;
                        bestMatch = filePath;
                    }
                }

                if (bestMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 FUZZY MATCH: '{Path.GetFileName(bestMatch)}' (Score: {bestScore})");
                    return LoadBitmapFromFile(bestMatch);
                }

                System.Diagnostics.Debug.WriteLine("❌ No fuzzy match found above threshold");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ FindSimilarImageInDirectory error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🧹 STRING CLEANING for better comparison
        /// </summary>
        private static string CleanStringForComparison(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input.ToLower()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace(":", "")
                .Replace("'", "")
                .Replace(".", "")
                .Replace("ı", "i")
                .Replace("ş", "s")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ç", "c");
        }

        /// <summary>
        /// 📊 ADVANCED SIMILARITY SCORING
        /// </summary>
        private static int CalculateAdvancedSimilarityScore(string target, string candidate)
        {
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(candidate))
                return 0;

            int score = 0;

            // Exact match
            if (target == candidate)
                return 100;

            // Contains checks
            if (candidate.Contains(target))
                score += target.Length * 3;

            if (target.Contains(candidate))
                score += candidate.Length * 3;

            // Word-based similarity
            var targetWords = target.Split(new char[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var candidateWords = candidate.Split(new char[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var targetWord in targetWords)
            {
                foreach (var candidateWord in candidateWords)
                {
                    if (targetWord == candidateWord)
                        score += targetWord.Length * 2;
                    else if (targetWord.Contains(candidateWord) || candidateWord.Contains(targetWord))
                        score += Math.Min(targetWord.Length, candidateWord.Length);
                }
            }

            // Character similarity
            int commonChars = 0;
            int minLength = Math.Min(target.Length, candidate.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (target[i] == candidate[i])
                    commonChars++;
            }

            score += commonChars;

            return score;
        }

        /// <summary>
        /// 💾 FILE TO BITMAP - Dosyadan BitmapImage yükler
        /// </summary>
        private static BitmapImage LoadBitmapFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze(); // Thread-safe hale getir

                System.Diagnostics.Debug.WriteLine($"✅ Successfully loaded bitmap from: {Path.GetFileName(filePath)}");
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadBitmapFromFile error {Path.GetFileName(filePath)}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎨 IMPROVED DEFAULT IMAGE
        /// </summary>
        public static BitmapImage GetDefaultImage()
        {
            if (_defaultGameImage == null)
            {
                _defaultGameImage = CreateDefaultImage();
            }
            return _defaultGameImage;
        }

        /// <summary>
        /// 🎨 CREATE DEFAULT - FIXED: No more "Yafes.resources" text
        /// </summary>
        private static BitmapImage CreateDefaultImage()
        {
            try
            {
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    // Modern gradient arkaplan
                    var gradientBrush = new LinearGradientBrush(
                        System.Windows.Media.Color.FromRgb(45, 45, 48),
                        System.Windows.Media.Color.FromRgb(25, 25, 28),
                        new System.Windows.Point(0, 0),
                        new System.Windows.Point(1, 1));

                    context.DrawRectangle(gradientBrush, null, new System.Windows.Rect(0, 0, 460, 215));

                    // Modern kenarlık
                    var borderPen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Orange, 2);
                    borderPen.DashStyle = DashStyles.Dash;
                    context.DrawRectangle(null, borderPen, new System.Windows.Rect(5, 5, 450, 205));

                    // Büyük oyun ikonu
                    var iconText = new FormattedText(
                        "🎮",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Segoe UI Emoji"),
                        48,
                        System.Windows.Media.Brushes.Orange,
                        96);

                    var iconX = (460 - iconText.Width) / 2;
                    var iconY = 50;
                    context.DrawText(iconText, new System.Windows.Point(iconX, iconY));

                    // "NO IMAGE" yazısı
                    var gameText = new FormattedText(
                        "NO IMAGE",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Trebuchet MS"),
                        20,
                        System.Windows.Media.Brushes.White,
                        96);

                    var gameTextX = (460 - gameText.Width) / 2;
                    var gameTextY = 120;
                    context.DrawText(gameText, new System.Windows.Point(gameTextX, gameTextY));

                    // "Check GamesIcons Folder" yazısı
                    var subText = new FormattedText(
                        "Check Game:/GamesIcons/ Folder",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Trebuchet MS"),
                        11,
                        System.Windows.Media.Brushes.LightGray,
                        96);

                    var subTextX = (460 - subText.Width) / 2;
                    var subTextY = 160;
                    context.DrawText(subText, new System.Windows.Point(subTextX, subTextY));
                }

                var renderTarget = new RenderTargetBitmap(460, 215, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ CreateDefaultImage error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🔄 REFRESH PATHS - Path'leri yeniden tara
        /// </summary>
        public static void RefreshPaths()
        {
            lock (_initLock)
            {
                _pathsInitialized = false;
                _gamesDiskPath = null;
                _gamesIconsPath = null;
                System.Diagnostics.Debug.WriteLine("🔄 Paths reset, will rescan on next request");
            }
        }

        /// <summary>
        /// 🧹 STRICT CACHE MANAGEMENT - Her oyun için benzersiz cache
        /// </summary>
        public static void ClearCache()
        {
            _imageCache.Clear();
            _defaultGameImage = null;
            System.Diagnostics.Debug.WriteLine("🧹 ALL IMAGE CACHE CLEARED - Fresh start");
        }

        /// <summary>
        /// 🗑️ REMOVE FROM CACHE - Specific game image removal
        /// </summary>
        public static void RemoveFromCache(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName)) return;

            string cacheKey = $"IMG_{imageName.Trim()}".ToLower();
            bool removed = _imageCache.Remove(cacheKey);
            System.Diagnostics.Debug.WriteLine($"🗑️ Cache removal for '{imageName}': {(removed ? "SUCCESS" : "NOT FOUND")}");
        }

        /// <summary>
        /// 🔍 CACHE INSPECTION - Debug cache contents
        /// </summary>
        public static void LogCacheContents()
        {
            System.Diagnostics.Debug.WriteLine($"\n=== CACHE CONTENTS ({_imageCache.Count} items) ===");

            if (_imageCache.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("📭 Cache is empty");
                return;
            }

            foreach (var kvp in _imageCache.Take(10)) // Show first 10
            {
                System.Diagnostics.Debug.WriteLine($"🔑 {kvp.Key}");
            }

            if (_imageCache.Count > 10)
            {
                System.Diagnostics.Debug.WriteLine($"... and {_imageCache.Count - 10} more");
            }

            System.Diagnostics.Debug.WriteLine("=== END CACHE CONTENTS ===\n");
        }

        /// <summary>
        /// 📊 CACHE STATS
        /// </summary>
        public static (int cachedCount, string gamesPath, int availableFiles) GetCacheStats()
        {
            int availableFiles = 0;

            if (!string.IsNullOrEmpty(_gamesIconsPath) && Directory.Exists(_gamesIconsPath))
            {
                availableFiles = Directory.GetFiles(_gamesIconsPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Count(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLower()));
            }

            return (_imageCache.Count, _gamesIconsPath ?? "Not Found", availableFiles);
        }

        /// <summary>
        /// 🔍 DEBUG - Disk durumu
        /// </summary>
        public static string GetDiskStatus()
        {
            var status = "=== DISK STATUS ===\n";

            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady);

                foreach (var drive in drives)
                {
                    status += $"Drive: {drive.Name} - Label: '{drive.VolumeLabel}' - Space: {drive.AvailableFreeSpace / (1024 * 1024 * 1024)}GB\n";
                }

                status += $"\nGame Disk Path: {_gamesDiskPath ?? "Not Found"}\n";
                status += $"GamesIcons Path: {_gamesIconsPath ?? "Not Found"}\n";
                status += $"Paths Initialized: {_pathsInitialized}\n";

                if (!string.IsNullOrEmpty(_gamesIconsPath) && Directory.Exists(_gamesIconsPath))
                {
                    var files = Directory.GetFiles(_gamesIconsPath, "*.*")
                        .Where(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLower()))
                        .ToArray();
                    status += $"Image Files: {files.Length}\n";

                    if (files.Length > 0)
                    {
                        status += "Sample files:\n";
                        foreach (var file in files.Take(5))
                        {
                            status += $"  - {Path.GetFileName(file)}\n";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                status += $"Error: {ex.Message}";
            }

            return status;
        }

        /// <summary>
        /// 🧪 TEST METHOD - Belirli bir isim için test yapar
        /// </summary>
        public static void TestImageSearch(string imageName)
        {
            System.Diagnostics.Debug.WriteLine($"\n🧪 === TESTING IMAGE SEARCH FOR: '{imageName}' ===");

            if (!EnsurePathsInitialized())
            {
                System.Diagnostics.Debug.WriteLine("❌ Paths not initialized");
                return;
            }

            var variations = GenerateExactImageNameVariations(imageName);
            System.Diagnostics.Debug.WriteLine($"📝 Generated {variations.Count} exact variations");

            var allFiles = Directory.GetFiles(_gamesIconsPath, "*.*")
                .Where(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLower()))
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToArray();

            System.Diagnostics.Debug.WriteLine($"📁 Available files: {allFiles.Length}");

            foreach (var variation in variations)
            {
                var exactMatches = allFiles.Where(f => f.Equals(variation, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (exactMatches.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"✅ EXACT MATCH '{variation}' -> Found: {string.Join(", ", exactMatches)}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ No match for '{variation}'");
                }
            }

            // Test the actual GetGameImage call
            System.Diagnostics.Debug.WriteLine($"\n🎯 Testing GetGameImage('{imageName}'):");
            var result = GetGameImage(imageName);
            var isDefault = (result == GetDefaultImage());
            System.Diagnostics.Debug.WriteLine($"Result: {(isDefault ? "DEFAULT IMAGE" : "FOUND SPECIFIC IMAGE")}");

            System.Diagnostics.Debug.WriteLine("🧪 === TEST COMPLETE ===\n");
        }
    }
}