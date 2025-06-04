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
                // Sol sidebar'ı bul (Name ile)
                _leftSidebar = FindElementByName<Border>(_parentWindow, "LeftSidebar");

                if (_leftSidebar != null)
                {
                    // TranslateTransform'u bul veya oluştur
                    _leftSidebarTransform = _leftSidebar.RenderTransform as TranslateTransform;
                    if (_leftSidebarTransform == null)
                    {
                        _leftSidebarTransform = new TranslateTransform();
                        _leftSidebar.RenderTransform = _leftSidebarTransform;
                    }
                    LogMessage?.Invoke("✅ Sol sidebar slide sistemi hazır");
                }
                else
                {
                    LogMessage?.Invoke("⚠️ LeftSidebar bulunamadı - Name='LeftSidebar' kontrol edin");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Sidebar başlatma hatası: {ex.Message}");
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

                // 4. Animasyonları başlat
                await StartShowAnimations(gamesPanel, terminalPanel);

                // 5. Kategori listesini gizle
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Collapsed;
                    LogMessage?.Invoke("✅ Kategori listesi gizlendi");
                }

                // 6. ✅ ENHANCED: Oyun verilerini tam genişlik modunda yükle
                LogMessage?.Invoke("📊 Oyun verileri tam genişlik modunda yükleniyor...");
                await LoadGamesIntoPanel(gamesPanel);

                LogMessage?.Invoke("✅ BİTİŞ: Games panel tamamen açıldı ve maksimum genişlikte yüklendi!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ShowGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ENHANCED: Games panelini gizler + Normal boyuta döndürür
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

                // ✅ YENİ: Games panel'i normal boyuta döndür
                ResizeGamesPanel(false);

                // Games panel'i gizle
                gamesPanel.Visibility = Visibility.Collapsed;
                LogMessage?.Invoke("✅ GamesPanel.Visibility = Collapsed");

                // Terminal'i normale döndür
                StartHideAnimations(terminalPanel);

                // Kategori listesini geri göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                    LogMessage?.Invoke("✅ Kategori listesi geri gösterildi");
                }

                LogMessage?.Invoke("✅ BİTİŞ: Games panel tamamen gizlendi ve normal layout restore edildi");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ HideGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ENHANCED: Panel gösterme animasyonları - Daha agresif layout değişikliği
        /// </summary>
        private async Task StartShowAnimations(Border gamesPanel, Border terminalPanel)
        {
            try
            {
                // Terminal animasyonu
                var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                if (terminalTransform == null)
                {
                    terminalTransform = new TranslateTransform();
                    terminalPanel.RenderTransform = terminalTransform;
                }

                var terminalMoveAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 520, // ✅ GÜNCELLENME: Terminal'i daha aşağı kay (520px)
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                // ✅ ENHANCED: Terminal yüksekliğini çok küçült
                var terminalResizeAnimation = new DoubleAnimation
                {
                    From = 596,
                    To = 76, // ✅ GÜNCELLENME: Sadece 76px yükseklik (minimal log alanı)
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

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

                LogMessage?.Invoke("🎬 Enhanced layout animasyonları başlatılıyor...");

                // ✅ ENHANCED: Tüm animasyonları birlikte başlat
                terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                terminalPanel.BeginAnimation(FrameworkElement.HeightProperty, terminalResizeAnimation);
                gamesPanelTransform.BeginAnimation(TranslateTransform.YProperty, gamesPanelShowAnimation);
                gamesPanel.BeginAnimation(UIElement.OpacityProperty, gamesPanelOpacityAnimation);

                // Animasyon tamamlanana kadar bekle
                await Task.Delay(700);

                LogMessage?.Invoke("✅ Layout animasyonları tamamlandı - Games modu aktif!");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartShowAnimations hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// MEVCUT: Panel gizleme animasyonları (terminal yüksekliği güncellendi)
        /// </summary>
        private void StartHideAnimations(Border terminalPanel)
        {
            try
            {
                var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                if (terminalTransform != null)
                {
                    var terminalMoveAnimation = new DoubleAnimation
                    {
                        To = 0, // Normal pozisyona dön
                        Duration = TimeSpan.FromMilliseconds(500),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    // ✅ YENİ: Terminal yüksekliğini normale döndür
                    var terminalResizeAnimation = new DoubleAnimation
                    {
                        To = 596, // Orijinal yüksekliğe dön
                        Duration = TimeSpan.FromMilliseconds(500),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                    terminalPanel.BeginAnimation(FrameworkElement.HeightProperty, terminalResizeAnimation);
                    LogMessage?.Invoke("🎬 Terminal gizleme animasyonu başlatıldı");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartHideAnimations hatası: {ex.Message}");
            }
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

                // ✅ ENHANCED: Tam genişlik modunda çok daha fazla sütun
                if (_leftSidebar != null && _leftSidebarTransform != null && _leftSidebarTransform.X < -200)
                {
                    gamesGrid.Columns = 12; // ✅ ENHANCED: Tam genişlik modunda 12 sütun! (maksimum oyun)
                    LogMessage?.Invoke("📊 TAM GENİŞLİK MODU: 12 sütun oyun grid'i - Maksimum oyun listesi!");
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

                // Her oyun için kart oluştur - ✅ ENHANCED: Daha fazla oyun göster
                foreach (var game in games.Take(60)) // ✅ ENHANCED: Maksimum 60 oyun göster (12x5 grid)
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

                    // ✅ YENİ: Panel boyutunu normale döndür
                    ResizeGamesPanel(false);

                    LogMessage?.Invoke("🔴 Games panel gizlendi ve normal boyuta döndürüldü");
                }

                // Terminal'i normale döndür - ENHANCED
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");
                if (terminalPanel != null)
                {
                    if (terminalPanel.RenderTransform is TranslateTransform terminalTransform)
                    {
                        terminalTransform.Y = 0; // Pozisyonu resetle
                    }

                    // Yüksekliği normale döndür
                    terminalPanel.Height = 596; // Orijinal yükseklik
                    LogMessage?.Invoke("📺 Terminal normal boyutta");
                }

                _isGamesVisible = false;
                LogMessage?.Invoke("✅ Enhanced force reset tamamlandı - Normal layout restored");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ForceReset hatası: {ex.Message}");
            }
        }
    }
}