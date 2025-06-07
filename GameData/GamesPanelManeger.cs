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

        // Oyun yükleme kuyruğu
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
                catch (Exception)
                {
                    // Silent fail
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

            // ✅ YENİ: Arama kutusunu başlat
            InitializeSearchBox();
        }

        public bool IsGamesVisible => _isGamesVisible;

        /// <summary>
        /// Arama kutusunu başlatır ve event handler'ları bağlar
        /// </summary>
        private void InitializeSearchBox()
        {
            try
            {
                // Biraz bekle ki UI elementleri hazır olsun
                Task.Run(async () =>
                {
                    await Task.Delay(500);

                    await _parentWindow.Dispatcher.InvokeAsync(() =>
                    {
                        SetupSearchBoxEventHandlers();
                    });
                });
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ InitializeSearchBox hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Arama kutusu event handler'larını kurar
        /// </summary>
        private void SetupSearchBoxEventHandlers()
        {
            try
            {
                var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel == null)
                {
                    LogMessage?.Invoke("⚠️ Games panel bulunamadı, arama kutusu başlatılamadı");
                    return;
                }

                var searchBox = UIHelperManager.FindElementByName<TextBox>(gamesPanel, "GameSearchBox");
                var searchPlaceholder = UIHelperManager.FindElementByName<TextBlock>(gamesPanel, "SearchPlaceholder");
                var clearButton = UIHelperManager.FindElementByName<Button>(gamesPanel, "ClearSearchButton");

                if (searchBox == null)
                {
                    LogMessage?.Invoke("⚠️ Arama kutusu bulunamadı");
                    return;
                }

                // Event handler'ları temizle (önceki bağlantıları kaldır)
                searchBox.TextChanged -= SearchBox_TextChanged;
                searchBox.GotFocus -= SearchBox_GotFocus;
                searchBox.LostFocus -= SearchBox_LostFocus;

                if (clearButton != null)
                {
                    clearButton.Click -= ClearButton_Click;
                }

                // Yeni event handler'ları bağla
                searchBox.TextChanged += SearchBox_TextChanged;
                searchBox.GotFocus += SearchBox_GotFocus;
                searchBox.LostFocus += SearchBox_LostFocus;

                if (clearButton != null)
                {
                    clearButton.Click += ClearButton_Click;
                }

                LogMessage?.Invoke("✅ Arama kutusu event handler'ları bağlandı");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SetupSearchBoxEventHandlers hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Arama kutusu TextChanged event handler
        /// </summary>
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                var searchText = textBox?.Text ?? "";

                // Placeholder ve clear button kontrolü
                var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel != null)
                {
                    var searchPlaceholder = UIHelperManager.FindElementByName<TextBlock>(gamesPanel, "SearchPlaceholder");
                    var clearButton = UIHelperManager.FindElementByName<Button>(gamesPanel, "ClearSearchButton");

                    if (searchPlaceholder != null)
                    {
                        searchPlaceholder.Visibility = string.IsNullOrEmpty(searchText)
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }

                    if (clearButton != null)
                    {
                        clearButton.Visibility = string.IsNullOrEmpty(searchText)
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                    }
                }

                // Arama gerçekleştir (sadece games paneli açıksa)
                if (_isGamesVisible)
                {
                    await PerformSearchAsync(searchText);

                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        LogMessage?.Invoke("🔍 Arama temizlendi, tüm oyunlar gösteriliyor");
                    }
                    else
                    {
                        LogMessage?.Invoke($"🔍 Arama: '{searchText}'");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Arama hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Arama kutusu GotFocus event handler
        /// </summary>
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel != null)
                {
                    var searchPlaceholder = UIHelperManager.FindElementByName<TextBlock>(gamesPanel, "SearchPlaceholder");
                    if (searchPlaceholder != null)
                    {
                        searchPlaceholder.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Arama kutusu LostFocus event handler
        /// </summary>
        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                if (string.IsNullOrEmpty(textBox?.Text))
                {
                    var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                    if (gamesPanel != null)
                    {
                        var searchPlaceholder = UIHelperManager.FindElementByName<TextBlock>(gamesPanel, "SearchPlaceholder");
                        if (searchPlaceholder != null)
                        {
                            searchPlaceholder.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Clear button Click event handler
        /// </summary>
        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gamesPanel = UIHelperManager.FindElementByTag<Border>(_parentWindow, "GamesPanel");
                if (gamesPanel != null)
                {
                    var searchBox = UIHelperManager.FindElementByName<TextBox>(gamesPanel, "GameSearchBox");
                    var searchPlaceholder = UIHelperManager.FindElementByName<TextBlock>(gamesPanel, "SearchPlaceholder");
                    var clearButton = UIHelperManager.FindElementByName<Button>(gamesPanel, "ClearSearchButton");

                    if (searchBox != null)
                    {
                        searchBox.Text = "";
                        searchBox.Focus();
                    }

                    if (searchPlaceholder != null)
                    {
                        searchPlaceholder.Visibility = Visibility.Visible;
                    }

                    if (clearButton != null)
                    {
                        clearButton.Visibility = Visibility.Collapsed;
                    }
                }

                // Tüm oyunları göster
                if (_isGamesVisible)
                {
                    await PerformSearchAsync("");
                    LogMessage?.Invoke("🔍 Arama temizlendi");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Arama temizleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Arama kutusunu yeniden başlatır (games paneli açıldığında çağrılır)
        /// </summary>
        public void RefreshSearchBox()
        {
            try
            {
                SetupSearchBoxEventHandlers();
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ RefreshSearchBox hatası: {ex.Message}");
            }
        }

        private async void TestGameInstallationQueue()
        {
            try
            {
                await Task.Delay(2000);

                if (_gameInstallationQueue != null && _gameInstallationQueueTransform != null)
                {
                    // Silent test
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
            catch (Exception)
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
            catch (Exception)
            {
                // Silent fail
            }
        }

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
            catch (Exception)
            {
                // Silent fail
            }
        }

        private void InitializeGameInstallationQueueElements()
        {
            try
            {
                var dummyStackPanel = new StackPanel();
                var dummyTextBlock = new TextBlock();
                _gameInstallationQueueManager = new GameInstallationQueueManager(dummyStackPanel, dummyTextBlock, _logTextBox, _parentWindow);

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

                    _gameInstallationQueue.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                _gameInstallationQueueManager = null;
                _gameInstallationQueue = null;
                _gameInstallationQueueTransform = null;
            }
        }

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
            catch (Exception)
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
            catch (Exception)
            {
                // Silent fail
            }

            return null;
        }

        private void InitializeManagers()
        {
            try
            {
                _gameCardManager = new GameCardManager();

                if (_gameInstallationQueueManager != null && _gameCardManager != null)
                {
                    _gameCardManager.SetQueueManager(_gameInstallationQueueManager);
                }

                if (_leftSidebar != null && _leftSidebarTransform != null &&
                    _progressBarContainer != null && _progressBarTransform != null &&
                    _installationQueue != null && _installationQueueTransform != null)
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
            catch (Exception)
            {
                try
                {
                    if (_gameCardManager == null)
                        _gameCardManager = new GameCardManager();

                    if (_gameSearchManager == null)
                        _gameSearchManager = new GameSearchManager(_allGames, CreateGameCardWrapper);
                }
                catch (Exception)
                {
                    // Silent fail
                }
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
            catch (Exception)
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
            catch (Exception)
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
            catch (Exception)
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
            catch (Exception)
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
            catch (Exception)
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
            catch (Exception)
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
            catch (Exception)
            {
                return false;
            }
        }

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

                    if (_gameInstallationQueue != null)
                    {
                        _gameInstallationQueue.Visibility = Visibility.Visible;
                    }

                    if (_gameInstallationQueueTransform != null)
                    {
                        try
                        {
                            await _animationManager.SlideGameInstallationQueueIn();
                        }
                        catch (Exception)
                        {
                            // Silent fail
                        }
                    }

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

                        if (_gameInstallationQueueTransform != null)
                        {
                            try
                            {
                                await _animationManager.SlideGameInstallationQueueOut();
                            }
                            catch (Exception)
                            {
                                // Silent fail
                            }
                        }

                        if (_gameInstallationQueue != null)
                        {
                            _gameInstallationQueue.Visibility = Visibility.Collapsed;
                        }

                        await _animationManager.SlideInstallationQueueUp();
                        await _animationManager.SlideSidebarIn();
                    }
                    return success;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

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

                // ✅ YENİ: Arama kutusunu yeniden başlat
                await Task.Delay(100); // Panel animasyonunun tamamlanması için bekle
                RefreshSearchBox();
                LogMessage?.Invoke("🔍 Games panel açıldı, arama kutusu aktif edildi");

                return true;
            }
            catch (Exception)
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
            catch (Exception)
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
                    catch (Exception)
                    {
                        // Silent fail for individual cards
                    }
                }

                gamesGrid.UpdateLayout();
            }
            catch (Exception)
            {
                try
                {
                    var gamesGrid = UIHelperManager.FindElementByName<UniformGrid>(gamesPanel, "gamesGrid");
                    if (gamesGrid != null)
                    {
                        _gameCardManager?.CreateDefaultGameCards(gamesGrid);
                    }
                }
                catch (Exception)
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

                    // Sonuç sayısını logla
                    int resultCount = gamesGrid.Children.Count;
                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        LogMessage?.Invoke($"📋 Tüm oyunlar gösteriliyor ({resultCount} oyun)");
                    }
                    else
                    {
                        LogMessage?.Invoke($"🎯 Arama sonucu: {resultCount} oyun bulundu");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ PerformSearchAsync hatası: {ex.Message}");
            }
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
            catch (Exception)
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