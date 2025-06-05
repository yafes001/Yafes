using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Yafes;
using Yafes.GameData;
using Yafes.Managers;

namespace Yafes.Managers
{
    public class InstallationManager
    {
        #region Fields from Main.xaml.cs
        private readonly HttpClient httpClient = new HttpClient();
        private List<DriverInfo> drivers = new List<DriverInfo>();
        private List<ProgramInfo> programs = new List<ProgramInfo>(); // Yeni program listesi
        private int currentDriverIndex = 0;
        private int currentProgramIndex = 0; // Yeni programlar için indeks
        private bool isInstalling = false;
        private Dictionary<string, string> driverStatusMap = new Dictionary<string, string>();
        private Dictionary<string, string> programStatusMap = new Dictionary<string, string>(); // Program durum haritası

        // Ana listeler - değişmeyecek kaynak listeler
        private List<DriverInfo> masterDrivers = new List<DriverInfo>();
        private List<ProgramInfo> masterPrograms = new List<ProgramInfo>();

        private readonly string driversFolder = "C:\\Drivers";
        private readonly string programsFolder = "C:\\Programs"; // Yeni programlar klasörü
        private readonly string alternativeDriversFolder = "F:\\MSI Drivers"; // Alternatif sürücü klasörü
        private readonly string alternativeProgramsFolder = "F:\\Programs"; // Alternatif programlar klasörü

        // Dependencies - Bu callback'ler Main'den inject edilecek
        private readonly Action<string> txtLogAppendText;
        private readonly Action<int> setProgressBarValue;
        private readonly Action<int> setProgressBarStatusValue;
        private readonly Action<string> setTxtStatusBarText;
        private readonly Func<bool> getChkRestartIsChecked;
        private readonly InstallationQueueManager queueManager;
        #endregion

        #region Constructor
        public InstallationManager(
            Action<string> txtLogAppendText,
            Action<int> setProgressBarValue,
            Action<int> setProgressBarStatusValue,
            Action<string> setTxtStatusBarText,
            Func<bool> getChkRestartIsChecked,
            InstallationQueueManager queueManager)
        {
            this.txtLogAppendText = txtLogAppendText;
            this.setProgressBarValue = setProgressBarValue;
            this.setProgressBarStatusValue = setProgressBarStatusValue;
            this.setTxtStatusBarText = setTxtStatusBarText;
            this.getChkRestartIsChecked = getChkRestartIsChecked;
            this.queueManager = queueManager;

            // Ana klasörleri oluştur
            if (!Directory.Exists(driversFolder))
                Directory.CreateDirectory(driversFolder);

            if (!Directory.Exists(programsFolder))
                Directory.CreateDirectory(programsFolder);

            // Ana listeleri oluştur
            masterDrivers = new List<DriverInfo>();
            masterPrograms = new List<ProgramInfo>();
            drivers = new List<DriverInfo>();
            programs = new List<ProgramInfo>();

            // Sürücü ve program bilgilerini ekle
            InitializeDrivers();
            InitializePrograms();
        }
        #endregion

        #region Properties
        public bool IsInstalling => isInstalling;
        public List<DriverInfo> MasterDrivers => masterDrivers;
        public List<ProgramInfo> MasterPrograms => masterPrograms;
        public Dictionary<string, string> DriverStatusMap => driverStatusMap;
        public Dictionary<string, string> ProgramStatusMap => programStatusMap;
        #endregion

        #region Initialize Methods from Main.xaml.cs
        private void InitializeDrivers()
        {
            // Önce listeleri temizle
            masterDrivers.Clear();
            drivers.Clear();

            // Sürücüleri ekle
            masterDrivers.Add(new DriverInfo
            {
                Name = "NVIDIA Graphics Driver",
                Url = "https://tr.download.nvidia.com/Windows/576.40/576.40-desktop-win10-win11-64bit-international-dch-whql.exe",
                FileName = "nvidia_driver.exe",
                ProcessName = "setup",
                InstallArguments = "/s /n",
                IsZip = false,
                AlternativeSearchPattern = "nvidia*.exe",
                ResourceName = "Yafes.Resources.nvidia_driver.exe"
            });

            masterDrivers.Add(new DriverInfo
            {
                Name = "Realtek PCIe LAN Driver",
                Url = "https://download.msi.com/dvr_exe/mb/realtek_pcielan_w10.zip",
                FileName = "realtek_lan.zip",
                ProcessName = "setup",
                InstallArguments = "/s",
                IsZip = true,
                AlternativeSearchPattern = "*lan*.zip",
                ResourceName = "Yafes.Resources.realtek_pcielan_w10.zip"
            });

            masterDrivers.Add(new DriverInfo
            {
                Name = "Realtek Audio Driver",
                Url = "https://download.msi.com/dvr_exe/mb/realtek_audio_R.zip",
                FileName = "realtek_audio.zip",
                ProcessName = "setup",
                InstallArguments = "/s",
                IsZip = true,
                AlternativeSearchPattern = "*audio*.zip",
                ResourceName = "Yafes.Resources.realtek_audio_R.zip"
            });

            // Her sürücü için durum haritası başlat
            foreach (var driver in masterDrivers)
            {
                driverStatusMap[driver.Name] = "Bekliyor";
            }
        }

