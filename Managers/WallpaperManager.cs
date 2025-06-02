using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Yafes
{
    /// <summary>
    /// Windows arkaplanını değiştirmek için yardımcı sınıf
    /// </summary>
    public static class WallpaperManager
    {
        // Windows API fonksiyonu - SystemParametersInfo
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(
            UInt32 action,
            UInt32 uParam,
            string vParam,
            UInt32 winIni);

        // Konstantlar
        private const UInt32 SPI_SETDESKWALLPAPER = 0x14;
        private const UInt32 SPIF_UPDATEINIFILE = 0x01;
        private const UInt32 SPIF_SENDWININICHANGE = 0x02;

        /// <summary>
        /// Arkaplan stilini ayarlar
        /// </summary>
        public enum WallpaperStyle
        {
            Tiled = 0,      // Döşeli
            Centered = 1,   // Ortalanmış  
            Stretched = 2,  // Gerilmiş
            Fill = 10,      // Doldur
            Fit = 6,        // Sığdır
            Span = 22       // Yay
        }

        /// <summary>
        /// Windows arkaplanını değiştirir
        /// </summary>
        /// <param name="imagePath">Resim dosyası yolu</param>
        /// <param name="style">Arkaplan stili</param>
        /// <returns>Başarılı ise true</returns>
        public static bool SetWallpaper(string imagePath, WallpaperStyle style = WallpaperStyle.Fill)
        {
            try
            {
                // Dosya var mı kontrol et
                if (!File.Exists(imagePath))
                {
                    throw new FileNotFoundException($"Arkaplan dosyası bulunamadı: {imagePath}");
                }

                // Registry'de arkaplan stilini ayarla
                SetWallpaperStyle(style);

                // Windows API ile arkaplanı değiştir
                int result = SystemParametersInfo(
                    SPI_SETDESKWALLPAPER,
                    0,
                    imagePath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE
                );

                return result != 0; // Başarılı ise 0 dışında değer döner
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Arkaplan değiştirme hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Registry'de arkaplan stilini ayarlar
        /// </summary>
        /// <param name="style">Arkaplan stili</param>
        private static void SetWallpaperStyle(WallpaperStyle style)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (key != null)
                    {
                        // WallpaperStyle ayarla
                        key.SetValue("WallpaperStyle", ((int)style).ToString());

                        // TileWallpaper ayarla (sadece Tiled için 1, diğerleri için 0)
                        string tileValue = (style == WallpaperStyle.Tiled) ? "1" : "0";
                        key.SetValue("TileWallpaper", tileValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Registry ayarlama hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Mevcut arkaplanı alır
        /// </summary>
        /// <returns>Mevcut arkaplan dosya yolu</returns>
        public static string GetCurrentWallpaper()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
                {
                    return key?.GetValue("Wallpaper")?.ToString() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// YAFES programı için arkaplanı ayarlar
        /// </summary>
        /// <param name="logCallback">Log mesajları için callback</param>
        /// <returns>Başarılı ise true</returns>
        public static bool SetYafesWallpaper(Action<string> logCallback = null)
        {
            try
            {
                // YAFES arkaplan dosyası yolu
                string wallpaperPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources", "GifIcons", "walpaper.jpg"
                );

                logCallback?.Invoke($"Arkaplan dosyası aranıyor: {wallpaperPath}");

                // Dosya var mı kontrol et
                if (!File.Exists(wallpaperPath))
                {
                    logCallback?.Invoke("❌ Arkaplan dosyası bulunamadı!");
                    return false;
                }

                logCallback?.Invoke("📄 Arkaplan dosyası bulundu, ayarlanıyor...");

                // Mevcut arkaplanı yedek al
                string currentWallpaper = GetCurrentWallpaper();
                if (!string.IsNullOrEmpty(currentWallpaper))
                {
                    logCallback?.Invoke($"💾 Mevcut arkaplan: {Path.GetFileName(currentWallpaper)}");
                }

                // YAFES arkaplanını ayarla (Fill stili ile)
                bool success = SetWallpaper(wallpaperPath, WallpaperStyle.Fill);

                if (success)
                {
                    logCallback?.Invoke("✅ YAFES arkaplanı başarıyla ayarlandı!");
                }
                else
                {
                    logCallback?.Invoke("❌ Arkaplan ayarlanamadı!");
                }

                return success;
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"❌ Hata: {ex.Message}");
                return false;
            }
        }
    }
}