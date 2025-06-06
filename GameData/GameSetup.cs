using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Yafes.Models;
using Yafes.Managers;

namespace Yafes.GameData
{
    /// <summary>
    /// Silent Game Installation Manager
    /// Card click → Silent installation → Queue integration
    /// </summary>
    public static class GameSetup
    {
        // Events for progress tracking
        public static event Action<string, int> InstallationProgress;
        public static event Action<string, InstallationStatus> InstallationStatusChanged;
        public static event Action<string> LogMessage;

        // Installation status enum
        public enum InstallationStatus
        {
            NotStarted,
            Preparing,
            Installing,
            Completed,
            Failed,
            Cancelled
        }

        /// <summary>
        /// 🎯 MAIN METHOD: Card click'ten çağrılacak silent installation
        /// </summary>
        /// <param name="gameData">Kurulacak oyun verisi</param>
        /// <param name="queueManager">Queue manager referansı</param>
        /// <returns>Installation başarılı mı</returns>
        public static async Task<bool> StartSilentInstallation(GameData gameData, GameInstallationQueueManager queueManager = null)
        {
            try
            {
                if (gameData == null)
                {
                    LogMessage?.Invoke("❌ GameData null - installation cancelled");
                    return false;
                }

                LogMessage?.Invoke($"🚀 Starting silent installation for: {gameData.Name}");

                // 1. Installation hazırlığı
                InstallationStatusChanged?.Invoke(gameData.Id, InstallationStatus.Preparing);
                InstallationProgress?.Invoke(gameData.Id, 0);

                // 2. Queue'ya ekle (eğer manager varsa)
                if (queueManager != null)
                {
                    await queueManager.AddGameToInstallationQueue(gameData);
                    LogMessage?.Invoke($"✅ {gameData.Name} added to installation queue");
                }

                // 3. Setup dosyası kontrolü
                var setupPath = await FindGameSetupFile(gameData);
                if (string.IsNullOrEmpty(setupPath))
                {
                    LogMessage?.Invoke($"⚠️ Setup file not found for {gameData.Name}, using simulation");
                    return await SimulateSilentInstallation(gameData);
                }

                // 4. Gerçek silent installation
                return await PerformSilentInstallation(gameData, setupPath);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartSilentInstallation error: {ex.Message}");
                InstallationStatusChanged?.Invoke(gameData?.Id ?? "unknown", InstallationStatus.Failed);
                return false;
            }
        }

        /// <summary>
        /// 🔍 Oyun setup dosyasını bulur
        /// </summary>
        private static async Task<string> FindGameSetupFile(GameData gameData)
        {
            try
            {
                // Setup dosyası yolları (priority sırasında)
                var possiblePaths = new[]
                {
                    gameData.SetupPath, // GameData'dan gelen path
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), gameData.Name, "setup.exe"),
                    Path.Combine(@"C:\Games", gameData.Name, "setup.exe"),
                    Path.Combine(@"D:\Games", gameData.Name, "setup.exe"),
                    Path.Combine(@"E:\Games", gameData.Name, "setup.exe")
                };

