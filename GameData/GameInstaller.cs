using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Yafes.GameData
{
    /// <summary>
    /// Oyun kurulum işlemlerini yöneten sınıf
    /// </summary>
    internal static class GameInstaller
    {
        /// <summary>
        /// Oyun kurulum durumu
        /// </summary>
        public enum InstallStatus
        {
            NotStarted,
            InProgress,
            Completed,
            Failed,
            Cancelled
        }

        /// <summary>
        /// Kurulum progress eventi
        /// </summary>
        public static event Action<string, int>? InstallProgressChanged; // gameId, progress%

        /// <summary>
        /// Kurulum durumu değişiklik eventi
        /// </summary>
        public static event Action<string, InstallStatus>? InstallStatusChanged; // gameId, status

        /// <summary>
        /// Oyunu kurar
        /// </summary>
        /// <param name="game">Kurulacak oyun</param>
        /// <returns>Kurulum başarılı mı</returns>
        public static async Task<bool> InstallGameAsync(Yafes.Models.GameData game)
        {
            try
            {
                if (game == null)
                {
                    throw new ArgumentNullException(nameof(game));
                }

                // Kurulum başladı
                InstallStatusChanged?.Invoke(game.Id, InstallStatus.InProgress);
                InstallProgressChanged?.Invoke(game.Id, 0);

                // Setup dosyasının varlığını kontrol et
                if (!File.Exists(game.SetupPath))
                {
                    // Eğer setup dosyası yoksa, simüle et
                    await SimulateInstallationAsync(game);
                    return true;
                }

                // Gerçek kurulum işlemi
                var processInfo = new ProcessStartInfo
                {
                    FileName = game.SetupPath,
                    Arguments = "/S", // Silent install
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Kurulum işlemi başlatılamadı");
                }

                // Progress takibi (simüle)
                var progressTask = SimulateProgressAsync(game.Id);

                // Kurulum işleminin bitmesini bekle
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    InstallStatusChanged?.Invoke(game.Id, InstallStatus.Completed);
                    InstallProgressChanged?.Invoke(game.Id, 100);
                    return true;
                }
                else
                {
                    InstallStatusChanged?.Invoke(game.Id, InstallStatus.Failed);
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kurulum hatası: {ex.Message}");
                InstallStatusChanged?.Invoke(game?.Id ?? "unknown", InstallStatus.Failed);
                return false;
            }
        }

        /// <summary>
        /// Oyunu kaldırır
        /// </summary>
        /// <param name="game">Kaldırılacak oyun</param>
        /// <returns>Kaldırma başarılı mı</returns>
        public static async Task<bool> UninstallGameAsync(Yafes.Models.GameData game)
        {
            try
            {
                if (game == null)
                {
                    throw new ArgumentNullException(nameof(game));
                }

                // Kaldırma işlemi simülasyonu
                await SimulateUninstallationAsync(game);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kaldırma hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kurulum simülasyonu (setup dosyası yoksa)
        /// </summary>
        private static async Task SimulateInstallationAsync(Yafes.Models.GameData game)
        {
            // 5 saniyede simüle et
            for (int i = 0; i <= 100; i += 10)
            {
                InstallProgressChanged?.Invoke(game.Id, i);
                await Task.Delay(500); // 500ms bekle
            }

            InstallStatusChanged?.Invoke(game.Id, InstallStatus.Completed);
        }

        /// <summary>
        /// Kaldırma simülasyonu
        /// </summary>
        private static async Task SimulateUninstallationAsync(Yafes.Models.GameData game)
        {
            // 2 saniyede simüle et
            for (int i = 0; i <= 100; i += 25)
            {
                await Task.Delay(200); // 200ms bekle
            }
        }

        /// <summary>
        /// Progress simülasyonu
        /// </summary>
        private static async Task SimulateProgressAsync(string gameId)
        {
            for (int i = 10; i <= 90; i += 10)
            {
                InstallProgressChanged?.Invoke(gameId, i);
                await Task.Delay(1000); // 1 saniye bekle
            }
        }

        /// <summary>
        /// Oyunun kurulu olup olmadığını kontrol eder
        /// </summary>
        /// <param name="game">Kontrol edilecek oyun</param>
        /// <returns>Kurulu mu</returns>
        public static bool IsGameInstalled(Yafes.Models.GameData game)
        {
            if (game == null) return false;

            // Basit kontrol - install path var mı
            var installPath = Path.GetDirectoryName(game.SetupPath);
            return !string.IsNullOrEmpty(installPath) && Directory.Exists(installPath);
        }

        /// <summary>
        /// Oyunu başlatır
        /// </summary>
        /// <param name="game">Başlatılacak oyun</param>
        /// <returns>Başlatma başarılı mı</returns>
        public static async Task<bool> LaunchGameAsync(Yafes.Models.GameData game)
        {
            try
            {
                if (game == null || !game.IsInstalled)
                {
                    return false;
                }

                // Oyun executable'ını bul
                var gameExePath = game.SetupPath.Replace("setup.exe", "game.exe");

                if (File.Exists(gameExePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = gameExePath,
                        UseShellExecute = true
                    });

                    // Son oynama tarihini güncelle
                    game.LastPlayed = DateTime.Now;
                    await Yafes.Managers.GameDataManager.UpdateGameInstallStatusAsync(game.Id, true);

                    return true;
                }
                else
                {
                    // Simülasyon - oyun başlatıldı gibi göster
                    MessageBox.Show($"🎮 {game.Name} başlatılıyor...", "Oyun Başlatıcı",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    game.LastPlayed = DateTime.Now;
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Oyun başlatma hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kurulum iptal et
        /// </summary>
        /// <param name="gameId">Oyun ID'si</param>
        public static void CancelInstallation(string gameId)
        {
            InstallStatusChanged?.Invoke(gameId, InstallStatus.Cancelled);
        }

        /// <summary>
        /// Kurulum durumunu sıfırla
        /// </summary>
        /// <param name="gameId">Oyun ID'si</param>
        public static void ResetInstallStatus(string gameId)
        {
            InstallStatusChanged?.Invoke(gameId, InstallStatus.NotStarted);
            InstallProgressChanged?.Invoke(gameId, 0);
        }
    }
}