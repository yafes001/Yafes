using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
// using Yafes.Data; ← KALDIR
using Yafes.Managers;

namespace Yafes.GameData
{
    public partial class GameCard : UserControl
    {
        // Game data property - EXPLICIT QUALIFIER ile
        private Yafes.Data.GameData? _gameData;
        public Yafes.Data.GameData? GameData
        {
            get => _gameData;
            set
            {
                _gameData = value;
                UpdateUI();
            }
        }

        // Events - EXPLICIT QUALIFIER ile
        public event Action<Yafes.Data.GameData>? GameSelected;
        public event Action<Yafes.Data.GameData>? InstallRequested;
        public event Action<Yafes.Data.GameData>? UninstallRequested;

        // UI Elements (will be created programmatically)
        private Border _mainBorder = null!;
        private Grid _mainGrid = null!;
        private Image _gameImage = null!;
        private Border _overlayBorder = null!;
        private StackPanel _infoPanel = null!;
        private TextBlock _gameNameText = null!;
        private TextBlock _gameCategoryText = null!;
        private TextBlock _gameSizeText = null!;
        private Border _installStatusBorder = null!;
        private TextBlock _installStatusText = null!;
        private Button _actionButton = null!;

        // Animation properties
        private ScaleTransform _scaleTransform = null!;
        private bool _isHovered = false;

        public GameCard()
        {
            InitializeComponent();
            CreateUI();
            SetupAnimations();
            SetupEventHandlers();
        }

        public GameCard(Yafes.Data.GameData? gameData) : this()  // EXPLICIT
        {
            GameData = gameData;
        }

