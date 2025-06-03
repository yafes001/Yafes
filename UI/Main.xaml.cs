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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Animation;
using Yafes;
using Yafes.GameData;

namespace Yafes
{
    public partial class Main : Window
    {
        private bool isGamesVisible = false;
        private ListBox _lstDrivers;
        private GamesManager gamesManager;
        private SystemInfoManager systemInfoManager;
        private bool _driversMessageShown = false;
        private bool _programsMessageShown = false;
        private readonly string driversFolder = "C:\\Drivers";
        private readonly string programsFolder = "C:\\Programs"; // Yeni programlar klasörü
        private readonly string alternativeDriversFolder = "F:\\MSI Drivers"; // Alternatif sürücü klasörü
        private readonly string alternativeProgramsFolder = "F:\\Programs"; // Alternatif programlar klasörü
        private readonly HttpClient httpClient = new HttpClient();
        private List<DriverInfo> drivers = new List<DriverInfo>();
        private List<ProgramInfo> programs = new List<ProgramInfo>(); // Yeni program listesi
        private int currentDriverIndex = 0;
        private int currentProgramIndex = 0; // Yeni programlar için indeks
        private bool isInstalling = false;
        private Dictionary<string, string> driverStatusMap = new Dictionary<string, string>();
        private Dictionary<string, string> programStatusMap = new Dictionary<string, string>(); // Program durum haritası
        private Dictionary<string, Dictionary<string, bool>> categorySelections = new Dictionary<string, Dictionary<string, bool>>();
        // Ana listeler - değişmeyecek kaynak listeler
        private List<DriverInfo> masterDrivers = new List<DriverInfo>();
        private List<ProgramInfo> masterPrograms = new List<ProgramInfo>();
        // Kategori değişkenleri
        private string currentCategory = "Sürücüler"; // Varsayılan kategori
        private InstallationQueueManager queueManager;

        public Main()
        {
            try
            {
                InitializeComponent();
                txtLog.AppendText("Yafes Kurulum Aracı başlatıldı\n");
                txtLog.AppendText("Lütfen 'Yükle' butonuna tıklayarak işleme başlayın\n");

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

                // KUYRUK YÖNETİCİSİNİ BAŞLAT
                InitializeQueueManager();

                // ✅ SİSTEM BİLGİSİ YÖNETİCİSİNİ BAŞLAT
                InitializeSystemInfo();

                // İnternet kontrolü ve diğer işlemler...
                if (IsInternetAvailable())
                {
                    txtLog.AppendText("Online Bağlantı Hazır...\n");
                }
                else
                {
                    txtLog.AppendText("İnternet bağlantısı bulunamadı. Gömülü kaynaklar veya alternatif klasörlerden yükleme yapılacak\n");
                }

                ListEmbeddedResources();

                // ❌ GAMES MANAGER BAŞLATMA BÖLÜMÜNÜ YORUM SATIRINA AL
                /*
                // ✅ GAMES MANAGER'I GÜVENLİ ŞEKİLDE BAŞLAT
                try
                {
                    txtLog.AppendText("🎮 Games Manager başlatılıyor...\n");
                    gamesManager = new GamesManager(this);
                    txtLog.AppendText("🎮 Games Manager başarıyla başlatıldı\n");
                }
                catch (Exception gameEx)
                {
                    txtLog.AppendText($"⚠️ Games Manager başlatma hatası: {gameEx.Message}\n");
                    txtLog.AppendText("🔄 Games özellikleri devre dışı - normal işlevler çalışmaya devam ediyor\n");
                    gamesManager = null; // Null olarak bırak, hata vermeden devam et
                }
                */

                // ✅ UI YÜKLENDİKTEN SONRA KATEGORİ SİSTEMİNİ BAŞLAT
                this.Loaded += Main_Loaded;
            }
            catch (Exception ex)
            {
                // Constructor hatası - Critical error
                MessageBox.Show($"Ana form başlatma hatası:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                               "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);

                // Uygulamayı kapat
                Application.Current.Shutdown();
            }
        }
        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // CheckButtonReferences(); ← BU SATIRI KALDIR!

                // UI tam yüklendikten sonra kategori sistemini başlat
                InitializeCategories();

                // Games Manager'ı başlat
                try
                {
                    gamesManager = new GamesManager(this);
                }
                catch (Exception gameEx)
                {
                    txtLog.AppendText($"⚠️ Games Manager hatası: {gameEx.Message}\\n");
                    gamesManager = null;
                }

