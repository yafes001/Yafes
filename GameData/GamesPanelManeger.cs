using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Yafes.Managers
{
    /// <summary>
    /// ENHANCED Games Panel yönetimi - Slide Animation eklendi
    /// Mevcut tüm özellikler korunmuş + Sol sidebar slide animasyonu
    /// </summary>
    public class GamesPanelManager
    {
        private readonly Window _parentWindow;
        private readonly TextBox _logTextBox;
        private bool _isGamesVisible = false;

        // ✅ YENİ: Slide Animation için gerekli referanslar
        private Border _leftSidebar;
        private TranslateTransform _leftSidebarTransform;
        private const double SIDEBAR_SLIDE_DISTANCE = -280; // Sol sidebar'ın kayacağı mesafe
        private const double ANIMATION_DURATION = 600; // Animasyon süresi (millisecond)

        // Events - MEVCUT
        public event Action<string> LogMessage;

        public GamesPanelManager(Window parentWindow, TextBox logTextBox)
        {
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));

            // MEVCUT Event subscription
            LogMessage += (message) => {
                _logTextBox.Dispatcher.Invoke(() => {
                    _logTextBox.AppendText(message + "\n");
                    _logTextBox.ScrollToEnd();
                });
            };

            // ✅ YENİ: Sidebar referanslarını başlat
            InitializeSidebarElements();
        }

        public bool IsGamesVisible => _isGamesVisible;

        /// <summary>
        /// ✅ YENİ: Sol sidebar için element referanslarını başlatır
        /// </summary>
        private void InitializeSidebarElements()
        {
            try
            {
                // Sol taraftaki sistem bilgisi/işlemler panelini bul
                // Muhtemel isimler: SystemInfo, LeftPanel, InfoPanel, SystemPanel
                string[] possibleNames = {
                    "SystemInfoPanel", "LeftPanel", "InfoPanel", "SystemPanel",
                    "LeftSidebar", "leftSidebar", "SidePanel"
                };

                foreach (var name in possibleNames)
                {
                    _leftSidebar = FindElementByName<Border>(_parentWindow, name);
                    if (_leftSidebar != null)
                    {
                        LogMessage?.Invoke($"✅ Sol sistem paneli bulundu: {name}");
                        break;
                    }
                }

                // Border bulunamazsa Grid veya StackPanel dene
                if (_leftSidebar == null)
                {
                    foreach (var name in possibleNames)
                    {
                        var panel = FindElementByName<Grid>(_parentWindow, name);
                        if (panel != null)
                        {
                            // Grid'in parent'ını Border olarak bul
                            var parent = VisualTreeHelper.GetParent(panel);
                            while (parent != null && !(parent is Border))
                            {
                                parent = VisualTreeHelper.GetParent(parent);
                            }
                            _leftSidebar = parent as Border;

                            if (_leftSidebar != null)
                            {
                                LogMessage?.Invoke($"✅ Sol sistem paneli (Grid parent) bulundu: {name}");
                                break;
                            }
                        }
                    }
                }

                // Hala bulunamazsa StackPanel dene
                if (_leftSidebar == null)
                {
                    foreach (var name in possibleNames)
                    {
                        var panel = FindElementByName<StackPanel>(_parentWindow, name);
                        if (panel != null)
                        {
                            // StackPanel'in parent'ını Border olarak bul
                            var parent = VisualTreeHelper.GetParent(panel);
                            while (parent != null && !(parent is Border))
                            {
                                parent = VisualTreeHelper.GetParent(parent);
                            }
                            _leftSidebar = parent as Border;

                            if (_leftSidebar != null)
                            {
                                LogMessage?.Invoke($"✅ Sol sistem paneli (StackPanel parent) bulundu: {name}");
                                break;
                            }
                        }
                    }
                }

                if (_leftSidebar != null)
                {
                    // TranslateTransform'u bul veya oluştur
                    _leftSidebarTransform = _leftSidebar.RenderTransform as TranslateTransform;
                    if (_leftSidebarTransform == null)
                    {
                        _leftSidebarTransform = new TranslateTransform();
                        _leftSidebar.RenderTransform = _leftSidebarTransform;
                    }
                    LogMessage?.Invoke("✅ Sol sistem paneli slide sistemi hazır");
                }
                else
                {
                    LogMessage?.Invoke("❌ Sol sistem paneli bulunamadı - slide animasyonu devre dışı");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Sol sistem paneli başlatma hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ YENİ: Games panel boyutunu ayarlar (tam genişlik/normal mod)
        /// </summary>
        private void ResizeGamesPanel(bool fullWidth)
        {
            try
            {
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel == null)
                {
                    LogMessage?.Invoke("⚠️ ResizeGamesPanel: GamesPanel bulunamadı");
                    return;
                }

                if (fullWidth)
                {
                    // Tam genişlik modu - Sol siyah alan + sağ kategori öncesi genişletme
                    gamesPanel.Width = Double.NaN; // Auto width
                    gamesPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                    gamesPanel.Margin = new Thickness(0, 0, 305, 0); // Sol:0 (tamamen sola), Sağ:305px (kategori öncesi)
                    LogMessage?.Invoke("📊 Games panel: Sol tam genişletme + sağ kategori öncesine kadar");
                }
                else
                {
                    // Normal mod - Orjinal merkez pozisyon
                    gamesPanel.Width = 800; // Varsayılan genişlik
                    gamesPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    gamesPanel.Margin = new Thickness(5); // Normal margin
                    LogMessage?.Invoke("📊 Games panel NORMAL boyuta döndü");
                }

                // Games grid'in sütun sayısını da güncelle
                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid != null)
                {
                    if (fullWidth)
                    {
                        gamesGrid.Columns = 10; // ✅ Tam genişlik kullanımı - 10 sütun optimal
                        LogMessage?.Invoke("🎮 Games grid: 10 sütun (tam genişlik - sol siyah alan dahil)");
                    }
                    else
                    {
                        gamesGrid.Columns = 4; // Normal modda 4 sütun
                        LogMessage?.Invoke("🎮 Games grid: 4 sütun (normal)");
                    }
                }

                // OYUNLAR başlığını da genişlet
                var gamesTitlePanel = FindElementByTag<Border>(_parentWindow, "GamesTitlePanel") ??
                                    FindElementByName<Border>(_parentWindow, "GamesTitlePanel");
                if (gamesTitlePanel != null)
                {
                    if (fullWidth)
                    {
                        gamesTitlePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                        gamesTitlePanel.Margin = new Thickness(0, 0, 305, 0);
                        LogMessage?.Invoke("📊 OYUNLAR başlığı genişletildi");
                    }
                    else
                    {
                        gamesTitlePanel.HorizontalAlignment = HorizontalAlignment.Center;
                        gamesTitlePanel.Margin = new Thickness(5);
                        LogMessage?.Invoke("📊 OYUNLAR başlığı normale döndü");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ResizeGamesPanel hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// MEVCUT + ENHANCED: Games panel toggle işlemi - Slide animation eklendi
        /// </summary>
        public async Task<bool> ToggleGamesPanel()
        {
            try
            {
                LogMessage?.Invoke($"🎮 Games butonu tıklandı - Mevcut durum: {(_isGamesVisible ? "AÇIK" : "KAPALI")}");

                if (!_isGamesVisible)
                {
                    LogMessage?.Invoke("🔛 Games panel açılıyor...");
                    bool success = await ShowGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = true;
                        LogMessage?.Invoke("✅ Games panel başarıyla açıldı");

                        // ✅ YENİ: Sol sidebar'ı gizle
                        await SlideSidebarOut();
                    }
                    return success;
                }
                else
                {
                    LogMessage?.Invoke("🔴 Games panel kapatılıyor...");

                    // ✅ DEĞİŞTİRİLDİ: Önce sol sidebar'ı geri getir
                    await SlideSidebarIn();

                    bool success = await HideGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = false;
                        LogMessage?.Invoke("✅ Games panel başarıyla kapatıldı");
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ToggleGamesPanel hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ✅ YENİ: Sol sidebar'ı sola kaydırarak gizler
        /// </summary>
        private async Task SlideSidebarOut()
        {
            try
            {
                if (_leftSidebar == null || _leftSidebarTransform == null)
                {
                    LogMessage?.Invoke("⚠️ Sidebar slide atlandı - elementler bulunamadı");
                    return;
                }

                LogMessage?.Invoke("⬅️ Sol sidebar gizleniyor...");

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = SIDEBAR_SLIDE_DISTANCE,
                    Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                // Animasyon tamamlanma kontrolü
                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => tcs.SetResult(true);

                _leftSidebarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;

                LogMessage?.Invoke("✅ Sol sidebar gizlendi - Daha geniş games alanı!");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Sidebar slide out hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ YENİ: Sol sidebar'ı normal pozisyona geri getirir
        /// </summary>
        private async Task SlideSidebarIn()
        {
            try
            {
                if (_leftSidebar == null || _leftSidebarTransform == null)
                {
                    LogMessage?.Invoke("⚠️ Sidebar slide atlandı - elementler bulunamadı");
                    return;
                }

                LogMessage?.Invoke("➡️ Sol sidebar geri getiriliyor...");

                var slideAnimation = new DoubleAnimation
                {
                    From = SIDEBAR_SLIDE_DISTANCE,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                // Animasyon tamamlanma kontrolü
                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => tcs.SetResult(true);

                _leftSidebarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;

                LogMessage?.Invoke("✅ Sol sidebar normal pozisyonda");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Sidebar slide in hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ENHANCED: Games panelini gösterir + Tam genişlik modunu aktifleştirir
        /// ✅ YENİ: Terminal tamamen kayarak kaybolur + Progress bar sağa kayar
        /// </summary>
        private async Task<bool> ShowGamesPanel()
        {
            try
            {
                LogMessage?.Invoke("🎮 BAŞLAMA: ShowGamesPanel çalışıyor...");

                // ✅ DEBUG: Resource'ları listele
                DebugListResources();

                // 1. Panel'leri bul
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null)
                {
                    LogMessage?.Invoke("❌ HATA: GamesPanel bulunamadı! Tag='GamesPanel' kontrolü");
                    return false;
                }

                if (terminalPanel == null)
                {
                    LogMessage?.Invoke("❌ HATA: TerminalPanel bulunamadı! Tag='TerminalPanel' kontrolü");
                    return false;
                }

                LogMessage?.Invoke("✅ Panel'ler bulundu");

                // 2. ✅ YENİ: Games Panel'i önce genişlet
                ResizeGamesPanel(true);

                // 3. Games Panel'i görünür yap
                gamesPanel.Visibility = Visibility.Visible;
                LogMessage?.Invoke("✅ GamesPanel.Visibility = Visible");

                // 4. ✅ DEĞİŞTİRİLDİ: Terminal'i tamamen kayarak kaybet
                await SlideTerminalOutCompletely(terminalPanel);

                // 4.5. ✅ YENİ: Progress bar'ı sağa kayarak kaybet
                await SlideProgressBarOut();

                // 5. Games Panel animasyonu
                await StartGamesOnlyAnimation(gamesPanel);

                // 6. Kategori listesini gizle
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Collapsed;
                    LogMessage?.Invoke("✅ Kategori listesi gizlendi");
                }

                // 7. ✅ ENHANCED: Oyun verilerini tam genişlik modunda yükle
                LogMessage?.Invoke("📊 Oyun verileri tam genişlik modunda yükleniyor...");
                await LoadGamesIntoPanel(gamesPanel);

                LogMessage?.Invoke("✅ BİTİŞ: Games panel tamamen açıldı - Terminal ve Progress bar kayboldu!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ShowGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ENHANCED: Games panelini gizler + Terminal ve Progress bar'ı geri getirir
        /// Sadece Games butonuna tekrar basıldığında kullanılır
        /// </summary>
        private async Task<bool> HideGamesPanel()
        {
            try
            {
                LogMessage?.Invoke("🔴 BAŞLAMA: HideGamesPanel çalışıyor...");

                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    LogMessage?.Invoke("❌ Panel'ler bulunamadı, gizleme iptal");
                    return false;
                }

                // ✅ YENİ: Games panel boyutunu normale döndür
                ResizeGamesPanel(false);

                // Games panel'i gizle
                gamesPanel.Visibility = Visibility.Collapsed;
                LogMessage?.Invoke("✅ GamesPanel.Visibility = Collapsed");

                // ✅ DEĞİŞTİRİLDİ: Terminal'i tamamen yukarıdan geri getir
                await SlideTerminalInCompletely(terminalPanel);

                // ✅ YENİ: Progress bar'ı soldan geri getir
                await SlideProgressBarIn();

                // Kategori listesini geri göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                    LogMessage?.Invoke("✅ Kategori listesi geri gösterildi");
                }

                LogMessage?.Invoke("✅ BİTİŞ: Games panel gizlendi, Terminal ve Progress bar geri geldi!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ HideGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ✅ YENİ: Terminal'i tamamen aşağıya kaydırarak kaybet
        /// </summary>
        private async Task SlideTerminalOutCompletely(Border terminalPanel)
        {
            try
            {
                LogMessage?.Invoke("⬇️ Terminal tamamen kayarak kaybolacak...");

                // Terminal transform'unu al veya oluştur
                var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                if (terminalTransform == null)
                {
                    terminalTransform = new TranslateTransform();
                    terminalPanel.RenderTransform = terminalTransform;
                }

                // Terminal'i tamamen aşağıya kaydır (yüksekliğinden fazla)
                var slideDownAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 650, // Terminal yüksekliğinden fazla (596 + margin)
                    Duration = TimeSpan.FromMilliseconds(800), // Biraz daha uzun animasyon
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                // Opacity ile de kaybet
                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    BeginTime = TimeSpan.FromMilliseconds(200) // 200ms sonra fade başlasın
                };

                LogMessage?.Invoke("🎬 Terminal tamamen kayma animasyonu başlatılıyor...");

                // Animasyon tamamlanma kontrolü
                var tcs = new TaskCompletionSource<bool>();
                slideDownAnimation.Completed += (s, e) => {
                    // Animasyon bitince tamamen gizle
                    terminalPanel.Visibility = Visibility.Collapsed;
                    LogMessage?.Invoke("✅ Terminal tamamen kayboldu!");
                    tcs.SetResult(true);
                };

                // Her iki animasyonu da başlat
                terminalTransform.BeginAnimation(TranslateTransform.YProperty, slideDownAnimation);
                terminalPanel.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);

                // Animasyon tamamlanana kadar bekle
                await tcs.Task;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SlideTerminalOutCompletely hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ YENİ: Progress bar'ı sola kaydırarak kaybet
        /// ENHANCED: Sol sidebar mantığını kullan
        /// </summary>
        private async Task SlideProgressBarOut()
        {
            try
            {
                LogMessage?.Invoke("➡️ Progress bar sağa kayarak kaybolacak...");

                // ✅ ANAHTAR: Main.xaml'de progress bar container'ının adını bul
                // Energy Bar - Canvas.Left="266" Canvas.Top="680" olan Border
                Border progressBarContainer = null;

                // 1. Energy Bar container'ını Canvas pozisyonuna göre bul
                progressBarContainer = FindProgressBarByPosition();
                LogMessage?.Invoke($"🔍 Progress bar pozisyon ile arama: {progressBarContainer != null}");

                // 2. ProgressBar element'ini bul ve parent'ını al
                if (progressBarContainer == null)
                {
                    var progressBarElement = FindElementByName<ProgressBar>(_parentWindow, "progressBar");
                    if (progressBarElement != null)
                    {
                        // Parent Border'ı bul
                        var parent = VisualTreeHelper.GetParent(progressBarElement);
                        while (parent != null && !(parent is Border))
                        {
                            parent = VisualTreeHelper.GetParent(parent);
                        }
                        progressBarContainer = parent as Border;
                        LogMessage?.Invoke($"🔍 ProgressBar parent bulma: {progressBarContainer != null}");
                    }
                }

                if (progressBarContainer == null)
                {
                    LogMessage?.Invoke("❌ Progress bar container bulunamadı - animasyon atlandı");
                    return;
                }

                // ✅ SOL SIDEBAR GİBİ: Transform oluştur veya al
                var progressTransform = progressBarContainer.RenderTransform as TranslateTransform;
                if (progressTransform == null)
                {
                    progressTransform = new TranslateTransform();
                    progressBarContainer.RenderTransform = progressTransform;
                    LogMessage?.Invoke("🔧 Progress bar transform oluşturuldu");
                }

                // Sağa kayma animasyonu
                var slideRightAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 900, // Sağa 900px kayacak (ekran dışına)
                    Duration = TimeSpan.FromMilliseconds(700),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                // Opacity ile kaybet
                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(500),
                    BeginTime = TimeSpan.FromMilliseconds(150)
                };

                LogMessage?.Invoke("🎬 Progress bar sağa kayma animasyonu başlatılıyor...");

                // Animasyon tamamlanma kontrolü
                var tcs = new TaskCompletionSource<bool>();
                slideRightAnimation.Completed += (s, e) => {
                    progressBarContainer.Visibility = Visibility.Collapsed;
                    LogMessage?.Invoke("✅ Progress bar sağa kayarak kayboldu!");
                    tcs.SetResult(true);
                };

                // Animasyonları başlat
                progressTransform.BeginAnimation(TranslateTransform.XProperty, slideRightAnimation);
                progressBarContainer.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);

                await tcs.Task;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SlideProgressBarOut hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ YENİ: Progress bar'ı soldan geri getir
        /// ENHANCED: Sol sidebar mantığını kullan
        /// </summary>
        private async Task SlideProgressBarIn()
        {
            try
            {
                LogMessage?.Invoke("⬅️ Progress bar soldan geri geliyor...");

                // ✅ ANAHTAR: Sol sidebar mantığını kullan
                Border progressBarContainer = null;

                // 1. Energy Bar container'ını Canvas pozisyonuna göre bul
                progressBarContainer = FindProgressBarByPosition();
                LogMessage?.Invoke($"🔍 Progress bar pozisyon ile arama: {progressBarContainer != null}");

                // 2. ProgressBar element'ini bul ve parent'ını al
                if (progressBarContainer == null)
                {
                    var progressBarElement = FindElementByName<ProgressBar>(_parentWindow, "progressBar");
                    if (progressBarElement != null)
                    {
                        // Sol sidebar gibi parent Border'ı bul
                        var parent = VisualTreeHelper.GetParent(progressBarElement);
                        while (parent != null && !(parent is Border))
                        {
                            parent = VisualTreeHelper.GetParent(parent);
                        }
                        progressBarContainer = parent as Border;
                        LogMessage?.Invoke($"🔍 ProgressBar parent bulma: {progressBarContainer != null}");
                    }
                    else
                    {
                        LogMessage?.Invoke("❌ ProgressBar element bulunamadı");
                    }
                }

                if (progressBarContainer == null)
                {
                    LogMessage?.Invoke("❌ Progress bar container hiçbir yöntemle bulunamadı");
                    return;
                }

                // Önce görünür yap
                progressBarContainer.Visibility = Visibility.Visible;
                progressBarContainer.Opacity = 1.0;

                // ✅ SOL SIDEBAR GİBİ: Transform al veya oluştur
                var progressTransform = progressBarContainer.RenderTransform as TranslateTransform;
                if (progressTransform == null)
                {
                    progressTransform = new TranslateTransform();
                    progressBarContainer.RenderTransform = progressTransform;
                    LogMessage?.Invoke("🔧 Progress bar transform oluşturuldu");
                }

                // ✅ ENHANCED: Transform pozisyonunu logla
                LogMessage?.Invoke($"🔍 Progress bar mevcut pozisyon: X={progressTransform.X}");

                // Soldan geri gelme animasyonu
                var slideLeftAnimation = new DoubleAnimation
                {
                    From = 900, // Sağdan başla
                    To = 0,     // Normal pozisyona
                    Duration = TimeSpan.FromMilliseconds(700),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                LogMessage?.Invoke("🎬 Progress bar geri gelme animasyonu başlatılıyor...");

                var tcs = new TaskCompletionSource<bool>();
                slideLeftAnimation.Completed += (s, e) => {
                    LogMessage?.Invoke("✅ Progress bar normal pozisyonda!");
                    tcs.SetResult(true);
                };

                progressTransform.BeginAnimation(TranslateTransform.XProperty, slideLeftAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SlideProgressBarIn hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ YENİ: Progress bar'ı pozisyonuna göre bul (Canvas.Top="680" civarında)
        /// </summary>
        private Border FindProgressBarByPosition()
        {
            try
            {
                // Ana canvas'ı bul
                var mainCanvas = FindElementByName<Canvas>(_parentWindow, "MainCanvas");
                if (mainCanvas == null) return null;

                // Canvas'taki tüm Border'ları kontrol et
                foreach (var child in mainCanvas.Children)
                {
                    if (child is Border border)
                    {
                        // Canvas pozisyonunu kontrol et
                        var top = Canvas.GetTop(border);
                        var left = Canvas.GetLeft(border);

                        // Energy Bar pozisyonu: Canvas.Left="266" Canvas.Top="680"
                        if (top >= 675 && top <= 685 && left >= 260 && left <= 270)
                        {
                            LogMessage?.Invoke($"✅ Progress bar pozisyon ile bulundu: Top={top}, Left={left}");
                            return border;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ FindProgressBarByPosition hatası: {ex.Message}");
                return null;
            }
        }
        private async Task SlideTerminalInCompletely(Border terminalPanel)
        {
            try
            {
                LogMessage?.Invoke("⬆️ Terminal yukarıdan geri geliyor...");

                // Önce görünür yap
                terminalPanel.Visibility = Visibility.Visible;
                terminalPanel.Opacity = 1.0; // Opacity'yi resetle

                // Terminal transform'unu al
                var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                if (terminalTransform == null)
                {
                    terminalTransform = new TranslateTransform();
                    terminalPanel.RenderTransform = terminalTransform;
                }

                // Yukarıdan aşağıya geri gelsin
                var slideUpAnimation = new DoubleAnimation
                {
                    From = 650, // Aşağıdan başla
                    To = 0,     // Normal pozisyona
                    Duration = TimeSpan.FromMilliseconds(800),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                LogMessage?.Invoke("🎬 Terminal geri gelme animasyonu başlatılıyor...");

                // Animasyon tamamlanma kontrolü
                var tcs = new TaskCompletionSource<bool>();
                slideUpAnimation.Completed += (s, e) => {
                    LogMessage?.Invoke("✅ Terminal normal pozisyonda!");
                    tcs.SetResult(true);
                };

                terminalTransform.BeginAnimation(TranslateTransform.YProperty, slideUpAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SlideTerminalInCompletely hatası: {ex.Message}");
            }
        }
        private async Task StartGamesOnlyAnimation(Border gamesPanel)
        {
            try
            {
                // Games Panel animasyonu
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
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                // Opacity animasyonu
                var gamesPanelOpacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(400),
                    BeginTime = TimeSpan.FromMilliseconds(200)
                };

                LogMessage?.Invoke("🎬 Games panel animasyonu başlatılıyor...");

                // ✅ Sadece Games Panel animasyonları
                gamesPanelTransform.BeginAnimation(TranslateTransform.YProperty, gamesPanelShowAnimation);
                gamesPanel.BeginAnimation(UIElement.OpacityProperty, gamesPanelOpacityAnimation);

                // Animasyon tamamlanana kadar bekle
                await Task.Delay(700);

                LogMessage?.Invoke("✅ Games panel animasyonu tamamlandı!");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartGamesOnlyAnimation hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ KALDIRILDI: Eski terminal animasyon metodu - artık kullanılmıyor
        /// </summary>
        private void StartHideAnimations(Border terminalPanel)
        {
            // Bu metod artık kullanılmıyor - Terminal direkt gizleniyor/gösteriliyor
            LogMessage?.Invoke("⚠️ StartHideAnimations artık kullanılmıyor - direkt visibility değişimi yapılıyor");
        }

        /// <summary>
        /// ENHANCED: Gerçek oyun verilerini panel'e yükler - GameDataManager'dan dinamik veri alır
        /// </summary>
        private async Task LoadGamesIntoPanel(Border gamesPanel)
        {
            try
            {
                LogMessage?.Invoke("🎮 Gerçek oyun verileri yükleniyor...");

                // ✅ DEBUG: Resource'ları listele
                DebugListResources();

                // UniformGrid'i bul
                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid == null)
                {
                    LogMessage?.Invoke("❌ gamesGrid bulunamadı!");
                    return;
                }

                // Önce mevcut kartları temizle
                gamesGrid.Children.Clear();

                // ✅ Sidebar slide durumuna göre sütun sayısı ayarla
                if (_leftSidebar != null && _leftSidebarTransform != null && _leftSidebarTransform.X < -200)
                {
                    gamesGrid.Columns = 8; // ✅ Sol alan dahil 8 sütun
                    LogMessage?.Invoke("📊 TAM GENİŞLİK MODU: 8 sütun oyun grid'i - Sol alan dahil!");
                }
                else
                {
                    gamesGrid.Columns = 4; // Normal modda 4 sütun
                    LogMessage?.Invoke("📊 Normal mod: 4 sütun oyun grid'i");
                }

                // ✅ YENİ: GameDataManager'dan gerçek oyun verilerini al
                var games = await Yafes.Managers.GameDataManager.GetAllGamesAsync();

                if (games == null || games.Count == 0)
                {
                    LogMessage?.Invoke("⚠️ Oyun verisi bulunamadı, varsayılan kartlar oluşturuluyor...");
                    CreateDefaultGameCards(gamesGrid);
                    return;
                }

                LogMessage?.Invoke($"✅ {games.Count} oyun verisi yüklendi, kartlar oluşturuluyor...");

                // Her oyun için kart oluştur
                foreach (var game in games.Take(40)) // ✅ 8 sütun x 5 satır = 40 oyun optimal
                {
                    var gameCard = CreateGameCard(game);
                    if (gameCard != null)
                    {
                        gamesGrid.Children.Add(gameCard);
                    }
                }

                LogMessage?.Invoke($"✅ {gamesGrid.Children.Count} oyun kartı başarıyla oluşturuldu!");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ LoadGamesIntoPanel hatası: {ex.Message}");

                // Hata durumunda varsayılan kartları göster
                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid != null)
                {
                    CreateDefaultGameCards(gamesGrid);
                }
            }
        }

        /// <summary>
        /// ✅ DEBUG: Embedded resource'ları listeler - ImageManager kullanır
        /// </summary>
        private void DebugListResources()
        {
            try
            {
                LogMessage?.Invoke("🔍 ImageManager ile resource kontrolü yapılıyor...");

                // ImageManager'ın PNG resource'larını al
                var pngResources = Yafes.Managers.ImageManager.GetPngResourceNames();
                LogMessage?.Invoke($"🖼️ ImageManager PNG sayısı: {pngResources.Length}");

                var gamePosters = pngResources.Where(r => r.Contains("GamePosters") || r.Contains("gameposters")).ToArray();
                LogMessage?.Invoke($"🎮 GamePosters: {gamePosters.Length} adet");

                foreach (var poster in gamePosters.Take(5)) // İlk 5'ini göster
                {
                    LogMessage?.Invoke($"  📁 {poster}");
                }

                if (gamePosters.Length == 0)
                {
                    LogMessage?.Invoke("❌ Hiç GamePosters resource'u bulunamadı!");

                    // Tüm PNG'leri listele
                    LogMessage?.Invoke($"🖼️ Tüm PNG'ler: {pngResources.Length} adet");

                    foreach (var png in pngResources.Take(3))
                    {
                        LogMessage?.Invoke($"  🖼️ {png}");
                    }
                }

                // ImageManager test - default image yükle
                var defaultImage = Yafes.Managers.ImageManager.GetDefaultImage();
                if (defaultImage != null)
                {
                    LogMessage?.Invoke("✅ ImageManager default image test başarılı");
                }
                else
                {
                    LogMessage?.Invoke("❌ ImageManager default image test başarısız");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ DebugListResources hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ YENİ: Gerçek oyun verisinden oyun kartı oluşturur
        /// </summary>
        private Border CreateGameCard(Yafes.Models.GameData game)
        {
            try
            {
                // Ana border (kart container)
                var gameCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), // #80000000
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)), // #FFA500
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Height = 100, // Biraz daha yüksek kart
                    Cursor = Cursors.Hand,
                    Tag = game, // Oyun verisini tag'e koy
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    }
                };

                // İçerik için StackPanel
                var stackPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5)
                };

                // ✅ DÜZELTME: ImageManager kullanarak PNG yükle (ORIJINAL YÖNTEM)
                if (!string.IsNullOrEmpty(game.ImageName))
                {
                    // Oyun afişi (Image) - ImageManager kullan
                    try
                    {
                        var gameImage = new Image
                        {
                            Width = 60,
                            Height = 40,
                            Stretch = Stretch.UniformToFill,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 5)
                        };

                        // ✅ ORIJINAL YÖNTEM: ImageManager kullan
                        var imageName = Path.GetFileNameWithoutExtension(game.ImageName); // .png'siz isim
                        var bitmapImage = Yafes.Managers.ImageManager.GetGameImage(imageName);

                        if (bitmapImage != null)
                        {
                            gameImage.Source = bitmapImage;
                            stackPanel.Children.Add(gameImage);
                            LogMessage?.Invoke($"✅ ImageManager'dan afiş yüklendi: {imageName}");
                        }
                        else
                        {
                            LogMessage?.Invoke($"⚠️ ImageManager null döndü: {imageName}");
                            AddCategoryIcon(stackPanel, game.Category);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"❌ ImageManager hatası ({game.ImageName}): {ex.Message}");
                        // Resim yüklenemezse kategori ikonu göster
                        AddCategoryIcon(stackPanel, game.Category);
                    }
                }
                else
                {
                    // Afiş yoksa kategori ikonu göster
                    AddCategoryIcon(stackPanel, game.Category);
                }

                // Oyun ismi
                var gameNameText = new TextBlock
                {
                    Text = game.Name,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)), // #FFA500
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 80,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 8,
                        ShadowDepth = 1,
                        Opacity = 0.6
                    }
                };

                // Boyut bilgisi
                var gameSizeText = new TextBlock
                {
                    Text = game.Size,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)), // #888
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                // Kategori bilgisi
                var gameCategoryText = new TextBlock
                {
                    Text = game.Category,
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 205, 170)), // Açık yeşil
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 1, 0, 0)
                };

                // StackPanel'e ekle
                stackPanel.Children.Add(gameNameText);
                stackPanel.Children.Add(gameSizeText);
                stackPanel.Children.Add(gameCategoryText);

                // StackPanel'i karta ekle
                gameCard.Child = stackPanel;

                // Event handler'ları ekle
                gameCard.MouseEnter += GameCard_MouseEnter;
                gameCard.MouseLeave += GameCard_MouseLeave;
                gameCard.MouseLeftButtonDown += GameCard_Click;

                return gameCard;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateGameCard hatası ({game?.Name}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ✅ YENİ: Kategori ikonunu ekler (afiş bulunamazsa)
        /// </summary>
        private void AddCategoryIcon(StackPanel stackPanel, string category)
        {
            var categoryIcon = GetCategoryIcon(category);
            var iconText = new TextBlock
            {
                Text = categoryIcon,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(iconText);
        }

        /// <summary>
        /// ✅ YENİ: Kategori ikonunu döndürür
        /// </summary>
        private string GetCategoryIcon(string category)
        {
            return category?.ToLower() switch
            {
                "fps" => "🔫",
                "rpg" => "🗡️",
                "racing" => "🏎️",
                "action" => "⚔️",
                "strategy" => "♟️",
                "sports" => "⚽",
                "horror" => "👻",
                "simulation" => "🎛️",
                "puzzle" => "🧩",
                _ => "🎮"
            };
        }

        /// <summary>
        /// ✅ YENİ: Varsayılan oyun kartları oluşturur (fallback)
        /// </summary>
        private void CreateDefaultGameCards(UniformGrid gamesGrid)
        {
            try
            {
                LogMessage?.Invoke("🎮 Varsayılan oyun kartları oluşturuluyor...");

                var defaultGames = new[]
                {
                    new { Name = "Steam", Icon = "🎯", Size = "150 MB", Category = "Platform" },
                    new { Name = "Epic Games", Icon = "🎮", Size = "200 MB", Category = "Platform" },
                    new { Name = "GOG Galaxy", Icon = "🎲", Size = "80 MB", Category = "Platform" },
                    new { Name = "Origin", Icon = "⚡", Size = "120 MB", Category = "Platform" },
                    new { Name = "Battle.net", Icon = "🚀", Size = "90 MB", Category = "Platform" },
                    new { Name = "Ubisoft Connect", Icon = "🎪", Size = "110 MB", Category = "Platform" },
                    new { Name = "Rockstar", Icon = "🎭", Size = "85 MB", Category = "Platform" },
                    new { Name = "Xbox App", Icon = "⭐", Size = "95 MB", Category = "Platform" }
                };

                foreach (var game in defaultGames)
                {
                    var gameCard = CreateDefaultGameCard(game.Name, game.Icon, game.Size, game.Category);
                    if (gameCard != null)
                    {
                        gamesGrid.Children.Add(gameCard);
                    }
                }

                LogMessage?.Invoke($"✅ {defaultGames.Length} varsayılan kart oluşturuldu");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateDefaultGameCards hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ YENİ: Varsayılan oyun kartı oluşturur
        /// </summary>
        private Border CreateDefaultGameCard(string name, string icon, string size, string category)
        {
            try
            {
                var gameCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Height = 80,
                    Cursor = Cursors.Hand,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    }
                };

                var stackPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // İkon
                stackPanel.Children.Add(new TextBlock
                {
                    Text = icon,
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                // İsim
                stackPanel.Children.Add(new TextBlock
                {
                    Text = name,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 8,
                        ShadowDepth = 1,
                        Opacity = 0.6
                    }
                });

                // Boyut
                stackPanel.Children.Add(new TextBlock
                {
                    Text = size,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                gameCard.Child = stackPanel;

                // Event handler'lar
                gameCard.MouseEnter += GameCard_MouseEnter;
                gameCard.MouseLeave += GameCard_MouseLeave;
                gameCard.MouseLeftButtonDown += GameCard_Click;

                return gameCard;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateDefaultGameCard hatası ({name}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// MEVCUT: Game Card hover enter event (orijinal kod korundu)
        /// </summary>
        private void GameCard_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    // Hover efekti - kartı büyüt ve renklendirr
                    var scaleTransform = new ScaleTransform(1.05, 1.05);
                    card.RenderTransform = scaleTransform;
                    card.RenderTransformOrigin = new Point(0.5, 0.5);

                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Gold
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 215, 0),
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ GameCard_MouseEnter hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// MEVCUT: Game Card hover leave event (orijinal kod korundu)
        /// </summary>
        private void GameCard_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    // Normal duruma dön
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    card.RenderTransform = scaleTransform;

                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Orange
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    };
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ GameCard_MouseLeave hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ENHANCED: Game Card click event - Gerçek oyun verisiyle çalışır
        /// </summary>
        private void GameCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    string gameName = "Unknown Game";

                    // ✅ YENİ: Gerçek oyun verisini Tag'den al
                    if (card.Tag is Yafes.Models.GameData gameData)
                    {
                        gameName = gameData.Name;
                        LogMessage?.Invoke($"🎯 {gameName} kurulum kuyruğuna eklendi!");
                        LogMessage?.Invoke($"📂 Kategori: {gameData.Category} | Boyut: {gameData.Size}");

                        // TODO: Gerçek kurulum kuyruğuna ekleme işlemi
                        // queueManager.AddGameToQueue(gameData);
                    }
                    else
                    {
                        // FALLBACK: Stack panel'den oyun adını bul (varsayılan kartlar için)
                        var stackPanel = card.Child as StackPanel;
                        if (stackPanel?.Children.Count >= 2 && stackPanel.Children[1] is TextBlock gameNameTextBlock)
                        {
                            gameName = gameNameTextBlock.Text;
                            LogMessage?.Invoke($"🎯 {gameName} kurulum kuyruğuna eklendi!");
                        }
                    }

                    // Kısa titreşim efekti
                    CreateShakeEffect(card);
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ GameCard_Click hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// MEVCUT: Kart titreşim efekti (orijinal kod korundu)
        /// </summary>
        private void CreateShakeEffect(Border card)
        {
            try
            {
                var shakeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 3,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                var translateTransform = new TranslateTransform();
                card.RenderTransform = translateTransform;
                translateTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateShakeEffect hatası: {ex.Message}");
            }
        }

        // ============ MEVCUT UTILITY METODLAR ============

        /// <summary>
        /// MEVCUT: Tag ile element bulur
        /// </summary>
        private T FindElementByTag<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Tag?.ToString() == tag)
                {
                    return element;
                }

                var result = FindElementByTag<T>(child, tag);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// MEVCUT: Name ile element bulur
        /// </summary>
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

        /// <summary>
        /// ✅ ENHANCED: Acil durum reset - Tüm animasyonları ve layout'u sıfırlar
        /// </summary>
        public void ForceReset()
        {
            try
            {
                LogMessage?.Invoke("🚨 GamesPanelManager ENHANCED FORCE RESET...");

                // Sidebar'ı normal pozisyona getir
                if (_leftSidebarTransform != null)
                {
                    _leftSidebarTransform.X = 0;
                    LogMessage?.Invoke("↻ Sidebar normal pozisyonda");
                }

                // Games panel'i gizle ve normal boyuta döndür
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel != null)
                {
                    gamesPanel.Visibility = Visibility.Collapsed;
                    gamesPanel.Opacity = 1; // Opacity'yi resetle

                    LogMessage?.Invoke("🔴 Games panel gizlendi");
                }

                // ✅ ENHANCED: Terminal'i direkt normal pozisyona getir
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");
                if (terminalPanel != null)
                {
                    // Terminal transform'larını sıfırla
                    if (terminalPanel.RenderTransform is TranslateTransform terminalTransform)
                    {
                        terminalTransform.Y = 0; // Pozisyonu resetle
                    }

                    // Terminal'i direkt göster
                    terminalPanel.Visibility = Visibility.Visible;
                    terminalPanel.Opacity = 1.0; // Opacity'yi de resetle
                    terminalPanel.Height = 596; // Orijinal yükseklik
                    LogMessage?.Invoke("📺 Terminal direkt normal pozisyonda");
                }

                // ✅ YENİ: Progress bar'ı da resetle
                ResetProgressBar();

                _isGamesVisible = false;
                LogMessage?.Invoke("✅ Force reset tamamlandı - Tüm elementler normal pozisyonda");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ForceReset hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ YENİ: Kategori değiştiğinde Games panel'den çıkış
        /// Driver/Program butonlarına basıldığında Games'ten çık
        /// ENHANCED: Debug mesajları eklendi
        /// </summary>
        public async Task<bool> ExitGamesMode()
        {
            try
            {
                if (!_isGamesVisible)
                {
                    LogMessage?.Invoke("⚠️ Games zaten kapalı");
                    return true;
                }

                LogMessage?.Invoke("🔄 Kategori değişimi: Games modundan çıkılıyor...");

                // ✅ DÜZELTME: Önce Games panel'i gizle ve animasyonları başlat
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                LogMessage?.Invoke($"🔍 Panel kontrolü - Games: {gamesPanel != null}, Terminal: {terminalPanel != null}");

                if (gamesPanel == null || terminalPanel == null)
                {
                    LogMessage?.Invoke("❌ Panel'ler bulunamadı");
                    return false;
                }

                // 1. Games panel boyutunu normale döndür
                LogMessage?.Invoke("📏 Games panel boyutu normale döndürülüyor...");
                ResizeGamesPanel(false);

                // 2. Games panel'i gizle
                gamesPanel.Visibility = Visibility.Collapsed;
                LogMessage?.Invoke("✅ GamesPanel gizlendi");

                // 3. Terminal'i yukarıdan geri getir
                LogMessage?.Invoke("⬆️ Terminal geri getirme başlıyor...");
                await SlideTerminalInCompletely(terminalPanel);

                // 4. Progress bar'ı soldan geri getir
                LogMessage?.Invoke("⬅️ Progress bar geri getirme başlıyor...");
                await SlideProgressBarIn();

                // 5. Sol sidebar'ı geri getir
                LogMessage?.Invoke("➡️ Sidebar geri getirme başlıyor...");
                await SlideSidebarIn();

                // 6. Kategori listesini geri göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                    LogMessage?.Invoke("✅ Kategori listesi geri gösterildi");
                }
                else
                {
                    LogMessage?.Invoke("⚠️ Kategori listesi bulunamadı");
                }

                _isGamesVisible = false;
                LogMessage?.Invoke("✅ Kategori değişimi tamamlandı - Normal mod");

                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ExitGamesMode hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ✅ YENİ: Driver kategorisine geçiş
        /// </summary>
        public async Task<bool> SwitchToDriverCategory()
        {
            try
            {
                LogMessage?.Invoke("🔧 Driver kategorisine geçiliyor...");

                if (_isGamesVisible)
                {
                    bool exitSuccess = await ExitGamesMode();
                    if (!exitSuccess)
                    {
                        LogMessage?.Invoke("❌ Games modundan çıkılamadı");
                        return false;
                    }
                }

                // Driver listesini göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                    LogMessage?.Invoke("✅ Driver listesi gösterildi");
                }

                LogMessage?.Invoke("✅ Driver kategorisi aktif");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SwitchToDriverCategory hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ✅ YENİ: Program kategorisine geçiş
        /// </summary>
        public async Task<bool> SwitchToProgramCategory()
        {
            try
            {
                LogMessage?.Invoke("📦 Program kategorisine geçiliyor...");

                if (_isGamesVisible)
                {
                    bool exitSuccess = await ExitGamesMode();
                    if (!exitSuccess)
                    {
                        LogMessage?.Invoke("❌ Games modundan çıkılamadı");
                        return false;
                    }
                }

                // Program listesini göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                    LogMessage?.Invoke("✅ Program listesi gösterildi");
                }

                LogMessage?.Invoke("✅ Program kategorisi aktif");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SwitchToProgramCategory hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ✅ YENİ: Tools kategorisine geçiş
        /// </summary>
        public async Task<bool> SwitchToToolsCategory()
        {
            try
            {
                LogMessage?.Invoke("⚙️ Tools kategorisine geçiliyor...");

                if (_isGamesVisible)
                {
                    bool exitSuccess = await ExitGamesMode();
                    if (!exitSuccess)
                    {
                        LogMessage?.Invoke("❌ Games modundan çıkılamadı");
                        return false;
                    }
                }

                // Tools listesini göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                    LogMessage?.Invoke("✅ Tools listesi gösterildi");
                }

                LogMessage?.Invoke("✅ Tools kategorisi aktif");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SwitchToToolsCategory hatası: {ex.Message}");
                return false;
            }
        }
        private void ResetProgressBar()
        {
            try
            {
                // Progress bar'ı bul
                Border progressBarContainer = null;
                string[] possibleNames = { "progressBar", "EnergyBar", "StatusBar", "ProgressBarContainer" };

                foreach (var name in possibleNames)
                {
                    var element = FindElementByName<ProgressBar>(_parentWindow, name);
                    if (element != null)
                    {
                        progressBarContainer = element.Parent as Border;
                        break;
                    }
                }

                if (progressBarContainer == null)
                {
                    foreach (var name in possibleNames)
                    {
                        progressBarContainer = FindElementByName<Border>(_parentWindow, name);
                        if (progressBarContainer != null) break;
                    }
                }

                if (progressBarContainer == null)
                {
                    progressBarContainer = FindProgressBarByPosition();
                }

                if (progressBarContainer != null)
                {
                    // Transform'u resetle
                    if (progressBarContainer.RenderTransform is TranslateTransform progressTransform)
                    {
                        progressTransform.X = 0; // Pozisyonu resetle
                    }

                    // Görünürlük ve opacity'yi resetle
                    progressBarContainer.Visibility = Visibility.Visible;
                    progressBarContainer.Opacity = 1.0;
                    LogMessage?.Invoke("📊 Progress bar normal pozisyonda");
                }
                else
                {
                    LogMessage?.Invoke("⚠️ Progress bar bulunamadı - reset atlandı");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ResetProgressBar hatası: {ex.Message}");
            }
        }

        // ✅ YENİ: StartShowAnimations metodu güncellendi
        private async Task StartShowAnimations(Border gamesPanel, Border terminalPanel)
        {
            try
            {
                LogMessage?.Invoke("🎬 Animasyonlar başlatılıyor...");

                // ✅ DEĞİŞTİRİLDİ: Terminal'i tamamen kayarak kaybet
                await SlideTerminalOutCompletely(terminalPanel);

                // Games panel animasyonu
                await StartGamesOnlyAnimation(gamesPanel);

                LogMessage?.Invoke("✅ Tüm animasyonlar tamamlandı");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartShowAnimations hatası: {ex.Message}");
            }
        }
    }
}