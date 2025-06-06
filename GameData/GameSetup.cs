using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Yafes.Managers;
using System.Linq;
using System.Threading;

namespace Yafes.GameData
{
    /// <summary>
    /// Silent Game Installation Manager
    /// Specific Path: "Game" Drive → "GameSetups" Folder → Game Folder → setup.exe
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
            SearchingGameDrive,
            SearchingGameSetups,
            SearchingGameFolder,
            FoundSetupExe,
            Installing,
            Completed,
            Failed,
            Cancelled
        }

        /// <summary>
        /// 🎯 MAIN METHOD: Card click'ten çağrılacak - Silent installation with NSIS parameters
        /// </summary>
        /// <param name="gameData">Kurulacak oyun verisi</param>
        /// <param name="queueManager">Queue manager referansı</param>
        /// <returns>Installation başarılı mı</returns>
        public static async Task<bool> StartSilentInstallation(Yafes.Models.GameData gameData, GameInstallationQueueManager queueManager = null)
        {
            try
            {
                if (gameData == null)
                {
                    MessageBox.Show("GameData null!", "HATA", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // 🎯 GERÇEK PATH GIRME EKRANI - DEFAULT DOĞRU PATH
                string realSetupPath = Microsoft.VisualBasic.Interaction.InputBox(
                    $"🎮 {gameData.Name}\n\n" +
                    "Lütfen gerçek setup.exe path'ini girin:\n\n" +
                    "Örnek: D:\\GameSetups\\[Game Folder]\\setup.exe",
                    "Setup.exe Path",
                    $@"D:\GameSetups\{gameData.Name}\setup.exe"
                );

                if (string.IsNullOrEmpty(realSetupPath))
                {
                    MessageBox.Show("Path girilmedi, işlem iptal.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                // 🔍 FILE EXISTENCE CHECK
                if (!File.Exists(realSetupPath))
                {
                    MessageBox.Show($"❌ Setup.exe bulunamadı!\n\n" +
                                   $"Girilen path: {realSetupPath}\n\n" +
                                   $"Lütfen doğru path'i kontrol edin.",
                                   "File Not Found",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                    return false;
                }

                // 🔧 SILENT INSTALLATION PARAMETERS - C:\Game\ target directory
                var silentParameters = new[]
                {
                    "/S /D=C:\\Game\\",               // Primary: NSIS with C:\Game\
                    "/VERYSILENT /DIR=C:\\Game\\",    // Alternative: Inno Setup with C:\Game\
                    "/S",                             // Only silent (no directory)
                    "/VERYSILENT",                    // Only silent (Inno)
                    "/quiet INSTALLDIR=\"C:\\Game\\\"", // MSI style with C:\Game\
                    "/SILENT /INSTALLDIR=C:\\Game\\"  // InstallShield style with C:\Game\
                };

                // 📋 INSTALLATION INFO MESSAGE
                var messageText = $"🎮 {gameData.Name}\n\n" +
                                 $"✅ Setup.exe bulundu!\n" +
                                 $"📁 Konum: {realSetupPath}\n\n" +
                                 $"⚙️ Silent Parameters ({silentParameters.Length} adet):\n";

                for (int i = 0; i < silentParameters.Length; i++)
                {
                    messageText += $"   {i + 1}. {silentParameters[i]}\n";
                }

                messageText += $"\n📂 Hedef Konum: C:\\Game\\{gameData.Name}\n\n" +
                              $"Silent kurulum başlatılsın mı?";

                var result = MessageBox.Show(messageText, "Silent Installation Ready", MessageBoxButton.YesNo, MessageBoxImage.Question);

                // 🚀 USER ACCEPTS INSTALLATION
                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show($"🚀 Silent kurulum başlatılıyor...\n\n" +
                                   $"Setup: {realSetupPath}\n" +
                                   $"Parametreler: {silentParameters.Length} adet\n\n" +
                                   $"Her parametre 30 saniye timeout ile denenecek.",
                                   "Installation Starting",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    // 🎯 TRY INSTALLATION WITH ALL PARAMETERS
                    bool installationSuccess = await TryInstallationWithAllParameters(realSetupPath, silentParameters, gameData.Name);

                    if (installationSuccess)
                    {
                        MessageBox.Show($"✅ {gameData.Name} başarıyla kuruldu!\n\n" +
                                       $"📁 Kurulum Konumu: C:\\Games\\{gameData.Name}\n\n" +
                                       $"Oyun artık oynanabilir!",
                                       "Installation Completed",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"❌ {gameData.Name} kurulum başarısız!\n\n" +
                                       $"Tüm {silentParameters.Length} silent parametre denendi.\n\n" +
                                       $"Çözüm önerileri:\n" +
                                       $"• Manuel kurulum deneyin\n" +
                                       $"• Setup.exe'yi double-click ile çalıştırın\n" +
                                       $"• Admin yetkisi gerekebilir",
                                       "Installation Failed",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Error);
                    }

                    return installationSuccess;
                }
                else
                {
                    MessageBox.Show("Kurulum iptal edildi.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"GameSetup Hata: {ex.Message}", "HATA", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 🎭 Installation process with all parameters - tüm parametreleri sırayla dener
        /// </summary>
        private static async Task<bool> TryInstallationWithAllParameters(string setupPath, string[] parameters, string gameName)
        {
            try
            {
                // 📂 C:\Game\ directory check/create
                string gamesDirectory = @"C:\Game\";
                if (!Directory.Exists(gamesDirectory))
                {
                    MessageBox.Show($"📁 C:\\Game\\ klasörü bulunamadı.\n\n" +
                                   "Klasör oluşturuluyor...",
                                   "Directory Creation",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    try
                    {
                        Directory.CreateDirectory(gamesDirectory);
                        MessageBox.Show("✅ C:\\Game\\ klasörü oluşturuldu!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ C:\\Game\\ klasörü oluşturulamadı!\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }

                // 🔧 TRY EACH PARAMETER
                for (int i = 0; i < parameters.Length; i++)
                {
                    string currentParam = parameters[i];

                    MessageBox.Show($"🔧 Parameter {i + 1}/{parameters.Length} deneniyor:\n\n" +
                                   $"Command: setup.exe {currentParam}\n\n" +
                                   $"30 saniye timeout ile test edilecek...",
                                   $"Trying Parameter {i + 1}",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    // 🎯 GERÇEK PARAMETER TEST
                    bool parameterSuccess = await TestSingleParameter(setupPath, currentParam, gameName);

                    if (parameterSuccess)
                    {
                        MessageBox.Show($"✅ Parameter başarılı!\n\n" +
                                       $"Kullanılan: {currentParam}\n\n" +
                                       $"Kurulum tamamlandı!",
                                       "Parameter Success",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Information);
                        return true;
                    }
                    else
                    {
                        MessageBox.Show($"❌ Parameter başarısız: {currentParam}\n\n" +
                                       $"Sonraki parameter deneniyor...",
                                       "Parameter Failed",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Warning);
                    }
                }

                // All parameters failed
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"TryInstallationWithAllParameters Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 🎲 Single parameter test - GERÇEK PROCESS EXECUTION
        /// </summary>
        private static async Task<bool> TestSingleParameter(string setupPath, string parameter, string gameName)
        {
            try
            {
                // 🚀 GERÇEK PROCESS EXECUTION
                MessageBox.Show($"🚀 Gerçek kurulum başlatılıyor!\n\n" +
                               $"Command: {setupPath} {parameter}\n\n" +
                               $"Process başlatılıyor...",
                               "Real Installation",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                var processInfo = new ProcessStartInfo
                {
                    FileName = setupPath,
                    Arguments = parameter,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas" // Admin olarak çalıştır
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    MessageBox.Show("❌ Process başlatılamadı!", "Process Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                MessageBox.Show($"✅ Process başlatıldı!\n\n" +
                               $"Process ID: {process.Id}\n\n" +
                               $"Kurulum devam ediyor...\n" +
                               $"Max 30 saniye beklenecek.",
                               "Process Started",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                // 🕐 PROCESS BITMESINI BEKLE (MAX 30 SANIYE)
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

                try
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("⏰ Kurulum zaman aşımına uğradı! (30 saniye)\n\n" +
                                   "Process sonlandırılıyor...",
                                   "Timeout",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);

                    process.Kill();
                    return false;
                }

                // 📊 EXIT CODE KONTROLÜ
                int exitCode = process.ExitCode;

                MessageBox.Show($"🏁 Process tamamlandı!\n\n" +
                               $"Exit Code: {exitCode}\n\n" +
                               $"Exit Code 0 = Başarılı\n" +
                               $"Exit Code ≠ 0 = Hata",
                               "Process Completed",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                if (exitCode == 0)
                {
                    // 📂 KURULUM DOĞRULAMA
                    string gameInstallPath = $@"C:\Game\{gameName}";
                    bool installationVerified = Directory.Exists(gameInstallPath);

                    MessageBox.Show($"📂 Kurulum doğrulaması:\n\n" +
                                   $"Kontrol edilen klasör: {gameInstallPath}\n" +
                                   $"Klasör var mı: {(installationVerified ? "✅ EVET" : "❌ HAYIR")}\n\n" +
                                   $"{(installationVerified ? "Kurulum başarılı!" : "Kurulum klasörü bulunamadı!")}",
                                   "Installation Verification",
                                   MessageBoxButton.OK,
                                   installationVerified ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    return installationVerified;
                }
                else
                {
                    MessageBox.Show($"❌ Kurulum başarısız!\n\n" +
                                   $"Exit Code: {exitCode}\n" +
                                   $"Parameter: {parameter}\n\n" +
                                   $"Sonraki parameter deneniyor...",
                                   "Installation Failed",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Process Execution Error:\n\n" +
                               $"{ex.Message}\n\n" +
                               $"Parameter: {parameter}",
                               "Execution Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
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
    }
}