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

namespace Yafes
{
    public partial class Main : Window
    {
        private GamesPanelManager gamesPanelManager;
        private bool isGamesVisible = false;
        private ListBox _lstDrivers;
        private SystemInfoManager systemInfoManager;
        private bool _driversMessageShown = false;
        private bool _programsMessageShown = false;

        // InstallationManager ile değiştirilecek alanlar
        private InstallationManager installationManager;
        private readonly HttpClient httpClient = new HttpClient();

        // Model listeleri - artık Yafes.Managers namespace'inden
        private List<Yafes.Managers.DriverInfo> drivers = new List<Yafes.Managers.DriverInfo>();
        private List<Yafes.Managers.ProgramInfo> programs = new List<Yafes.Managers.ProgramInfo>();
        private Dictionary<string, Dictionary<string, bool>> categorySelections = new Dictionary<string, Dictionary<string, bool>>();

        // Ana listeler - değişmeyecek kaynak listeler
        private List<Yafes.Managers.DriverInfo> masterDrivers = new List<Yafes.Managers.DriverInfo>();
        private List<Yafes.Managers.ProgramInfo> masterPrograms = new List<Yafes.Managers.ProgramInfo>();

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

                // Ana listeleri oluştur
                masterDrivers = new List<Yafes.Managers.DriverInfo>();
                masterPrograms = new List<Yafes.Managers.ProgramInfo>();
                drivers = new List<Yafes.Managers.DriverInfo>();
                programs = new List<Yafes.Managers.ProgramInfo>();

                // Sürücü ve program bilgilerini ekle
                InitializeDrivers();
                InitializePrograms();

                // KUYRUK YÖNETİCİSİNİ BAŞLAT
                InitializeQueueManager();

                // ✅ SİSTEM BİLGİSİ YÖNETİCİSİNİ BAŞLAT
                InitializeSystemInfo();

                // ✅ INSTALLATION MANAGER'I BAŞLAT
                InitializeInstallationManager();

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

        /// <summary>
        /// InstallationManager'ı başlatır
        /// </summary>
        private void InitializeInstallationManager()
        {
            try
            {
                // InstallationManager'ı dependency injection ile oluştur
                installationManager = new InstallationManager(
                    progressBar,
                    progressBarStatus,
                    txtStatusBar,
                    this.Dispatcher,
                    queueManager
                );

                // Event'leri bağla
                installationManager.LogMessage += InstallationManager_LogMessage;
                installationManager.ProgressChanged += InstallationManager_ProgressChanged;
                installationManager.InstallationComplete += InstallationManager_InstallationComplete;

                txtLog.AppendText("✅ InstallationManager başarıyla başlatıldı\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ InstallationManager başlatma hatası: {ex.Message}\n");
            }
        }

        /// <summary>
        /// InstallationManager'dan gelen log mesajları
        /// </summary>
        private void InstallationManager_LogMessage(object sender, string message)
        {
            AddLog(message);
        }

        /// <summary>
        /// InstallationManager'dan gelen progress güncellemeleri
        /// </summary>
        private void InstallationManager_ProgressChanged(object sender, ProgressEventArgs e)
        {
            // Progress bar zaten InstallationManager tarafından güncelleniyor
            // Burada ek işlemler yapılabilir
        }

        /// <summary>
        /// Kurulum tamamlandığında çalışır
        /// </summary>
        private void InstallationManager_InstallationComplete(object sender, InstallationCompleteEventArgs e)
        {
            try
            {
                // Kurulum istatistiklerini göster
                string stats = $"Kurulum Tamamlandı!\n" +
                              $"Toplam Sürücü: {e.TotalDrivers} (Başarılı: {e.SuccessfulDrivers}, Başarısız: {e.FailedDrivers})\n" +
                              $"Toplam Program: {e.TotalPrograms} (Başarılı: {e.SuccessfulPrograms}, Başarısız: {e.FailedPrograms})\n" +
                              $"Tamamlanma Zamanı: {e.CompletionTime:HH:mm:ss}";

                AddLog(stats);

                // Butonları tekrar aktif et
                btnInstall.IsEnabled = true;
                btnAddDriver.IsEnabled = true;

                // Yeniden başlatma kontrolü
                if (chkRestart.IsChecked == true)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Kurulum tamamlandı. Bilgisayarı yeniden başlatmak istiyor musunuz?",
                        "Yeniden Başlat",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start("shutdown", "/r /t 10");
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Kurulum tamamlama işlemi hatası: {ex.Message}");
            }
        }

