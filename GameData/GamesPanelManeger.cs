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
    public class GamesPanelManager
    {
        private readonly Window _parentWindow;
        private readonly TextBox _logTextBox;
        private bool _isGamesVisible = false;

        private Border _leftSidebar;
        private TranslateTransform _leftSidebarTransform;
        private const double SIDEBAR_SLIDE_DISTANCE = -280;
        private const double ANIMATION_DURATION = 600;

        private Border _progressBarContainer;
        private TranslateTransform _progressBarTransform;
        private const double PROGRESSBAR_SLIDE_DISTANCE = 800;
        private const double PROGRESSBAR_ANIMATION_DURATION = 800;

        private double _originalWindowHeight;
        private double _progressBarHeight = 0;
        private bool _isWindowCompact = false;
        private const double WINDOW_HEIGHT_DURATION = 700;
        private Canvas _mainCanvas;

        private Storyboard _terminalSlideOut;
        private Storyboard _terminalSlideIn;
        private Storyboard _progressBarSlideOut;
        private Storyboard _progressBarSlideIn;

        private readonly Dictionary<string, bool> _imageLoadingStates = new Dictionary<string, bool>();
        private static bool _diskStatusChecked = false;

        // 🔍 v9 - Search sistemi için yeni field'lar
        private List<Yafes.Models.GameData> _allGames = new List<Yafes.Models.GameData>();
        private string _currentSearchText = "";

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
            InitializeProgressBarElements();
            InitializeWindowHeightAnimation();
            InitializeStoryboards();

            if (!_diskStatusChecked)
            {
                _diskStatusChecked = true;
            }
        }

        public bool IsGamesVisible => _isGamesVisible;

        public async Task ShowDebugInfoPopup()
        {
            try
            {
                var debugInfo = await Task.Run(() =>
                {
                    var info = "=== IMAGE MANAGER DEBUG INFO ===\n";
                    var diskStatus = ImageManager.GetDiskStatus();
                    info += diskStatus + "\n";

                    var cacheStats = ImageManager.GetCacheStats();
                    info += $"Cache Items: {cacheStats.cachedCount}\n";
                    info += $"Available Files: {cacheStats.availableFiles}\n";
                    info += $"GamesIcons Path: {cacheStats.gamesPath}\n\n";

                    info += "=== FILE TEST ===\n";
                    var testFile = "age_of_darkness_final_stand_FG_5.1GB.png";
                    var testResult = ImageManager.GetGameImage(testFile);
                    var isDefault = (testResult == ImageManager.GetDefaultImage());
                    info += $"Test File: {testFile}\n";
                    info += $"Result: {(isDefault ? "NOT FOUND (DEFAULT)" : "FOUND (SUCCESS)")}\n\n";

                    if (!string.IsNullOrEmpty(cacheStats.gamesPath) && Directory.Exists(cacheStats.gamesPath))
                    {
                        var files = Directory.GetFiles(cacheStats.gamesPath, "*.png").Take(10);
                        info += "=== ACTUAL FILES IN FOLDER ===\n";
                        foreach (var file in files)
                        {
                            info += $"- {Path.GetFileName(file)}\n";
                        }
                    }
                    else
                    {
                        info += "=== MANUAL PATH CHECK ===\n";
                        string manualPath = @"D:\GamesIcons";
                        bool exists = Directory.Exists(manualPath);
                        info += $"D:\\GamesIcons exists: {exists}\n";

                        if (exists)
                        {
                            var files = Directory.GetFiles(manualPath, "*.png").Take(5);
                            info += $"Found {files.Count()} PNG files:\n";
                            foreach (var file in files)
                            {
                                info += $"- {Path.GetFileName(file)}\n";
                            }
                        }
                    }
                    return info;
                });

                MessageBox.Show(debugInfo, "🔍 Image Manager Debug Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Debug error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RefreshDiskPaths()
        {
            try
            {
                await Task.Run(() =>
                {
                    ImageManager.RefreshPaths();
                    ImageManager.ClearCache();
                });

                if (_isGamesVisible)
                {
                    var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                    if (gamesPanel != null)
                    {
                        await LoadGamesIntoPanel(gamesPanel);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void InitializeWindowHeightAnimation()
        {
            try
            {
                _originalWindowHeight = _parentWindow.Height;
                _mainCanvas = FindElementByName<Canvas>(_parentWindow, "MainCanvas");
                CalculateProgressBarHeight();
            }
            catch (Exception ex)
            {
            }
        }

        private void CalculateProgressBarHeight()
        {
            try
            {
                if (_progressBarContainer != null)
                {
                    _progressBarHeight = _progressBarContainer.ActualHeight;

                    if (_progressBarHeight <= 0)
                    {
                        _progressBarHeight = _progressBarContainer.Height;
                    }

                    if (double.IsNaN(_progressBarHeight) || _progressBarHeight <= 0)
                    {
                        _progressBarHeight = 22;
                    }

                    _progressBarHeight += 10;
                    _progressBarHeight *= 2;
                }
                else
                {
                    _progressBarHeight = 64;
                }
            }
            catch
            {
                _progressBarHeight = 64;
            }
        }

        private async Task CompactWindowHeight()
        {
            try
            {
                if (_isWindowCompact) return;

                var heightAnimation = new DoubleAnimation
                {
                    From = _originalWindowHeight,
                    To = _originalWindowHeight - _progressBarHeight,
                    Duration = TimeSpan.FromMilliseconds(WINDOW_HEIGHT_DURATION),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                heightAnimation.Completed += (s, e) => {
                    _isWindowCompact = true;
                    tcs.SetResult(true);
                };

                _parentWindow.BeginAnimation(Window.HeightProperty, heightAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        private async Task ExpandWindowHeight()
        {
            try
            {
                if (!_isWindowCompact) return;

                var heightAnimation = new DoubleAnimation
                {
                    From = _originalWindowHeight - _progressBarHeight,
                    To = _originalWindowHeight,
                    Duration = TimeSpan.FromMilliseconds(WINDOW_HEIGHT_DURATION),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                heightAnimation.Completed += (s, e) => {
                    _isWindowCompact = false;
                    tcs.SetResult(true);
                };

                _parentWindow.BeginAnimation(Window.HeightProperty, heightAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        private void InitializeStoryboards()
        {
            try
            {
                _terminalSlideOut = _parentWindow.FindResource("TerminalSlideOut") as Storyboard;
                _terminalSlideIn = _parentWindow.FindResource("TerminalSlideIn") as Storyboard;
                _progressBarSlideOut = _parentWindow.FindResource("ProgressBarSlideOut") as Storyboard;
                _progressBarSlideIn = _parentWindow.FindResource("ProgressBarSlideIn") as Storyboard;
            }
            catch (Exception ex)
            {
            }
        }

        private void InitializeSidebarElements()
        {
            try
            {
                _leftSidebar = FindElementByName<Border>(_parentWindow, "LeftSidebar");

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
            }
        }

        private void InitializeProgressBarElements()
        {
            try
            {
                _progressBarContainer = FindElementByName<Border>(_parentWindow, "ProgressBarContainer");

                if (_progressBarContainer != null)
                {
                    _progressBarTransform = _progressBarContainer.RenderTransform as TranslateTransform;
                    if (_progressBarTransform == null)
                    {
                        _progressBarTransform = new TranslateTransform();
                        _progressBarContainer.RenderTransform = _progressBarTransform;
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

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
                    gamesPanel.Margin = new Thickness(0, 0, 20, 0);
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
                    // 🚀 v7 - Daha fazla oyun için daha fazla sütun
                    if (fullWidth && IsProgressBarHidden())
                    {
                        gamesGrid.Columns = 14; // ⬆️ 12'den 14'e çıkarıldı
                    }
                    else if (fullWidth)
                    {
                        gamesGrid.Columns = 12; // ⬆️ 10'dan 12'ye çıkarıldı  
                    }
                    else
                    {
                        gamesGrid.Columns = 6; // ⬆️ 4'ten 6'ya çıkarıldı
                    }
                }

                var gamesTitlePanel = FindElementByTag<Border>(_parentWindow, "GamesTitlePanel") ??
                                    FindElementByName<Border>(_parentWindow, "GamesTitlePanel");
                if (gamesTitlePanel != null)
                {
                    if (fullWidth)
                    {
                        gamesTitlePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                        gamesTitlePanel.Margin = new Thickness(0, 0, 20, 0);
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
                System.Diagnostics.Debug.WriteLine($"ResizeGamesPanel Hatası: {ex.Message}");
            }
        }

        private async Task SlideProgressBarOut()
        {
            try
            {
                if (_progressBarContainer == null || _progressBarTransform == null) return;

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = PROGRESSBAR_SLIDE_DISTANCE,
                    Duration = TimeSpan.FromMilliseconds(PROGRESSBAR_ANIMATION_DURATION),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += async (s, e) => {
                    _progressBarContainer.Visibility = Visibility.Collapsed;
                    await CompactWindowHeight();
                    tcs.SetResult(true);
                };

                _progressBarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        private async Task SlideProgressBarIn()
        {
            try
            {
                if (_progressBarContainer == null || _progressBarTransform == null) return;

                await ExpandWindowHeight();

                _progressBarContainer.Visibility = Visibility.Visible;
                _progressBarTransform.X = PROGRESSBAR_SLIDE_DISTANCE;

                var slideAnimation = new DoubleAnimation
                {
                    From = PROGRESSBAR_SLIDE_DISTANCE,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(PROGRESSBAR_ANIMATION_DURATION),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    tcs.SetResult(true);
                };

                _progressBarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        public bool IsProgressBarHidden()
        {
            try
            {
                if (_progressBarContainer == null) return false;
                return _progressBarContainer.Visibility == Visibility.Collapsed ||
                       (_progressBarTransform != null && _progressBarTransform.X > 600);
            }
            catch
            {
                return false;
            }
        }

        public bool IsWindowCompact()
        {
            return _isWindowCompact;
        }

        public async Task<bool> ToggleProgressBar()
        {
            try
            {
                if (!IsProgressBarHidden())
                {
                    await SlideProgressBarOut();
                    return true;
                }
                else
                {
                    await SlideProgressBarIn();
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> ToggleGamesPanel()
        {
            try
            {
                if (!_isGamesVisible)
                {
                    await SlideSidebarOut();
                    bool success = await ShowGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = true;
                        await SlideProgressBarOut();
                    }
                    return success;
                }
                else
                {
                    await SlideProgressBarIn();
                    bool success = await HideGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = false;
                    }
                    await SlideSidebarIn();
                    return success;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task SlideSidebarOut()
        {
            try
            {
                if (_leftSidebar == null || _leftSidebarTransform == null) return;

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = SIDEBAR_SLIDE_DISTANCE,
                    Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    tcs.SetResult(true);
                };

                _leftSidebarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        private async Task SlideSidebarIn()
        {
            try
            {
                if (_leftSidebar == null || _leftSidebarTransform == null) return;

                var slideAnimation = new DoubleAnimation
                {
                    From = SIDEBAR_SLIDE_DISTANCE,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    tcs.SetResult(true);
                };

                _leftSidebarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        private async Task<bool> ShowGamesPanel()
        {
            try
            {
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    return false;
                }

                ResizeGamesPanel(true);
                gamesPanel.Visibility = Visibility.Visible;

                if (_terminalSlideOut != null)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    _terminalSlideOut.Completed += (s, e) => {
                        terminalPanel.Visibility = Visibility.Collapsed;
                        tcs.SetResult(true);
                    };

                    _terminalSlideOut.Begin();
                    await tcs.Task;
                }
                else
                {
                    terminalPanel.Visibility = Visibility.Collapsed;
                }

                if (_progressBarSlideOut != null)
                {
                    var progressContainer = FindElementByName<Border>(_parentWindow, "ProgressBarContainer");
                    var tcs2 = new TaskCompletionSource<bool>();
                    _progressBarSlideOut.Completed += (s, e) => {
                        if (progressContainer != null) progressContainer.Visibility = Visibility.Collapsed;
                        tcs2.SetResult(true);
                    };

                    _progressBarSlideOut.Begin();
                    await tcs2.Task;
                }

                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Collapsed;
                }

                await Task.Delay(100);
                await LoadGamesIntoPanel(gamesPanel);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<bool> HideGamesPanel()
        {
            try
            {
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    return false;
                }

                ResizeGamesPanel(false);
                gamesPanel.Visibility = Visibility.Collapsed;

                if (_terminalSlideIn != null)
                {
                    terminalPanel.Visibility = Visibility.Visible;
                    terminalPanel.Opacity = 0;

                    var tcs = new TaskCompletionSource<bool>();
                    _terminalSlideIn.Completed += (s, e) => {
                        tcs.SetResult(true);
                    };

                    _terminalSlideIn.Begin();
                    await tcs.Task;
                }
                else
                {
                    terminalPanel.Visibility = Visibility.Visible;
                    terminalPanel.Opacity = 1;

                    var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                    if (terminalTransform != null)
                    {
                        terminalTransform.Y = 0;
                    }
                }

                if (_progressBarSlideIn != null)
                {
                    var progressContainer = FindElementByName<Border>(_parentWindow, "ProgressBarContainer");
                    if (progressContainer != null)
                    {
                        progressContainer.Visibility = Visibility.Visible;
                        progressContainer.Opacity = 0;
                    }

                    var tcs2 = new TaskCompletionSource<bool>();
                    _progressBarSlideIn.Completed += (s, e) => {
                        tcs2.SetResult(true);
                    };

                    _progressBarSlideIn.Begin();
                    await tcs2.Task;
                }

                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task LoadGamesIntoPanel(Border gamesPanel)
        {
            try
            {
                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid == null) return;

                gamesGrid.Children.Clear();
                await Task.Delay(50);

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

                // 🔍 v9 - Oyunları cache'e al (search için)
                _allGames = games.ToList();

                // Search varsa filtrele
                List<Yafes.Models.GameData> displayGames;
                if (string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    displayGames = games;
                }
                else
                {
                    displayGames = games.Where(game =>
                        game.Name.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) ||
                        game.Category.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) ||
                        (game.ImageName?.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) == true)
                    ).ToList();
                }

                // 🚀 v8 - TÜM OYUNLARI GÖSTER (Take(40) kaldırıldı)
                foreach (var game in displayGames) // ✅ Artık tüm oyunlar gösteriliyor
                {
                    try
                    {
                        var gameCard = await CreateGameCard(game);
                        if (gameCard != null)
                        {
                            gamesGrid.Children.Add(gameCard);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GameCard oluşturma hatası: {ex.Message}");
                    }
                }

                gamesGrid.UpdateLayout();

                // 📊 Debug bilgisi
                System.Diagnostics.Debug.WriteLine($"📋 Toplam {games.Count} oyun yüklendi, {displayGames.Count} gösteriliyor, {gamesGrid.Children.Count} kart oluşturuldu");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadGamesIntoPanel Hatası: {ex.Message}");
                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid != null)
                {
                    CreateDefaultGameCards(gamesGrid);
                }
            }
        }

        // 🎮 v7 - Repacker etiket sistemi eklendi
        private async Task<Border> CreateGameCard(Yafes.Models.GameData game)
        {
            try
            {
                var gameCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Height = 120,
                    Cursor = Cursors.Hand,
                    Tag = game,
                    CornerRadius = new CornerRadius(8),
                    ClipToBounds = true,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    }
                };

                // 📐 Grid ana container - Image + Repacker Badge + Text overlay için
                var mainGrid = new Grid();

                // 🖼️ BACKGROUND IMAGE
                if (!string.IsNullOrEmpty(game.ImageName))
                {
                    try
                    {
                        var gameImage = new Image
                        {
                            Stretch = Stretch.UniformToFill,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };

                        BitmapImage bitmapImage = await Task.Run(() => ImageManager.GetGameImage(game.ImageName));

                        if (bitmapImage != null && bitmapImage != ImageManager.GetDefaultImage())
                        {
                            gameImage.Source = bitmapImage;
                            mainGrid.Children.Add(gameImage);
                        }
                        else
                        {
                            var iconGrid = CreateFullFrameCategoryIcon(game.Category);
                            mainGrid.Children.Add(iconGrid);
                        }
                    }
                    catch (Exception ex)
                    {
                        var iconGrid = CreateFullFrameCategoryIcon(game.Category);
                        mainGrid.Children.Add(iconGrid);
                    }
                }
                else
                {
                    var iconGrid = CreateFullFrameCategoryIcon(game.Category);
                    mainGrid.Children.Add(iconGrid);
                }

                // 🏷️ REPACKER BADGE - Sağ üst köşe
                var repackerInfo = ExtractRepackerFromFileName(game.ImageName);
                if (!string.IsNullOrEmpty(repackerInfo.repacker))
                {
                    var repackerBadge = CreateRepackerBadge(repackerInfo);
                    mainGrid.Children.Add(repackerBadge);
                }

                // 📝 TEXT OVERLAY - Başlangıçta gizli, hover'da çıkacak
                var textOverlay = new Border
                {
                    Background = new LinearGradientBrush(
                        Color.FromArgb(0, 0, 0, 0),     // Üst: Şeffaf
                        Color.FromArgb(200, 0, 0, 0),   // Alt: Koyu
                        new Point(0, 0), new Point(0, 1)),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 50, // Overlay yüksekliği
                    Opacity = 0, // Başlangıçta görünmez
                    Name = "TextOverlay" // Mouse event'lerde bulabilmek için
                };

                var textStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(5, 0, 5, 8)
                };

                // 🎮 GAME NAME
                var gameNameText = new TextBlock
                {
                    Text = game.Name,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 90,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 4,
                        ShadowDepth = 1,
                        Opacity = 0.8
                    }
                };

                // 📦 SIZE TEXT
                var gameSizeText = new TextBlock
                {
                    Text = game.Size,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 3,
                        ShadowDepth = 1,
                        Opacity = 0.8
                    }
                };

                textStack.Children.Add(gameNameText);
                textStack.Children.Add(gameSizeText);
                textOverlay.Child = textStack;

                // Transform for animation
                var overlayTransform = new TranslateTransform { Y = 50 }; // Başlangıçta aşağıda
                textOverlay.RenderTransform = overlayTransform;

                mainGrid.Children.Add(textOverlay);
                gameCard.Child = mainGrid;

                // 🎯 MOUSE EVENTS - Overlay animasyonları
                gameCard.MouseEnter += (s, e) => GameCard_MouseEnter_WithOverlay(s, e);
                gameCard.MouseLeave += (s, e) => GameCard_MouseLeave_WithOverlay(s, e);
                gameCard.MouseLeftButtonDown += GameCard_Click;

                return gameCard;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // 🔍 v7 - Dosya adından repacker bilgisini çıkarma
        private (string repacker, Color badgeColor, string displayName) ExtractRepackerFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return ("", Colors.Gray, "");

            var upperFileName = fileName.ToUpper();

            // 🎯 Bilinen repacker'ları tanımla
            var repackers = new Dictionary<string, (Color color, string display)>
            {
                // FitGirl - Yeşil
                { "FG", (Color.FromRgb(46, 204, 113), "FitGirl") },
                { "FITGIRL", (Color.FromRgb(46, 204, 113), "FitGirl") },
                
                // DODI - Mavi  
                { "DODI", (Color.FromRgb(52, 152, 219), "DODI") },
                
                // CODEX - Kırmızı
                { "CODEX", (Color.FromRgb(231, 76, 60), "CODEX") },
                
                // ElAmigos - Turuncu
                { "ELAMIGOS", (Color.FromRgb(230, 126, 34), "ElAmigos") },
                { "AMIGOS", (Color.FromRgb(230, 126, 34), "ElAmigos") },
                
                // Skidrow - Mor
                { "SKIDROW", (Color.FromRgb(155, 89, 182), "SKIDROW") },
                { "SKR", (Color.FromRgb(155, 89, 182), "SKIDROW") },
                
                // CPY - Pembe
                { "CPY", (Color.FromRgb(244, 143, 177), "CPY") },
                
                // PLAZA - Sarı
                { "PLAZA", (Color.FromRgb(241, 196, 15), "PLAZA") },
                
                // EMPRESS - Altın
                { "EMPRESS", (Color.FromRgb(212, 175, 55), "EMPRESS") },
                
                // HOODLUM - Gri
                { "HOODLUM", (Color.FromRgb(149, 165, 166), "HOODLUM") },
                
                // TinyRepacks - Cyan
                { "TINY", (Color.FromRgb(26, 188, 156), "TinyRepacks") },
                { "TINYREPACKS", (Color.FromRgb(26, 188, 156), "TinyRepacks") },
                
                // RELOADED - Koyu Kırmızı
                { "RLD", (Color.FromRgb(192, 57, 43), "RELOADED") },
                { "RELOADED", (Color.FromRgb(192, 57, 43), "RELOADED") }
            };

            // 🔍 Dosya adından repacker ara
            foreach (var repacker in repackers)
            {
                // Çeşitli pattern'leri dene
                var patterns = new[]
                {
                    $"_{repacker.Key}_",     // _FG_
                    $"-{repacker.Key}-",     // -FG-
                    $"_{repacker.Key}.",     // _FG.
                    $"-{repacker.Key}.",     // -FG.
                    $"[{repacker.Key}]",     // [FG]
                    $"({repacker.Key})",     // (FG)
                    $"{repacker.Key}_",      // FG_
                    $"{repacker.Key}-"       // FG-
                };

                foreach (var pattern in patterns)
                {
                    if (upperFileName.Contains(pattern.ToUpper()))
                    {
                        return (repacker.Key, repacker.Value.color, repacker.Value.display);
                    }
                }
            }

            return ("", Colors.Gray, "Unknown");
        }

        // 🏷️ v7 - Repacker badge oluşturma
        private Border CreateRepackerBadge((string repacker, Color badgeColor, string displayName) repackerInfo)
        {
            var badge = new Border
            {
                Background = new SolidColorBrush(repackerInfo.badgeColor),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 5, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 4,
                    ShadowDepth = 2,
                    Opacity = 0.7
                }
            };

            var badgeText = new TextBlock
            {
                Text = repackerInfo.displayName,
                FontSize = 7,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            badge.Child = badgeText;
            return badge;
        }
        private Grid CreateFullFrameCategoryIcon(string category)
        {
            var iconGrid = new Grid
            {
                Background = new LinearGradientBrush(
                    Color.FromArgb(80, 0, 0, 0),
                    Color.FromArgb(120, 0, 0, 0),
                    new Point(0, 0),
                    new Point(1, 1)
                ),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch // 🔧 DÜZELTİLDİ: HorizontalAlignment -> VerticalAlignment
            };

            var categoryIcon = GetCategoryIcon(category);
            var iconText = new TextBlock
            {
                Text = categoryIcon,
                FontSize = 48, // 🔍 Çok büyük icon
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 0, 0),
                    BlurRadius = 15,
                    ShadowDepth = 3,
                    Opacity = 0.8
                }
            };

            iconGrid.Children.Add(iconText);
            return iconGrid;
        }

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
            }
        }

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

                stackPanel.Children.Add(new TextBlock
                {
                    Text = icon,
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5)
                });

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

                stackPanel.Children.Add(new TextBlock
                {
                    Text = size,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                gameCard.Child = stackPanel;

                gameCard.MouseEnter += GameCard_MouseEnter_WithOverlay;
                gameCard.MouseLeave += GameCard_MouseLeave_WithOverlay;
                gameCard.MouseLeftButtonDown += GameCard_Click;

                return gameCard;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // 🎨 v6 - Overlay ile mouse enter animasyonu
        private void GameCard_MouseEnter_WithOverlay(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    // 🔍 Scale effect
                    var scaleTransform = new ScaleTransform(1.05, 1.05);
                    card.RenderTransform = scaleTransform;
                    card.RenderTransformOrigin = new Point(0.5, 0.5);

                    // 🌟 Border glow effect
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 215, 0),
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };

                    // 📝 TEXT OVERLAY ANIMATION - Yukarı çık
                    var mainGrid = card.Child as Grid;
                    if (mainGrid != null)
                    {
                        foreach (var child in mainGrid.Children)
                        {
                            if (child is Border overlay && overlay.Name == "TextOverlay")
                            {
                                var transform = overlay.RenderTransform as TranslateTransform;
                                if (transform != null)
                                {
                                    // 🚀 Opacity animasyonu
                                    var opacityAnimation = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 1,
                                        Duration = TimeSpan.FromMilliseconds(300),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };

                                    // ⬆️ Yukarı çıkma animasyonu
                                    var slideAnimation = new DoubleAnimation
                                    {
                                        From = 50,
                                        To = 0,
                                        Duration = TimeSpan.FromMilliseconds(300),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };

                                    overlay.BeginAnimation(Border.OpacityProperty, opacityAnimation);
                                    transform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        // 🎨 v6 - Overlay ile mouse leave animasyonu
        private void GameCard_MouseLeave_WithOverlay(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    // 🔍 Scale geri al
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    card.RenderTransform = scaleTransform;

                    // 🌟 Border normal hale getir
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    };

                    // 📝 TEXT OVERLAY ANIMATION - Aşağı in
                    var mainGrid = card.Child as Grid;
                    if (mainGrid != null)
                    {
                        foreach (var child in mainGrid.Children)
                        {
                            if (child is Border overlay && overlay.Name == "TextOverlay")
                            {
                                var transform = overlay.RenderTransform as TranslateTransform;
                                if (transform != null)
                                {
                                    // 🚀 Opacity animasyonu
                                    var opacityAnimation = new DoubleAnimation
                                    {
                                        From = 1,
                                        To = 0,
                                        Duration = TimeSpan.FromMilliseconds(200),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                                    };

                                    // ⬇️ Aşağı inme animasyonu
                                    var slideAnimation = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 50,
                                        Duration = TimeSpan.FromMilliseconds(200),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                                    };

                                    overlay.BeginAnimation(Border.OpacityProperty, opacityAnimation);
                                    transform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

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
                    }
                    else
                    {
                        var stackPanel = card.Child as StackPanel;
                        if (stackPanel?.Children.Count >= 2 && stackPanel.Children[1] is TextBlock gameNameTextBlock)
                        {
                            gameName = gameNameTextBlock.Text;
                        }
                    }

                    CreateShakeEffect(card);
                }
            }
            catch (Exception ex)
            {
            }
        }

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
            }
        }

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
        // 🔍 GamesPanelManager.cs dosyasına eklenecek PUBLIC metot
        // Diğer public metotların yanına (örneğin ToggleGamesPanel'den sonra) ekle

        /// <summary>
        /// Main.xaml.cs'den çağrılabilen public search metodu
        /// </summary>
        /// <param name="searchText">Aranacak metin</param>

        // GamesPanelManager.cs dosyasındaki PerformSearchAsync metodunu bu ile değiştir

        /// <summary>
        /// Tam eşleşme search metodu - Sadece eşleşen oyunları gösterir
        /// </summary>
        /// <param name="searchText">Aranacak metin</param>
        public async Task PerformSearchAsync(string searchText)
        {
            try
            {
                _currentSearchText = searchText;

                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ GamesPanel bulunamadı");
                    return;
                }

                var gamesGrid = FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ gamesGrid bulunamadı");
                    return;
                }

                List<Yafes.Models.GameData> filteredGames;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // Arama boşsa tüm oyunları göster
                    filteredGames = _allGames;
                    System.Diagnostics.Debug.WriteLine($"🔍 Tüm oyunlar gösteriliyor: {_allGames.Count}");
                }
                else
                {
                    // 🎯 TAM EŞLEŞME SEARCH - Sadece eşleşenleri göster
                    filteredGames = _allGames
                        .Where(game => IsExactMatch(game, searchText))
                        .OrderByDescending(game => CalculateExactMatchPriority(game, searchText))
                        .ThenBy(game => game.Name) // Alfabetik sıralama
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"🔍 Search: '{searchText}' - {filteredGames.Count} tam eşleşme bulundu");
                }

                // Grid'i temizle ve yeni sonuçları ekle
                gamesGrid.Children.Clear();

                foreach (var game in filteredGames)
                {
                    try
                    {
                        var gameCard = await CreateGameCard(game);
                        if (gameCard != null)
                        {
                            gamesGrid.Children.Add(gameCard);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GameCard creation error: {ex.Message}");
                    }
                }

                gamesGrid.UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PerformSearchAsync error: {ex.Message}");
            }
        }
        private string GetExactMatchType(Yafes.Models.GameData game, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return "No Search";

            var search = searchText.Trim().ToLowerInvariant();
            var gameName = game.Name?.ToLowerInvariant() ?? "";
            var category = game.Category?.ToLowerInvariant() ?? "";

            if (gameName == search)
                return "Exact Name Match";
            if (gameName.StartsWith(search))
                return "Name Starts With";
            if (ContainsAllWords(gameName, search))
                return "All Words Match";
            if (category == search)
                return "Category Match";
            if (gameName.Contains(search))
                return "Name Contains";

            return "File Match";
        }
        /// <summary>
        /// Tam eşleşme kontrolü - Oyun adı, kategori veya dosya adında tam eşleşme var mı?
        /// </summary>
        /// <param name="game">Kontrol edilecek oyun</param>
        /// <param name="searchText">Arama metni</param>
        /// <returns>True = Tam eşleşme var, False = Eşleşme yok</returns>
        private bool IsExactMatch(Yafes.Models.GameData game, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return false;

            var search = searchText.Trim().ToLowerInvariant();
            var gameName = game.Name?.ToLowerInvariant() ?? "";
            var category = game.Category?.ToLowerInvariant() ?? "";
            var imageName = game.ImageName?.ToLowerInvariant() ?? "";

            // 🎯 TAM EŞLEŞME KRİTERLERİ:

            // 1. Oyun adı tam eşleşme
            if (gameName == search)
                return true;

            // 2. Oyun adı kelime kelime eşleşme (büyük/küçük harf duyarsız)
            if (gameName.Contains(search))
                return true;

            // 3. Kategori tam eşleşme
            if (category == search)
                return true;

            // 4. Oyun adında arama kelimeleri bulunuyor mu? (tüm kelimeler olmalı)
            if (ContainsAllWords(gameName, search))
                return true;

            // 5. Dosya adında arama metni var mı? (repacker, format bilgisi için)
            if (imageName.Contains(search))
                return true;

            return false;
        }

        /// <summary>
        /// Tüm kelimelerin oyun adında bulunup bulunmadığını kontrol eder
        /// </summary>
        /// <param name="gameName">Oyun adı</param>
        /// <param name="searchText">Arama metni</param>
        /// <returns>True = Tüm kelimeler bulundu</returns>
        private bool ContainsAllWords(string gameName, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || string.IsNullOrWhiteSpace(gameName))
                return false;

            // Arama metnini kelimelere ayır
            var searchWords = searchText.Split(new char[] { ' ', '-', '_', '.' },
                StringSplitOptions.RemoveEmptyEntries);

            // Her arama kelimesinin oyun adında bulunması gerekir
            foreach (var word in searchWords)
            {
                if (!gameName.Contains(word.ToLowerInvariant()))
                    return false;
            }

            return searchWords.Length > 0; // En az bir kelime olmalı
        }

        /// <summary>
        /// Tam eşleşme öncelik puanı hesaplar
        /// </summary>
        /// <param name="game">Oyun verisi</param>
        /// <param name="searchText">Arama metni</param>
        /// <returns>Öncelik puanı (yüksek = önce gösterilir)</returns>
        private int CalculateExactMatchPriority(Yafes.Models.GameData game, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return 0;

            var search = searchText.Trim().ToLowerInvariant();
            var gameName = game.Name?.ToLowerInvariant() ?? "";
            var category = game.Category?.ToLowerInvariant() ?? "";

            // 🥇 Oyun adı tam eşleşme (En yüksek öncelik)
            if (gameName == search)
                return 1000;

            // 🥈 Oyun adı başlangıç eşleşmesi
            if (gameName.StartsWith(search))
                return 800;

            // 🥉 Tüm kelimeler oyun adında var
            if (ContainsAllWords(gameName, search))
                return 600;

            // 🏅 Kategori tam eşleşme
            if (category == search)
                return 400;

            // 🏅 Oyun adında arama metni geçiyor
            if (gameName.Contains(search))
                return 200;

            // 🏅 Dosya adında eşleşme
            return 100;
        }

        private int CalculateSearchPriority(Yafes.Models.GameData game, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return 0;

            var search = searchText.ToLowerInvariant();
            var gameName = game.Name?.ToLowerInvariant() ?? "";
            var category = game.Category?.ToLowerInvariant() ?? "";
            var imageName = game.ImageName?.ToLowerInvariant() ?? "";

            int priority = 0;

            // 🥇 TAM EŞLEŞME (En yüksek öncelik)
            if (gameName == search)
                return 1000; // Mükemmel eşleşme

            // 🥈 BAŞLANGIC EŞLEŞMESİ (Çok yüksek öncelik)
            if (gameName.StartsWith(search))
                priority += 500;

            // 🥉 İÇERİK EŞLEŞMESİ (Yüksek öncelik)
            if (gameName.Contains(search))
                priority += 300;

            // 🏅 KATEGORİ EŞLEŞMESİ (Orta öncelik)
            if (category.Contains(search))
                priority += 200;

            // 🏅 DOSYA ADI EŞLEŞMESİ (Düşük öncelik)
            if (imageName.Contains(search))
                priority += 100;

            // 🎯 BONUS PUANLAR

            // Search kelimesi oyun adının büyük kısmını içeriyorsa bonus
            if (search.Length >= 3 && gameName.Length > 0)
            {
                double similarity = (double)search.Length / gameName.Length;
                if (similarity > 0.5) // Arama %50'den fazla benzer
                    priority += (int)(similarity * 50);
            }

            // Birden fazla kelime eşleşiyorsa bonus
            var searchWords = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var nameWords = gameName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int wordMatches = 0;
            foreach (var searchWord in searchWords)
            {
                if (nameWords.Any(nameWord => nameWord.Contains(searchWord)))
                    wordMatches++;
            }

            if (wordMatches > 1)
                priority += wordMatches * 25; // Her ek kelime eşleşmesi için bonus

            return priority;
        }

        /// <summary>
        /// Eşleşme tipini döndürür (debug amaçlı)
        /// </summary>
        private string GetMatchType(Yafes.Models.GameData game, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return "No Match";

            var search = searchText.ToLowerInvariant();
            var gameName = game.Name?.ToLowerInvariant() ?? "";
            var category = game.Category?.ToLowerInvariant() ?? "";
            var imageName = game.ImageName?.ToLowerInvariant() ?? "";

            if (gameName == search)
                return "Exact Match";
            if (gameName.StartsWith(search))
                return "Starts With";
            if (gameName.Contains(search))
                return "Name Contains";
            if (category.Contains(search))
                return "Category Match";
            if (imageName.Contains(search))
                return "File Match";

            return "No Match";
        }


        public void ForceReset()
        {
            try
            {
                if (_leftSidebarTransform != null)
                {
                    _leftSidebarTransform.X = 0;
                }

                if (_progressBarTransform != null)
                {
                    _progressBarTransform.X = 0;
                }

                _parentWindow.Height = _originalWindowHeight;
                _isWindowCompact = false;

                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel != null)
                {
                    gamesPanel.Visibility = Visibility.Collapsed;
                    gamesPanel.Opacity = 1;
                }

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
            }
            catch (Exception ex)
            {
            }
        }
    }
}