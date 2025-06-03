using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;

namespace Yafes.Managers
{
    internal static class ImageManager
    {
        // Cache sistemi - Bir kez yüklenen imageler RAM'de kalır
        private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        // Default image cache
        private static BitmapImage _defaultGameImage;

        // 🔧 DÜZELTME: Resource path constants - DOĞRU PATH'LER
        private static readonly string[] POSSIBLE_RESOURCE_PATHS = {
            "Yafes.Resources.GamePosters.",     // Ana path
            "Resources.GamePosters.",           // Alternatif path 1
            "Yafes.GamePosters.",              // Alternatif path 2
            "GamePosters."                      // Alternatif path 3
        };

        private const string DEFAULT_IMAGE_PATH = "Yafes.Resources.default_game.png";

        /// <summary>
        /// 🚀 GELİŞTİRİLMİŞ - WPF için BitmapImage döndürür (XAML Image kontrolları için)
        /// </summary>
        public static BitmapImage GetGameImage(string imageName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== IMAGE MANAGER: {imageName} aranıyor ===");

                // Cache'de var mı kontrol et
                if (_imageCache.TryGetValue(imageName, out var cachedImage))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Cache'den döndürüldü: {imageName}");
                    return cachedImage;
                }

                // Embedded resource'dan yüklemeyi dene - AKILLI PATH ARAMA
                var image = LoadImageFromEmbeddedResourceSmart(imageName);
                if (image != null)
                {
                    _imageCache[imageName] = image;
                    System.Diagnostics.Debug.WriteLine($"✅ Embedded resource'dan yüklendi: {imageName}");
                    return image;
                }

