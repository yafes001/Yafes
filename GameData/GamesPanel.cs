using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Yafes.Data;  // ❗ GameData class'ı için
using Yafes.Managers;

namespace Yafes.GameData
{
    public partial class GamesPanel : UserControl
    {
        // Properties - ❗ Tamamen temizlendi
        private List<GameData> _allGames = new List<GameData>();
        private List<GameData> _filteredGames = new List<GameData>();
        private string _currentSearchText = string.Empty;
        private string _currentCategory = "All";

        // UI Elements
        private Grid _mainGrid = null!;
        private StackPanel _headerPanel = null!;
        private TextBox _searchBox = null!;
        private ComboBox _categoryComboBox = null!;
        private TextBlock _statsText = null!;
        private ScrollViewer _scrollViewer = null!;
        private WrapPanel _gamesWrapPanel = null!;
        private TextBlock _loadingText = null!;
        private TextBlock _noGamesText = null!;

        // Configuration
        private const int CARDS_PER_ROW = 3;
        private const double CARD_MARGIN = 10;

        // Events - ❗ Tamamen temizlendi
        public event Action<GameData>? GameSelected;
        public event Action<GameData>? InstallRequested;
        public event Action<GameData>? UninstallRequested;

        // Loading state
        private bool _isLoading = false;

        public GamesPanel()
        {
            InitializeComponent();
            CreateUI();
            SetupEventHandlers();
            _ = LoadGamesAsync();
        }

        /// <summary>
        /// UI elementlerini oluşturur
        /// </summary>
        private void CreateUI()
        {
            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            CreateHeaderPanel();
            CreateContentPanel();
            Content = _mainGrid;
        }