                // Varsayılan kategoriyi ayarla
                currentCategory = "Programlar";
                SetSelectedCategory("Programlar");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"⚠️ Yükleme hatası: {ex.Message}\\n");
            }
        }
        private void CheckButtonReferences()
        {
            txtLog.AppendText("🔍 Buton referansları kontrol ediliyor...\n");

            try
            {
                // Butonları Name ile bul
                btnDriverCategory = FindName("btnDriverCategory") as Button;
                btnProgramsCategory = FindName("btnProgramsCategory") as Button;
                btnGamesCategory = FindName("btnGamesCategory") as Button;
                btnToolsCategory = FindName("btnToolsCategory") as Button;

                // Detaylı buton durumlarını logla
                txtLog.AppendText($"📱 btnDriverCategory: {(btnDriverCategory != null ? "✅ BULUNDU" : "❌ BULUNAMADI")}\n");
                if (btnDriverCategory != null)
                {
                    txtLog.AppendText($"   - Content: {btnDriverCategory.Content}\n");
                    txtLog.AppendText($"   - Name: {btnDriverCategory.Name}\n");
                }

                txtLog.AppendText($"📱 btnProgramsCategory: {(btnProgramsCategory != null ? "✅ BULUNDU" : "❌ BULUNAMADI")}\n");
                if (btnProgramsCategory != null)
                {
                    txtLog.AppendText($"   - Content: {btnProgramsCategory.Content}\n");
                    txtLog.AppendText($"   - Name: {btnProgramsCategory.Name}\n");
                }

                txtLog.AppendText($"📱 btnGamesCategory: {(btnGamesCategory != null ? "✅ BULUNDU" : "❌ BULUNAMADI")}\n");
                if (btnGamesCategory != null)
                {
                    txtLog.AppendText($"   - Content: {btnGamesCategory.Content}\n");
                    txtLog.AppendText($"   - Name: {btnGamesCategory.Name}\n");
                }

                txtLog.AppendText($"📱 btnToolsCategory: {(btnToolsCategory != null ? "✅ BULUNDU" : "❌ BULUNAMADI")}\n");
                if (btnToolsCategory != null)
                {
                    txtLog.AppendText($"   - Content: {btnToolsCategory.Content}\n");
                    txtLog.AppendText($"   - Name: {btnToolsCategory.Name}\n");
                }

                // Genel durum kontrolü
                int foundCount = 0;
                if (btnDriverCategory != null) foundCount++;
                if (btnProgramsCategory != null) foundCount++;
                if (btnGamesCategory != null) foundCount++;
                if (btnToolsCategory != null) foundCount++;

                txtLog.AppendText($"📊 Toplam: {foundCount}/4 buton bulundu\n");

                if (foundCount == 0)
                {
                    txtLog.AppendText("🚨 HİÇBİR BUTON BULUNAMADI! XAML'de Name attribute'ları kontrol edin\n");
                }
                else if (foundCount < 4)
                {
                    txtLog.AppendText("⚠️ Bazı butonlar bulunamadı! XAML Name attribute'larını kontrol edin\n");
                }
                else
                {
                    txtLog.AppendText("✅ Tüm kategori butonları başarıyla bulundu\n");
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ CheckButtonReferences hatası: {ex.Message}\n");
            }
        }
        public ListBox lstDrivers
        {
            get
            {
                if (_lstDrivers == null)
                {
                    // HATA BURADA: İki parametre gerekli - parent ve tag
                    _lstDrivers = FindElementByTag<ListBox>(this, "MainDriversList");
                }
                return _lstDrivers;
            }
        }

        private async void InitializeSystemInfo()
        {
            try
            {
                txtLog.AppendText("Sistem bilgileri yükleniyor...\n");

                // SystemInfoManager'ı başlat
                systemInfoManager = new SystemInfoManager(
                    txtOSInfo,      // XAML'de OS için TextBlock
                    txtGPUInfo,     // XAML'de GPU için TextBlock  
                    txtRAMInfo,     // XAML'de RAM için TextBlock
                    txtOSVersion    // XAML'de OS Version için TextBlock
                );

                // Sistem bilgilerini asenkron yükle
                await systemInfoManager.LoadSystemInfoAsync();

                txtLog.AppendText("Sistem bilgileri başarıyla yüklendi\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"Sistem bilgisi yükleme hatası: {ex.Message}\n");

                // Hata durumunda varsayılan değerler göster
                txtOSInfo.Text = "Windows";
                txtGPUInfo.Text = "Bilinmiyor";
                txtRAMInfo.Text = "Bilinmiyor";
                txtOSVersion.Text = "Bilinmiyor";
            }
        }

        private async Task SetYafesWallpaperAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    bool success = WallpaperManager.SetYafesWallpaper((message) =>
                    {
                        // UI thread'de log'a yaz
                        Dispatcher.Invoke(() => txtLog.AppendText(message + "\n"));
                    });
                });
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"Arkaplan ayarlama hatası: {ex.Message}\n");
            }
        }
        private void InitializeQueueManager()
        {
            queueManager = new InstallationQueueManager(
                activeInstallationsPanel,
                noActiveInstallationsText,
                txtLog
            );
            queueManager.Initialize();
        }
        // Gömülü kaynakları listele ve log'a yaz (DEBUG amaçlı)
        private void ListEmbeddedResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Gömülü kaynaklar:");
                foreach (var name in resourceNames)
                {
                    sb.AppendLine($"  - {name}");
                }

                // Debug konsola yaz
                Console.WriteLine(sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Gömülü kaynaklar listelenirken hata: {ex.Message}");
            }
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

        private void InitializeCategories()
        {
            try
            {
                // Button referanslarını Name ile al
                btnDriverCategory = FindName("btnDriverCategory") as Button;
                btnProgramsCategory = FindName("btnProgramsCategory") as Button;
                btnGamesCategory = FindName("btnGamesCategory") as Button;
                btnToolsCategory = FindName("btnToolsCategory") as Button;

                // Buton kontrolü
                if (btnDriverCategory == null || btnProgramsCategory == null ||
                    btnGamesCategory == null || btnToolsCategory == null)
                {
                    txtLog.AppendText("⚠️ Bazı kategori butonları bulunamadı!\n");
                }

                // ✅ DOĞRU TAG EŞLEŞMELERİ:
                if (btnProgramsCategory != null)
                    btnProgramsCategory.Tag = "Sürücüler";    // "📦 Drivers" butonu -> Sürücüler kategorisi
                if (btnDriverCategory != null)
                    btnDriverCategory.Tag = "Programlar";     // "🔧 Programs" butonu -> Programlar kategorisi

                // İLK AÇILIŞTA PROGRAMLAR KATEGORİSİNİ GÖSTER
                UpdateCategoryView("Programlar");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ InitializeCategories hatası: {ex.Message}\n");
            }
        }

        // Kategori butonu tıklama olayı
        // CategoryButton_Click metodunu GEÇİCİ OLARAK bu basit versiyonla değiştir:

        // Mevcut CategoryButton_Click metodunuzu bu ile değiştirin
        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button clickedButton = sender as Button;
                if (clickedButton == null) return;

                // Kurulum kontrol
                if (isInstalling)
                {
                    txtLog.AppendText("⚠️ Kurulum devam ediyor, kategori değişimi engellendi\\n");
                    return;
                }

                txtLog.AppendText($"🔘 Buton tıklandı: {clickedButton.Content}\\n");

                // GAMES BUTONU ÖZEL İŞLEMİ
                if (clickedButton == btnGamesCategory)
                {
                    txtLog.AppendText($"🎮 Games butonu tıklandı - Mevcut durum: {(isGamesVisible ? "AÇIK" : "KAPALI")}\\n");

                    if (!isGamesVisible)
                    {
                        // Panel kapalıysa aç
                        txtLog.AppendText("🔛 Games panel açılıyor...\\n");
                        ShowRealGamesPanel();
                        isGamesVisible = true;
                        SetSelectedCategory("Games");
                    }
                    else
                    {
                        // Panel açıksa kapat
                        txtLog.AppendText("🔴 Games panel kapatılıyor...\\n");
                        HideRealGamesPanel();
                        isGamesVisible = false;
                        SetSelectedCategory("Programlar"); // Varsayılan kategoriye dön
                    }

                    // ❌ GamesManager çağrılarını KALDIR - Conflict yaratabiliyor
                    // ESKİ ZARARLII KOD:
                    // if (gamesManager != null && gamesManager.IsGamesPanelVisible)
                    // {
                    //     gamesManager.HideGamesPanel();
                    // }
                }
                else
                {
                    // DİĞER BUTONLAR (Programs, Drivers, Tools)
                    txtLog.AppendText($"📦 Normal kategori butonu: {clickedButton.Content}\\n");

                    // Games açıksa kapat
                    if (isGamesVisible)
                    {
                        txtLog.AppendText("🔴 Games panel normal kategoriye geçiş için kapatılıyor...\\n");
                        HideRealGamesPanel();
                        isGamesVisible = false;
                    }

                    // Normal kategori geçişleri
                    if (clickedButton == btnDriverCategory)
                    {
                        UpdateCategoryView("Programlar");
                        SetSelectedCategory("Programlar");
                    }
                    else if (clickedButton == btnProgramsCategory)
                    {
                        UpdateCategoryView("Sürücüler");
                        SetSelectedCategory("Sürücüler");
                    }
                    else if (clickedButton == btnToolsCategory)
                    {
                        currentCategory = "Tools";
                        SetSelectedCategory("Tools");
                        lstDrivers.Items.Clear();
                        txtLog.AppendText("🔧 Tools kategorisi seçildi\\n");
                    }
                }

                txtLog.AppendText($"✅ Kategori işlemi tamamlandı\\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ CategoryButton_Click hatası: {ex.Message}\\n");
                txtLog.AppendText($"   Stack: {ex.StackTrace}\\n");
            }
        }

        /// <summary>
        /// ✅ DÜZELTME - Gerçek GamesPanel'i ANA CONTENT AREA'ya yerleştirir
        /// </summary>
        private async void ShowRealGamesPanel()
        {
            try
            {
                txtLog.AppendText("🎮 BAŞLAMA: ShowRealGamesPanel çalışıyor...\\n");

                // 1. Panel'leri bul
                var gamesPanel = FindElementByTag<Border>(this, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(this, "TerminalPanel");

                if (gamesPanel == null)
                {
                    txtLog.AppendText("❌ HATA: GamesPanel bulunamadı! Tag='GamesPanel' kontrolü\\n");
                    return;
                }

                if (terminalPanel == null)
                {
                    txtLog.AppendText("❌ HATA: TerminalPanel bulunamadı! Tag='TerminalPanel' kontrolü\\n");
                    return;
                }

                txtLog.AppendText("✅ Panel'ler bulundu\\n");

                // 2. Games Panel'i görünür yap
                gamesPanel.Visibility = Visibility.Visible;
                txtLog.AppendText("✅ GamesPanel.Visibility = Visible\\n");

                // 3. Terminal animasyonu
                var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                if (terminalTransform == null)
                {
                    terminalTransform = new TranslateTransform();
                    terminalPanel.RenderTransform = terminalTransform;
                }

                var terminalMoveAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 306,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // 4. Games Panel animasyonu
                var gamesPanelTransform = gamesPanel.RenderTransform as TranslateTransform;
                if (gamesPanelTransform == null)
                {
                    gamesPanelTransform = new TranslateTransform();
                    gamesPanel.RenderTransform = gamesPanelTransform;
                }

                var gamesPanelShowAnimation = new DoubleAnimation
                {
                    From = -50,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // 5. Opacity animasyonu
                var gamesPanelOpacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(400),
                    BeginTime = TimeSpan.FromMilliseconds(200)
                };

                txtLog.AppendText("🎬 Animasyonlar başlatılıyor...\\n");

                // 6. Animasyonları başlat
                terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                gamesPanelTransform.BeginAnimation(TranslateTransform.YProperty, gamesPanelShowAnimation);
                gamesPanel.BeginAnimation(UIElement.OpacityProperty, gamesPanelOpacityAnimation);

                // 7. Kategori listesini gizle
                lstDrivers.Visibility = Visibility.Collapsed;
                txtLog.AppendText("✅ Kategori listesi gizlendi\\n");

                // 8. Oyun verilerini yükle
                txtLog.AppendText("📊 Oyun verileri yükleniyor...\\n");
                await LoadRealGamesIntoXAMLPanel(gamesPanel);

                currentCategory = "Games";
                txtLog.AppendText("✅ BİTİŞ: Games panel tamamen açıldı ve yüklendi!\\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ ShowRealGamesPanel HATA: {ex.Message}\\n");
                txtLog.AppendText($"   Stack: {ex.StackTrace}\\n");
            }
        }
        /// <summary>
        /// ✅ YENİ - XAML Games Panel'ine gerçek oyun verilerini yükler
        /// </summary>
        private async Task LoadRealGamesIntoXAMLPanel(Border gamesPanel)
        {
            try
            {
                txtLog.AppendText("🔄 LoadRealGamesIntoXAMLPanel başlatıldı\\n");

                // YÖNTEM 1: Name ile bul
                var gamesGrid = FindChild<UniformGrid>(gamesPanel, "gamesGrid");
                txtLog.AppendText($"🔍 Name ile arama: {(gamesGrid != null ? "✅ BULUNDU" : "❌ BULUNAMADI")}\\n");

                // YÖNTEM 2: Tag ile bul (eğer Name çalışmazsa)
                if (gamesGrid == null)
                {
                    gamesGrid = FindElementByTag<UniformGrid>(gamesPanel, "gamesGrid");
                    txtLog.AppendText($"🔍 Tag ile arama: {(gamesGrid != null ? "✅ BULUNDU" : "❌ BULUNAMADI")}\\n");
                }

                // YÖNTEM 3: Type bazlı arama (son çare)
                if (gamesGrid == null)
                {
                    gamesGrid = FindChild<UniformGrid>(gamesPanel, null); // Name null = ilk UniformGrid'i bul
                    txtLog.AppendText($"🔍 Type bazlı arama: {(gamesGrid != null ? "✅ BULUNDU" : "❌ BULUNAMADI")}\\n");
                }

                // YÖNTEM 4: Visual Tree taraması (kesin çözüm)
                if (gamesGrid == null)
                {
                    gamesGrid = FindUniformGridInVisualTree(gamesPanel);
                    txtLog.AppendText($"🔍 Visual Tree tarama: {(gamesGrid != null ? "✅ BULUNDU" : "❌ BULUNAMADI")}\\n");
                }

                if (gamesGrid == null)
                {
                    txtLog.AppendText("❌ Tüm yöntemler başarısız! UniformGrid bulunamadı\\n");

                    // XAML yapısını debug et
                    txtLog.AppendText("🔍 XAML yapısı debug ediliyor...\\n");
                    DebugXAMLStructure(gamesPanel, 0);
                    return;
                }

                txtLog.AppendText($"✅ UniformGrid bulundu! Columns: {gamesGrid.Columns}\\n");

                // Gerçek oyun verilerini yükle
                txtLog.AppendText("📊 GameDataManager'dan oyunlar alınıyor...\\n");
                var games = await Yafes.Managers.GameDataManager.GetAllGamesAsync();

                if (games == null || games.Count == 0)
                {
                    txtLog.AppendText("⚠️ Oyun verisi bulunamadı - static kartları koru\\n");
                    return;
                }

                txtLog.AppendText($"✅ {games.Count} oyun bulundu\\n");

                // Mevcut kartları temizle
                int existingCount = gamesGrid.Children.Count;
                gamesGrid.Children.Clear();
                txtLog.AppendText($"🧹 {existingCount} mevcut kart temizlendi\\n");

                // Gerçek oyun kartlarını ekle (ilk 8)
                int addedCount = 0;
                foreach (var game in games.Take(8))
                {
                    var gameCard = CreateGameCard(game);
                    gamesGrid.Children.Add(gameCard);
                    addedCount++;
                }

                txtLog.AppendText($"✅ {addedCount} oyun kartı eklendi (Toplam oyun: {games.Count})\\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ LoadRealGamesIntoXAMLPanel HATA: {ex.Message}\\n");
                txtLog.AppendText($"   Stack: {ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))}...\\n");
                //                                                                      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                //                                                                      StackTrace null olabilir ama Length değil!
            }

        }
        private UniformGrid FindUniformGridInVisualTree(DependencyObject parent)
        {
            if (parent == null) return null;

            // Eğer bu element UniformGrid ise
            if (parent is UniformGrid uniformGrid)
            {
                return uniformGrid;
            }

            // Alt elementlerde ara
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindUniformGridInVisualTree(child);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// XAML yapısını debug eder - ne olduğunu anlayalım
        /// </summary>
        private void DebugXAMLStructure(DependencyObject parent, int depth)
        {
            if (parent == null || depth > 3) return; // Max 3 seviye

            string indent = new string(' ', depth * 2);
            string elementInfo = "";

            if (parent is FrameworkElement element)
            {
                elementInfo = $"{element.GetType().Name}";
                if (!string.IsNullOrEmpty(element.Name))
                    elementInfo += $" Name='{element.Name}'";
                if (element.Tag != null)
                    elementInfo += $" Tag='{element.Tag}'";
            }
            else
            {
                elementInfo = parent.GetType().Name;
            }

            txtLog.AppendText($"🔍 {indent}{elementInfo}\\n");

            // Alt elementleri de göster
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                DebugXAMLStructure(child, depth + 1);
            }
        }

        private T FindElementByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == name)
                {
                    return element;
                }

                var result = FindElementByName<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // İlk kontrol: tip eşleşmesi
                T childType = child as T;
                if (childType == null)
                {
                    // Recursive arama - alt elementlerde ara
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // Name kontrolü
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // Name belirtilmemişse, ilk bulunan tipte elemanı döndür
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        /// <summary>
        /// ✅ YENİ - Gerçek oyun verisi için kart oluşturur
        /// </summary>
        private Border CreateGameCard(Yafes.Models.GameData game)
        {
            var gameCard = new Border();

            // XAML'deki GameCardStyle'ı uygula
            gameCard.SetResourceReference(Border.StyleProperty, "GameCardStyle");

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Oyun ikonu (kategori bazlı emoji)
            var iconText = new TextBlock
            {
                Text = GetGameIcon(game.Category),
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Oyun adı
            var nameText = new TextBlock
            {
                Text = game.Name,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap // Uzun isimleri sarmalayacak
            };
            nameText.SetResourceReference(TextBlock.StyleProperty, "LambdaTextStyle");

            // Oyun boyutu veya kurulum durumu
            var statusText = new TextBlock
            {
                Text = game.IsInstalled ? "✅ Kurulu" : $"📥 {game.Size ?? "Bilinmiyor"}",
                FontSize = 8,
                Foreground = game.IsInstalled ?
                    Brushes.LightGreen :
                    new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // StackPanel'e elementleri ekle
            stackPanel.Children.Add(iconText);
            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(statusText);
            gameCard.Child = stackPanel;

            // Tıklama event'i ekle - oyun bilgilerini log'a yazsın
            gameCard.MouseLeftButtonDown += (s, e) => {
                txtLog.AppendText($"🎯 {game.Name} seçildi!\\n");
                txtLog.AppendText($"📂 Kategori: {game.Category} | Boyut: {game.Size ?? "Bilinmiyor"}\\n");
                if (game.IsInstalled)
                {
                    txtLog.AppendText($"✅ Kurulu - Son oynama: {game.LastPlayed}\\n");
                }
                else
                {
                    txtLog.AppendText($"📥 Kurulum gerekiyor - Setup: {game.SetupPath ?? "Belirtilmemiş"}\\n");
                }
            };

            return gameCard;
        }


        /// <summary>
        /// ✅ Ana content grid bulucu yardımcı method
        /// </summary>
        private Grid FindMainContentGrid(DependencyObject parent)
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Grid grid)
                {
                    // Ana grid olabilecek özellikleri kontrol et
                    if (grid.RowDefinitions.Count >= 2 && grid.ColumnDefinitions.Count >= 2)
                    {
                        return grid; // Muhtemelen ana layout grid'i
                    }
                }

                var result = FindMainContentGrid(child);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// ✅ Fallback - Overlay olarak göster
        /// </summary>
        private void ShowGamesPanelAsOverlay()
        {
            try
            {
                var gamesPanel = new Yafes.GameData.GamesPanel();
                gamesPanel.Width = 800;
                gamesPanel.Height = 400;
                gamesPanel.HorizontalAlignment = HorizontalAlignment.Center;
                gamesPanel.VerticalAlignment = VerticalAlignment.Center;
                gamesPanel.Background = new SolidColorBrush(Color.FromArgb(240, 30, 30, 35));

                // Ana Window'a overlay ekle
                var mainGrid = this.Content as Grid;
                if (mainGrid != null)
                {
                    Grid.SetRowSpan(gamesPanel, mainGrid.RowDefinitions.Count);
                    Grid.SetColumnSpan(gamesPanel, mainGrid.ColumnDefinitions.Count);
                    mainGrid.Children.Add(gamesPanel);
                    txtLog.AppendText("✅ GamesPanel overlay olarak eklendi\\n");
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ Overlay GamesPanel hatası: {ex.Message}\\n");
            }
        }
        private string GetGameIcon(string category)
        {
            return category?.ToLower() switch
            {
                "fps" => "🔫",
                "rpg" => "🗡️",
                "racing" => "🏎️",
                "action" => "⚔️",
                "adventure" => "🗺️",
                "strategy" => "♟️",
                "sports" => "⚽",
                "simulation" => "🎛️",
                "sandbox" => "🧱",
                "general" => "🎮",
                _ => "🎮"
            };
        }
        /// <summary>
        /// ✅ DÜZELTME - GamesPanel'i gizler ve LOG terminalini yukarı geri getir  
        /// </summary>
        private void HideRealGamesPanel()
        {
            try
            {
                txtLog.AppendText("🔴 BAŞLAMA: HideRealGamesPanel çalışıyor...\\n");

                var gamesPanel = FindElementByTag<Border>(this, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(this, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    txtLog.AppendText("❌ Panel'ler bulunamadı, gizleme iptal\\n");
                    return;
                }

                // Games panel'i gizle
                gamesPanel.Visibility = Visibility.Collapsed;
                txtLog.AppendText("✅ GamesPanel.Visibility = Collapsed\\n");

                // Terminal'i normale döndür
                var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                if (terminalTransform != null)
                {
                    var terminalMoveAnimation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(500),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                    txtLog.AppendText("✅ Terminal normal pozisyona döndürüldü\\n");
                }

                // Kategori listesini geri göster
                lstDrivers.Visibility = Visibility.Visible;
                txtLog.AppendText("✅ Kategori listesi geri gösterildi\\n");

                txtLog.AppendText("✅ BİTİŞ: Games panel tamamen gizlendi\\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ HideRealGamesPanel HATA: {ex.Message}\\n");
            }
        }
        private T FindElementByTag<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            return FindElementByTagRecursive<T>(parent, tag);
        }

        private T FindElementByTagRecursive<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Tag?.ToString() == tag)
                {
                    return element;
                }

                var result = FindElementByTagRecursive<T>(child, tag);
                if (result != null) return result;
            }
            return null;
        }

        public void AddLog(string message)
        {
            try
            {
                if (txtLog.Dispatcher.CheckAccess())
                {
                    txtLog.AppendText(message + "\n");
                    txtLog.ScrollToEnd();
                }
                else
                {
                    txtLog.Dispatcher.Invoke(() =>
                    {
                        txtLog.AppendText(message + "\n");
                        txtLog.ScrollToEnd();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddLog hatası: {ex.Message}");
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
                    Dispatcher.Invoke(() => txtLog.AppendText(message + "\n"));
                });

                // Progress bar'ı güncelle
                Dispatcher.Invoke(() =>
                {
                    progressBar.Value = 0;
                    progressBarStatus.Value = 0;
                    txtStatusBar.Text = "Opera kuruluyor ve şifreler import ediliyor...";
                });

                // Opera kurulum + şifre import işlemini başlat
                bool success = await operaManager.InstallOperaWithPasswordImport(installPath, program.InstallArguments);

                // Progress bar'ı tamamla
                Dispatcher.Invoke(() =>
                {
                    progressBar.Value = 100;
                    progressBarStatus.Value = 100;
                    txtStatusBar.Text = success ? "Opera kurulumu ve şifre import tamamlandı" : "Opera kurulumu tamamlandı";
                });

                if (!success)
                {
                    throw new Exception("Opera kurulum işlemi başarısız oldu.");
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"Opera kurulum hatası: {ex.Message}\n");
                throw; // Hatayı üst katmana ilet
            }
        }

        private void UpdateCategoryView(string category)
        {
            try
            {
                // Aynı kategoriye tekrar tıklandıysa hiçbir şey yapma
                if (currentCategory == category) return;

                // Mevcut seçimleri kaydet
                if (!string.IsNullOrEmpty(currentCategory))
                {
                    SaveCurrentSelections(currentCategory);
                }

                // Kategori değiştir
                currentCategory = category;
                RefreshListWithSavedSelections(category);
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ Kategori güncelleme hatası: {ex.Message}\\n");
            }
        }

        /// <summary>
        /// Tüm kategori butonlarını varsayılan (seçilmemiş) renge çevirir
        /// </summary>

        private void SetSelectedCategory(string selectedCategory)
        {
            try
            {
                // Renk tanımları
                var defaultColor = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Turuncu
                var selectedColor = Brushes.Black; // Siyah

                // Tüm butonları varsayılan renge çevir
                if (btnDriverCategory != null)
                {
                    btnDriverCategory.Foreground = defaultColor;
                    btnDriverCategory.Tag = "Programlar";
                }
                if (btnProgramsCategory != null)
                {
                    btnProgramsCategory.Foreground = defaultColor;
                    btnProgramsCategory.Tag = "Sürücüler";
                }
                if (btnGamesCategory != null)
                {
                    btnGamesCategory.Foreground = defaultColor;
                    btnGamesCategory.Tag = null;
                }
                if (btnToolsCategory != null)
                {
                    btnToolsCategory.Foreground = defaultColor;
                    btnToolsCategory.Tag = null;
                }

                // Seçili kategoriye göre renk değiştir
                switch (selectedCategory)
                {
                    case "Sürücüler":
                        if (btnProgramsCategory != null)
                        {
                            btnProgramsCategory.Tag = "Selected";
                            btnProgramsCategory.Foreground = selectedColor;
                        }
                        break;
                    case "Programlar":
                        if (btnDriverCategory != null)
                        {
                            btnDriverCategory.Tag = "Selected";
                            btnDriverCategory.Foreground = selectedColor;
                        }
                        break;
                    case "Games":
                        if (btnGamesCategory != null)
                        {
                            btnGamesCategory.Tag = "Selected";
                            btnGamesCategory.Foreground = selectedColor;
                        }
                        break;
                    case "Tools":
                        if (btnToolsCategory != null)
                        {
                            btnToolsCategory.Tag = "Selected";
                            btnToolsCategory.Foreground = selectedColor;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ Buton güncelleme hatası: {ex.Message}\\n");
            }
        }


        /// <summary>
        /// Buton görünümlerini güvenli şekilde günceller
        /// </summary>
        private void SafeUpdateButtonAppearance(string selectedCategory)
        {
            try
            {
                // Varsayılan renk
                var defaultColor = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Turuncu
                var selectedColor = Brushes.Black; // Siyah

                // Tüm butonları güvenli şekilde varsayılan renge çevir
                if (btnDriverCategory != null)
                {
                    btnDriverCategory.Foreground = defaultColor;
                    btnDriverCategory.Tag = "Programlar";
                }

                if (btnProgramsCategory != null)
                {
                    btnProgramsCategory.Foreground = defaultColor;
                    btnProgramsCategory.Tag = "Sürücüler";
                }

                if (btnGamesCategory != null)
                {
                    btnGamesCategory.Foreground = defaultColor;
                    btnGamesCategory.Tag = null;
                }

                if (btnToolsCategory != null)
                {
                    btnToolsCategory.Foreground = defaultColor;
                    btnToolsCategory.Tag = null;
                }

                // Seçili kategoriyi siyaha çevir
                switch (selectedCategory)
                {
                    case "Sürücüler":
                        if (btnProgramsCategory != null)
                        {
                            btnProgramsCategory.Foreground = selectedColor;
                            btnProgramsCategory.Tag = "Selected";
                        }
                        break;

                    case "Programlar":
                        if (btnDriverCategory != null)
                        {
                            btnDriverCategory.Foreground = selectedColor;
                            btnDriverCategory.Tag = "Selected";
                        }
                        break;

                    case "Games":
                        if (btnGamesCategory != null)
                        {
                            btnGamesCategory.Foreground = selectedColor;
                            btnGamesCategory.Tag = "Selected";
                        }
                        break;

                    case "Tools":
                        if (btnToolsCategory != null)
                        {
                            btnToolsCategory.Foreground = selectedColor;
                            btnToolsCategory.Tag = "Selected";
                        }
                        break;
                }

                txtLog.AppendText($"✅ Buton görünümleri güncellendi: {selectedCategory}\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ Buton görünüm güncelleme hatası: {ex.Message}\n");
            }
        }

        // Mevcut seçimleri sakla
        private void SaveCurrentSelections(string category)
        {
            Dictionary<string, bool> selections = new Dictionary<string, bool>();

            foreach (ListBoxItem item in lstDrivers.Items)
            {
                if (item.Content is CheckBox checkBox && checkBox.Content != null)
                {
                    string name = checkBox.Content.ToString();
                    bool isChecked = checkBox.IsChecked ?? false;
                    selections[name] = isChecked;
                }
            }

            categorySelections[category] = selections;
        }

        // Liste içeriğini kaydedilmiş seçimlerle yenile
        private void RefreshListWithSavedSelections(string category)
        {
            try
            {
                if (lstDrivers == null) return;

                lstDrivers.Items.Clear();

                Dictionary<string, bool> savedSelections = null;
                if (categorySelections.ContainsKey(category))
                {
                    savedSelections = categorySelections[category];
                }

                if (category == "Sürücüler")
                {
                    foreach (var driver in masterDrivers)
                    {
                        bool isChecked = true;
                        if (savedSelections != null && savedSelections.ContainsKey(driver.Name))
                        {
                            isChecked = savedSelections[driver.Name];
                        }

                        var checkBox = new CheckBox
                        {
                            Content = driver.Name,
                            IsChecked = isChecked,
                            Tag = driver,
                            Margin = new Thickness(5, 2, 5, 2),
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)),
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 11,
                            FontWeight = FontWeights.Bold
                        };

                        var item = new ListBoxItem();
                        item.Content = checkBox;
                        lstDrivers.Items.Add(item);
                    }
                }
                else if (category == "Programlar")
                {
                    foreach (var program in masterPrograms)
                    {
                        bool isChecked = true;
                        if (savedSelections != null && savedSelections.ContainsKey(program.Name))
                        {
                            isChecked = savedSelections[program.Name];
                        }

                        var checkBox = new CheckBox
                        {
                            Content = program.Name,
                            IsChecked = isChecked,
                            Tag = program,
                            Margin = new Thickness(5, 2, 5, 2),
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)),
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 11,
                            FontWeight = FontWeights.Bold
                        };

                        var item = new ListBoxItem();
                        item.Content = checkBox;
                        lstDrivers.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ Liste yenileme hatası: {ex.Message}\\n");
            }
        }


        private void UpdateDriverList()
        {
            lstDrivers.Items.Clear();
            foreach (var driver in drivers)
            {
                var checkBox = new CheckBox
                {
                    Content = driver.Name,
                    IsChecked = true,
                    Tag = driver,
                    Margin = new Thickness(5, 2, 5, 2),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                };

                var item = new ListBoxItem();
                item.Content = checkBox;
                lstDrivers.Items.Add(item);
            }
        }

        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (isInstalling)
            {
                MessageBox.Show("Kurulum zaten devam ediyor!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Log'u temizle ve başlangıç mesajını yaz
                txtLog.Clear();
                txtLog.AppendText("Yafes Kurulum Aracı başlatıldı\n");

                // ✅ ARKAPLAN DOSYASI KONTROLÜ VE AYARLAMA
                txtLog.AppendText("\n🎨 Arkaplan ayarlanıyor...\n");

                // Farklı dosya yollarını dene
                string[] possiblePaths = {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "GifIcons", "walpaper.jpg"),
            Path.Combine(Environment.CurrentDirectory, "Resources", "GifIcons", "walpaper.jpg"),
            @"C:\Users\Menesam\source\repos\Yafes\Resources\GifIcons\walpaper.jpg",
            Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Resources", "GifIcons", "walpaper.jpg")
        };

                string foundPath = null;
                foreach (string path in possiblePaths)
                {
                    txtLog.AppendText($"🔍 Kontrol ediliyor: {path}\n");
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        txtLog.AppendText($"✅ Dosya bulundu: {path}\n");
                        break;
                    }
                    else
                    {
                        txtLog.AppendText($"❌ Dosya bulunamadı: {path}\n");
                    }
                }

                if (foundPath != null)
                {
                    // WallpaperManager ile arkaplanı değiştir ve log mesajlarını göster
                    txtLog.AppendText("🔧 YAFES WallpaperManager ile arkaplan ayarlanıyor...\n");

                    await Task.Run(() =>
                    {
                        try
                        {
                            // WallpaperManager'ı logCallback ile kullan
                            bool success = WallpaperManager.SetWallpaper(foundPath, WallpaperManager.WallpaperStyle.Fill);

                            Dispatcher.Invoke(() =>
                            {
                                if (success)
                                {
                                    txtLog.AppendText($"✅ Arkaplan başarıyla değiştirildi!\n");
                                    txtLog.AppendText($"📁 Dosya: {Path.GetFileName(foundPath)}\n");
                                    txtLog.AppendText($"🎨 Stil: Fill (Doldur)\n");
                                }
                                else
                                {
                                    txtLog.AppendText($"❌ SystemParametersInfo başarısız oldu!\n");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => txtLog.AppendText($"❌ API Hatası: {ex.Message}\n"));
                        }
                    });
                }
                else
                {
                    txtLog.AppendText("❌ Hiçbir yolda arkaplan dosyası bulunamadı!\n");
                }

                txtLog.AppendText("\n🚀 Kurulum işlemleri başlatılıyor...\n");

                // Normal kurulum işlemlerine devam et
                PrepareInstallation();
            }
            catch (Exception ex)
            {
                isInstalling = false;
                btnInstall.IsEnabled = true;
                btnAddDriver.IsEnabled = true;
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // TEK PrepareInstallation METODU - Düzeltilmiş versiyon
        private void PrepareInstallation()
        {
            // Butonları devre dışı bırak
            btnInstall.IsEnabled = false;
            btnAddDriver.IsEnabled = false;

            isInstalling = true;
            currentDriverIndex = 0;
            currentProgramIndex = 0;

            // Mevcut kategorideki seçimleri kaydet
            SaveCurrentSelections(currentCategory);

            // Kurulacak sürücü ve program listelerini kopya olarak hazırla
            drivers = new List<DriverInfo>();
            programs = new List<ProgramInfo>();

            // Seçili kategoriye göre sadece işaretli öğeleri ekle
            if (currentCategory == "Sürücüler")
            {
                foreach (ListBoxItem item in lstDrivers.Items)
                {
                    if (item.Content is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Tag is DriverInfo driver)
                    {
                        drivers.Add(driver);
                    }
                }
            }
            else if (currentCategory == "Programlar")
            {
                foreach (ListBoxItem item in lstDrivers.Items)
                {
                    if (item.Content is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Tag is ProgramInfo program)
                    {
                        programs.Add(program);
                    }
                }
            }

            // KUYRUK YÖNETİCİSİNDE KUYRUĞU OLUŞTUR - YENİ EKLENDİ
            queueManager.CreateQueue(drivers, programs);

            txtLog.AppendText($"ADIM 1: Toplam {drivers.Count + programs.Count} kurulum başlatılıyor...\n");

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
                txtLog.AppendText("Kurulacak öğe bulunamadı. İşlem tamamlandı.\n");
                CompleteInstallation();
            }
        }

        private async void StartNextDriverInstallation()
        {
            if (currentDriverIndex >= drivers.Count)
            {
                txtLog.AppendText("Tüm sürücü kurulumları tamamlandı!\n");
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
                    txtLog.AppendText($"Gömülü kaynaktan {currentDriver.Name} çıkarıldı\n");
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
                        txtLog.AppendText($"Alternatif klasörde sürücü bulundu: {alternativeFilePath}\n");
                        await InstallDriver(currentDriver, alternativeFilePath);
                    }
                    else
                    {
                        txtLog.AppendText($"Alternatif klasörde sürücü bulunamadı! Desen: {currentDriver.AlternativeSearchPattern}\n");
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
            txtLog.AppendText("\nADIM 2: Program kurulumları başlatılıyor...\n");
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
                txtLog.AppendText("Tüm program kurulumları tamamlandı!\n");
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
                    txtLog.AppendText($"Gömülü kaynaktan {currentProgram.Name} çıkarıldı\n");
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
                        txtLog.AppendText($"Alternatif klasörde program bulundu: {alternativeFilePath}\n");
                        await InstallProgram(currentProgram, alternativeFilePath);
                    }
                    else
                    {
                        txtLog.AppendText($"Alternatif klasörde program bulunamadı! Desen: {currentProgram.AlternativeSearchPattern}\n");
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
            btnInstall.IsEnabled = true;
            btnAddDriver.IsEnabled = true;

            progressBar.Value = 100;
            progressBarStatus.Value = 100;
            txtStatusBar.Text = "Tüm kurulumlar tamamlandı";

            // KUYRUK YÖNETİCİSİNİ DURDUR - YENİ EKLENDİ
            queueManager.Stop();

            txtLog.AppendText("\n*** TÜM KURULUMLAR TAMAMLANDI! ***\n");

            // ❌ BU SATIRI KALDIR - Liste siliniyor!
            // RefreshListWithSavedSelections(currentCategory);

            // ✅ Bunun yerine sadece kategori görünümünü güncelle AMA kuyruk panelini DOKUNMA
            // Kuyruk paneli kurulumları göstermeye devam etsin!

            if (chkRestart.IsChecked == true)
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
                        txtLog.AppendText($"Kaynak bulunamadı: {resourceName}\n");
                        return false;
                    }

                    long expectedSize = resourceStream.Length;
                    txtLog.AppendText($"Kaynak boyutu: {expectedSize / 1024} KB\n");

                    // Dosya içeriğini buffer'a oku ve dosyaya yaz
                    using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long totalBytesRead = 0;
                        long totalBytes = resourceStream.Length;

                        // İlerleme çubuğunu başlat
                        Dispatcher.Invoke(() =>
                        {
                            progressBar.Value = 0;
                            progressBarStatus.Value = 0;
                            txtStatusBar.Text = $"Gömülü kaynak çıkartılıyor... %0";
                        });

                        // Dosyayı oku ve yaz
                        while ((bytesRead = await resourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);

                            totalBytesRead += bytesRead;

                            // İlerleme durumunu güncelle
                            int progressPercentage = (int)((double)totalBytesRead / totalBytes * 100);

                            Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = progressPercentage;
                                progressBarStatus.Value = progressPercentage;
                                txtStatusBar.Text = $"Gömülü kaynak çıkartılıyor... %{progressPercentage}";
                            });
                        }
                    }

                    // Dosya boyutunu kontrol et
                    FileInfo fileInfo = new FileInfo(outputFilePath);
                    if (fileInfo.Length < expectedSize * 0.9) // En az beklenen boyutun %90'ı olmalı
                    {
                        txtLog.AppendText($"UYARI: Çıkarılan dosya eksik olabilir. Beklenen: {expectedSize / 1024} KB, Gerçek: {fileInfo.Length / 1024} KB\n");
                        return false;
                    }

                    // Başarıyla tamamlandı
                    txtLog.AppendText($"Dosya başarıyla çıkarıldı: {fileInfo.Length / 1024} KB\n");
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Hata oluşursa consola yaz ve başarısız olarak dön
                txtLog.AppendText($"[HATA] Gömülü kaynak çıkartma hatası: {ex.Message}\n");
                Console.WriteLine($"[ERROR] Gömülü kaynak çıkartma hatası: {ex.Message}");
                return false;
            }
        }

        // Tüm log bilgisini güncelle - hem sürücüler hem de programlar için
        private void UpdateFormattedLogs()
        {
            UpdateCombinedLogs();
        }

        // Hem sürücüleri hem programları içeren kombine log
        private void UpdateCombinedLogs()
        {
            // Tüm log içeriğini temizle
            txtLog.Clear();
            txtLog.AppendText("Yafes Kurulum Aracı başlatıldı\n");

            // Sürücü bölümünü ekle
            if (drivers.Count > 0)
            {
                txtLog.AppendText("\n=== SÜRÜCÜLER ===\n");

                // Her sürücü için formatlanmış log bilgisini ekle
                for (int i = 0; i < drivers.Count; i++)
                {
                    DriverInfo driver = drivers[i];
                    string status = driverStatusMap.ContainsKey(driver.Name) ? driverStatusMap[driver.Name] : "Bekliyor";

                    txtLog.AppendText($"{i + 1}. {driver.Name} kurulumu\n");

                    if (status == "Başarılı")
                    {
                        txtLog.AppendText($"   - İndiriliyor... Tamamlandı\n");
                        if (driver.IsZip)
                            txtLog.AppendText($"   - Çıkartılıyor... Tamamlandı\n");
                        txtLog.AppendText($"   - Kuruluyor... Tamamlandı\n");
                        txtLog.AppendText($"   - Durum: Başarılı ✓\n");
                    }
                    else if (status == "İndiriliyor")
                    {
                        txtLog.AppendText($"   - İndiriliyor... \n");
                        txtLog.AppendText($"   - Durum: İşlem devam ediyor\n");
                    }
                    else if (status == "Çıkartılıyor")
                    {
                        txtLog.AppendText($"   - İndiriliyor... Tamamlandı\n");
                        txtLog.AppendText($"   - Çıkartılıyor... \n");
                        txtLog.AppendText($"   - Durum: İşlem devam ediyor\n");
                    }
                    else if (status == "Kuruluyor")
                    {
                        txtLog.AppendText($"   - İndiriliyor... Tamamlandı\n");
                        if (driver.IsZip)
                            txtLog.AppendText($"   - Çıkartılıyor... Tamamlandı\n");
                        txtLog.AppendText($"   - Kuruluyor... \n");
                        txtLog.AppendText($"   - Durum: İşlem devam ediyor\n");
                    }
                    else if (status == "Alternatiften Kurulum")
                    {
                        txtLog.AppendText($"   - Alternatif klasörden yükleniyor... \n");
                        txtLog.AppendText($"   - Durum: İşlem devam ediyor\n");
                    }
                    else if (status == "Hata")
                    {
                        txtLog.AppendText($"   - Durum: Hata ✗\n");
                    }
                    else
                    {
                        txtLog.AppendText($"   - Durum: {status}\n");
                    }
                }
            }

            // Program bölümünü ekle
            if (programs.Count > 0)
            {
                txtLog.AppendText("\n=== PROGRAMLAR ===\n");

                // Her program için formatlanmış log bilgisini ekle
                for (int i = 0; i < programs.Count; i++)
                {
                    ProgramInfo program = programs[i];
                    string status = programStatusMap.ContainsKey(program.Name) ? programStatusMap[program.Name] : "Bekliyor";

                    txtLog.AppendText($"{i + 1}. {program.Name} kurulumu\n");

                    if (status == "Başarılı")
                    {
                        txtLog.AppendText($"   - İndiriliyor... Tamamlandı\n");
                        if (program.IsZip)
                            txtLog.AppendText($"   - Çıkartılıyor... Tamamlandı\n");
                        txtLog.AppendText($"   - Kuruluyor... Tamamlandı\n");
                        txtLog.AppendText($"   - Durum: Başarılı ✓\n");
                    }
                    else if (status == "İndiriliyor")
                    {
                        txtLog.AppendText($"   - İndiriliyor... \n");
                        txtLog.AppendText($"   - Durum: İşlem devam ediyor\n");
                    }
                    else if (status == "Çıkartılıyor")
                    {
                        txtLog.AppendText($"   - İndiriliyor... Tamamlandı\n");
                        txtLog.AppendText($"   - Çıkartılıyor... \n");
                        txtLog.AppendText($"   - Durum: İşlem devam ediyor\n");
                    }
                    else if (status == "Kuruluyor")
                    {
                        txtLog.AppendText($"   - İndiriliyor... Tamamlandı\n");
                        if (program.IsZip)
                            txtLog.AppendText($"   - Çıkartılıyor... Tamamlandı\n");
                        txtLog.AppendText($"   - Kuruluyor... \n");
                        txtLog.AppendText($"   - Durum: İşlem devam ediyor\n");
                    }
                    else if (status == "Alternatiften Kurulum")
                    {
                        txtLog.AppendText($"   - Alternatif klasörden yükleniyor... \n");
                        txtLog.AppendText($"   - Durum: İşlem devam ediyor\n");
                    }
                    else if (status == "Hata")
                    {
                        txtLog.AppendText($"   - Durum: Hata ✗\n");
                    }
                    else
                    {
                        txtLog.AppendText($"   - Durum: {status}\n");
                    }
                }
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
                    txtLog.AppendText($"Alternatif sürücü klasörü bulunamadı: {alternativeDriversFolder}\n");
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
                txtLog.AppendText($"Alternatif klasörde arama hatası: {ex.Message}\n");
                return null;
            }
        }

        private async Task DownloadDriver(DriverInfo driver, string filePath)
        {
            try
            {
                txtStatusBar.Text = $"{driver.Name} indiriliyor...";
                progressBarStatus.Value = 0;

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
                                    progressBar.Value = progressPercentage;
                                    progressBarStatus.Value = progressPercentage;
                                    txtStatusBar.Text = $"{driver.Name} indiriliyor... %{progressPercentage}";
                                    lastPercentage = progressPercentage;
                                }
                                else
                                {
                                    // Daima progress bar'ı güncelle ama log'a yazma
                                    progressBar.Value = progressPercentage;
                                    progressBarStatus.Value = progressPercentage;
                                    txtStatusBar.Text = $"{driver.Name} indiriliyor... %{progressPercentage}";
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
                UpdateFormattedLogs();
                txtLog.AppendText($"İndirme hatası: {ex.Message}\n");
                HandleDriverError();
            }
        }

        private async Task InstallDriver(DriverInfo driver, string filePath)
        {
            try
            {
                txtStatusBar.Text = $"{driver.Name} kuruluyor...";
                progressBar.Value = 0;

                string installPath = filePath;
                string extractPath = "";

                // Eğer ZIP dosyası ise, çıkart
                if (driver.IsZip)
                {
                    // Durum güncelle - Çıkartılıyor
                    driverStatusMap[driver.Name] = "Çıkartılıyor";
                    UpdateFormattedLogs();

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
                        UpdateFormattedLogs();
                        txtLog.AppendText("Kurulum dosyası bulunamadı!\n");
                        HandleDriverError();
                        return;
                    }
                }

                // Durumu güncelle - Kuruluyor
                driverStatusMap[driver.Name] = "Kuruluyor";
                UpdateFormattedLogs();

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
                    UpdateFormattedLogs();
                    txtLog.AppendText("Kurulum işlemi başlatılamadı!\n");
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

                        Dispatcher.Invoke(() =>
                        {
                            progressBar.Value = progress;
                            progressBarStatus.Value = progress;
                            txtStatusBar.Text = $"{driver.Name} kuruluyor... %{progress}";
                        });

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
                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = 100;
                        progressBarStatus.Value = 100;
                        txtStatusBar.Text = $"{driver.Name} kurulumu tamamlandı";
                    });
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
                txtLog.AppendText($"Kurulum hatası: {ex.Message}\n");

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
                    txtLog.AppendText($"Alternatif program klasörü bulunamadı: {alternativeProgramsFolder}\n");
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
                txtLog.AppendText($"Alternatif klasörde arama hatası: {ex.Message}\n");
                return null;
            }
        }

        // Program indirme işlemi
        private async Task DownloadProgram(ProgramInfo program, string filePath)
        {
            try
            {
                txtStatusBar.Text = $"{program.Name} indiriliyor...";
                progressBarStatus.Value = 0;

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
                                    progressBar.Value = progressPercentage;
                                    progressBarStatus.Value = progressPercentage;
                                    txtStatusBar.Text = $"{program.Name} indiriliyor... %{progressPercentage}";
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
                UpdateFormattedLogs();
                txtLog.AppendText($"İndirme hatası: {ex.Message}\n");
                HandleProgramError();
            }
        }

        // Program kurulum işlemi - TAM VERSİYON
        private async Task InstallProgram(ProgramInfo program, string filePath)
        {
            try
            {
                txtStatusBar.Text = $"{program.Name} kuruluyor...";
                progressBar.Value = 0;

                string installPath = filePath;
                string extractPath = "";

                // Eğer ZIP dosyası ise, çıkart
                if (program.IsZip)
                {
                    // Durum güncelle - Çıkartılıyor
                    programStatusMap[program.Name] = "Çıkartılıyor";
                    UpdateFormattedLogs();

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
                        UpdateFormattedLogs();
                        txtLog.AppendText("Kurulum dosyası bulunamadı!\n");
                        HandleProgramError();
                        return;
                    }
                }

                // Durumu güncelle - Kuruluyor
                programStatusMap[program.Name] = "Kuruluyor";
                UpdateFormattedLogs();

                // ÖZEL KURULUM KONTROLLERİ

                // WinRAR için özel kurulum
                if (program.SpecialInstallation && program.Name == "WinRAR")
                {
                    await InstallWinRARWithPowerShell(program, installPath);
                }
                // Driver Booster için özel kurulum
              
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
                        UpdateFormattedLogs();
                        txtLog.AppendText("Kurulum işlemi başlatılamadı!\n");
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

                            Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = progress;
                                progressBarStatus.Value = progress;
                                txtStatusBar.Text = $"{program.Name} kuruluyor... %{progress}";
                            });

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
                        Dispatcher.Invoke(() =>
                        {
                            progressBar.Value = 100;
                            progressBarStatus.Value = 100;
                            txtStatusBar.Text = $"{program.Name} kurulumu tamamlandı";
                        });
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
                txtLog.AppendText($"Kurulum hatası: {ex.Message}\n");

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
                txtLog.AppendText($"WinRAR için özel CMD kurulumu başlatılıyor...\n");

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
                    Dispatcher.Invoke(() => {
                        txtLog.AppendText("WinRAR kurulum işlemi devam ediyor, tamamlanması bekleniyor...\n");
                    });

                    System.Threading.Thread.Sleep(15000); // 15 saniye bekle (kurulum için yeterli süre)

                    // WinRAR'ın kurulup kurulmadığını kontrol et
                    bool isWinRarInstalled = Directory.Exists(@"C:\Program Files\WinRAR") ||
                                            Directory.Exists(@"C:\Program Files (x86)\WinRAR");

                    Dispatcher.Invoke(() => {
                        if (isWinRarInstalled)
                        {
                            txtLog.AppendText("WinRAR kurulumu başarıyla tamamlandı!\n");
                        }
                        else
                        {
                            txtLog.AppendText("WinRAR kurulumu tamamlandı, ancak kurulum klasörü bulunamadı.\n");
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
                });
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"WinRAR CMD kurulum hatası: {ex.Message}\n");
                throw; // Hatayı üst katmana ilet
            }
        }

        // Geliştirilmiş dosya kopyalama metodu - Debug bilgileri ile
        private async Task CopySpecialFileFromResources(string resourceFileName, string targetPath)
        {
            try
            {
                txtLog.AppendText($"Dosya kopyalama başlatılıyor: {resourceFileName}\n");
                txtLog.AppendText($"Hedef yol: {targetPath}\n");

                // Hedef klasörün var olduğundan emin ol
                string? targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    txtLog.AppendText($"Hedef klasör mevcut değil, oluşturuluyor: {targetDir}\n");
                    Directory.CreateDirectory(targetDir);
                    txtLog.AppendText($"✓ Hedef klasör oluşturuldu: {targetDir}\n");
                }
                else if (!string.IsNullOrEmpty(targetDir))
                {
                    txtLog.AppendText($"✓ Hedef klasör zaten mevcut: {targetDir}\n");
                }

                // Assembly'den kaynağı bul
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"Yafes.Resources.{resourceFileName}";

                txtLog.AppendText($"Aranan kaynak: {resourceName}\n");

                // Önce kaynağın varlığını kontrol et
                var allResources = assembly.GetManifestResourceNames();
                bool resourceFound = false;
                string actualResourceName = resourceName;

                foreach (var res in allResources)
                {
                    if (res.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        resourceFound = true;
                        actualResourceName = res;
                        txtLog.AppendText($"✓ Kaynak bulundu: {res}\n");
                        break;
                    }
                }

                // Eğer tam eşleşme bulunamazsa, içeriyor mu kontrol et
                if (!resourceFound)
                {
                    foreach (var res in allResources)
                    {
                        if (res.Contains(resourceFileName.Replace(".", "")) || res.EndsWith(resourceFileName))
                        {
                            resourceFound = true;
                            actualResourceName = res;
                            txtLog.AppendText($"✓ Alternatif kaynak bulundu: {res}\n");
                            break;
                        }
                    }
                }

                // Hala bulunamadıysa hata ver
                if (!resourceFound)
                {
                    txtLog.AppendText("✗ Kaynak bulunamadı! Mevcut kaynaklar:\n");
                    foreach (var res in allResources)
                    {
                        txtLog.AppendText($"  - {res}\n");
                    }
                    throw new Exception($"Kaynak dosyası bulunamadı: {resourceName}");
                }

                // Kaynağı stream olarak al
                using (Stream? resourceStream = assembly.GetManifestResourceStream(actualResourceName))
                {
                    if (resourceStream == null)
                    {
                        throw new Exception($"Kaynak stream'i alınamadı: {actualResourceName}");
                    }

                    txtLog.AppendText($"Kaynak boyutu: {resourceStream.Length} bayt\n");

                    // Dosyayı hedef yere kopyala
                    using (FileStream fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        await resourceStream.CopyToAsync(fileStream);
                    }
                }

                // Kopyalanan dosyayı doğrula
                if (File.Exists(targetPath))
                {
                    var fileInfo = new FileInfo(targetPath);
                    txtLog.AppendText($"✓ Dosya başarıyla kopyalandı!\n");
                    txtLog.AppendText($"  Hedef: {targetPath}\n");
                    txtLog.AppendText($"  Boyut: {fileInfo.Length} bayt\n");
                    txtLog.AppendText($"  Oluşturma zamanı: {fileInfo.CreationTime}\n");
                }
                else
                {
                    throw new Exception("Dosya kopyalandı ancak hedef yolda bulunamıyor!");
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"✗ Dosya kopyalama hatası ({resourceFileName}): {ex.Message}\n");
                throw;
            }
        }

       
        // Program kurulum hatası yönetimi
        private void HandleProgramError()
        {
            // Hata durumunda bir sonraki programa geç
            currentProgramIndex++;
            StartNextProgramInstallation();
        }

        private void btnFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentCategory == "Sürücüler")
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
                else if (currentCategory == "Programlar")
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
                txtLog.AppendText("Klasör açılırken hata: " + ex.Message + "\n");
            }
        }

        private void btnAddDriver_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Kurulum Dosyaları (*.exe;*.msi;*.zip)|*.exe;*.msi;*.zip|Tüm Dosyalar (*.*)|*.*";

                if (currentCategory == "Sürücüler")
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
                        txtLog.AppendText($"Dosya başarıyla kopyalandı: {destPath}\n");

                        // Yeni sürücüyü listeye ekle
                        drivers.Add(newDriver);
                        driverStatusMap[newDriver.Name] = "Bekliyor";

                        // Sürücü listesini güncelle
                        var checkBox = new CheckBox
                        {
                            Content = driverName,
                            IsChecked = true,
                            Tag = newDriver
                        };
                        var item = new ListBoxItem();
                        item.Content = checkBox;
                        lstDrivers.Items.Add(item);
                    }
                }
                else if (currentCategory == "Programlar")
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
                        txtLog.AppendText($"Dosya başarıyla kopyalandı: {destPath}\n");

                        // Yeni programı listeye ekle
                        programs.Add(newProgram);
                        programStatusMap[newProgram.Name] = "Bekliyor";

                        // Program listesini güncelle
                        var checkBox = new CheckBox
                        {
                            Content = programName,
                            IsChecked = true,
                            Tag = newProgram
                        };
                        var item = new ListBoxItem();
                        item.Content = checkBox;
                        lstDrivers.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText("Dosya ekleme hatası: " + ex.Message + "\n");
            }
        }

        private void chkRestart_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkRestart.IsChecked == true)
            {
                txtLog.AppendText("Kurulum sonrası yeniden başlatma seçeneği aktif edildi\n");
            }
            else
            {
                txtLog.AppendText("Kurulum sonrası yeniden başlatma seçeneği kapatıldı\n");
            }
        }

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
        // Main.xaml.cs dosyasına eklenecek method'lar:

        // Window drag functionality
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // Minimize window
        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Close window
        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void txtLog_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}