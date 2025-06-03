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

        // Resource path constants
        private const string RESOURCE_BASE_PATH = "Yafes.Resources.GamePosters.";
        private const string DEFAULT_IMAGE_PATH = "Yafes.Resources.default_game.png";

        /// <summary>
        /// WPF için BitmapImage döndürür (XAML Image kontrolları için)
        /// </summary>
        public static BitmapImage GetGameImage(string imageName)
        {
            try
            {
                // Cache'de var mı kontrol et
                if (_imageCache.TryGetValue(imageName, out var cachedImage))
                {
                    return cachedImage;
                }

                // Embedded resource'dan yüklemeyi dene
                var image = LoadImageFromEmbeddedResource(imageName);
                if (image != null)
                {
                    _imageCache[imageName] = image;
                    return image;
                }

                // Fallback: Default image
                return GetDefaultImage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Image yükleme hatası: {ex.Message}");
                return GetDefaultImage();
            }
        }

        /// <summary>
        /// Embedded resource'dan BitmapImage yükler
        /// </summary>
        private static BitmapImage LoadImageFromEmbeddedResource(string imageName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = RESOURCE_BASE_PATH + imageName;

                // Resource stream'i al
                var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    // .png uzantısı yoksa ekle
                    if (!imageName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = RESOURCE_BASE_PATH + imageName + ".png";
                        stream = assembly.GetManifestResourceStream(resourceName);
                    }

                    if (stream == null)
                        return null;
                }

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
                System.Diagnostics.Debug.WriteLine($"Embedded resource yükleme hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Default game image'ını döndürür
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
        /// Default image oluşturur
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
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    stream.Dispose();
                    return bitmap;
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
        /// Programmatic olarak default image oluşturur
        /// </summary>
        private static BitmapImage CreateProgrammaticDefaultImage()
        {
            try
            {
                // 460x215 boyutunda DrawingVisual oluştur
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    // Gradient arkaplan
                    var gradientBrush = new LinearGradientBrush(
                        System.Windows.Media.Color.FromRgb(45, 45, 48),
                        System.Windows.Media.Color.FromRgb(25, 25, 28),
                        new System.Windows.Point(0, 0),
                        new System.Windows.Point(1, 1));

                    context.DrawRectangle(gradientBrush, null, new System.Windows.Rect(0, 0, 460, 215));

                    // Kenarlık
                    var borderPen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Orange, 2);
                    context.DrawRectangle(null, borderPen, new System.Windows.Rect(1, 1, 458, 213));

                    // "GAME" yazısı
                    var formattedText = new FormattedText(
                        "GAME",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Segoe UI"),
                        24,
                        System.Windows.Media.Brushes.White,
                        96);

                    var textX = (460 - formattedText.Width) / 2;
                    var textY = 90;
                    context.DrawText(formattedText, new System.Windows.Point(textX, textY));

                    // "No Image Available" yazısı
                    var subText = new FormattedText(
                        "No Image Available",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Segoe UI"),
                        12,
                        System.Windows.Media.Brushes.LightGray,
                        96);

                    var subTextX = (460 - subText.Width) / 2;
                    var subTextY = 130;
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
            catch
            {
                // En son çare: Solid color bitmap
                return CreateSolidColorBitmap();
            }
        }

        /// <summary>
        /// Solid color bitmap oluşturur (en son çare)
        /// </summary>
        private static BitmapImage CreateSolidColorBitmap()
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

        /// <summary>
        /// Cache'i temizler (memory management için)
        /// </summary>
        public static void ClearCache()
        {
            _imageCache.Clear();
            _defaultGameImage = null;
        }

        /// <summary>
        /// Belirli bir image'ı cache'den kaldırır
        /// </summary>
        public static void RemoveFromCache(string imageName)
        {
            _imageCache.Remove(imageName);
        }

        /// <summary>
        /// Cache istatistiklerini döndürür
        /// </summary>
        public static int GetCacheCount()
        {
            return _imageCache.Count;
        }

        /// <summary>
        /// Embedded resource'ların listesini debug için döndürür
        /// </summary>
        public static string[] GetEmbeddedResourceNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames();
        }
    }
}