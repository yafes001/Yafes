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
    /// V3 Final: Window Height Animation + Clean Code
    /// </summary>
    public class GamesPanelManager
    {
        private readonly Window _parentWindow;
        private readonly TextBox _logTextBox;
        private bool _isGamesVisible = false;

        // Sol Sidebar Animation
        private Border _leftSidebar;
        private TranslateTransform _leftSidebarTransform;
        private const double SIDEBAR_SLIDE_DISTANCE = -280;
        private const double ANIMATION_DURATION = 600;

        // Progress Bar Animation
        private Border _progressBarContainer;
        private TranslateTransform _progressBarTransform;
        private const double PROGRESSBAR_SLIDE_DISTANCE = 800;
        private const double PROGRESSBAR_ANIMATION_DURATION = 800;

        // Window Height Animation
        private double _originalWindowHeight;
        private double _progressBarHeight = 0;
        private bool _isWindowCompact = false;
        private const double WINDOW_HEIGHT_DURATION = 700;
        private Canvas _mainCanvas;

        // XAML Storyboards
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
            InitializeProgressBarElements();
            InitializeWindowHeightAnimation();
            InitializeStoryboards();
        }

        public bool IsGamesVisible => _isGamesVisible;

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
                // Silent error handling
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
                        _progressBarHeight = 22; // XAML default
                    }

                    _progressBarHeight += 10;
                    _progressBarHeight *= 2; // Double height reduction
                }
                else
                {
                    _progressBarHeight = 64; // Fallback
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
                // Silent error handling
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
                // Silent error handling
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
                // Silent error handling
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
                // Silent error handling
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
                // Silent error handling
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
                    if (fullWidth && IsProgressBarHidden())
                    {
                        gamesGrid.Columns = 12;
                    }
                    else if (fullWidth)
                    {
                        gamesGrid.Columns = 10;
                    }
                    else
                    {
                        gamesGrid.Columns = 4;
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
                // Silent error handling
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
                // Silent error handling
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
                // Silent error handling
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
                    bool success = await ShowGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = true;
                        await SlideSidebarOut();
                        await SlideProgressBarOut();
                    }
                    return success;
                }
                else
                {
                    await SlideSidebarIn();
                    await SlideProgressBarIn();
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
                // Silent error handling
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
                // Silent error handling
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

                // Küçük gecikme ile layout'un tamamlanmasını bekle
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

                // Layout update için kısa bekleme
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

                foreach (var game in games.Take(40))
                {
                    var gameCard = await CreateGameCard(game);
                    if (gameCard != null)
                    {
                        gamesGrid.Children.Add(gameCard);
                    }
                }

                // Final layout update
                gamesGrid.UpdateLayout();
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

                        // ImageManager'ın hazır olmasını kontrol et
                        BitmapImage bitmapImage = null;
                        for (int retry = 0; retry < 3; retry++)
                        {
                            bitmapImage = Yafes.Managers.ImageManager.GetGameImage(imageName);
                            if (bitmapImage != null) break;
                            await Task.Delay(10); // Kısa retry delay
                        }

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

                var gameSizeText = new TextBlock
                {
                    Text = game.Size,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };

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
                // Silent error handling
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
                // Silent error handling
            }
        }

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
                // Silent error handling
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
                        LogMessage?.Invoke($"🎯 {gameName} kurulum kuyruğuna eklendi!");
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
                // Silent error handling
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
                // Silent error handling
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
                // Silent error handling
            }
        }
    }
}