        private void InitializeGamesPanelManager()
        {
            try
            {
                // GamesPanelManager'ı başlat
                gamesPanelManager = new GamesPanelManager(this, txtLog);
                txtLog.AppendText("🎮 GamesPanelManager başarıyla başlatıldı\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ GamesPanelManager başlatma hatası: {ex.Message}\n");
                gamesPanelManager = null;
            }
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // UI tam yüklendikten sonra kategori sistemini başlat
                InitializeCategories();

                // ✅ YENİ: GamesPanelManager'ı başlat
                InitializeGamesPanelManager();

                // Varsayılan kategoriyi ayarla
                currentCategory = "Programlar";
                SetSelectedCategory("Programlar");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"⚠️ Yükleme hatası: {ex.Message}\n");
            }
        }

        public ListBox lstDrivers
        {
            get
            {
                if (_lstDrivers == null)
                {
                    _lstDrivers = FindElementByTag<ListBox>(this, "MainDriversList");
                }
                return _lstDrivers;
            }
        }

        private T FindElementByTag<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            return FindElementByTagRecursive<T>(parent, tag);
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

        private void InitializeQueueManager()
        {
            queueManager = new InstallationQueueManager(
                activeInstallationsPanel,
                noActiveInstallationsText,
                txtLog
            );
            queueManager.Initialize();
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
            masterDrivers.Add(new Yafes.Managers.DriverInfo
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

            masterDrivers.Add(new Yafes.Managers.DriverInfo
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

            masterDrivers.Add(new Yafes.Managers.DriverInfo
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
        }

        private void InitializePrograms()
        {
            // Önce listeleri tamamen temizle
            masterPrograms.Clear();
            programs.Clear();

            // Log ekle
            Console.WriteLine("Program listesi yükleniyor...");

            // Programları ekle
            masterPrograms.Add(new Yafes.Managers.ProgramInfo
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

            masterPrograms.Add(new Yafes.Managers.ProgramInfo
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

            masterPrograms.Add(new Yafes.Managers.ProgramInfo
            {
                Name = "Opera",
                Url = "https://www.opera.com/tr/computer/thanks?ni=stable&os=windows",
                FileName = "OperaSetup.exe",
                ProcessName = "opera",
                InstallArguments = "--silent --installfolder=\"C:\\Program Files\\Opera\"",
                IsZip = false,
                AlternativeSearchPattern = "opera*.exe",
                ResourceName = "Yafes.Resources.OperaSetup.exe",
                SpecialInstallation = true
            });

            // Diğer programlar...
            masterPrograms.Add(new Yafes.Managers.ProgramInfo
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

            // Program sayısını logla
            Console.WriteLine($"Toplam {masterPrograms.Count} program yüklendi.");
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

        /// <summary>
        /// Ana kurulum butonu - artık InstallationManager kullanıyor
        /// </summary>
        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (installationManager?.IsInstalling == true)
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
                await SetYafesWallpaperAsync();

                txtLog.AppendText("\n🚀 Kurulum işlemleri başlatılıyor...\n");

                // Butonları devre dışı bırak
                btnInstall.IsEnabled = false;
                btnAddDriver.IsEnabled = false;

                // Mevcut kategorideki seçimleri kaydet
                SaveCurrentSelections(currentCategory);

                // Seçili öğeleri topla
                var selectedDrivers = new List<Yafes.Managers.DriverInfo>();
                var selectedPrograms = new List<Yafes.Managers.ProgramInfo>();

                if (currentCategory == "Sürücüler")
                {
                    foreach (ListBoxItem item in lstDrivers.Items)
                    {
                        if (item.Content is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Tag is Yafes.Managers.DriverInfo driver)
                        {
                            selectedDrivers.Add(driver);
                        }
                    }
                }
                else if (currentCategory == "Programlar")
                {
                    foreach (ListBoxItem item in lstDrivers.Items)
                    {
                        if (item.Content is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Tag is Yafes.Managers.ProgramInfo program)
                        {
                            selectedPrograms.Add(program);
                        }
                    }
                }

                // InstallationManager ile kurulumu başlat
                installationManager.PrepareInstallation(selectedDrivers, selectedPrograms);
            }
            catch (Exception ex)
            {
                btnInstall.IsEnabled = true;
                btnAddDriver.IsEnabled = true;
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // UI metodları (kategori, görünüm, vs.) - bunlar değişmeden kalacak
        private void InitializeCategories()
        {
            try
            {
                // Button referanslarını Name ile al
                btnDriverCategory = FindName("btnDriverCategory") as Button;
                btnProgramsCategory = FindName("btnProgramsCategory") as Button;
                btnGamesCategory = FindName("btnGamesCategory") as Button;
                btnToolsCategory = FindName("btnToolsCategory") as Button;

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

        private async void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button clickedButton = sender as Button;
                if (clickedButton == null) return;

                // MEVCUT: Kurulum kontrol
                if (installationManager?.IsInstalling == true)
                {
                    txtLog.AppendText("⚠️ Kurulum devam ediyor, kategori değişimi engellendi\n");
                    return;
                }

                // Diğer kategori işlemleri...
                if (clickedButton == btnGamesCategory)
                {
                    if (gamesPanelManager != null)
                    {
                        bool success = await gamesPanelManager.ToggleGamesPanel();
                        // Games panel işlemleri...
                    }
                }
                else
                {
                    // Normal kategori geçişleri
                    if (clickedButton == btnDriverCategory)
                    {
                        UpdateCategoryView("Programlar");
                        SetSelectedCategory("Programlar");
                        txtLog.AppendText("🔧 Programlar kategorisi seçildi\n");
                    }
                    else if (clickedButton == btnProgramsCategory)
                    {
                        UpdateCategoryView("Sürücüler");
                        SetSelectedCategory("Sürücüler");
                        txtLog.AppendText("📦 Sürücüler kategorisi seçildi\n");
                    }
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ CategoryButton_Click hatası: {ex.Message}\n");
            }
        }

        // Diğer UI metodları...
        private void UpdateCategoryView(string category)
        {
            try
            {
                if (currentCategory == category) return;

                if (!string.IsNullOrEmpty(currentCategory))
                {
                    SaveCurrentSelections(currentCategory);
                }

                currentCategory = category;
                RefreshListWithSavedSelections(category);
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ Kategori güncelleme hatası: {ex.Message}\n");
            }
        }

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
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ Buton güncelleme hatası: {ex.Message}\n");
            }
        }

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
                txtLog.AppendText($"❌ Liste yenileme hatası: {ex.Message}\n");
            }
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

        // Diğer Event Handler'lar...
        private void btnFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folderPath = currentCategory == "Sürücüler" ? "C:\\Drivers" : "C:\\Programs";

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
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
                    // Sürücü ekleme mantığı...
                }
                else if (currentCategory == "Programlar")
                {
                    openFileDialog.Title = "Program Seç";
                    // Program ekleme mantığı...
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

        // Window Controls
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Empty event handler
        }

        // Diğer event handler'lar (GameSearchBox, vs.) burada olacak...
        // Bu metodlar değişmeden kalabilir
    }
}