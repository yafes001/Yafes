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
    /// Games Panel yönetimi - XAML Storyboard entegrasyonlu
    /// Terminal ve Progress Bar animasyonları XAML'den çalıştırılıyor
    /// </summary>
    public class GamesPanelManager
    {
        private readonly Window _parentWindow;
        private readonly TextBox _logTextBox;
        private bool _isGamesVisible = false;

        // Slide Animation için gerekli referanslar
        private Border _leftSidebar;
        private TranslateTransform _leftSidebarTransform;
        private const double SIDEBAR_SLIDE_DISTANCE = -280;
        private const double ANIMATION_DURATION = 600;

        // XAML Storyboard referansları
        private Storyboard _terminalSlideOut;
        private Storyboard _terminalSlideIn;
        private Storyboard _progressBarSlideOut;
        private Storyboard _progressBarSlideIn;

        // Events
        public event Action<string> LogMessage;

        public GamesPanelManager(Window parentWindow, TextBox logTextBox)
        {
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));

            LogMessage += (message) => {
                _logTextBox.Dispatcher.Invoke(() => {
                    _logTextBox.AppendText(message + "\n");
                    _logTextBox.ScrollToEnd();
                });
            };

            InitializeSidebarElements();
            InitializeStoryboards();
        }

        public bool IsGamesVisible => _isGamesVisible;

        /// <summary>
        /// XAML Storyboard'ları initialize eder
        /// </summary>
        private void InitializeStoryboards()
        {
            try
            {
                // XAML'deki Storyboard'ları bul
                _terminalSlideOut = _parentWindow.FindResource("TerminalSlideOut") as Storyboard;
                _terminalSlideIn = _parentWindow.FindResource("TerminalSlideIn") as Storyboard;
                _progressBarSlideOut = _parentWindow.FindResource("ProgressBarSlideOut") as Storyboard;
                _progressBarSlideIn = _parentWindow.FindResource("ProgressBarSlideIn") as Storyboard;

                LogMessage?.Invoke($"✅ Storyboard'lar yüklendi: Terminal({_terminalSlideOut != null}), Progress({_progressBarSlideOut != null})");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Storyboard initialization hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Sol sidebar için element referanslarını başlatır
        /// </summary>
        private void InitializeSidebarElements()
        {
            try
            {
                string[] possibleNames = {
                    "SystemInfoPanel", "LeftPanel", "InfoPanel", "SystemPanel",
                    "LeftSidebar", "leftSidebar", "SidePanel"
                };

                foreach (var name in possibleNames)
                {
                    _leftSidebar = FindElementByName<Border>(_parentWindow, name);
                    if (_leftSidebar != null) break;
                }

                if (_leftSidebar == null)
                {
                    foreach (var name in possibleNames)
                    {
                        var panel = FindElementByName<Grid>(_parentWindow, name);
                        if (panel != null)
                        {
                            var parent = VisualTreeHelper.GetParent(panel);
                            while (parent != null && !(parent is Border))
                            {
                                parent = VisualTreeHelper.GetParent(parent);
                            }
                            _leftSidebar = parent as Border;
                            if (_leftSidebar != null) break;
                        }
                    }
                }

                if (_leftSidebar == null)
                {
                    foreach (var name in possibleNames)
                    {
                        var panel = FindElementByName<StackPanel>(_parentWindow, name);
                        if (panel != null)
                        {
                            var parent = VisualTreeHelper.GetParent(panel);
                            while (parent != null && !(parent is Border))
                            {
                                parent = VisualTreeHelper.GetParent(parent);
                            }
                            _leftSidebar = parent as Border;
                            if (_leftSidebar != null) break;
                        }
                    }
                }

                if (_leftSidebar != null)
                {
                    _leftSidebarTransform = _leftSidebar.RenderTransform as TranslateTransform;
                    if (_leftSidebarTransform == null)
                    {
                        _leftSidebarTransform = new TranslateTransform();
                        _leftSidebar.RenderTransform = _leftSidebarTransform;
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda sidebar devre dışı
            }
        }

        /// <summary>
        /// Games panel boyutunu ayarlar (tam genişlik/normal mod)
        /// </summary>
        private void ResizeGamesPanel(bool fullWidth)
        {
            try
            {
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel == null) return;

                if (fullWidth)
                {
                    gamesPanel.Width = Double.NaN;
                    gamesPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                    gamesPanel.Margin = new Thickness(0, 0, 305, 0);
                }
                else
                {
                    gamesPanel.Width = 800;
                    gamesPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    gamesPanel.Margin = new Thickness(5);
                }

                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid != null)
                {
                    gamesGrid.Columns = fullWidth ? 10 : 4;
                }

                var gamesTitlePanel = FindElementByTag<Border>(_parentWindow, "GamesTitlePanel") ??
                                    FindElementByName<Border>(_parentWindow, "GamesTitlePanel");
                if (gamesTitlePanel != null)
                {
                    if (fullWidth)
                    {
                        gamesTitlePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                        gamesTitlePanel.Margin = new Thickness(0, 0, 305, 0);
                    }
                    else
                    {
                        gamesTitlePanel.HorizontalAlignment = HorizontalAlignment.Center;
                        gamesTitlePanel.Margin = new Thickness(5);
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda işlem iptal
            }
        }

        /// <summary>
        /// Games panel toggle işlemi
        /// </summary>
        public async Task<bool> ToggleGamesPanel()
        {
            try
            {
                LogMessage?.Invoke($"🎮 Games toggle - Mevcut durum: {(_isGamesVisible ? "AÇIK" : "KAPALI")}");

                if (!_isGamesVisible)
                {
                    bool success = await ShowGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = true;
                        await SlideSidebarOut();
                    }
                    return success;
                }
                else
                {
                    LogMessage?.Invoke("🔄 Games panel kapatılıyor...");
                    await SlideSidebarIn();
                    bool success = await HideGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = false;
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
        /// Sol sidebar'ı sola kaydırarak gizler
        /// </summary>
        private async Task SlideSidebarOut()
        {
            try
            {
                if (_leftSidebar == null || _leftSidebarTransform == null) return;

                LogMessage?.Invoke("⬅️ Sol sidebar gizleniyor...");

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = SIDEBAR_SLIDE_DISTANCE,
                    Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    LogMessage?.Invoke("✅ Sol sidebar gizlendi");
                    tcs.SetResult(true);
                };

                _leftSidebarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Sidebar slide out hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Sol sidebar'ı normal pozisyona geri getirir
        /// </summary>
        private async Task SlideSidebarIn()
        {
            try
            {
                if (_leftSidebar == null || _leftSidebarTransform == null) return;

                LogMessage?.Invoke("➡️ Sol sidebar geri getiriliyor...");

                var slideAnimation = new DoubleAnimation
                {
                    From = SIDEBAR_SLIDE_DISTANCE,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    LogMessage?.Invoke("✅ Sol sidebar normal pozisyonda");
                    tcs.SetResult(true);
                };

                _leftSidebarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Sidebar slide in hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Games panelini gösterir - XAML Storyboard kullanır
        /// </summary>
        private async Task<bool> ShowGamesPanel()
        {
            try
            {
                LogMessage?.Invoke("🎮 Games panel açılıyor...");

                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    LogMessage?.Invoke("❌ Panel'ler bulunamadı");
                    return false;
                }

                // 1. Games panel'i göster ve boyutlandır
                ResizeGamesPanel(true);
                gamesPanel.Visibility = Visibility.Visible;
                LogMessage?.Invoke("✅ Games panel görünür yapıldı");

                // 2. Terminal'i XAML storyboard ile gizle
                if (_terminalSlideOut != null)
                {
                    LogMessage?.Invoke("🎬 Terminal slide out animasyonu başlatılıyor...");

                    var tcs = new TaskCompletionSource<bool>();
                    _terminalSlideOut.Completed += (s, e) => {
                        terminalPanel.Visibility = Visibility.Collapsed;
                        LogMessage?.Invoke("✅ Terminal gizlendi");
                        tcs.SetResult(true);
                    };

                    _terminalSlideOut.Begin();
                    await tcs.Task;
                }
                else
                {
                    LogMessage?.Invoke("⚠️ Terminal storyboard bulunamadı, direkt gizleniyor");
                    terminalPanel.Visibility = Visibility.Collapsed;
                }

                // 3. Progress bar'ı XAML storyboard ile gizle
                if (_progressBarSlideOut != null)
                {
                    LogMessage?.Invoke("🎬 Progress bar slide out animasyonu başlatılıyor...");

                    var progressContainer = FindElementByName<Border>(_parentWindow, "ProgressBarContainer");
                    var tcs2 = new TaskCompletionSource<bool>();
                    _progressBarSlideOut.Completed += (s, e) => {
                        if (progressContainer != null) progressContainer.Visibility = Visibility.Collapsed;
                        LogMessage?.Invoke("✅ Progress bar gizlendi");
                        tcs2.SetResult(true);
                    };

                    _progressBarSlideOut.Begin();
                    await tcs2.Task;
                }

                // 4. Kategori listesini gizle
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Collapsed;
                }

                // 5. Oyun verilerini yükle
                await LoadGamesIntoPanel(gamesPanel);

                LogMessage?.Invoke("✅ Games panel tamamen açıldı!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ShowGamesPanel hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Games panelini gizler - XAML Storyboard kullanır
        /// </summary>
        private async Task<bool> HideGamesPanel()
        {
            try
            {
                LogMessage?.Invoke("🔄 Games panel gizleniyor...");

                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    LogMessage?.Invoke("❌ Panel'ler bulunamadı");
                    return false;
                }

                // 1. Games panel boyutunu normale döndür ve gizle
                ResizeGamesPanel(false);
                gamesPanel.Visibility = Visibility.Collapsed;
                LogMessage?.Invoke("✅ Games panel gizlendi");

                // 2. Terminal'i XAML storyboard ile geri getir
                if (_terminalSlideIn != null)
                {
                    LogMessage?.Invoke("🎬 Terminal slide in animasyonu başlatılıyor...");

                    // Önce terminal'i görünür yap
                    terminalPanel.Visibility = Visibility.Visible;
                    terminalPanel.Opacity = 0; // Başlangıçta görünmez

                    var tcs = new TaskCompletionSource<bool>();
                    _terminalSlideIn.Completed += (s, e) => {
                        LogMessage?.Invoke("✅ Terminal geri geldi");
                        tcs.SetResult(true);
                    };

                    _terminalSlideIn.Begin();
                    await tcs.Task;
                }
                else
                {
                    LogMessage?.Invoke("⚠️ Terminal storyboard bulunamadı, direkt gösteriliyor");
                    terminalPanel.Visibility = Visibility.Visible;
                    terminalPanel.Opacity = 1;

                    // Transform'u sıfırla
                    var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                    if (terminalTransform != null)
                    {
                        terminalTransform.Y = 0;
                    }
                }

                // 3. Progress bar'ı XAML storyboard ile geri getir
                if (_progressBarSlideIn != null)
                {
                    LogMessage?.Invoke("🎬 Progress bar slide in animasyonu başlatılıyor...");

                    var progressContainer = FindElementByName<Border>(_parentWindow, "ProgressBarContainer");
                    if (progressContainer != null)
                    {
                        progressContainer.Visibility = Visibility.Visible;
                        progressContainer.Opacity = 0; // Başlangıçta görünmez
                    }

                    var tcs2 = new TaskCompletionSource<bool>();
                    _progressBarSlideIn.Completed += (s, e) => {
                        LogMessage?.Invoke("✅ Progress bar geri geldi");
                        tcs2.SetResult(true);
                    };

                    _progressBarSlideIn.Begin();
                    await tcs2.Task;
                }
                else
                {
                    LogMessage?.Invoke("⚠️ Progress bar storyboard bulunamadı");
                }

                // 4. Kategori listesini geri göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                }

                LogMessage?.Invoke("✅ Games panel kapatıldı, tüm elementler geri geldi!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ HideGamesPanel hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Oyun verilerini panel'e yükler
        /// </summary>
        private async Task LoadGamesIntoPanel(Border gamesPanel)
        {
            try
            {
                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid == null) return;

                gamesGrid.Children.Clear();

                if (_leftSidebar != null && _leftSidebarTransform != null && _leftSidebarTransform.X < -200)
                {
                    gamesGrid.Columns = 8;
                }
                else
                {
                    gamesGrid.Columns = 4;
                }

                var games = await Yafes.Managers.GameDataManager.GetAllGamesAsync();

                if (games == null || games.Count == 0)
                {
                    CreateDefaultGameCards(gamesGrid);
                    return;
                }

                foreach (var game in games.Take(40))
                {
                    var gameCard = CreateGameCard(game);
                    if (gameCard != null)
                    {
                        gamesGrid.Children.Add(gameCard);
                    }
                }
            }
            catch (Exception ex)
            {
                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid != null)
                {
                    CreateDefaultGameCards(gamesGrid);
                }
            }
        }

        /// <summary>
        /// Gerçek oyun verisinden oyun kartı oluşturur
        /// </summary>
        private Border CreateGameCard(Yafes.Models.GameData game)
        {
            try
            {
                var gameCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Height = 100,
                    Cursor = Cursors.Hand,
                    Tag = game,
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
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5)
                };

                if (!string.IsNullOrEmpty(game.ImageName))
                {
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

                        var imageName = Path.GetFileNameWithoutExtension(game.ImageName);
                        var bitmapImage = Yafes.Managers.ImageManager.GetGameImage(imageName);

                        if (bitmapImage != null)
                        {
                            gameImage.Source = bitmapImage;
                            stackPanel.Children.Add(gameImage);
                        }
                        else
                        {
                            AddCategoryIcon(stackPanel, game.Category);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddCategoryIcon(stackPanel, game.Category);
                    }
                }
                else
                {
                    AddCategoryIcon(stackPanel, game.Category);
                }

                // Oyun ismi
                var gameNameText = new TextBlock
                {
                    Text = game.Name,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
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
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                // Kategori bilgisi
                var gameCategoryText = new TextBlock
                {
                    Text = game.Category,
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 205, 170)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 1, 0, 0)
                };

                stackPanel.Children.Add(gameNameText);
                stackPanel.Children.Add(gameSizeText);
                stackPanel.Children.Add(gameCategoryText);

                gameCard.Child = stackPanel;

                gameCard.MouseEnter += GameCard_MouseEnter;
                gameCard.MouseLeave += GameCard_MouseLeave;
                gameCard.MouseLeftButtonDown += GameCard_Click;

                return gameCard;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Kategori ikonunu ekler (afiş bulunamazsa)
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
        /// Kategori ikonunu döndürür
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
        /// Varsayılan oyun kartları oluşturur (fallback)
        /// </summary>
        private void CreateDefaultGameCards(UniformGrid gamesGrid)
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                // Hata durumunda sessiz devam
            }
        }

        /// <summary>
        /// Varsayılan oyun kartı oluşturur
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

                gameCard.MouseEnter += GameCard_MouseEnter;
                gameCard.MouseLeave += GameCard_MouseLeave;
                gameCard.MouseLeftButtonDown += GameCard_Click;

                return gameCard;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Game Card hover enter event
        /// </summary>
        private void GameCard_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    var scaleTransform = new ScaleTransform(1.05, 1.05);
                    card.RenderTransform = scaleTransform;
                    card.RenderTransformOrigin = new Point(0.5, 0.5);

                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
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
                // Hata durumunda sessiz devam
            }
        }

        /// <summary>
        /// Game Card hover leave event
        /// </summary>
        private void GameCard_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    card.RenderTransform = scaleTransform;

                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
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
                // Hata durumunda sessiz devam
            }
        }

        /// <summary>
        /// Game Card click event
        /// </summary>
        private void GameCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    string gameName = "Unknown Game";

                    if (card.Tag is Yafes.Models.GameData gameData)
                    {
                        gameName = gameData.Name;
                        LogMessage?.Invoke($"🎯 {gameName} kurulum kuyruğuna eklendi!");
                        // TODO: Gerçek kurulum kuyruğuna ekleme işlemi
                        // queueManager.AddGameToQueue(gameData);
                    }
                    else
                    {
                        var stackPanel = card.Child as StackPanel;
                        if (stackPanel?.Children.Count >= 2 && stackPanel.Children[1] is TextBlock gameNameTextBlock)
                        {
                            gameName = gameNameTextBlock.Text;
                            LogMessage?.Invoke($"🎯 {gameName} kurulum kuyruğuna eklendi!");
                        }
                    }

                    CreateShakeEffect(card);
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda sessiz devam
            }
        }

        /// <summary>
        /// Kart titreşim efekti
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
                // Hata durumunda sessiz devam
            }
        }

        // ============ UTILITY METODLAR ============

        /// <summary>
        /// Tag ile element bulur
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
        /// Name ile element bulur
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
        /// Acil durum reset - Tüm animasyonları ve layout'u sıfırlar
        /// </summary>
        public void ForceReset()
        {
            try
            {
                LogMessage?.Invoke("🚨 Force reset yapılıyor...");

                // Sidebar'ı normal pozisyona getir
                if (_leftSidebarTransform != null)
                {
                    _leftSidebarTransform.X = 0;
                }

                // Games panel'i gizle
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel != null)
                {
                    gamesPanel.Visibility = Visibility.Collapsed;
                    gamesPanel.Opacity = 1;
                }

                // Terminal'i göster ve resetle
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");
                if (terminalPanel != null)
                {
                    terminalPanel.Visibility = Visibility.Visible;
                    terminalPanel.Opacity = 1;

                    var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                    if (terminalTransform != null)
                    {
                        terminalTransform.Y = 0;
                    }
                }

                // Progress bar'ı göster ve resetle
                var progressContainer = FindElementByName<Border>(_parentWindow, "ProgressBarContainer");
                if (progressContainer != null)
                {
                    progressContainer.Visibility = Visibility.Visible;
                    progressContainer.Opacity = 1;

                    var progressTransform = progressContainer.RenderTransform as TranslateTransform;
                    if (progressTransform != null)
                    {
                        progressTransform.X = 0;
                    }
                }

                _isGamesVisible = false;
                LogMessage?.Invoke("✅ Force reset tamamlandı");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ForceReset hatası: {ex.Message}");
            }
        }
    }
}