        /// <summary>
        /// Header panel oluşturur
        /// </summary>
        private void CreateHeaderPanel()
        {
            _headerPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 35)),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var topRow = new Grid { Margin = new Thickness(15, 10, 15, 5) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            // Search box
            var searchBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 50)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10, 5, 10, 5)
            };

            _searchBox = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            var searchPlaceholder = new TextBlock
            {
                Text = "🔍 Search games...",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                IsHitTestVisible = false
            };

            var searchGrid = new Grid();
            searchGrid.Children.Add(_searchBox);
            searchGrid.Children.Add(searchPlaceholder);
            searchBorder.Child = searchGrid;
            Grid.SetColumn(searchBorder, 0);

            // Category filter
            _categoryComboBox = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 50)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 75)),
                FontSize = 12,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_categoryComboBox, 1);

            // Stats text
            _statsText = new TextBlock
            {
                Text = "Loading games...",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(_statsText, 2);

            _searchBox.TextChanged += (s, e) =>
            {
                searchPlaceholder.Visibility = string.IsNullOrEmpty(_searchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                _ = FilterGamesAsync(_searchBox.Text);
            };

            topRow.Children.Add(searchBorder);
            topRow.Children.Add(_categoryComboBox);
            topRow.Children.Add(_statsText);
            _headerPanel.Children.Add(topRow);
            Grid.SetRow(_headerPanel, 0);
            _mainGrid.Children.Add(_headerPanel);
        }

        /// <summary>
        /// Content panel oluşturur
        /// </summary>
        private void CreateContentPanel()
        {
            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 30))
            };

            _gamesWrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _loadingText = new TextBlock
            {
                Text = "🎮 Loading games...",
                Foreground = Brushes.White,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Visible
            };

            _noGamesText = new TextBlock
            {
                Text = "🚫 No games found",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            var contentGrid = new Grid();
            contentGrid.Children.Add(_scrollViewer);
            contentGrid.Children.Add(_loadingText);
            contentGrid.Children.Add(_noGamesText);
            _scrollViewer.Content = _gamesWrapPanel;
            Grid.SetRow(contentGrid, 1);
            _mainGrid.Children.Add(contentGrid);
        }

        /// <summary>
        /// Event handler'ları setup eder
        /// </summary>
        private void SetupEventHandlers()
        {
            _categoryComboBox.SelectionChanged += async (s, e) =>
            {
                if (_categoryComboBox.SelectedItem is ComboBoxItem item)
                {
                    _currentCategory = item.Content.ToString() ?? "All";
                    await FilterGamesAsync(_currentSearchText);
                }
            };

            var searchTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            searchTimer.Tick += async (s, e) =>
            {
                searchTimer.Stop();
                await FilterGamesAsync(_searchBox.Text);
            };

            _searchBox.TextChanged += (s, e) =>
            {
                _currentSearchText = _searchBox.Text;
                searchTimer.Stop();
                searchTimer.Start();
            };
        }

        /// <summary>
        /// Oyunları yükler
        /// </summary>
        private async Task LoadGamesAsync()
        {
            try
            {
                SetLoadingState(true);
                _allGames = await GameDataManager.GetAllGamesAsync();
                await LoadCategoriesAsync();
                await FilterGamesAsync(string.Empty);
                SetLoadingState(false);
            }
            catch (Exception ex)
            {
                SetLoadingState(false);
                ShowError($"Oyunlar yüklenirken hata: {ex.Message}");
            }
        }

        /// <summary>
        /// Kategorileri yükler
        /// </summary>
        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await GameDataManager.GetAvailableCategoriesAsync();
                _categoryComboBox.Items.Clear();

                var allItem = new ComboBoxItem { Content = "All", Foreground = Brushes.White };
                _categoryComboBox.Items.Add(allItem);

                foreach (var category in categories)
                {
                    var item = new ComboBoxItem { Content = category, Foreground = Brushes.White };
                    _categoryComboBox.Items.Add(item);
                }

                _categoryComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kategori yükleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Oyunları filtreler
        /// </summary>
        private async Task FilterGamesAsync(string searchText)
        {
            try
            {
                List<GameData> filteredGames;

                if (string.IsNullOrWhiteSpace(searchText) && _currentCategory == "All")
                {
                    filteredGames = _allGames;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        filteredGames = await GameDataManager.SearchGamesAsync(searchText);
                    }
                    else
                    {
                        filteredGames = _allGames;
                    }

                    if (_currentCategory != "All")
                    {
                        filteredGames = filteredGames.Where(g =>
                            g.Category.Equals(_currentCategory, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                }

                _filteredGames = filteredGames;
                await DisplayGamesAsync();
                UpdateStats();
            }
            catch (Exception ex)
            {
                ShowError($"Filtreleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Oyunları gösterir
        /// </summary>
        private async Task DisplayGamesAsync()
        {
            try
            {
                _gamesWrapPanel.Children.Clear();

                if (!_filteredGames.Any())
                {
                    _noGamesText.Visibility = Visibility.Visible;
                    _scrollViewer.Visibility = Visibility.Collapsed;
                    return;
                }

                _noGamesText.Visibility = Visibility.Collapsed;
                _scrollViewer.Visibility = Visibility.Visible;

                foreach (var game in _filteredGames)
                {
                    var gameCard = new GameCard(game);
                    gameCard.GameSelected += OnGameSelected;
                    gameCard.InstallRequested += OnInstallRequested;
                    gameCard.UninstallRequested += OnUninstallRequested;
                    _gamesWrapPanel.Children.Add(gameCard);

                    if (_gamesWrapPanel.Children.Count % 10 == 0)
                    {
                        await Task.Delay(1);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Oyunlar gösterilirken hata: {ex.Message}");
            }
        }

        /// <summary>
        /// Game card events - ❗ Tamamen temizlendi
        /// </summary>
        private void OnGameSelected(GameData game) => GameSelected?.Invoke(game);
        private void OnInstallRequested(GameData game) => InstallRequested?.Invoke(game);
        private void OnUninstallRequested(GameData game) => UninstallRequested?.Invoke(game);

        /// <summary>
        /// İstatistikleri günceller
        /// </summary>
        private void UpdateStats()
        {
            var totalGames = _allGames.Count;
            var filteredCount = _filteredGames.Count;
            var installedCount = _filteredGames.Count(g => g.IsInstalled);

            if (filteredCount != totalGames)
            {
                _statsText.Text = $"📊 {filteredCount}/{totalGames} games • {installedCount} installed";
            }
            else
            {
                _statsText.Text = $"📊 {totalGames} games • {installedCount} installed";
            }
        }

        /// <summary>
        /// Loading state ayarlar
        /// </summary>
        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;
            _loadingText.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            _scrollViewer.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            _headerPanel.IsEnabled = !isLoading;
        }

        /// <summary>
        /// Hata gösterir
        /// </summary>
        private void ShowError(string message)
        {
            _statsText.Text = $"❌ {message}";
            _statsText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
        }

        /// <summary>
        /// Public metodlar
        /// </summary>
        public async Task RefreshGamesAsync()
        {
            GameDataManager.ClearCache();
            await LoadGamesAsync();
        }

        public async Task UpdateGameInstallStatus(string gameId, bool isInstalled)
        {
            try
            {
                await GameDataManager.UpdateGameInstallStatusAsync(gameId, isInstalled);

                var gameCard = _gamesWrapPanel.Children.OfType<GameCard>()
                    .FirstOrDefault(card => card.GameData?.Id == gameId);
                gameCard?.SetInstallStatus(isInstalled);

                var game = _allGames.FirstOrDefault(g => g.Id == gameId);
                if (game != null)
                {
                    game.IsInstalled = isInstalled;
                    UpdateStats();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Kurulum durumu güncellenirken hata: {ex.Message}");
            }
        }

        public void SetSearchText(string searchText) => _searchBox.Text = searchText;

        public void SetCategory(string category)
        {
            var item = _categoryComboBox.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == category);
            if (item != null) _categoryComboBox.SelectedItem = item;
        }

        public void SetGameCardLoading(string gameId, bool isLoading)
        {
            var gameCard = _gamesWrapPanel.Children.OfType<GameCard>()
                .FirstOrDefault(card => card.GameData?.Id == gameId);
            gameCard?.SetLoading(isLoading);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        private void Cleanup()
        {
            GameSelected = null;
            InstallRequested = null;
            UninstallRequested = null;

            foreach (var card in _gamesWrapPanel.Children.OfType<GameCard>())
            {
                card.GameSelected -= OnGameSelected;
                card.InstallRequested -= OnInstallRequested;
                card.UninstallRequested -= OnUninstallRequested;
            }
        }

        /// <summary>
        /// Initialize component
        /// </summary>
        private void InitializeComponent()
        {
            // WPF UserControl için gerekli method
        }
    }
}