        private void InitializePrograms()
        {
            // Önce listeleri tamamen temizle
            masterPrograms.Clear();
            programs.Clear();

            // Log ekle
            Console.WriteLine("Program listesi yükleniyor...");

            // Programları ekle - TÜM PROGRAMLARI EKLEDİĞİMİZDEN EMİN OLALIM
            masterPrograms.Add(new ProgramInfo
            {
                Name = "Discord",
                Url = "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64",
                FileName = "DiscordSetup.exe",
                ProcessName = "DiscordSetup",
                InstallArguments = "-s",
                IsZip = false,
                AlternativeSearchPattern = "discord*.exe",
                ResourceName = "Yafes.Resources.DiscordSetup.exe",
                SpecialInstallation = false
            });

            masterPrograms.Add(new ProgramInfo
            {
                Name = "WinRAR",
                Url = "https://www.win-rar.com/postdownload.html?&L=5",
                FileName = "winrar-x64-711tr.exe",
                ProcessName = "WinRAR",
                InstallArguments = "/S",
                IsZip = false,
                AlternativeSearchPattern = "winrar*.exe",
                ResourceName = "Yafes.Resources.winrar-x64-711tr.exe",
                SpecialInstallation = true
            });

            masterPrograms.Add(new ProgramInfo
            {
                Name = "Opera",
                Url = "https://www.opera.com/tr/computer/thanks?ni=stable&os=windows",
                FileName = "OperaSetup.exe",
                ProcessName = "opera",
                InstallArguments = "--silent --installfolder=\"C:\\Program Files\\Opera\"",
                IsZip = false,
                AlternativeSearchPattern = "opera*.exe",
                ResourceName = "Yafes.Resources.OperaSetup.exe",
                SpecialInstallation = true // ← Bu satırı true yapın
            });

            // Eksik programları ekleyelim
            masterPrograms.Add(new ProgramInfo
            {
                Name = "Steam",
                Url = "https://cdn.fastly.steamstatic.com/client/installer/SteamSetup.exe",
                FileName = "steam_installer.exe",
                ProcessName = "Steam",
                InstallArguments = "/S",
                IsZip = false,
                AlternativeSearchPattern = "steam*.exe",
                ResourceName = "Yafes.Resources.steam_installer.exe",
                SpecialInstallation = false
            });

            masterPrograms.Add(new ProgramInfo
            {
                Name = "Lightshot",
                Url = "https://app.prntscr.com/build/setup-lightshot.exe",
                FileName = "lightshot_installer.exe",
                ProcessName = "setup-lightshot",
                InstallArguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOCANCEL",
                IsZip = false,
                AlternativeSearchPattern = "*lightshot*.exe",
                ResourceName = "Yafes.Resources.lightshot_installer.exe",
                SpecialInstallation = false
            });

            masterPrograms.Add(new ProgramInfo
            {
                Name = "Notepad++",
                Url = "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.7/npp.8.7.7.Installer.x64.exe",
                FileName = "npp_installer.exe",
                ProcessName = "notepad++",
                InstallArguments = "/S",
                IsZip = false,
                AlternativeSearchPattern = "npp*.exe",
                ResourceName = "Yafes.Resources.npp_installer.exe",
                SpecialInstallation = false
            });

            masterPrograms.Add(new ProgramInfo
            {
                Name = "Visual Studio Setup",
                Url = "", // İndirme yok, sadece Resources'dan
                FileName = "VisualStudioSetup.exe",
                ProcessName = "VisualStudioSetup",
                InstallArguments = "/quiet", // Visual Studio için silent kurulum
                IsZip = false,
                AlternativeSearchPattern = "VisualStudioSetup*.exe",
                ResourceName = "Yafes.Resources.VisualStudioSetup.exe",
                SpecialInstallation = false
            });

            // uTorrent
            masterPrograms.Add(new ProgramInfo
            {
                Name = "uTorrent",
                Url = "", // İndirme yok, sadece Resources'dan
                FileName = "uTorrent 3.6.0.47196.exe",
                ProcessName = "uTorrent 3.6.0.47196",
                InstallArguments = "/S", // uTorrent için silent kurulum
                IsZip = false,
                AlternativeSearchPattern = "uTorrent*.exe",
                ResourceName = "Yafes.Resources.uTorrent 3.6.0.47196.exe",
                SpecialInstallation = false
            });

            // EA App Installer
            masterPrograms.Add(new ProgramInfo
            {
                Name = "EA App",
                Url = "", // İndirme yok, sadece Resources'dan
                FileName = "EAappInstaller.exe",
                ProcessName = "EAappInstaller",
                InstallArguments = "/quiet", // EA App için silent kurulum
                IsZip = false,
                AlternativeSearchPattern = "EAapp*.exe",
                ResourceName = "Yafes.Resources.EAappInstaller.exe",
                SpecialInstallation = false
            });

            masterPrograms.Add(new ProgramInfo
            {
                Name = "Driver Booster",
                Url = "", // İndirme yok, sadece Resources'dan
                FileName = "driver_booster_setup.exe",
                ProcessName = "driver_booster_setup",
                InstallArguments = "/VERYSILENT /NORESTART /NoAutoRun", // Silent kurulum
                IsZip = false,
                AlternativeSearchPattern = "driver_booster*.exe",
                ResourceName = "Yafes.Resources.driver_booster_setup.exe",
                SpecialInstallation = false// Özel kurulum gerekli
            });

            // Revo Uninstaller Pro - Özel kurulum gerekli
            masterPrograms.Add(new ProgramInfo
            {
                Name = "Revo Uninstaller Pro",
                Url = "", // İndirme yok, sadece Resources'dan
                FileName = "RevoUninProSetup.exe",
                ProcessName = "RevoUninProSetup",
                InstallArguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART", // Silent kurulum
                IsZip = false,
                AlternativeSearchPattern = "RevoUnin*.exe",
                ResourceName = "Yafes.Resources.RevoUninProSetup.exe",
                SpecialInstallation = false // Özel kurulum gerekli
            });

            // Program sayısını logla
            Console.WriteLine($"Toplam {masterPrograms.Count} program yüklendi.");

            // Her program için durum haritası başlat
            foreach (var program in masterPrograms)
            {
                programStatusMap[program.Name] = "Bekliyor";
            }
        }
        #endregion

