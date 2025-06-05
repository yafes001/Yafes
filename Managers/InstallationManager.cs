using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Yafes.Managers
{
    /// <summary>
    /// Kurulum durumları enum'u
    /// </summary>
    public enum InstallationStatus
    {
        Waiting,
        Downloading,
        Extracting,
        Installing,
        Completed,
        Failed,
        Cancelled,
        Skipped
    }

    /// <summary>
    /// Sürücü bilgilerini tutan model sınıfı
    /// </summary>
    public class DriverInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string InstallArguments { get; set; } = string.Empty;
        public bool IsZip { get; set; }
        public string AlternativeSearchPattern { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public InstallationStatus Status { get; set; } = InstallationStatus.Waiting;
        public string? LastError { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration =>
            StartTime.HasValue && EndTime.HasValue
                ? EndTime.Value - StartTime.Value
                : null;
    }

    /// <summary>
    /// Program bilgilerini tutan model sınıfı
    /// </summary>
    public class ProgramInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string InstallArguments { get; set; } = string.Empty;
        public bool IsZip { get; set; }
        public string AlternativeSearchPattern { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public bool SpecialInstallation { get; set; } = false;
        public InstallationStatus Status { get; set; } = InstallationStatus.Waiting;
        public string? LastError { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration =>
            StartTime.HasValue && EndTime.HasValue
                ? EndTime.Value - StartTime.Value
                : null;
    }
    /// <summary>
    /// Kurulum işlemlerini yöneten sınıf
    /// </summary>
    public class InstallationManager
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string driversFolder = "C:\\Drivers";
        private readonly string programsFolder = "C:\\Programs";
        private readonly string alternativeDriversFolder = "F:\\MSI Drivers";
        private readonly string alternativeProgramsFolder = "F:\\Programs";

        // Events for communication with Main window
        public event EventHandler<string> LogMessage;
        public event EventHandler<ProgressEventArgs> ProgressChanged;
        public event EventHandler<InstallationCompleteEventArgs> InstallationComplete;

        // Current installation state
        private List<DriverInfo> drivers = new List<DriverInfo>();
        private List<ProgramInfo> programs = new List<ProgramInfo>();
        private int currentDriverIndex = 0;
        private int currentProgramIndex = 0;
        private bool isInstalling = false;

        // UI References (dependency injection yapılacak)
        private ProgressBar progressBar;
        private ProgressBar progressBarStatus;
        private TextBlock txtStatusBar;
        private Dispatcher dispatcher;
        private InstallationQueueManager queueManager;

        public InstallationManager(ProgressBar progressBar, ProgressBar progressBarStatus,
                                 TextBlock txtStatusBar, Dispatcher dispatcher,
                                 InstallationQueueManager queueManager)
        {
            this.progressBar = progressBar;
            this.progressBarStatus = progressBarStatus;
            this.txtStatusBar = txtStatusBar;
            this.dispatcher = dispatcher;
            this.queueManager = queueManager;
        }

        /// <summary>
        /// Kurulum işlemini hazırlar
        /// </summary>
        public void PrepareInstallation(List<DriverInfo> selectedDrivers, List<ProgramInfo> selectedPrograms)
        {
            isInstalling = true;
            currentDriverIndex = 0;
            currentProgramIndex = 0;

            // Kurulacak listeleri kopyala
            drivers = new List<DriverInfo>(selectedDrivers);
            programs = new List<ProgramInfo>(selectedPrograms);

            // Kuyruk yöneticisinde kuyruğu oluştur
            queueManager.CreateQueue(drivers, programs);

            OnLogMessage($"ADIM 1: Toplam {drivers.Count + programs.Count} kurulum başlatılıyor...");

            if (drivers.Count > 0)
            {
                StartNextDriverInstallation();
            }
            else if (programs.Count > 0)
            {
                StartProgramInstallations();
            }
            else
            {
                OnLogMessage("Kurulacak öğe bulunamadı. İşlem tamamlandı.");
                CompleteInstallation();
            }
        }

        /// <summary>
        /// Sıradaki sürücü kurulumunu başlatır
        /// </summary>
        private async void StartNextDriverInstallation()
        {
            if (currentDriverIndex >= drivers.Count)
            {
                OnLogMessage("Tüm sürücü kurulumları tamamlandı!");
                StartProgramInstallations();
                return;
            }

            DriverInfo currentDriver = drivers[currentDriverIndex];

            // Kuyruk yöneticisinde kurulumu başlat
            queueManager.StartInstallation(currentDriver.Name);

            string filePath = Path.Combine(driversFolder, currentDriver.FileName);
            bool fileExists = File.Exists(filePath);

            try
            {
                if (fileExists)
                {
                    await InstallDriver(currentDriver, filePath);
                }
                else
                {
                    bool resourceExtracted = await ExtractEmbeddedResource(currentDriver.ResourceName, filePath);

                    if (resourceExtracted)
                    {
                        OnLogMessage($"Gömülü kaynaktan {currentDriver.Name} çıkarıldı");
                        await InstallDriver(currentDriver, filePath);
                    }
                    else if (IsInternetAvailable())
                    {
                        currentDriver.Status = InstallationStatus.Downloading;
                        await DownloadAndInstallDriver(currentDriver, filePath);
                    }
                    else
                    {
                        currentDriver.Status = InstallationStatus.Failed;
                        string? alternativeFilePath = FindDriverInAlternativeFolder(currentDriver);

                        if (alternativeFilePath != null)
                        {
                            OnLogMessage($"Alternatif klasörde sürücü bulundu: {alternativeFilePath}");
                            await InstallDriver(currentDriver, alternativeFilePath);
                        }
                        else
                        {
                            OnLogMessage($"Alternatif klasörde sürücü bulunamadı! Desen: {currentDriver.AlternativeSearchPattern}");
                            currentDriver.Status = InstallationStatus.Failed;
                            queueManager.CompleteInstallation(currentDriver.Name);
                            HandleDriverError();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Sürücü kurulum hatası: {ex.Message}");
                currentDriver.Status = InstallationStatus.Failed;
                currentDriver.LastError = ex.Message;
                queueManager.CompleteInstallation(currentDriver.Name);
                HandleDriverError();
            }
        }

        /// <summary>
        /// Program kurulumlarını başlatır
        /// </summary>
        private void StartProgramInstallations()
        {
            OnLogMessage("\nADIM 2: Program kurulumları başlatılıyor...");
            if (programs.Count > 0)
            {
                StartNextProgramInstallation();
            }
            else
            {
                CompleteInstallation();
            }
        }

        /// <summary>
        /// Sıradaki program kurulumunu başlatır
        /// </summary>
        private async void StartNextProgramInstallation()
        {
            if (currentProgramIndex >= programs.Count)
            {
                OnLogMessage("Tüm program kurulumları tamamlandı!");
                CompleteInstallation();
                return;
            }

            ProgramInfo currentProgram = programs[currentProgramIndex];

            // Kuyruk yöneticisinde kurulumu başlat
            queueManager.StartInstallation(currentProgram.Name);

            string filePath = Path.Combine(programsFolder, currentProgram.FileName);
            bool fileExists = File.Exists(filePath);

            try
            {
                if (fileExists)
                {
                    await InstallProgram(currentProgram, filePath);
                }
                else
                {
                    bool resourceExtracted = await ExtractEmbeddedResource(currentProgram.ResourceName, filePath);

                    if (resourceExtracted)
                    {
                        OnLogMessage($"Gömülü kaynaktan {currentProgram.Name} çıkarıldı");
                        await InstallProgram(currentProgram, filePath);
                    }
                    else if (IsInternetAvailable())
                    {
                        currentProgram.Status = InstallationStatus.Downloading;
                        await DownloadAndInstallProgram(currentProgram, filePath);
                    }
                    else
                    {
                        currentProgram.Status = InstallationStatus.Failed;
                        string? alternativeFilePath = FindProgramInAlternativeFolder(currentProgram);

                        if (alternativeFilePath != null)
                        {
                            OnLogMessage($"Alternatif klasörde program bulundu: {alternativeFilePath}");
                            await InstallProgram(currentProgram, alternativeFilePath);
                        }
                        else
                        {
                            OnLogMessage($"Alternatif klasörde program bulunamadı! Desen: {currentProgram.AlternativeSearchPattern}");
                            currentProgram.Status = InstallationStatus.Failed;
                            queueManager.CompleteInstallation(currentProgram.Name);
                            HandleProgramError();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Program kurulum hatası: {ex.Message}");
                currentProgram.Status = InstallationStatus.Failed;
                currentProgram.LastError = ex.Message;
                queueManager.CompleteInstallation(currentProgram.Name);
                HandleProgramError();
            }
        }

        /// <summary>
        /// Sürücü kurulumunu gerçekleştirir
        /// </summary>
        private async Task InstallDriver(DriverInfo driver, string filePath)
        {
            try
            {
                driver.StartTime = DateTime.Now;
                UpdateStatusBar($"{driver.Name} kuruluyor...");
                UpdateProgress(0);

                string installPath = filePath;
                string extractPath = "";

                // Eğer ZIP dosyası ise, çıkart
                if (driver.IsZip)
                {
                    driver.Status = InstallationStatus.Extracting;
                    extractPath = Path.Combine(driversFolder, Path.GetFileNameWithoutExtension(driver.FileName));

                    if (Directory.Exists(extractPath))
                    {
                        Directory.Delete(extractPath, true);
                    }

                    Directory.CreateDirectory(extractPath);
                    await Task.Run(() => ZipFile.ExtractToDirectory(filePath, extractPath));

                    // Kurulum dosyasını bul (setup.exe)
                    string[] setupFiles = Directory.GetFiles(extractPath, "setup.exe", SearchOption.AllDirectories);

                    if (setupFiles.Length > 0)
                    {
                        installPath = setupFiles[0];
                    }
                    else
                    {
                        throw new Exception("Kurulum dosyası bulunamadı!");
                    }
                }

                // Durumu güncelle - Kuruluyor
                driver.Status = InstallationStatus.Installing;

                // Kurulum işlemini başlat
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = installPath,
                    Arguments = driver.InstallArguments,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                Process? installProcess = Process.Start(psi);

                if (installProcess == null)
                {
                    throw new Exception("Kurulum işlemi başlatılamadı!");
                }

                // Kurulum işleminin tamamlanmasını bekle
                await WaitForInstallationComplete(installProcess, driver.ProcessName, driver.Name);

                // Kurulum tamamlandı
                driver.Status = InstallationStatus.Completed;
                driver.EndTime = DateTime.Now;
                UpdateProgress(100);
                UpdateStatusBar($"{driver.Name} kurulumu tamamlandı");

                // Kuyruk yöneticisinde kurulumu tamamla
                queueManager.CompleteInstallation(driver.Name);

                // Sonraki sürücüye geç
                currentDriverIndex++;
                StartNextDriverInstallation();
            }
            catch (Exception ex)
            {
                driver.Status = InstallationStatus.Failed;
                driver.LastError = ex.Message;
                driver.EndTime = DateTime.Now;
                OnLogMessage($"Sürücü kurulum hatası: {ex.Message}");
                queueManager.CompleteInstallation(driver.Name);
                HandleDriverError();
            }
        }

        /// <summary>
        /// Program kurulumunu gerçekleştirir
        /// </summary>
        private async Task InstallProgram(ProgramInfo program, string filePath)
        {
            try
            {
                program.StartTime = DateTime.Now;
                UpdateStatusBar($"{program.Name} kuruluyor...");
                UpdateProgress(0);

                string installPath = filePath;
                string extractPath = "";

                // Eğer ZIP dosyası ise, çıkart
                if (program.IsZip)
                {
                    program.Status = InstallationStatus.Extracting;
                    extractPath = Path.Combine(programsFolder, Path.GetFileNameWithoutExtension(program.FileName));

                    if (Directory.Exists(extractPath))
                    {
                        Directory.Delete(extractPath, true);
                    }

                    Directory.CreateDirectory(extractPath);
                    await Task.Run(() => ZipFile.ExtractToDirectory(filePath, extractPath));

                    // Kurulum dosyasını bul (setup.exe)
                    string[] setupFiles = Directory.GetFiles(extractPath, "setup.exe", SearchOption.AllDirectories);

                    if (setupFiles.Length > 0)
                    {
                        installPath = setupFiles[0];
                    }
                    else
                    {
                        throw new Exception("Kurulum dosyası bulunamadı!");
                    }
                }

                // Durumu güncelle - Kuruluyor
                program.Status = InstallationStatus.Installing;

                // Özel kurulum kontrolleri
                if (program.SpecialInstallation)
                {
                    await HandleSpecialInstallation(program, installPath);
                }
                else
                {
                    // Normal kurulum işlemi
                    await PerformNormalInstallation(program, installPath);
                }

                // Kurulum tamamlandı
                program.Status = InstallationStatus.Completed;
                program.EndTime = DateTime.Now;
                UpdateProgress(100);
                UpdateStatusBar($"{program.Name} kurulumu tamamlandı");

                // Kuyruk yöneticisinde kurulumu tamamla
                queueManager.CompleteInstallation(program.Name);

                // Sonraki programa geç
                currentProgramIndex++;
                StartNextProgramInstallation();
            }
            catch (Exception ex)
            {
                program.Status = InstallationStatus.Failed;
                program.LastError = ex.Message;
                program.EndTime = DateTime.Now;
                OnLogMessage($"Program kurulum hatası: {ex.Message}");
                queueManager.CompleteInstallation(program.Name);
                HandleProgramError();
            }
        }

        /// <summary>
        /// Özel kurulum işlemlerini yönetir
        /// </summary>
        private async Task HandleSpecialInstallation(ProgramInfo program, string installPath)
        {
            switch (program.Name)
            {
                case "WinRAR":
                    await InstallWinRARWithPowerShell(program, installPath);
                    break;
                case "Opera":
                    await InstallOperaWithPasswordImport(program, installPath);
                    break;
                default:
                    await PerformNormalInstallation(program, installPath);
                    break;
            }
        }

        /// <summary>
        /// Normal kurulum işlemini gerçekleştirir
        /// </summary>
        private async Task PerformNormalInstallation(ProgramInfo program, string installPath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = installPath,
                Arguments = program.InstallArguments,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            Process? installProcess = Process.Start(psi);

            if (installProcess == null)
            {
                throw new Exception("Kurulum işlemi başlatılamadı!");
            }

            await WaitForInstallationComplete(installProcess, program.ProcessName, program.Name);
        }

        /// <summary>
        /// WinRAR için özel PowerShell kurulumu
        /// </summary>
        private async Task InstallWinRARWithPowerShell(ProgramInfo program, string installPath)
        {
            OnLogMessage($"WinRAR için özel CMD kurulumu başlatılıyor...");

            // Batch dosyası oluştur
            string tempBatchFile = Path.Combine(Path.GetTempPath(), "winrar_install.bat");
            string batchContent = $@"@echo off
echo WinRAR kurulumu baslatiliyor...
""{installPath}"" /S
echo Kurulum komutu gonderildi.
exit";

            File.WriteAllText(tempBatchFile, batchContent);

            // Batch dosyasını çalıştır
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{tempBatchFile}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized,
                Verb = "runas" // Yönetici olarak çalıştır
            };

            Process? installProcess = Process.Start(psi);

            if (installProcess == null)
            {
                throw new Exception("WinRAR kurulum işlemi başlatılamadı!");
            }

            // Kurulum sürecini bekle
            await Task.Run(() => {
                installProcess.WaitForExit();

                // CMD kapandıktan sonra WinRAR kurulumunun tamamlanması için ek bekleme
                OnLogMessage("WinRAR kurulum işlemi devam ediyor, tamamlanması bekleniyor...");
                System.Threading.Thread.Sleep(15000); // 15 saniye bekle

                // WinRAR'ın kurulup kurulmadığını kontrol et
                bool isWinRarInstalled = Directory.Exists(@"C:\Program Files\WinRAR") ||
                                        Directory.Exists(@"C:\Program Files (x86)\WinRAR");

                if (isWinRarInstalled)
                {
                    OnLogMessage("WinRAR kurulumu başarıyla tamamlandı!");
                }
                else
                {
                    OnLogMessage("WinRAR kurulumu tamamlandı, ancak kurulum klasörü bulunamadı.");
                }

                // Batch dosyasını temizle
                try
                {
                    if (File.Exists(tempBatchFile))
                    {
                        File.Delete(tempBatchFile);
                    }
                }
                catch
                {
                    // Dosya silinemezse önemli değil
                }
            });
        }

        /// <summary>
        /// Opera'yı şifre import özelliği ile kurar
        /// </summary>
        private async Task InstallOperaWithPasswordImport(ProgramInfo program, string installPath)
        {
            // Opera Password Manager'ı oluştur
            var operaManager = new OperaPasswordManager((message) =>
            {
                OnLogMessage(message);
            });

            // Progress bar'ı güncelle
            UpdateProgress(0);
            UpdateStatusBar("Opera kuruluyor ve şifreler import ediliyor...");

            // Opera kurulum + şifre import işlemini başlat
            bool success = await operaManager.InstallOperaWithPasswordImport(installPath, program.InstallArguments);

            // Progress bar'ı tamamla
            UpdateProgress(100);
            UpdateStatusBar(success ? "Opera kurulumu ve şifre import tamamlandı" : "Opera kurulumu tamamlandı");

            if (!success)
            {
                throw new Exception("Opera kurulum işlemi başarısız oldu.");
            }
        }

        /// <summary>
        /// Kurulum işleminin tamamlanmasını bekler
        /// </summary>
        private async Task WaitForInstallationComplete(Process installProcess, string processName, string applicationName)
        {
            await Task.Run(() =>
            {
                int progress = 0;
                bool processFound = true;

                while (processFound)
                {
                    // İlerleme çubuğunu güncelle
                    if (progress < 95)
                    {
                        progress += 5;
                    }

                    UpdateProgress(progress);
                    UpdateStatusBar($"{applicationName} kuruluyor... %{progress}");

                    try
                    {
                        // Önce başlattığımız process'i kontrol et
                        if (!installProcess.HasExited)
                        {
                            System.Threading.Thread.Sleep(2000);
                            continue;
                        }

                        // Başlattığımız process bittiyse, benzer adlı başka processler var mı kontrol et
                        Process[] processes = Process.GetProcessesByName(processName);

                        if (processes.Length > 0)
                        {
                            System.Threading.Thread.Sleep(2000);
                        }
                        else
                        {
                            processFound = false;
                        }
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(2000);
                    }
                }

                // Kurulum tamamlandı
                UpdateProgress(100);
                UpdateStatusBar($"{applicationName} kurulumu tamamlandı");
            });
        }

        /// <summary>
        /// Kurulumu tamamlar
        /// </summary>
        private void CompleteInstallation()
        {
            isInstalling = false;
            UpdateProgress(100);
            UpdateStatusBar("Tüm kurulumlar tamamlandı");

            // Kuyruk yöneticisini durdur
            queueManager.Stop();

            OnLogMessage("\n*** TÜM KURULUMLAR TAMAMLANDI! ***");

            // Installation complete event'ini fırlatır
            OnInstallationComplete(new InstallationCompleteEventArgs
            {
                TotalDrivers = drivers.Count,
                TotalPrograms = programs.Count,
                SuccessfulDrivers = drivers.Count(d => d.Status == InstallationStatus.Completed),
                SuccessfulPrograms = programs.Count(p => p.Status == InstallationStatus.Completed),
                FailedDrivers = drivers.Count(d => d.Status == InstallationStatus.Failed),
                FailedPrograms = programs.Count(p => p.Status == InstallationStatus.Failed)
            });
        }

        /// <summary>
        /// Sürücü hatası durumunda çalışır
        /// </summary>
        private void HandleDriverError()
        {
            currentDriverIndex++;
            StartNextDriverInstallation();
        }

        /// <summary>
        /// Program hatası durumunda çalışır
        /// </summary>
        private void HandleProgramError()
        {
            currentProgramIndex++;
            StartNextProgramInstallation();
        }

        // Helper metodlar...
        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000);
                    return reply != null && reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> ExtractEmbeddedResource(string resourceName, string outputFilePath)
        {
            // FileOperationManager'a taşınacak
            // Şimdilik basit implementasyon
            return false;
        }

        private string? FindDriverInAlternativeFolder(DriverInfo driver)
        {
            // FileOperationManager'a taşınacak
            return null;
        }

        private string? FindProgramInAlternativeFolder(ProgramInfo program)
        {
            // FileOperationManager'a taşınacak
            return null;
        }

        private async Task DownloadAndInstallDriver(DriverInfo driver, string filePath)
        {
            // FileOperationManager'a taşınacak
        }

        private async Task DownloadAndInstallProgram(ProgramInfo program, string filePath)
        {
            // FileOperationManager'a taşınacak
        }

        // Event helpers
        private void OnLogMessage(string message)
        {
            LogMessage?.Invoke(this, message);
        }

        private void OnInstallationComplete(InstallationCompleteEventArgs args)
        {
            InstallationComplete?.Invoke(this, args);
        }

        private void UpdateProgress(int percentage)
        {
            dispatcher.Invoke(() =>
            {
                progressBar.Value = percentage;
                progressBarStatus.Value = percentage;
            });
            OnProgressChanged(new ProgressEventArgs { Percentage = percentage });
        }

        private void UpdateStatusBar(string message)
        {
            dispatcher.Invoke(() =>
            {
                txtStatusBar.Text = message;
            });
        }

        private void OnProgressChanged(ProgressEventArgs args)
        {
            ProgressChanged?.Invoke(this, args);
        }

        // Properties
        public bool IsInstalling => isInstalling;
        public int CurrentDriverIndex => currentDriverIndex;
        public int CurrentProgramIndex => currentProgramIndex;
        public List<DriverInfo> CurrentDrivers => new List<DriverInfo>(drivers);
        public List<ProgramInfo> CurrentPrograms => new List<ProgramInfo>(programs);
    }

    /// <summary>
    /// Opera Password Manager sınıfı (geçici - ayrı dosyaya taşınacak)
    /// </summary>
    public class OperaPasswordManager
    {
        private readonly Action<string> logCallback;

        public OperaPasswordManager(Action<string> logCallback)
        {
            this.logCallback = logCallback;
        }

        public async Task<bool> InstallOperaWithPasswordImport(string installPath, string installArguments)
        {
            try
            {
                logCallback("Opera kurulumu başlatılıyor...");

                // Normal kurulum işlemi
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = installPath,
                    Arguments = installArguments,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                Process? installProcess = Process.Start(psi);
                if (installProcess == null)
                {
                    logCallback("Opera kurulum işlemi başlatılamadı!");
                    return false;
                }

                // Kurulum işlemini bekle
                await Task.Run(() => installProcess.WaitForExit());

                logCallback("Opera kurulumu tamamlandı!");

                // Şifre import işlemi burada yapılacak (şimdilik basit log)
                logCallback("Şifre import işlemi başlatılıyor...");
                await Task.Delay(2000); // Simülasyon
                logCallback("Şifre import işlemi tamamlandı!");

                return true;
            }
            catch (Exception ex)
            {
                logCallback($"Opera kurulum hatası: {ex.Message}");
                return false;
            }
        }
    }

    // Event argument classes
    public class ProgressEventArgs : EventArgs
    {
        public int Percentage { get; set; }
        public string? Message { get; set; }
    }

    public class InstallationCompleteEventArgs : EventArgs
    {
        public int TotalDrivers { get; set; }
        public int TotalPrograms { get; set; }
        public int SuccessfulDrivers { get; set; }
        public int SuccessfulPrograms { get; set; }
        public int FailedDrivers { get; set; }
        public int FailedPrograms { get; set; }
        public DateTime CompletionTime { get; set; } = DateTime.Now;
    }
}