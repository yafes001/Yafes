using System;
using System.Collections.Generic;
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
    /// Games Panel yönetimi için özel class
    /// Main.xaml.cs'den games logic'ini ayırır
    /// </summary>
    public class GamesPanelManager
    {
        private readonly Window _parentWindow;
        private readonly TextBox _logTextBox;
        private bool _isGamesVisible = false;

        // Events
        public event Action<string> LogMessage;

        public GamesPanelManager(Window parentWindow, TextBox logTextBox)
        {
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));

            // Event subscription
            LogMessage += (message) => {
                _logTextBox.Dispatcher.Invoke(() => {
                    _logTextBox.AppendText(message + "\n");
                    _logTextBox.ScrollToEnd();
                });
            };
        }

        public bool IsGamesVisible => _isGamesVisible;

        /// <summary>
        /// Games panel toggle işlemi
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
                    }
                    return success;
                }
                else
                {
                    LogMessage?.Invoke("🔴 Games panel kapatılıyor...");
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
        /// Games panelini gösterir
        /// </summary>
        private async Task<bool> ShowGamesPanel()
        {
            try
            {
                LogMessage?.Invoke("🎮 BAŞLAMA: ShowGamesPanel çalışıyor...");

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

                // 2. Games Panel'i görünür yap
                gamesPanel.Visibility = Visibility.Visible;
                LogMessage?.Invoke("✅ GamesPanel.Visibility = Visible");

                // 3. Animasyonları başlat
                await StartShowAnimations(gamesPanel, terminalPanel);

                // 4. Kategori listesini gizle
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Collapsed;
                    LogMessage?.Invoke("✅ Kategori listesi gizlendi");
                }

                // 5. Oyun verilerini yükle
                LogMessage?.Invoke("📊 Oyun verileri yükleniyor...");
                await LoadGamesIntoPanel(gamesPanel);

                LogMessage?.Invoke("✅ BİTİŞ: Games panel tamamen açıldı ve yüklendi!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ShowGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Games panelini gizler
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

                LogMessage?.Invoke("✅ BİTİŞ: Games panel tamamen gizlendi");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ HideGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Panel gösterme animasyonları
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
                    To = 306,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
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
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // Opacity animasyonu
                var gamesPanelOpacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(400),
                    BeginTime = TimeSpan.FromMilliseconds(200)
                };

                LogMessage?.Invoke("🎬 Animasyonlar başlatılıyor...");

                // Animasyonları başlat
                terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                gamesPanelTransform.BeginAnimation(TranslateTransform.YProperty, gamesPanelShowAnimation);
                gamesPanel.BeginAnimation(UIElement.OpacityProperty, gamesPanelOpacityAnimation);

                // Animasyon tamamlanana kadar bekle
                await Task.Delay(700);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartShowAnimations hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Panel gizleme animasyonları
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
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(500),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                    LogMessage?.Invoke("✅ Terminal normal pozisyona döndürüldü");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartHideAnimations hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// XAML Games Panel'ine gerçek PNG posterli oyun kartları yükler
        /// </summary>
        private async Task LoadGamesIntoPanel(Border gamesPanel)
        {
            try
            {
                LogMessage?.Invoke("🔄 LoadGamesIntoPanel başlatıldı");

                // UniformGrid'i bul
                var gamesGrid = FindUniformGridMultipleWays(gamesPanel);

                if (gamesGrid == null)
                {
                    LogMessage?.Invoke("❌ KRITIK: UniformGrid bulunamadı!");
                    gamesGrid = CreateFallbackUniformGrid(gamesPanel);
                    if (gamesGrid == null)
                    {
                        LogMessage?.Invoke("❌ HATA: Fallback UniformGrid de oluşturulamadı!");
                        return;
                    }
                }

                LogMessage?.Invoke($"✅ UniformGrid bulundu! Columns: {gamesGrid.Columns}");

                // Gerçek oyun verilerini yükle
                LogMessage?.Invoke("📊 GameDataManager'dan oyunlar alınıyor...");
                var games = await Yafes.Managers.GameDataManager.GetAllGamesAsync();

                LogMessage?.Invoke($"📊 GameDataManager'dan {games?.Count ?? 0} oyun geldi");

                if (games == null || games.Count == 0)
                {
                    LogMessage?.Invoke("⚠️ GameDataManager'dan oyun gelmedi, fallback stratejiler deneniyor...");
                    Yafes.Managers.GameDataManager.ClearCache();
                    games = await Yafes.Managers.GameDataManager.GetAllGamesAsync();

                    if (games == null || games.Count == 0)
                    {
                        LogMessage?.Invoke("⚠️ Cache temizleme de işe yaramadı, statik oyun listesi oluşturuluyor...");
                        games = CreateFallbackGamesList();
                    }
                }

                LogMessage?.Invoke($"✅ Toplam işlenecek oyun: {games.Count}");

                // Mevcut kartları temizle
                int existingCount = gamesGrid.Children.Count;
                gamesGrid.Children.Clear();
                LogMessage?.Invoke($"🧹 {existingCount} mevcut kart temizlendi");

                // PNG posterli gerçek oyun kartlarını ekle
                int addedCount = 0;
                int maxCards = Math.Min(games.Count, 12);

                foreach (var game in games.Take(maxCards))
                {
                    try
                    {
                        var gameCard = CreateRealPosterGameCard(game);
                        gamesGrid.Children.Add(gameCard);
                        addedCount++;

                        if (addedCount % 4 == 0)
                        {
                            LogMessage?.Invoke($"📊 {addedCount}/{maxCards} PNG posterli kart oluşturuldu");
                            await Task.Delay(10);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"❌ PNG posterli kart oluşturma hatası: {game.Name} - {ex.Message}");
                    }
                }

                LogMessage?.Invoke($"✅ {addedCount} PNG posterli oyun kartı başarıyla eklendi!");
                LogMessage?.Invoke($"🎮 Toplam oyun: {games.Count}, Gösterilen: {addedCount}");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ LoadGamesIntoPanel KRITIK HATA: {ex.Message}");
                await CreateEmergencyGameCards(gamesPanel);
            }
        }

        /// <summary>
        /// PNG posterli oyun kartı oluşturur
        /// </summary>
        private Border CreateRealPosterGameCard(Yafes.Models.GameData game)
        {
            try
            {
                var gameCard = new Border
                {
                    Width = 140,
                    Height = 200,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };

                var grid = new Grid();

                // Ana poster image
                var posterImage = new Image
                {
                    Source = Yafes.Managers.ImageManager.GetGameImage(game.ImageName),
                    Stretch = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Hover overlay
                var hoverOverlay = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 255, 165, 0)),
                    Opacity = 0
                };

                // Bottom overlay panel
                var overlayPanel = new StackPanel
                {
                    Background = new LinearGradientBrush(
                        Color.FromArgb(200, 0, 0, 0),
                        Color.FromArgb(100, 0, 0, 0),
                        new Point(0, 1),
                        new Point(0, 0)
                    ),
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                // Game name
                var nameText = new TextBlock
                {
                    Text = game.Name,
                    Foreground = Brushes.White,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5, 3, 5, 2),
                    TextAlignment = TextAlignment.Center,
                    MaxHeight = 30
                };

                // Game status
                var statusText = new TextBlock
                {
                    Text = game.IsInstalled ? "✅ Kurulu" : $"📥 {game.Size ?? "Bilinmiyor"}",
                    Foreground = game.IsInstalled ?
                        new SolidColorBrush(Color.FromRgb(0, 255, 100)) :
                        new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    FontSize = 7,
                    Margin = new Thickness(5, 0, 5, 3),
                    TextAlignment = TextAlignment.Center
                };

                overlayPanel.Children.Add(nameText);
                overlayPanel.Children.Add(statusText);

                grid.Children.Add(posterImage);
                grid.Children.Add(hoverOverlay);
                grid.Children.Add(overlayPanel);

                gameCard.Child = grid;

                // Click event
                gameCard.MouseLeftButtonDown += (s, e) => {
                    LogMessage?.Invoke($"🎯 {game.Name} seçildi!");
                    LogMessage?.Invoke($"📂 Kategori: {game.Category} | Boyut: {game.Size ?? "Bilinmiyor"}");
                    LogMessage?.Invoke($"🖼️ Poster: {game.ImageName}");
                };

                // Hover effects
                AddHoverEffects(gameCard, hoverOverlay);

                return gameCard;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateRealPosterGameCard hatası: {game.Name} - {ex.Message}");
                return CreateSimpleFallbackCard(game);
            }
        }

        /// <summary>
        /// Hover efektleri ekler
        /// </summary>
        private void AddHoverEffects(Border gameCard, Border hoverOverlay)
        {
            gameCard.MouseEnter += (s, e) => {
                var storyboard = new Storyboard();
                var scaleAnimation = new DoubleAnimation(1.0, 1.05, TimeSpan.FromMilliseconds(200));
                Storyboard.SetTarget(scaleAnimation, gameCard);
                Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("RenderTransform.ScaleX"));
                storyboard.Children.Add(scaleAnimation);

                var scaleAnimationY = new DoubleAnimation(1.0, 1.05, TimeSpan.FromMilliseconds(200));
                Storyboard.SetTarget(scaleAnimationY, gameCard);
                Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath("RenderTransform.ScaleY"));
                storyboard.Children.Add(scaleAnimationY);

                gameCard.RenderTransform = new ScaleTransform();
                gameCard.RenderTransformOrigin = new Point(0.5, 0.5);
                storyboard.Begin();

                hoverOverlay.Opacity = 1;
            };

            gameCard.MouseLeave += (s, e) => {
                var storyboard = new Storyboard();
                var scaleAnimation = new DoubleAnimation(1.05, 1.0, TimeSpan.FromMilliseconds(200));
                Storyboard.SetTarget(scaleAnimation, gameCard);
                Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("RenderTransform.ScaleX"));
                storyboard.Children.Add(scaleAnimation);

                var scaleAnimationY = new DoubleAnimation(1.05, 1.0, TimeSpan.FromMilliseconds(200));
                Storyboard.SetTarget(scaleAnimationY, gameCard);
                Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath("RenderTransform.ScaleY"));
                storyboard.Children.Add(scaleAnimationY);

                storyboard.Begin();
                hoverOverlay.Opacity = 0;
            };
        }

        // ========================================
        // GamesPanelManager.cs - Part 2 (Helper Methods)
        // Önceki kodun devamı - aynı class içine ekle
        // ========================================

        /// <summary>
        /// Basit fallback kart oluşturur
        /// </summary>
        private Border CreateSimpleFallbackCard(Yafes.Models.GameData game)
        {
            var gameCard = new Border
            {
                Width = 140,
                Height = 200,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 65)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Oyun ikonu
            var iconText = new TextBlock
            {
                Text = GetGameIcon(game.Category),
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 10)
            };

            // Oyun adı
            var nameText = new TextBlock
            {
                Text = game.Name,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                Margin = new Thickness(5, 0, 5, 5),
                TextAlignment = TextAlignment.Center
            };

            // Durum
            var statusText = new TextBlock
            {
                Text = game.IsInstalled ? "✅ Kurulu" : $"📥 {game.Size ?? "Bilinmiyor"}",
                FontSize = 8,
                Foreground = game.IsInstalled ? Brushes.LightGreen : Brushes.LightGray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            stackPanel.Children.Add(iconText);
            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(statusText);
            gameCard.Child = stackPanel;

            return gameCard;
        }

        /// <summary>
        /// Emergency kartlar oluşturur
        /// </summary>
        private async Task CreateEmergencyGameCards(Border gamesPanel)
        {
            try
            {
                LogMessage?.Invoke("🆘 EMERGENCY: CreateEmergencyGameCards başlatıldı");

                var gamesGrid = FindUniformGridMultipleWays(gamesPanel);

                if (gamesGrid == null)
                {
                    LogMessage?.Invoke("❌ EMERGENCY: UniformGrid bulunamadı, emergency de iptal");
                    return;
                }

                var emergencyGames = new List<Yafes.Models.GameData>
                {
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_1",
                        Name = "Games Loading...",
                        ImageName = "loading.png",
                        Category = "System",
                        Size = "...",
                        IsInstalled = false,
                        Description = "Oyunlar yükleniyor..."
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_2",
                        Name = "Please Wait",
                        ImageName = "wait.png",
                        Category = "System",
                        Size = "...",
                        IsInstalled = false,
                        Description = "Lütfen bekleyin..."
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_3",
                        Name = "Loading Error",
                        ImageName = "error.png",
                        Category = "System",
                        Size = "Error",
                        IsInstalled = false,
                        Description = "Yükleme hatası oluştu"
                    }
                };

                LogMessage?.Invoke($"🆘 {emergencyGames.Count} emergency kart oluşturuluyor...");

                foreach (var game in emergencyGames)
                {
                    try
                    {
                        var emergencyCard = CreateSimpleFallbackCard(game);
                        gamesGrid.Children.Add(emergencyCard);
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"❌ Emergency kart hatası: {game.Name} - {ex.Message}");
                    }
                }

                LogMessage?.Invoke("✅ Emergency kartlar oluşturuldu");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateEmergencyGameCards KRITIK HATA: {ex.Message}");
            }
        }

        /// <summary>
        /// Fallback oyun listesi
        /// </summary>
        private List<Yafes.Models.GameData> CreateFallbackGamesList()
        {
            var fallbackGames = new List<Yafes.Models.GameData>
            {
                new Yafes.Models.GameData
                {
                    Id = "steam",
                    Name = "Steam",
                    ImageName = "steam.png",
                    Category = "Platform",
                    Size = "150 MB",
                    IsInstalled = false,
                    Description = "PC Gaming Platform"
                },
                new Yafes.Models.GameData
                {
                    Id = "epic_games",
                    Name = "Epic Games",
                    ImageName = "epic_games.png",
                    Category = "Platform",
                    Size = "200 MB",
                    IsInstalled = false,
                    Description = "Epic Games Store"
                },
                new Yafes.Models.GameData
                {
                    Id = "gog_galaxy",
                    Name = "GOG Galaxy",
                    ImageName = "gog_galaxy.png",
                    Category = "Platform",
                    Size = "80 MB",
                    IsInstalled = false,
                    Description = "DRM-Free Gaming Platform"
                },
                new Yafes.Models.GameData
                {
                    Id = "origin",
                    Name = "Origin",
                    ImageName = "origin.png",
                    Category = "Platform",
                    Size = "120 MB",
                    IsInstalled = false,
                    Description = "EA Games Platform"
                },
                new Yafes.Models.GameData
                {
                    Id = "battle_net",
                    Name = "Battle.net",
                    ImageName = "battle_net.png",
                    Category = "Platform",
                    Size = "90 MB",
                    IsInstalled = false,
                    Description = "Blizzard Games Platform"
                },
                new Yafes.Models.GameData
                {
                    Id = "ubisoft_connect",
                    Name = "Ubisoft Connect",
                    ImageName = "ubisoft_connect.png",
                    Category = "Platform",
                    Size = "110 MB",
                    IsInstalled = false,
                    Description = "Ubisoft Gaming Platform"
                }
            };

            LogMessage?.Invoke($"🆘 {fallbackGames.Count} fallback platform kartı oluşturuldu");
            return fallbackGames;
        }

        /// <summary>
        /// UniformGrid çoklu yöntemle bulur
        /// </summary>
        private UniformGrid FindUniformGridMultipleWays(Border gamesPanel)
        {
            try
            {
                // YÖNTEM 1: Name ile bul
                var gamesGrid = FindChild<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid != null)
                {
                    LogMessage?.Invoke("✅ UniformGrid bulundu (Name ile)");
                    return gamesGrid;
                }

                // YÖNTEM 2: Type bazlı arama
                gamesGrid = FindChild<UniformGrid>(gamesPanel, null);
                if (gamesGrid != null)
                {
                    LogMessage?.Invoke("✅ UniformGrid bulundu (Type ile)");
                    return gamesGrid;
                }

                // YÖNTEM 3: Visual Tree taraması
                gamesGrid = FindUniformGridInVisualTree(gamesPanel);
                if (gamesGrid != null)
                {
                    LogMessage?.Invoke("✅ UniformGrid bulundu (Visual Tree)");
                    return gamesGrid;
                }

                LogMessage?.Invoke("❌ Hiçbir yöntemle UniformGrid bulunamadı");
                return null;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ FindUniformGridMultipleWays hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fallback UniformGrid oluşturur
        /// </summary>
        private UniformGrid CreateFallbackUniformGrid(Border gamesPanel)
        {
            try
            {
                LogMessage?.Invoke("🆘 Fallback UniformGrid oluşturuluyor...");

                var content = gamesPanel.Child as Canvas;
                if (content == null)
                {
                    LogMessage?.Invoke("❌ GamesPanel Canvas'ı bulunamadı");
                    return null;
                }

                ScrollViewer scrollViewer = null;
                foreach (var child in content.Children)
                {
                    if (child is ScrollViewer sv)
                    {
                        scrollViewer = sv;
                        break;
                    }
                }

                if (scrollViewer == null)
                {
                    LogMessage?.Invoke("❌ ScrollViewer bulunamadı");
                    return null;
                }

                var newGrid = new UniformGrid
                {
                    Columns = 4,
                    Margin = new Thickness(10)
                };

                scrollViewer.Content = newGrid;
                LogMessage?.Invoke("✅ Yeni UniformGrid oluşturuldu ve atandı");

                return newGrid;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateFallbackUniformGrid hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Visual Tree'de UniformGrid arar
        /// </summary>
        private UniformGrid FindUniformGridInVisualTree(DependencyObject parent)
        {
            if (parent == null) return null;

            if (parent is UniformGrid uniformGrid)
            {
                return uniformGrid;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindUniformGridInVisualTree(child);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// Generic child finder
        /// </summary>
        private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
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

        /// <summary>
        /// Element finder by name
        /// </summary>
        private static T FindElementByName<T>(DependencyObject parent, string name) where T : FrameworkElement
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
        /// Element finder by tag
        /// </summary>
        private static T FindElementByTag<T>(DependencyObject parent, string tag) where T : FrameworkElement
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
        /// Kategori ikonları
        /// </summary>
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
                "platform" => "🎮",
                "system" => "⚙️",
                "general" => "🎮",
                _ => "🎮"
            };
        }

        /// <summary>
        /// PNG poster test metodu
        /// </summary>
        public void TestPosterLoading()
        {
            LogMessage?.Invoke("🔍 PNG Poster yükleme testi başlatılıyor...");

            var testImages = new[] {
                "cyberpunk.png",
                "grand_theft_auto_v.png",
                "the_witcher_3_ce.png",
                "elden_rıng.png",
                "god_of_war.png",
                "baldur_s_gate_3.png",
                "hogwarts_legacy.png",
                "red_dead_redemption_2.png"
            };

            foreach (var imageName in testImages)
            {
                try
                {
                    var image = Yafes.Managers.ImageManager.GetGameImage(imageName);
                    bool loaded = (image != null && image != Yafes.Managers.ImageManager.GetDefaultImage());
                    LogMessage?.Invoke($"🖼️ {imageName}: {(loaded ? "✅ LOADED" : "❌ DEFAULT FALLBACK")}");
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"❌ {imageName}: HATA - {ex.Message}");
                }
            }

            LogMessage?.Invoke("🔍 Poster test tamamlandı");
        }

        /// <summary>
        /// Memory cleanup
        /// </summary>
        public void Dispose()
        {
            LogMessage?.Invoke("🧹 GamesPanelManager temizleniyor...");
            // Event unsubscribe vs. gerekirse buraya eklenebilir
        }
    }
}