        #region Installation Methods from Main.xaml.cs
        // TEK PrepareInstallation METODU - Düzeltilmiş versiyon
        public void PrepareInstallation(List<DriverInfo> selectedDrivers, List<ProgramInfo> selectedPrograms)
        {
            isInstalling = true;
            currentDriverIndex = 0;
            currentProgramIndex = 0;

            // Kurulacak sürücü ve program listelerini kopya olarak hazırla
            drivers = new List<DriverInfo>(selectedDrivers);
            programs = new List<ProgramInfo>(selectedPrograms);

            // KUYRUK YÖNETİCİSİNDE KUYRUĞU OLUŞTUR
            queueManager.CreateQueue(drivers, programs);

            txtLogAppendText($"ADIM 1: Toplam {drivers.Count + programs.Count} kurulum başlatılıyor...\n");

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
                txtLogAppendText("Kurulacak öğe bulunamadı. İşlem tamamlandı.\n");
                CompleteInstallation();
            }
        }

        private async void StartNextDriverInstallation()
        {
            if (currentDriverIndex >= drivers.Count)
            {
                txtLogAppendText("Tüm sürücü kurulumları tamamlandı!\n");
                StartProgramInstallations();
                return;
            }

            DriverInfo currentDriver = drivers[currentDriverIndex];

            // KUYRUK YÖNETİCİSİNDE KURULUMU BAŞLAT - YENİ EKLENDİ
            queueManager.StartInstallation(currentDriver.Name);

            string filePath = Path.Combine(driversFolder, currentDriver.FileName);
            bool fileExists = File.Exists(filePath);

            if (fileExists)
            {
                await InstallDriver(currentDriver, filePath);
            }
            else
            {
                bool resourceExtracted = await ExtractEmbeddedResource(currentDriver.ResourceName, filePath);

                if (resourceExtracted)
                {
                    txtLogAppendText($"Gömülü kaynaktan {currentDriver.Name} çıkarıldı\n");
                    await InstallDriver(currentDriver, filePath);
                }
                else if (IsInternetAvailable())
                {
                    driverStatusMap[currentDriver.Name] = "İndiriliyor";
                    await DownloadDriver(currentDriver, filePath);
                }
                else
                {
                    driverStatusMap[currentDriver.Name] = "Alternatiften Kurulum";
                    string? alternativeFilePath = FindDriverInAlternativeFolder(currentDriver);

                    if (alternativeFilePath != null)
                    {
                        txtLogAppendText($"Alternatif klasörde sürücü bulundu: {alternativeFilePath}\n");
                        await InstallDriver(currentDriver, alternativeFilePath);
                    }
                    else
                    {
                        txtLogAppendText($"Alternatif klasörde sürücü bulunamadı! Desen: {currentDriver.AlternativeSearchPattern}\n");
                        driverStatusMap[currentDriver.Name] = "Hata";

                        // KUYRUK YÖNETİCİSİNDE HATALI KURULUMU TAMAMLA
                        queueManager.CompleteInstallation(currentDriver.Name);

                        HandleDriverError();
                    }
                }
            }
        }

        // Program kurulumlarını başlat
        private void StartProgramInstallations()
        {
            txtLogAppendText("\nADIM 2: Program kurulumları başlatılıyor...\n");
            if (programs.Count > 0)
            {
                StartNextProgramInstallation();
            }
            else
            {
                CompleteInstallation();
            }
        }