                // Fallback: Default image
                System.Diagnostics.Debug.WriteLine($"⚠️ Image bulunamadı, default döndürülüyor: {imageName}");
                return GetDefaultImage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Image yükleme hatası: {imageName} - {ex.Message}");
                return GetDefaultImage();
            }
        }

        /// <summary>
        /// 🎯 YENİ - AKILLI PATH ARAMA ile embedded resource'dan BitmapImage yükler
        /// </summary>
        private static BitmapImage LoadImageFromEmbeddedResourceSmart(string imageName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Önce mevcut tüm resource'ları listele (debug için)
                var allResources = assembly.GetManifestResourceNames();
                System.Diagnostics.Debug.WriteLine($"🔍 Toplam {allResources.Length} embedded resource var");

                // PNG olan resource'ları filtrele
                var pngResources = Array.FindAll(allResources, r => r.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                System.Diagnostics.Debug.WriteLine($"🖼️ PNG resource sayısı: {pngResources.Length}");

                // Debug: İlk 5 PNG resource'u göster
                for (int i = 0; i < Math.Min(5, pngResources.Length); i++)
                {
                    System.Diagnostics.Debug.WriteLine($"  📁 {pngResources[i]}");
                }

                // 1. DİREKT İSİM ARAMASI
                string targetImageName = imageName;
                if (!targetImageName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    targetImageName += ".png";
                }

                // Tüm olası path'leri dene
                foreach (var basePath in POSSIBLE_RESOURCE_PATHS)
                {
                    var fullResourceName = basePath + targetImageName;
                    System.Diagnostics.Debug.WriteLine($"🔍 Deneniyor: {fullResourceName}");

                    var stream = assembly.GetManifestResourceStream(fullResourceName);
                    if (stream != null)
                    {
                        var bitmap = CreateBitmapFromStream(stream);
                        if (bitmap != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ BULUNDU: {fullResourceName}");
                            return bitmap;
                        }
                    }
                }

                // 2. FUZZY SEARCH - Benzer isim arama
                System.Diagnostics.Debug.WriteLine($"🔍 Fuzzy search başlatılıyor: {targetImageName}");
                var fuzzyMatch = FindFuzzyMatch(pngResources, targetImageName);
                if (!string.IsNullOrEmpty(fuzzyMatch))
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 Fuzzy match bulundu: {fuzzyMatch}");
                    var stream = assembly.GetManifestResourceStream(fuzzyMatch);
                    if (stream != null)
                    {
                        var bitmap = CreateBitmapFromStream(stream);
                        if (bitmap != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ FUZZY MATCH BAŞARILI: {fuzzyMatch}");
                            return bitmap;
                        }
                    }
                }

                // 3. PARTIAL MATCH - Kısmi eşleşme
                var partialMatch = FindPartialMatch(pngResources, imageName);
                if (!string.IsNullOrEmpty(partialMatch))
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 Partial match bulundu: {partialMatch}");
                    var stream = assembly.GetManifestResourceStream(partialMatch);
                    if (stream != null)
                    {
                        var bitmap = CreateBitmapFromStream(stream);
                        if (bitmap != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ PARTIAL MATCH BAŞARILI: {partialMatch}");
                            return bitmap;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"❌ Hiçbir yöntemle bulunamadı: {imageName}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadImageFromEmbeddedResourceSmart hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎯 YENİ - Fuzzy string matching ile benzer isim bulur
        /// </summary>
        private static string FindFuzzyMatch(string[] resources, string targetName)
        {
            try
            {
                var cleanTarget = targetName.ToLower().Replace(".png", "");

                foreach (var resource in resources)
                {
                    var resourceFileName = Path.GetFileNameWithoutExtension(resource).ToLower();

                    // Tam eşleşme
                    if (resourceFileName == cleanTarget)
                    {
                        return resource;
                    }

                    // İçerir kontrolü
                    if (resourceFileName.Contains(cleanTarget) || cleanTarget.Contains(resourceFileName))
                    {
                        return resource;
                    }

                    // Underscore'ları temizleyerek dene
                    var cleanResource = resourceFileName.Replace("_", "");
                    var cleanTargetNoUnderscore = cleanTarget.Replace("_", "");

                    if (cleanResource == cleanTargetNoUnderscore)
                    {
                        return resource;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ FindFuzzyMatch hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎯 YENİ - Kısmi eşleşme ile benzer isim bulur
        /// </summary>
        private static string FindPartialMatch(string[] resources, string targetName)
        {
            try
            {
                var cleanTarget = targetName.ToLower().Replace(".png", "").Replace("_", " ");
                var targetWords = cleanTarget.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                string bestMatch = null;
                int bestScore = 0;

                foreach (var resource in resources)
                {
                    var resourceFileName = Path.GetFileNameWithoutExtension(resource).ToLower().Replace("_", " ");
                    var resourceWords = resourceFileName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    int matchScore = 0;
                    foreach (var targetWord in targetWords)
                    {
                        foreach (var resourceWord in resourceWords)
                        {
                            if (resourceWord.Contains(targetWord) || targetWord.Contains(resourceWord))
                            {
                                matchScore++;
                            }
                        }
                    }

                    if (matchScore > bestScore && matchScore >= targetWords.Length / 2) // En az yarısı eşleşmeli
                    {
                        bestScore = matchScore;
                        bestMatch = resource;
                    }
                }

                return bestMatch;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ FindPartialMatch hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎯 YENİ - Stream'den BitmapImage oluşturur
        /// </summary>
        private static BitmapImage CreateBitmapFromStream(Stream stream)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Stream kapandıktan sonra da kullanılabilir
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // Thread-safe hale getir

                stream.Dispose(); // Stream'i temizle
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ CreateBitmapFromStream hatası: {ex.Message}");
                stream?.Dispose();
                return null;
            }
        }

        /// <summary>
        /// 🚀 GELİŞTİRİLMİŞ - Default game image'ını döndürür
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
        /// 🚀 GELİŞTİRİLMİŞ - Default image oluşturur
        /// </summary>
        private static BitmapImage CreateDefaultImage()
        {
            try
            {
                // Önce embedded default image'ı yüklemeyi dene
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(DEFAULT_IMAGE_PATH);
                if (stream != null)
                {
                    return CreateBitmapFromStream(stream);
                }

                // Embedded bulunamazsa programmatically oluştur
                return CreateProgrammaticDefaultImage();
            }
            catch
            {
                return CreateProgrammaticDefaultImage();
            }
        }

        /// <summary>
        /// 🎨 IMPROVED - Programmatic olarak güzel default image oluşturur
        /// </summary>
        private static BitmapImage CreateProgrammaticDefaultImage()
        {
            try
            {
                // 460x215 boyutunda DrawingVisual oluştur
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

                    // "GAME" yazısı
                    var gameText = new FormattedText(
                        "GAME",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Trebuchet MS"),
                        24,
                        System.Windows.Media.Brushes.White,
                        96);

                    var gameTextX = (460 - gameText.Width) / 2;
                    var gameTextY = 120;
                    context.DrawText(gameText, new System.Windows.Point(gameTextX, gameTextY));

                    // "Image Not Found" yazısı
                    var subText = new FormattedText(
                        "Image Not Found",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Trebuchet MS"),
                        12,
                        System.Windows.Media.Brushes.LightGray,
                        96);

                    var subTextX = (460 - subText.Width) / 2;
                    var subTextY = 160;
                    context.DrawText(subText, new System.Windows.Point(subTextX, subTextY));
                }

                // Visual'ı bitmap'e çevir
                var renderTarget = new RenderTargetBitmap(460, 215, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(visual);

                // BitmapImage'a çevir
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
                System.Diagnostics.Debug.WriteLine($"❌ CreateProgrammaticDefaultImage hatası: {ex.Message}");
                // En son çare: Solid color bitmap
                return CreateSolidColorBitmap();
            }
        }

        /// <summary>
        /// Solid color bitmap oluşturur (en son çare)
        /// </summary>
        private static BitmapImage CreateSolidColorBitmap()
        {
            try
            {
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    context.DrawRectangle(System.Windows.Media.Brushes.DarkGray, null, new System.Windows.Rect(0, 0, 460, 215));
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
            catch
            {
                // Son çare olarak null döndür, GetDefaultImage tekrar deneyecek
                return null;
            }
        }

        /// <summary>
        /// 🔧 YENİ - Belirli bir oyun için tüm olası isimleri dener
        /// </summary>
        public static BitmapImage TryLoadGameImageWithVariations(string baseGameName)
        {
            var variations = new List<string>
            {
                baseGameName,
                baseGameName.ToLower(),
                baseGameName.Replace(" ", "_"),
                baseGameName.Replace(" ", "_").ToLower(),
                baseGameName.Replace(" ", ""),
                baseGameName.Replace(" ", "").ToLower(),
                baseGameName.Replace(":", ""),
                baseGameName.Replace(":", "").Replace(" ", "_").ToLower()
            };

            foreach (var variation in variations)
            {
                var image = GetGameImage(variation);
                if (image != null && image != GetDefaultImage())
                {
                    return image;
                }
            }

            return GetDefaultImage();
        }

        /// <summary>
        /// Cache'i temizler (memory management için)
        /// </summary>
        public static void ClearCache()
        {
            _imageCache.Clear();
            _defaultGameImage = null;
            System.Diagnostics.Debug.WriteLine("🧹 Image cache temizlendi");
        }

        /// <summary>
        /// Belirli bir image'ı cache'den kaldırır
        /// </summary>
        public static void RemoveFromCache(string imageName)
        {
            _imageCache.Remove(imageName);
            System.Diagnostics.Debug.WriteLine($"🗑️ Cache'den kaldırıldı: {imageName}");
        }

        /// <summary>
        /// Cache istatistiklerini döndürür
        /// </summary>
        public static (int cachedCount, int totalSize) GetCacheStats()
        {
            return (_imageCache.Count, _imageCache.Count * 1024); // Rough estimate
        }

        /// <summary>
        /// 🔍 DEBUG - Embedded resource'ların listesini döndürür
        /// </summary>
        public static string[] GetEmbeddedResourceNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames();
        }

        /// <summary>
        /// 🔍 DEBUG - PNG resource'ları filtreler ve döndürür
        /// </summary>
        public static string[] GetPngResourceNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allResources = assembly.GetManifestResourceNames();
            return Array.FindAll(allResources, r => r.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        }
    }
}