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
        private Dictionary<string, Dictionary<string, bool>> categorySelections = new Dictionary<string, Dictionary<string, bool>>();
        // Kategori değişkenleri
        private string currentCategory = "Sürücüler"; // Varsayılan kategori
        private InstallationQueueManager queueManager;
        private InstallationManager installationManager;

        public Main()
        {
            try
            {
                InitializeComponent();
                txtLog.AppendText("Yafes Kurulum Aracı başlatıldı\n");
                txtLog.AppendText("Lütfen 'Yükle' butonuna tıklayarak işleme başlayın\n");

                // KUYRUK YÖNETİCİSİNİ BAŞLAT
                InitializeQueueManager();

                // ✅ SİSTEM BİLGİSİ YÖNETİCİSİNİ BAŞLAT
                InitializeSystemInfo();

                // INSTALLATION MANAGER'I BAŞLAT
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

        private void InitializeInstallationManager()
        {
            installationManager = new InstallationManager(
                txtLogAppendText: (msg) => txtLog.AppendText(msg),
                setProgressBarValue: (val) => progressBar.Value = val,
                setProgressBarStatusValue: (val) => progressBarStatus.Value = val,
                setTxtStatusBarText: (text) => txtStatusBar.Text = text,
                getChkRestartIsChecked: () => chkRestart.IsChecked == true,
                queueManager: queueManager
            );
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
                    // İKİ PARAMETRELİ ÇAĞRI - DÜZELTME
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

        private async void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button clickedButton = sender as Button;
                if (clickedButton == null) return;

                // MEVCUT: Kurulum kontrol
                if (installationManager.IsInstalling)
                {
                    txtLog.AppendText("⚠️ Kurulum devam ediyor, kategori değişimi engellendi\n");
                    return;
                }

                txtLog.AppendText($"🔘 Buton tıklandı: {clickedButton.Content}\n");

                // ✅ GAMES BUTONU - GamesPanelManager'a delege et (slide animation ile)
                if (clickedButton == btnGamesCategory)
                {
                    if (gamesPanelManager != null)
                    {
                        var gameCardManager = new GameCardManager();
                        gameCardManager.HideMainBackgroundLogo(this);
                        bool success = await gamesPanelManager.ToggleGamesPanel();

                        if (success)
                        {
                            // Panel durumuna göre kategori ayarla
                            if (gamesPanelManager.IsGamesVisible)
                            {

                                isGamesVisible = true;
                                SetSelectedCategory("Games");
                                txtLog.AppendText("🎮 Games modu aktif - Sol sidebar gizlendi, Games panel genişletildi\n");
                                txtLog.AppendText("💡 Daha geniş oyun kataloğu için sidebar slide edildi!\n");
                            }
                            else
                            {
                                gameCardManager.ShowMainBackgroundLogo(this);
                                isGamesVisible = false;
                                SetSelectedCategory("Programlar");
                                txtLog.AppendText("🔄 Normal mod - Sol sidebar gösterildi, Terminal restore edildi\n");
                            }
                        }
                        else
                        {
                            txtLog.AppendText("❌ Games panel toggle işlemi başarısız!\n");
                        }
                    }
                    else
                    {
                        txtLog.AppendText("❌ GamesPanelManager bulunamadı!\n");
                    }
                }
                else
                {
                    // ✅ DİĞER BUTONLAR (Programs, Drivers, Tools)
                    txtLog.AppendText($"📦 Normal kategori butonu: {clickedButton.Content}\n");

                    // ✅ ENHANCED: Games açıksa kapat - DÜZELTME: ToggleGamesPanel kullan
                    if (isGamesVisible && gamesPanelManager != null)
                    {
                        txtLog.AppendText("🔴 Games panel normal kategoriye geçiş için kapatılıyor...\n");
                        txtLog.AppendText("➡️ Sol sidebar geri getiriliyor...\n");

                        // ✅ DÜZELTME: Mevcut ToggleGamesPanel metodunu kullan (Games paneli kapalıysa açar, açıksa kapatır)
                        bool closeSuccess = await gamesPanelManager.ToggleGamesPanel();

                        if (closeSuccess)
                        {
                            isGamesVisible = false;
                            txtLog.AppendText("✅ Games panel kapatıldı, sidebar restore edildi, normal kategoriye geçiliyor\n");
                        }
                        else
                        {
                            txtLog.AppendText("⚠️ Games panel kapatma işlemi başarısız, yine de devam ediliyor\n");
                            isGamesVisible = false;

                            // Force reset - acil durum
                            gamesPanelManager.ForceReset();
                        }
                    }

                    // MEVCUT: Normal kategori geçişleri (orijinal kod korundu)
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
                    else if (clickedButton == btnToolsCategory)
                    {
                        currentCategory = "Tools";
                        SetSelectedCategory("Tools");
                        if (lstDrivers != null)
                        {
                            lstDrivers.Items.Clear();
                        }
                        txtLog.AppendText("🔧 Tools kategorisi seçildi\n");
                    }
                }

                txtLog.AppendText($"✅ Kategori işlemi tamamlandı - Aktif kategori: {currentCategory}\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ CategoryButton_Click hatası: {ex.Message}\n");

                // ENHANCED: Hata durumunda Games panel'i güvenli sıfırla
                if (gamesPanelManager != null)
                {
                    txtLog.AppendText("🚨 Hata nedeniyle GamesPanelManager force reset yapılıyor...\n");
                    gamesPanelManager.ForceReset();
                    isGamesVisible = false;
                }
            }
        }
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

        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                T childType = child as T;
                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
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
                    foreach (var driver in installationManager.MasterDrivers)
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
                    foreach (var program in installationManager.MasterPrograms)
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

        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (installationManager.IsInstalling)
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

                // Seçili driver ve program listelerini al
                var selectedDrivers = GetSelectedDrivers();
                var selectedPrograms = GetSelectedPrograms();

                // InstallationManager ile kurulumu başlat
                installationManager.PrepareInstallation(selectedDrivers, selectedPrograms);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<InstallationManager.DriverInfo> GetSelectedDrivers()
        {
            var selectedDrivers = new List<InstallationManager.DriverInfo>();

            if (currentCategory == "Sürücüler")
            {
                foreach (ListBoxItem item in lstDrivers.Items)
                {
                    if (item.Content is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Tag is InstallationManager.DriverInfo driver)
                    {
                        selectedDrivers.Add(driver);
                    }
                }
            }

            return selectedDrivers;
        }

        private List<InstallationManager.ProgramInfo> GetSelectedPrograms()
        {
            var selectedPrograms = new List<InstallationManager.ProgramInfo>();

            if (currentCategory == "Programlar")
            {
                foreach (ListBoxItem item in lstDrivers.Items)
                {
                    if (item.Content is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Tag is InstallationManager.ProgramInfo program)
                    {
                        selectedPrograms.Add(program);
                    }
                }
            }

            return selectedPrograms;
        }

        private void btnFolder_Click(object sender, RoutedEventArgs e)
        {
            installationManager.OpenCategoryFolder(currentCategory);
        }

        private void btnAddDriver_Click(object sender, RoutedEventArgs e)
        {
            installationManager.AddUserDefinedItem(currentCategory);

            // Liste güncelle
            RefreshListWithSavedSelections(currentCategory);
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