        private async void StartNextProgramInstallation()
        {
            if (currentProgramIndex >= programs.Count)
            {
                txtLogAppendText("Tüm program kurulumları tamamlandı!\n");
                CompleteInstallation();
                return;
            }

            ProgramInfo currentProgram = programs[currentProgramIndex];

            // KUYRUK YÖNETİCİSİNDE KURULUMU BAŞLAT - YENİ EKLENDİ
            queueManager.StartInstallation(currentProgram.Name);

            string filePath = Path.Combine(programsFolder, currentProgram.FileName);
            bool fileExists = File.Exists(filePath);

            if (fileExists)
            {
                await InstallProgram(currentProgram, filePath);
            }
            else
            {
                bool resourceExtracted = await ExtractEmbeddedResource(currentProgram.ResourceName, filePath);

                if (resourceExtracted)
                {
                    txtLogAppendText($"Gömülü kaynaktan {currentProgram.Name} çıkarıldı\n");
                    await InstallProgram(currentProgram, filePath);
                }
                else if (IsInternetAvailable())
                {
                    programStatusMap[currentProgram.Name] = "İndiriliyor";
                    await DownloadProgram(currentProgram, filePath);
                }
                else
                {
                    programStatusMap[currentProgram.Name] = "Alternatiften Kurulum";
                    string? alternativeFilePath = FindProgramInAlternativeFolder(currentProgram);

                    if (alternativeFilePath != null)
                    {
                        txtLogAppendText($"Alternatif klasörde program bulundu: {alternativeFilePath}\n");
                        await InstallProgram(currentProgram, alternativeFilePath);
                    }
                    else
                    {
                        txtLogAppendText($"Alternatif klasörde program bulunamadı! Desen: {currentProgram.AlternativeSearchPattern}\n");
                        programStatusMap[currentProgram.Name] = "Hata";

                        // KUYRUK YÖNETİCİSİNDE HATALI KURULUMU TAMAMLA
                        queueManager.CompleteInstallation(currentProgram.Name);

                        HandleProgramError();
                    }
                }
            }
        }

        // Kurulum tamamlandığında yapılacak işlemler
        private void CompleteInstallation()
        {
            isInstalling = false;

            setProgressBarValue(100);
            setProgressBarStatusValue(100);
            setTxtStatusBarText("Tüm kurulumlar tamamlandı");

            // KUYRUK YÖNETİCİSİNİ DURDUR - YENİ EKLENDİ
            queueManager.Stop();

            txtLogAppendText("\n*** TÜM KURULUMLAR TAMAMLANDI! ***\n");

            if (getChkRestartIsChecked())
            {
                MessageBoxResult result = MessageBox.Show("Kurulum tamamlandı. Bilgisayarı yeniden başlatmak istiyor musunuz?",
                    "Yeniden Başlat", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("shutdown", "/r /t 10");
                    Application.Current.Shutdown();
                }
            }
        }

        // Gömülü kaynağı dosyaya çıkar - bool döndürür (başarılı mı?)
        private async Task<bool> ExtractEmbeddedResource(string resourceName, string outputFilePath)
        {
            try
            {
                // Gömülü kaynağı assembly'den al
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream? resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        // Kaynak bulunamadıysa başarısız
                        txtLogAppendText($"Kaynak bulunamadı: {resourceName}\n");
                        return false;
                    }

                    long expectedSize = resourceStream.Length;
                    txtLogAppendText($"Kaynak boyutu: {expectedSize / 1024} KB\n");

                    // Dosya içeriğini buffer'a oku ve dosyaya yaz
                    using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long totalBytesRead = 0;
                        long totalBytes = resourceStream.Length;

                        // İlerleme çubuğunu başlat
                        setProgressBarValue(0);
                        setProgressBarStatusValue(0);
                        setTxtStatusBarText($"Gömülü kaynak çıkartılıyor... %0");

                        // Dosyayı oku ve yaz
                        while ((bytesRead = await resourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);

                            totalBytesRead += bytesRead;

                            // İlerleme durumunu güncelle
                            int progressPercentage = (int)((double)totalBytesRead / totalBytes * 100);

                            setProgressBarValue(progressPercentage);
                            setProgressBarStatusValue(progressPercentage);
                            setTxtStatusBarText($"Gömülü kaynak çıkartılıyor... %{progressPercentage}");
                        }
                    }

                    // Dosya boyutunu kontrol et
                    FileInfo fileInfo = new FileInfo(outputFilePath);
                    if (fileInfo.Length < expectedSize * 0.9) // En az beklenen boyutun %90'ı olmalı
                    {
                        txtLogAppendText($"UYARI: Çıkarılan dosya eksik olabilir. Beklenen: {expectedSize / 1024} KB, Gerçek: {fileInfo.Length / 1024} KB\n");
                        return false;
                    }