        /// <summary>
        /// UI elementlerini programmatik olarak oluşturur
        /// </summary>
        private void CreateUI()
        {
            // Ana container
            Width = 460;
            Height = 215;
            Margin = new Thickness(5);

            // Scale transform for animations
            _scaleTransform = new ScaleTransform(1.0, 1.0);
            RenderTransform = _scaleTransform;
            RenderTransformOrigin = new Point(0.5, 0.5);

            // Main border with rounded corners
            _mainBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 75)),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 5,
                    Opacity = 0.3,
                    BlurRadius = 10
                }
            };

            // Main grid layout
            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) }); // Info panel height

            // Game image
            _gameImage = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Image border with clipping
            var imageBorder = new Border
            {
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Child = _gameImage,
                ClipToBounds = true
            };
            Grid.SetRow(imageBorder, 0);

            // Overlay for hover effects
            _overlayBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), // Transparent initially
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Opacity = 0
            };
            Grid.SetRow(_overlayBorder, 0);

            // Info panel at bottom
            _infoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(35, 35, 38)),
                Height = 50
            };
            Grid.SetRow(_infoPanel, 1);

            // Game info grid inside info panel
            var infoGrid = new Grid
            {
                Margin = new Thickness(10, 5, 10, 5)
            };
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Game name
            _gameNameText = new TextBlock
            {
                Text = "Game Name",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Top,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };
            Grid.SetColumn(_gameNameText, 0);

            // Game category and size info
            var gameInfoStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 2)
            };

            _gameCategoryText = new TextBlock
            {
                Text = "Category",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 10,
                Margin = new Thickness(0, 0, 10, 0)
            };

            _gameSizeText = new TextBlock
            {
                Text = "Size",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 10
            };

            gameInfoStack.Children.Add(_gameCategoryText);
            gameInfoStack.Children.Add(_gameSizeText);
            Grid.SetColumn(gameInfoStack, 0);

            // Install status indicator
            _installStatusBorder = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };

            _installStatusText = new TextBlock
            {
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            _installStatusBorder.Child = _installStatusText;
            Grid.SetColumn(_installStatusBorder, 1);

            // Action button
            _actionButton = new Button
            {
                Content = "INSTALL",
                Width = 60,
                Height = 25,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Style = CreateButtonStyle()
            };
            Grid.SetColumn(_actionButton, 2);

            // Add controls to info grid
            infoGrid.Children.Add(_gameNameText);
            infoGrid.Children.Add(gameInfoStack);
            infoGrid.Children.Add(_installStatusBorder);
            infoGrid.Children.Add(_actionButton);

            _infoPanel.Children.Add(infoGrid);

            // Add all to main grid
            _mainGrid.Children.Add(imageBorder);
            _mainGrid.Children.Add(_overlayBorder);
            _mainGrid.Children.Add(_infoPanel);

            _mainBorder.Child = _mainGrid;
            Content = _mainBorder;
        }

        // ... diğer method'lar aynı kalır, sadece GameData referansları explicit olur ...

        /// <summary>
        /// Event handler'ları setup eder
        /// </summary>
        private void SetupEventHandlers()
        {
            // Mouse events
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseLeftButtonDown += OnMouseLeftButtonDown;

            // Button click
            _actionButton.Click += OnActionButtonClick;

            // Hover effect for action button
            _actionButton.MouseEnter += (s, e) =>
            {
                _actionButton.Background = new SolidColorBrush(Color.FromRgb(20, 140, 235));
            };

            _actionButton.MouseLeave += (s, e) =>
            {
                var color = _gameData?.IsInstalled == true ?
                    Color.FromRgb(220, 53, 69) : // Red for uninstall
                    Color.FromRgb(0, 120, 215);  // Blue for install
                _actionButton.Background = new SolidColorBrush(color);
            };
        }

        // Tüm method'lar için aynı pattern - gerekli yerler için sadece örnek veriyorum:

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_gameData != null)
            {
                GameSelected?.Invoke(_gameData);
            }
        }

        private void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (_gameData != null)
            {
                if (_gameData.IsInstalled)
                {
                    UninstallRequested?.Invoke(_gameData);
                }
                else
                {
                    InstallRequested?.Invoke(_gameData);
                }
            }
        }

        private void UpdateUI()
        {
            if (_gameData == null) return;

            if (!string.IsNullOrEmpty(_gameData.ImageName))
            {
                var image = ImageManager.GetGameImage(_gameData.ImageName);
                _gameImage.Source = image;
            }

            _gameNameText.Text = _gameData.Name ?? "Unknown Game";
            _gameCategoryText.Text = _gameData.Category ?? "General";
            _gameSizeText.Text = _gameData.Size ?? "Unknown";

            UpdateInstallStatus();
        }

        private void UpdateInstallStatus()
        {
            if (_gameData == null) return;

            if (_gameData.IsInstalled)
            {
                _installStatusBorder.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                _installStatusText.Text = "INSTALLED";
                _installStatusText.Foreground = Brushes.White;

                _actionButton.Content = "REMOVE";
                _actionButton.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
            else
            {
                _installStatusBorder.Background = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                _installStatusText.Text = "NOT INSTALLED";
                _installStatusText.Foreground = Brushes.White;

                _actionButton.Content = "INSTALL";
                _actionButton.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215));
            }
        }

        public void SetInstallStatus(bool isInstalled)
        {
            if (_gameData != null)
            {
                _gameData.IsInstalled = isInstalled;
                UpdateInstallStatus();
            }
        }

        public void SetLoading(bool isLoading)
        {
            _actionButton.IsEnabled = !isLoading;
            _actionButton.Content = isLoading ? "..." : (_gameData?.IsInstalled == true ? "REMOVE" : "INSTALL");
        }

        // Diğer methodlar aynı...
        private Style CreateButtonStyle()
        {
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            factory.AppendChild(contentPresenter);
            template.VisualTree = factory;
            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            return style;
        }

        private void SetupAnimations()
        {
            // Animation setup kodu aynı
            var scaleUpAnimation = new DoubleAnimation
            {
                To = 1.03,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var scaleDownAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var overlayShowAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            var overlayHideAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            Resources.Add("ScaleUpAnimation", scaleUpAnimation);
            Resources.Add("ScaleDownAnimation", scaleDownAnimation);
            Resources.Add("OverlayShowAnimation", overlayShowAnimation);
            Resources.Add("OverlayHideAnimation", overlayHideAnimation);
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (_isHovered) return;
            _isHovered = true;

            var scaleAnimation = Resources["ScaleUpAnimation"] as DoubleAnimation;
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            _overlayBorder.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            var overlayAnimation = Resources["OverlayShowAnimation"] as DoubleAnimation;
            _overlayBorder.BeginAnimation(OpacityProperty, overlayAnimation);

            Cursor = Cursors.Hand;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isHovered) return;
            _isHovered = false;

            var scaleAnimation = Resources["ScaleDownAnimation"] as DoubleAnimation;
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            var overlayAnimation = Resources["OverlayHideAnimation"] as DoubleAnimation;
            _overlayBorder.BeginAnimation(OpacityProperty, overlayAnimation);

            Cursor = Cursors.Arrow;
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            Opacity = enabled ? 1.0 : 0.6;
        }

        private void Cleanup()
        {
            GameSelected = null;
            InstallRequested = null;
            UninstallRequested = null;
        }

        private void InitializeComponent()
        {
            // WPF UserControl için gerekli method
        }
    }
}