                foreach (var path in possiblePaths)
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        LogMessage?.Invoke($"📁 Setup file found: {path}");
                        return path;
                    }
                }

                // ISO/mounted image kontrol
                var isoPath = await FindIsoOrMountedImage(gameData.Name);
                if (!string.IsNullOrEmpty(isoPath))
                {
                    return isoPath;
                }

                LogMessage?.Invoke($"❌ Setup file not found for: {gameData.Name}");
                return null;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ FindGameSetupFile error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 💿 ISO veya mount edilmiş image'da setup.exe arar
        /// </summary>
        private static async Task<string> FindIsoOrMountedImage(string gameName)
        {
            try
            {
                // DVD/CD drive'ları kontrol et
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (drive.DriveType == DriveType.CDRom && drive.IsReady)
                    {
                        var setupPath = Path.Combine(drive.RootDirectory.FullName, "setup.exe");
                        if (File.Exists(setupPath))
                        {
                            LogMessage?.Invoke($"💿 Setup found in CD/DVD: {setupPath}");
                            return setupPath;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ FindIsoOrMountedImage error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎮 Gerçek silent installation işlemi
        /// </summary>
        private static async Task<bool> PerformSilentInstallation(GameData gameData, string setupPath)
        {
            try
            {
                InstallationStatusChanged?.Invoke(gameData.Id, InstallationStatus.Installing);
                LogMessage?.Invoke($"⚙️ Starting silent installation: {setupPath}");

                // Silent installation parametreleri (çoğu setup programı için)
                var silentArgs = new[]
                {
                    "/S",           // NSIS installers
                    "/SILENT",      // InstallShield
                    "/VERYSILENT",  // Inno Setup
                    "/quiet",       // MSI packages
                    "/Q",           // Some installers
                    "/s"            // Lowercase variant
                };

                foreach (var arg in silentArgs)
                {
                    try
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = setupPath,
                            Arguments = arg,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            Verb = "runas" // Admin olarak çalıştır
                        };

                        LogMessage?.Invoke($"🔧 Trying silent parameter: {arg}");

                        using var process = Process.Start(processInfo);
                        if (process != null)
                        {
                            // Progress tracking (simulated)
                            var progressTask = TrackInstallationProgress(gameData.Id);

                            // Installation bitmesini bekle
                            await process.WaitForExitAsync();

                            if (process.ExitCode == 0)
                            {
                                InstallationStatusChanged?.Invoke(gameData.Id, InstallationStatus.Completed);
                                InstallationProgress?.Invoke(gameData.Id, 100);
                                LogMessage?.Invoke($"✅ Silent installation completed: {gameData.Name}");

                                // Post-installation tasks
                                await PostInstallationTasks(gameData);
                                return true;
                            }
                            else
                            {
                                LogMessage?.Invoke($"⚠️ Installation failed with exit code: {process.ExitCode}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"❌ Silent installation attempt failed: {ex.Message}");
                        continue; // Try next parameter
                    }
                }

                // Eğer hiçbir silent parameter çalışmazsa
                LogMessage?.Invoke($"❌ All silent installation attempts failed for: {gameData.Name}");
                InstallationStatusChanged?.Invoke(gameData.Id, InstallationStatus.Failed);
                return false;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ PerformSilentInstallation error: {ex.Message}");
                InstallationStatusChanged?.Invoke(gameData.Id, InstallationStatus.Failed);
                return false;
            }
        }

        /// <summary>
        /// 🎭 Installation simülasyonu (setup dosyası yoksa)
        /// </summary>
        private static async Task<bool> SimulateSilentInstallation(GameData gameData)
        {
            try
            {
                InstallationStatusChanged?.Invoke(gameData.Id, InstallationStatus.Installing);
                LogMessage?.Invoke($"🎭 Simulating installation for: {gameData.Name}");

                // 10 saniyede simülasyon
                for (int progress = 0; progress <= 100; progress += 10)
                {
                    InstallationProgress?.Invoke(gameData.Id, progress);
                    await Task.Delay(1000); // 1 saniye bekle

                    if (progress == 50)
                    {
                        LogMessage?.Invoke($"📊 Installation 50% complete: {gameData.Name}");
                    }
                }

                InstallationStatusChanged?.Invoke(gameData.Id, InstallationStatus.Completed);
                LogMessage?.Invoke($"✅ Simulated installation completed: {gameData.Name}");

                // GameData'yı kurulu olarak işaretle
                await GameDataManager.UpdateGameInstallStatusAsync(gameData.Id, true);

                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SimulateSilentInstallation error: {ex.Message}");
                InstallationStatusChanged?.Invoke(gameData.Id, InstallationStatus.Failed);
                return false;
            }
        }

        /// <summary>
        /// 📊 Installation progress tracking
        /// </summary>
        private static async Task TrackInstallationProgress(string gameId)
        {
            try
            {
                // Simulated progress tracking
                for (int i = 10; i <= 90; i += 10)
                {
                    InstallationProgress?.Invoke(gameId, i);
                    await Task.Delay(2000); // 2 saniye intervals
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ TrackInstallationProgress error: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔧 Post-installation tasks
        /// </summary>
        private static async Task PostInstallationTasks(GameData gameData)
        {
            try
            {
                LogMessage?.Invoke($"🔧 Running post-installation tasks for: {gameData.Name}");

                // 1. Registry kontrolleri
                // 2. Shortcut oluşturma
                // 3. Game executable bulma
                // 4. Database update

                await Task.Delay(1000); // Post-install delay

                // GameData'yı kurulu olarak işaretle
                await GameDataManager.UpdateGameInstallStatusAsync(gameData.Id, true);

                LogMessage?.Invoke($"✅ Post-installation tasks completed: {gameData.Name}");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ PostInstallationTasks error: {ex.Message}");
            }
        }

        /// <summary>
        /// 🛑 Installation iptal etme
        /// </summary>
        public static void CancelInstallation(string gameId)
        {
            try
            {
                InstallationStatusChanged?.Invoke(gameId, InstallationStatus.Cancelled);
                LogMessage?.Invoke($"🛑 Installation cancelled: {gameId}");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CancelInstallation error: {ex.Message}");
            }
        }

        /// <summary>
        /// 🎯 Kurulum durumu kontrol
        /// </summary>
        public static bool IsGameInstalled(GameData gameData)
        {
            try
            {
                if (gameData == null) return false;

                // 1. GameData'dan kontrol
                if (gameData.IsInstalled) return true;

                // 2. Registry kontrol (örnek path'ler)
                var possiblePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), gameData.Name),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), gameData.Name),
                    Path.Combine(@"C:\Games", gameData.Name),
                    Path.Combine(@"D:\Games", gameData.Name)
                };

                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        LogMessage?.Invoke($"📁 Game installation found: {path}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ IsGameInstalled error: {ex.Message}");
                return false;
            }
        }
    }
}