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
using System.Windows.Shapes;

namespace Yafes.Managers
{
    public class GamesPanelManager
    {
        private readonly Window _parentWindow;
        private readonly TextBox _logTextBox;
        private bool _isGamesVisible = false;

        private Border _leftSidebar;
        private TranslateTransform _leftSidebarTransform;
        private Border _progressBarContainer;
        private TranslateTransform _progressBarTransform;

        // Kurulum kuyruğu
        private Border _installationQueue;
        private TranslateTransform _installationQueueTransform;

        // YENİ: Oyun yükleme kuyruğu
        private Border _gameInstallationQueue;
        private TranslateTransform _gameInstallationQueueTransform;

        private double _originalWindowHeight;
        private double _progressBarHeight = 0;
        private Canvas _mainCanvas;

        private Storyboard _terminalSlideOut;
        private Storyboard _terminalSlideIn;
        private Storyboard _progressBarSlideOut;
        private Storyboard _progressBarSlideIn;

        private readonly Dictionary<string, bool> _imageLoadingStates = new Dictionary<string, bool>();
        private static bool _diskStatusChecked = false;

        private List<Yafes.Models.GameData> _allGames = new List<Yafes.Models.GameData>();
        private string _currentSearchText = "";

        // Manager sınıfları
        private AnimationManager _animationManager;
        private GameSearchManager _gameSearchManager;
        private GameCardManager _gameCardManager;
        private GameInstallationQueueManager _gameInstallationQueueManager;

        public event Action<string> LogMessage;

        public GamesPanelManager(Window parentWindow, TextBox logTextBox)
        {
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));

