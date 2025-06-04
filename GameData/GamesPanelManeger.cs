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

                    // ✅ YENİ: Önce sol sidebar'ı geri getir
                    await SlideSidebarIn();

                    bool success = HideGamesPanel();
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

                // 5. Animasyonları başlat
                await StartShowAnimations(gamesPanel, terminalPanel);

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

                LogMessage?.Invoke("✅ BİTİŞ: Games panel tamamen açıldı ve manuel test uygulandı!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ShowGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ENHANCED: Games panelini gizler + LOG terminal'i geri gösterir
        /// </summary>
        private bool HideGamesPanel()
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

                // ✅ YENİ: LOG Terminal'i geri göster (kayma animasyonu yok)
                terminalPanel.Visibility = Visibility.Visible;
                LogMessage?.Invoke("✅ LOG Terminal geri gösterildi");

                // Kategori listesini geri göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                    LogMessage?.Invoke("✅ Kategori listesi geri gösterildi");
                }

                LogMessage?.Invoke("✅ BİTİŞ: Games panel gizlendi, LOG terminal geri geldi");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ HideGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ✅ YENİ: Sadece Games Panel animasyonu (Terminal yok)
        /// </summary>
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

                // ✅ YENİ: Terminal'i direkt göster (animasyon yok)
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
                    terminalPanel.Height = 596; // Orijinal yükseklik
                    LogMessage?.Invoke("📺 Terminal direkt gösterildi - animasyon yok");
                }

                _isGamesVisible = false;
                LogMessage?.Invoke("✅ Force reset tamamlandı - Terminal animasyonsuz gösterildi");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ForceReset hatası: {ex.Message}");
            }
        }
    }
}