                    // Başarıyla tamamlandı
                    txtLogAppendText($"Dosya başarıyla çıkarıldı: {fileInfo.Length / 1024} KB\n");
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Hata oluşursa consola yaz ve başarısız olarak dön
                txtLogAppendText($"[HATA] Gömülü kaynak çıkartma hatası: {ex.Message}\n");
                Console.WriteLine($"[ERROR] Gömülü kaynak çıkartma hatası: {ex.Message}");
                return false;
            }
        }

        // Alternatif klasörde sürücü ara - CS8603 düzeltmesi: Nullable dönüş tipi
        private string? FindDriverInAlternativeFolder(DriverInfo driver)
        {
            try
            {
                // Alternatif klasörün varlığını kontrol et
                if (!Directory.Exists(alternativeDriversFolder))
                {
                    txtLogAppendText($"Alternatif sürücü klasörü bulunamadı: {alternativeDriversFolder}\n");
                    return null;
                }

                // Alternatif klasörde belirtilen desene göre dosya ara
                string[] files = Directory.GetFiles(alternativeDriversFolder, driver.AlternativeSearchPattern, SearchOption.AllDirectories);

                if (files.Length > 0)
                {
                    // İlk bulunan dosyayı döndür
                    return files[0];
                }

                return null;
            }
            catch (Exception ex)
            {
                txtLogAppendText($"Alternatif klasörde arama hatası: {ex.Message}\n");
                return null;
            }
        }

        private async Task DownloadDriver(DriverInfo driver, string filePath)
        {
            try
            {
                setTxtStatusBarText($"{driver.Name} indiriliyor...");
                setProgressBarStatusValue(0);

                // İndirme işlemi
                using (var response = await httpClient.GetAsync(driver.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    // Toplam dosya boyutunu al
                    long totalBytes = response.Content.Headers.ContentLength ?? -1;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;
                        int lastPercentage = 0;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);

                            totalBytesRead += bytesRead;

                            // İlerleme çubuğunu güncelle
                            if (totalBytes > 0)
                            {
                                int progressPercentage = (int)((double)totalBytesRead / totalBytes * 100);

                                // Sadece yüzde değeri değiştiyse ve her %25'lik artışta güncelle
                                if (progressPercentage > lastPercentage && (progressPercentage % 25 == 0 || progressPercentage == 100))
                                {
                                    setProgressBarValue(progressPercentage);
                                    setProgressBarStatusValue(progressPercentage);
                                    setTxtStatusBarText($"{driver.Name} indiriliyor... %{progressPercentage}");
                                    lastPercentage = progressPercentage;
                                }
                                else
                                {
                                    // Daima progress bar'ı güncelle ama log'a yazma
                                    setProgressBarValue(progressPercentage);
                                    setProgressBarStatusValue(progressPercentage);
                                    setTxtStatusBarText($"{driver.Name} indiriliyor... %{progressPercentage}");
                                }
                            }
                        }
                    }
                }

                // Sürücüyü kur
                await InstallDriver(driver, filePath);
            }
            catch (Exception ex)
            {
                // Hata durumunda durumu güncelle
                driverStatusMap[driver.Name] = "Hata";
                txtLogAppendText($"İndirme hatası: {ex.Message}\n");
                HandleDriverError();
            }
        }

        private async Task InstallDriver(DriverInfo driver, string filePath)
        {
            try
            {
                setTxtStatusBarText($"{driver.Name} kuruluyor...");
                setProgressBarValue(0);

                string installPath = filePath;
                string extractPath = "";

                // Eğer ZIP dosyası ise, çıkart
                if (driver.IsZip)
                {
                    // Durum güncelle - Çıkartılıyor
                    driverStatusMap[driver.Name] = "Çıkartılıyor";

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
                        driverStatusMap[driver.Name] = "Hata";
                        txtLogAppendText("Kurulum dosyası bulunamadı!\n");
                        HandleDriverError();
                        return;
                    }
                }

                // Durumu güncelle - Kuruluyor
                driverStatusMap[driver.Name] = "Kuruluyor";

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
                    driverStatusMap[driver.Name] = "Hata";
                    txtLogAppendText("Kurulum işlemi başlatılamadı!\n");
                    HandleDriverError();
                    return;
                }

                string processName = driver.ProcessName;

                // Kurulum işleminin tamamlanmasını bekle
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

                        setProgressBarValue(progress);
                        setProgressBarStatusValue(progress);
                        setTxtStatusBarText($"{driver.Name} kuruluyor... %{progress}");

                        // Process listesini kontrol et
                        try
                        {
                            // Önce başlattığımız process'i kontrol et
                            if (!installProcess.HasExited)
                            {
                                // Process hala çalışıyor
                                System.Threading.Thread.Sleep(2000); // 2 saniye bekle
                                continue;
                            }

                            // Başlattığımız process bittiyse, benzer adlı başka processler var mı diye kontrol et
                            Process[] processes = Process.GetProcessesByName(processName);

                            if (processes.Length > 0)
                            {
                                // Hala kurulum işlemi devam ediyor
                                System.Threading.Thread.Sleep(2000); // 2 saniye bekle
                            }
                            else
                            {
                                // Kurulum işlemi tamamlandı
                                processFound = false;
                            }
                        }
                        catch
                        {
                            // Process listesine erişemiyorsak da 2 saniye bekle
                            System.Threading.Thread.Sleep(2000);
                        }
                    }

                    // Kurulum tamamlandı, ilerleme çubuğunu %100 yap
                    setProgressBarValue(100);
                    setProgressBarStatusValue(100);
                    setTxtStatusBarText($"{driver.Name} kurulumu tamamlandı");
                });

                // Kurulum tamamlandı, durumu başarılı olarak işaretle
                driverStatusMap[driver.Name] = "Başarılı";

                // KUYRUK YÖNETİCİSİNDE KURULUMU TAMAMLA - YENİ EKLENDİ
                queueManager.CompleteInstallation(driver.Name);

                // Sonraki sürücüye geç
                currentDriverIndex++;
                StartNextDriverInstallation();
            }
            catch (Exception ex)
            {
                driverStatusMap[driver.Name] = "Hata";
                txtLogAppendText($"Kurulum hatası: {ex.Message}\n");

                // KUYRUK YÖNETİCİSİNDE HATALI KURULUMU TAMAMLA - YENİ EKLENDİ
                queueManager.CompleteInstallation(driver.Name);

                HandleDriverError();
            }
        }

        private void HandleDriverError()
        {
            // Hata durumunda bir sonraki sürücüye geç
            currentDriverIndex++;
            StartNextDriverInstallation();
        }

        // Alternatif klasörde program arama
        private string? FindProgramInAlternativeFolder(ProgramInfo program)
        {
            try
            {
                // Alternatif klasörün varlığını kontrol et
                if (!Directory.Exists(alternativeProgramsFolder))
                {
                    txtLogAppendText($"Alternatif program klasörü bulunamadı: {alternativeProgramsFolder}\n");
                    return null;
                }

                // Alternatif klasörde belirtilen desene göre dosya ara
                string[] files = Directory.GetFiles(alternativeProgramsFolder, program.AlternativeSearchPattern, SearchOption.AllDirectories);

                if (files.Length > 0)
                {
                    // İlk bulunan dosyayı döndür
                    return files[0];
                }

                return null;
            }
            catch (Exception ex)
            {
                txtLogAppendText($"Alternatif klasörde arama hatası: {ex.Message}\n");
                return null;
            }
        }

        // Program indirme işlemi
        private async Task DownloadProgram(ProgramInfo program, string filePath)
        {
            try
            {
                setTxtStatusBarText($"{program.Name} indiriliyor...");
                setProgressBarStatusValue(0);

                // İndirme işlemi
                using (var response = await httpClient.GetAsync(program.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    // Toplam dosya boyutunu al
                    long totalBytes = response.Content.Headers.ContentLength ?? -1;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;
                        int lastPercentage = 0;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);

                            totalBytesRead += bytesRead;

                            // İlerleme çubuğunu güncelle
                            if (totalBytes > 0)
                            {
                                int progressPercentage = (int)((double)totalBytesRead / totalBytes * 100);

                                // Sadece yüzde değeri değiştiyse güncelle
                                if (progressPercentage > lastPercentage)
                                {
                                    setProgressBarValue(progressPercentage);
                                    setProgressBarStatusValue(progressPercentage);
                                    setTxtStatusBarText($"{program.Name} indiriliyor... %{progressPercentage}");
                                    lastPercentage = progressPercentage;
                                }
                            }
                        }
                    }
                }

                // Programı kur
                await InstallProgram(program, filePath);
            }
            catch (Exception ex)
            {
                // Hata durumunda durumu güncelle
                programStatusMap[program.Name] = "Hata";
                txtLogAppendText($"İndirme hatası: {ex.Message}\n");
                HandleProgramError();
            }
        }

        // Program kurulum işlemi - TAM VERSİYON
        private async Task InstallProgram(ProgramInfo program, string filePath)
        {
            try
            {
                setTxtStatusBarText($"{program.Name} kuruluyor...");
                setProgressBarValue(0);

                string installPath = filePath;
                string extractPath = "";

                // Eğer ZIP dosyası ise, çıkart
                if (program.IsZip)
                {
                    // Durum güncelle - Çıkartılıyor
                    programStatusMap[program.Name] = "Çıkartılıyor";

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
                        programStatusMap[program.Name] = "Hata";
                        txtLogAppendText("Kurulum dosyası bulunamadı!\n");
                        HandleProgramError();
                        return;
                    }
                }

                // Durumu güncelle - Kuruluyor
                programStatusMap[program.Name] = "Kuruluyor";

                // ÖZEL KURULUM KONTROLLERİ

                // WinRAR için özel kurulum
                if (program.SpecialInstallation && program.Name == "WinRAR")
                {
                    await InstallWinRARWithPowerShell(program, installPath);
                }
                // Opera için özel kurulum + şifre import
                else if (program.Name == "Opera")
                {
                    await InstallOperaWithPasswordImport(program, installPath);
                }
                else
                {
                    // NORMAL KURULUM İŞLEMİ
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
                        programStatusMap[program.Name] = "Hata";
                        txtLogAppendText("Kurulum işlemi başlatılamadı!\n");
                        HandleProgramError();
                        return;
                    }

                    string processName = program.ProcessName;

                    // Kurulum işleminin tamamlanmasını bekle
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

                            setProgressBarValue(progress);
                            setProgressBarStatusValue(progress);
                            setTxtStatusBarText($"{program.Name} kuruluyor... %{progress}");

                            // Process listesini kontrol et
                            try
                            {
                                // Önce başlattığımız process'i kontrol et
                                if (!installProcess.HasExited)
                                {
                                    // Process hala çalışıyor
                                    System.Threading.Thread.Sleep(2000); // 2 saniye bekle
                                    continue;
                                }

                                // Başlattığımız process bittiyse, benzer adlı başka processler var mı diye kontrol et
                                Process[] processes = Process.GetProcessesByName(processName);

                                if (processes.Length > 0)
                                {
                                    // Hala kurulum işlemi devam ediyor
                                    System.Threading.Thread.Sleep(2000); // 2 saniye bekle
                                }
                                else
                                {
                                    // Kurulum işlemi tamamlandı
                                    processFound = false;
                                }
                            }
                            catch
                            {
                                // Process listesine erişemiyorsak da 2 saniye bekle
                                System.Threading.Thread.Sleep(2000);
                            }
                        }

                        // Kurulum tamamlandı, ilerleme çubuğunu %100 yap
                        setProgressBarValue(100);
                        setProgressBarStatusValue(100);
                        setTxtStatusBarText($"{program.Name} kurulumu tamamlandı");
                    });
                }

                // Kurulum tamamlandı, durumu başarılı olarak işaretle
                programStatusMap[program.Name] = "Başarılı";

                // KUYRUK YÖNETİCİSİNDE KURULUMU TAMAMLA - YENİ EKLENDİ
                queueManager.CompleteInstallation(program.Name);

                // Sonraki programa geç
                currentProgramIndex++;
                StartNextProgramInstallation();
            }
            catch (Exception ex)
            {
                programStatusMap[program.Name] = "Hata";
                txtLogAppendText($"Kurulum hatası: {ex.Message}\n");

                // KUYRUK YÖNETİCİSİNDE HATALI KURULUMU TAMAMLA - YENİ EKLENDİ
                queueManager.CompleteInstallation(program.Name);

                HandleProgramError();
            }
        }

        // WinRAR için özel PowerShell kurulumu
        private async Task InstallWinRARWithPowerShell(ProgramInfo program, string installPath)
        {
            try
            {
                txtLogAppendText($"WinRAR için özel CMD kurulumu başlatılıyor...\n");

                // Batch dosyası oluştur - Doğrudan CMD komutu kullan, PowerShell kullanma
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
                    txtLogAppendText("WinRAR kurulum işlemi devam ediyor, tamamlanması bekleniyor...\n");

                    System.Threading.Thread.Sleep(15000); // 15 saniye bekle (kurulum için yeterli süre)

                    // WinRAR'ın kurulup kurulmadığını kontrol et
                    bool isWinRarInstalled = Directory.Exists(@"C:\Program Files\WinRAR") ||
                                            Directory.Exists(@"C:\Program Files (x86)\WinRAR");

                    if (isWinRarInstalled)
                    {
                        txtLogAppendText("WinRAR kurulumu başarıyla tamamlandı!\n");
                    }
                    else
                    {
                        txtLogAppendText("WinRAR kurulumu tamamlandı, ancak kurulum klasörü bulunamadı.\n");
                    }

                    // Batch dosyasını temizlemeyi dene
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
            catch (Exception ex)
            {
                txtLogAppendText($"WinRAR CMD kurulum hatası: {ex.Message}\n");
                throw; // Hatayı üst katmana ilet
            }
        }

        /// <summary>
        /// Opera'yı şifre import özelliği ile kurar
        /// </summary>
        private async Task InstallOperaWithPasswordImport(ProgramInfo program, string installPath)
        {
            try
            {
                // Opera Password Manager'ı oluştur
                var operaManager = new OperaPasswordManager((message) =>
                {
                    // Log callback - UI thread'de güvenli şekilde çalışır
                    txtLogAppendText(message + "\n");
                });

                // Progress bar'ı güncelle
                setProgressBarValue(0);
                setProgressBarStatusValue(0);
                setTxtStatusBarText("Opera kuruluyor ve şifreler import ediliyor...");

                // Opera kurulum + şifre import işlemini başlat
                bool success = await operaManager.InstallOperaWithPasswordImport(installPath, program.InstallArguments);

                // Progress bar'ı tamamla
                setProgressBarValue(100);
                setProgressBarStatusValue(100);
                setTxtStatusBarText(success ? "Opera kurulumu ve şifre import tamamlandı" : "Opera kurulumu tamamlandı");

                if (!success)
                {
                    throw new Exception("Opera kurulum işlemi başarısız oldu.");
                }
            }
            catch (Exception ex)
            {
                txtLogAppendText($"Opera kurulum hatası: {ex.Message}\n");
                throw; // Hatayı üst katmana ilet
            }
        }

        // Program kurulum hatası yönetimi
        private void HandleProgramError()
        {
            // Hata durumunda bir sonraki programa geç
            currentProgramIndex++;
            StartNextProgramInstallation();
        }

        // İnternet bağlantısını kontrol et
        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000); // Google DNS sunucusuna 2 saniye timeout ile ping at
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false; // Herhangi bir hata durumunda internet yok olarak kabul et
            }
        }

        public void OpenCategoryFolder(string category)
        {
            try
            {
                if (category == "Sürücüler")
                {
                    // Sürücü klasörünü aç
                    if (!Directory.Exists(driversFolder))
                        Directory.CreateDirectory(driversFolder);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = driversFolder,
                        UseShellExecute = true
                    });
                }
                else if (category == "Programlar")
                {
                    // Program klasörünü aç
                    if (!Directory.Exists(programsFolder))
                        Directory.CreateDirectory(programsFolder);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = programsFolder,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                txtLogAppendText("Klasör açılırken hata: " + ex.Message + "\n");
            }
        }

        public void AddUserDefinedItem(string category)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Kurulum Dosyaları (*.exe;*.msi;*.zip)|*.exe;*.msi;*.zip|Tüm Dosyalar (*.*)|*.*";

                if (category == "Sürücüler")
                {
                    openFileDialog.Title = "Sürücü Seç";

                    if (openFileDialog.ShowDialog() == true)
                    {
                        string filePath = openFileDialog.FileName;
                        string fileName = Path.GetFileName(filePath);
                        string driverName = Path.GetFileNameWithoutExtension(filePath);
                        bool isZip = Path.GetExtension(filePath).ToLower() == ".zip";

                        // Sürücüyü listeye ekle
                        DriverInfo newDriver = new DriverInfo
                        {
                            Name = driverName,
                            Url = string.Empty, // Url boş olabilir, kullanıcı tarafından eklenen sürücü için
                            FileName = fileName,
                            ProcessName = "setup",
                            InstallArguments = "/s",
                            IsZip = isZip,
                            AlternativeSearchPattern = Path.GetFileName(filePath) // Dosya adını alternatif desen olarak kullan
                        };

                        // Dosyayı Drivers klasörüne kopyala
                        string destPath = Path.Combine(driversFolder, fileName);
                        File.Copy(filePath, destPath, true);
                        txtLogAppendText($"Dosya başarıyla kopyalandı: {destPath}\n");

                        // Yeni sürücüyü listeye ekle
                        masterDrivers.Add(newDriver);
                        driverStatusMap[newDriver.Name] = "Bekliyor";
                    }
                }
                else if (category == "Programlar")
                {
                    openFileDialog.Title = "Program Seç";

                    if (openFileDialog.ShowDialog() == true)
                    {
                        string filePath = openFileDialog.FileName;
                        string fileName = Path.GetFileName(filePath);
                        string programName = Path.GetFileNameWithoutExtension(filePath);
                        bool isZip = Path.GetExtension(filePath).ToLower() == ".zip";

                        // Program ekle
                        ProgramInfo newProgram = new ProgramInfo
                        {
                            Name = programName,
                            Url = string.Empty, // Url boş olabilir, kullanıcı tarafından eklenen program için
                            FileName = fileName,
                            ProcessName = Path.GetFileNameWithoutExtension(fileName),
                            InstallArguments = "/S", // Varsayılan sessiz kurulum parametresi
                            IsZip = isZip,
                            AlternativeSearchPattern = Path.GetFileName(filePath), // Dosya adını alternatif desen olarak kullan
                            SpecialInstallation = false
                        };

                        // Dosyayı Programs klasörüne kopyala
                        string destPath = Path.Combine(programsFolder, fileName);
                        File.Copy(filePath, destPath, true);
                        txtLogAppendText($"Dosya başarıyla kopyalandı: {destPath}\n");

                        // Yeni programı listeye ekle
                        masterPrograms.Add(newProgram);
                        programStatusMap[newProgram.Name] = "Bekliyor";
                    }
                }
            }
            catch (Exception ex)
            {
                txtLogAppendText("Dosya ekleme hatası: " + ex.Message + "\n");
            }
        }
        #endregion

        #region Model Classes from Main.xaml.cs
        // Driver sınıfını genişlet - ResourceName özelliği ekle
        public class DriverInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string ProcessName { get; set; } = string.Empty;
            public string InstallArguments { get; set; } = string.Empty;
            public bool IsZip { get; set; }
            public string AlternativeSearchPattern { get; set; } = string.Empty;
            public string ResourceName { get; set; } = string.Empty; // Gömülü kaynak adı
        }

        // Program bilgisi sınıfını genişlet - ResourceName özelliği ekle
        public class ProgramInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string ProcessName { get; set; } = string.Empty;
            public string InstallArguments { get; set; } = string.Empty;
            public bool IsZip { get; set; }
            public string AlternativeSearchPattern { get; set; } = string.Empty;
            public string ResourceName { get; set; } = string.Empty; // Gömülü kaynak adı
            public bool SpecialInstallation { get; set; } = false; // Özel kurulum yöntemi
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            httpClient?.Dispose();
        }
        #endregion
    }
}