            LogMessage += (message) => {
                try
                {
                    if (_logTextBox?.Dispatcher?.CheckAccess() == true)
                    {
                        _logTextBox.AppendText(message + "\n");
                        _logTextBox.ScrollToEnd();
                    }
                    else
                    {
                        _logTextBox?.Dispatcher?.Invoke(() => {
                            _logTextBox.AppendText(message + "\n");
                            _logTextBox.ScrollToEnd();
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LogMessage error: {ex.Message}");
                }
            };

            InitializeSidebarElements();
            InitializeProgressBarElements();
            InitializeInstallationQueueElements();
            InitializeGameInstallationQueueElements();
            InitializeWindowHeightAnimation();
            InitializeStoryboards();
            InitializeManagers();

            if (!_diskStatusChecked)
            {
                TestGameInstallationQueue();
                _diskStatusChecked = true;
            }
        }

        public bool IsGamesVisible => _isGamesVisible;

        // YENİ DEBUG METOD - DÜZELTİLDİ: Test metodunu kaldır
        private async void TestGameInstallationQueue()
        {
            try
            {
                await Task.Delay(2000);

                if (_gameInstallationQueue != null)
                {
                    if (_gameInstallationQueueTransform != null)
                    {
                        // Silent test - no logs
                    }
                }

                var mainCanvas = UIHelperManager.FindElementByName<Canvas>(_parentWindow, "MainCanvas");
                if (mainCanvas != null)
                {
                    foreach (var child in mainCanvas.Children)
                    {
                        if (child is Border border && border == _gameInstallationQueue)
                        {
                            double left = Canvas.GetLeft(border);
                            double top = Canvas.GetTop(border);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail
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
                    var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                    if (gamesPanel != null)
                    {
                        await LoadGamesIntoPanel(gamesPanel);
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail
            }
        }

        // MEVCUT: Kurulum kuyruğu elementlerini initialize eder
        private void InitializeInstallationQueueElements()
        {
            try
            {
                _installationQueue = FindInstallationQueueBorder();

                if (_installationQueue != null)
                {
                    _installationQueueTransform = _installationQueue.RenderTransform as TranslateTransform;
                    if (_installationQueueTransform == null)
                    {
                        _installationQueueTransform = new TranslateTransform();
                        _installationQueue.RenderTransform = _installationQueueTransform;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail
            }
        }

        // YENİ METOD: Oyun yükleme kuyruğu elementlerini initialize eder - DÜZELTİLDİ
        private void InitializeGameInstallationQueueElements()
        {
            try
            {
                // GameInstallationQueueManager'ı oluştur
                var dummyStackPanel = new StackPanel();
                var dummyTextBlock = new TextBlock();
                _gameInstallationQueueManager = new GameInstallationQueueManager(dummyStackPanel, dummyTextBlock, _logTextBox, _parentWindow);

                // Panel oluştur
                _gameInstallationQueue = _gameInstallationQueueManager.CreateGameInstallationQueuePanel();

                if (_gameInstallationQueue != null)
                {
                    _gameInstallationQueueTransform = _gameInstallationQueue.RenderTransform as TranslateTransform;

                    if (_gameInstallationQueueTransform == null)
                    {
                        _gameInstallationQueueTransform = new TranslateTransform();
                        _gameInstallationQueueTransform.X = 220;
                        _gameInstallationQueue.RenderTransform = _gameInstallationQueueTransform;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail
            }
        }

        // MEVCUT: Kurulum kuyruğu bulma metodları
        private Border FindInstallationQueueBorder()
        {
            try
            {
                var mainCanvas = UIHelperManager.FindElementByName<Canvas>(_parentWindow, "MainCanvas");
                if (mainCanvas == null) return null;

                foreach (var child in mainCanvas.Children)
                {
                    if (child is Border border)
                    {
                        double leftValue = Canvas.GetLeft(border);
                        if (Math.Abs(leftValue - 890) < 1)
                        {
                            return FindInstallationQueueInSidebar(border);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail
            }

            return null;
        }

        private Border FindInstallationQueueInSidebar(Border rightSidebar)
        {
            try
            {
                if (rightSidebar.Child is Canvas sidebarCanvas)
                {
                    foreach (var child in sidebarCanvas.Children)
                    {
                        if (child is Border border)
                        {
                            double topValue = Canvas.GetTop(border);
                            if (Math.Abs(topValue - 168) < 1)
                            {
                                return border;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail
            }

            return null;
        }

        // GÜNCELLENMİŞ: InitializeManagers - Oyun kuyruğu parametresi eklendi
        private void InitializeManagers()
        {
            try
            {
                _gameCardManager = new GameCardManager();

                if (_leftSidebar != null && _leftSidebarTransform != null &&
                    _progressBarContainer != null && _progressBarTransform != null &&
                    _installationQueue != null && _installationQueueTransform != null &&
                    _gameInstallationQueue != null && _gameInstallationQueueTransform != null)
                {
                    _animationManager = new AnimationManager(
                        _parentWindow,
                        _leftSidebar,
                        _leftSidebarTransform,
                        _progressBarContainer,
                        _progressBarTransform,
                        _installationQueue,
                        _installationQueueTransform,
                        _gameInstallationQueue,
                        _gameInstallationQueueTransform,
                        _originalWindowHeight,
                        _progressBarHeight
                    );
                }

                _gameSearchManager = new GameSearchManager(_allGames, CreateGameCardWrapper);
            }
            catch (Exception ex)
            {
                // Silent fail
            }
        }

        private async Task<Border> CreateGameCardWrapper(Yafes.Models.GameData game)
        {
            return await _gameCardManager.CreateGameCard(game);
        }

        private void InitializeWindowHeightAnimation()
        {
            try
            {
                _originalWindowHeight = _parentWindow.Height;
                _mainCanvas = UIHelperManager.FindElementByName<Canvas>(_parentWindow, "MainCanvas");
                CalculateProgressBarHeight();
            }
            catch (Exception ex)
            {
                // Silent fail
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
            catch (Exception ex)
            {
                _progressBarHeight = 64;
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
                // Silent fail
            }
        }

        private void InitializeSidebarElements()
        {
            try
            {
                _leftSidebar = UIHelperManager.FindElementByName<Border>(_parentWindow, "LeftSidebar");

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
                // Silent fail
            }
        }

        private void InitializeProgressBarElements()
        {
            try
            {
                _progressBarContainer = UIHelperManager.FindElementByName<Border>(_parentWindow, "ProgressBarContainer");

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
                // Silent fail
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
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool IsWindowCompact()
        {
            return _animationManager?.IsWindowCompact ?? false;
        }

        public async Task<bool> ToggleProgressBar()
        {
            try
            {
                if (_animationManager == null)
                {
                    return false;
                }

                if (!IsProgressBarHidden())
                {
                    await _animationManager.SlideProgressBarOut();
                    return true;
                }
                else
                {
                    await _animationManager.SlideProgressBarIn();
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // GÜNCELLENMİŞ: ToggleGamesPanel - Oyun kuyruğu animasyonu eklendi
        public async Task<bool> ToggleGamesPanel()
        {
            try
            {
                if (_animationManager == null)
                {
                    return false;
                }

                if (!_isGamesVisible)
                {
                    await _animationManager.SlideSidebarOut();
                    await _animationManager.SlideInstallationQueueDown();
                    await _animationManager.SlideGameInstallationQueueIn();

                    bool success = await ShowGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = true;
                        await _animationManager.SlideProgressBarOut();
                    }
                    return success;
                }
                else
                {
                    await _animationManager.SlideProgressBarIn();

                    bool success = await HideGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = false;
                        await _animationManager.SlideGameInstallationQueueOut();
                        await _animationManager.SlideInstallationQueueUp();
                        await _animationManager.SlideSidebarIn();
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // MEVCUT METODLAR (değişmez)
        private async Task<bool> ShowGamesPanel()
        {
            try
            {
                var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    return false;
                }

                UIHelperManager.ResizeGamesPanel(_parentWindow, true, IsProgressBarHidden());
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
                    var progressContainer = UIHelperManager.FindElementByName<Border>(_parentWindow, "ProgressBarContainer");
                    var tcs2 = new TaskCompletionSource<bool>();
                    _progressBarSlideOut.Completed += (s, e) => {
                        if (progressContainer != null)
                        {
                            progressContainer.Visibility = Visibility.Collapsed;
                        }
                        tcs2.SetResult(true);
                    };

                    _progressBarSlideOut.Begin();
                    await tcs2.Task;
                }

                var lstDrivers = UIHelperManager.FindElementByName<ListBox>(_parentWindow, "lstDrivers");
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
                var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    return false;
                }

                UIHelperManager.ResizeGamesPanel(_parentWindow, false, IsProgressBarHidden());
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
                    var progressContainer = UIHelperManager.FindElementByName<Border>(_parentWindow, "ProgressBarContainer");
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

                var lstDrivers = UIHelperManager.FindElementByName<ListBox>(_parentWindow, "lstDrivers");
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
                var gamesGrid = UIHelperManager.FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid == null)
                {
                    return;
                }

                gamesGrid.Children.Clear();
                await Task.Delay(50);

                if (_leftSidebar != null && _leftSidebarTransform != null && _leftSidebarTransform.X < -200)
                {
                    gamesGrid.Columns = 5;
                }
                else
                {
                    gamesGrid.Columns = 5;
                }

                var games = await Yafes.Managers.GameDataManager.GetAllGamesAsync();

                if (games == null || games.Count == 0)
                {
                    _gameCardManager?.CreateDefaultGameCards(gamesGrid);
                    return;
                }

                _allGames = games.ToList();
                _gameSearchManager?.UpdateGamesList(_allGames);

                List<Yafes.Models.GameData> displayGames;
                if (string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    displayGames = games;
                }
                else
                {
                    displayGames = _gameSearchManager?.FilterGames(_currentSearchText) ?? games;
                }

                int cardCount = 0;
                foreach (var game in displayGames)
                {
                    try
                    {
                        var gameCard = await _gameCardManager.CreateGameCard(game);
                        if (gameCard != null)
                        {
                            gamesGrid.Children.Add(gameCard);
                            cardCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Silent fail for individual cards
                    }
                }

                gamesGrid.UpdateLayout();
            }
            catch (Exception ex)
            {
                try
                {
                    var gamesGrid = UIHelperManager.FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                    if (gamesGrid != null)
                    {
                        _gameCardManager?.CreateDefaultGameCards(gamesGrid);
                    }
                }
                catch (Exception fallbackEx)
                {
                    // Silent fail
                }
            }
        }

        public async Task PerformSearchAsync(string searchText)
        {
            try
            {
                _currentSearchText = searchText;

                var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel == null)
                {
                    return;
                }

                var gamesGrid = UIHelperManager.FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                if (gamesGrid == null)
                {
                    return;
                }

                if (_gameSearchManager != null)
                {
                    await _gameSearchManager.PerformSearchAsync(searchText, gamesGrid);
                }
            }
            catch (Exception ex)
            {
                // Silent fail
            }
        }

        // GÜNCELLENMİŞ: ForceReset - Oyun kuyruğu reset'i eklendi
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

                if (_installationQueueTransform != null)
                {
                    _installationQueueTransform.Y = 0;
                }
                if (_installationQueue != null)
                {
                    _installationQueue.Visibility = Visibility.Visible;
                }

                if (_gameInstallationQueueTransform != null)
                {
                    _gameInstallationQueueTransform.X = 220;
                }
                if (_gameInstallationQueue != null)
                {
                    _gameInstallationQueue.Visibility = Visibility.Collapsed;
                }

                _parentWindow.Height = _originalWindowHeight;

                var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel != null)
                {
                    gamesPanel.Visibility = Visibility.Collapsed;
                    gamesPanel.Opacity = 1;
                }

                var terminalPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "TerminalPanel");
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

                var progressContainer = UIHelperManager.FindElementByName<Border>(_parentWindow, "ProgressBarContainer");
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
                // Silent fail
            }
        }

        /// <summary>
        /// GameInstallationQueueManager referansını döndürür
        /// </summary>
        public GameInstallationQueueManager GetGameInstallationQueueManager()
        {
            return _gameInstallationQueueManager;
        }
    }
}