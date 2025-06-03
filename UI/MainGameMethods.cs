using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Yafes.Models;
using Yafes.GameData;

namespace Yafes
{
    /// <summary>
    /// Main.xaml.cs için eksik oyun metotları
    /// CS0103 hatalarını çözer
    /// </summary>
    public partial class Main : Window
    {
        /// <summary>
        /// ✅ ÇÖZÜM - CreateAdvancedGameCard metodu
        /// Gelişmiş oyun kartı oluşturur
        /// </summary>
        private Border CreateAdvancedGameCard(Yafes.Models.GameData game)
        {
            try
            {
                txtLog.AppendText($"🎮 Gelişmiş kart oluşturuluyor: {game.Name}\n");
                
                var gameCard = new Border
                {
                    Width = 140,
                    Height = 80,
                    Margin = new Thickness(5),
                    CornerRadius = new CornerRadius(8),
                    Background = new LinearGradientBrush(
                        Color.FromRgb(45, 45, 55),
                        Color.FromRgb(35, 35, 45),
                        90),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    BorderThickness = new Thickness(1),
                    Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 10,
                        ShadowDepth = 0,
                        Opacity = 0.5
                    },
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // İçerik grid'i
                var contentGrid = new Grid();
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Üst kısım - Oyun bilgileri
                var infoPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8)
                };

                // Oyun ikonu
                var gameIcon = new TextBlock
                {
                    Text = GetGameIcon(game.Category),
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 150, 255)),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                // Oyun adı
                var nameText = new TextBlock
                {
                    Text = game.Name,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxHeight = 24
                };

                // Boyut bilgisi
                var sizeText = new TextBlock
                {
                    Text = game.Size ?? "Bilinmiyor",
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                infoPanel.Children.Add(gameIcon);
                infoPanel.Children.Add(nameText);
                infoPanel.Children.Add(sizeText);

                Grid.SetRow(infoPanel, 0);

                // Alt kısım - Durum göstergesi
                var statusBorder = new Border
                {
                    Height = 16,
                    Background = game.IsInstalled ? 
                        new SolidColorBrush(Color.FromRgb(40, 167, 69)) : 
                        new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    CornerRadius = new CornerRadius(0, 0, 8, 8)
                };

                var statusText = new TextBlock
                {
                    Text = game.IsInstalled ? "✅ KURULU" : "📥 KURULACAK",
                    FontSize = 7,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                statusBorder.Child = statusText;
                Grid.SetRow(statusBorder, 1);

                contentGrid.Children.Add(infoPanel);
                contentGrid.Children.Add(statusBorder);
                gameCard.Child = contentGrid;

                // Hover efekti
                var scaleTransform = new ScaleTransform(1.0, 1.0);
                gameCard.RenderTransform = scaleTransform;
                gameCard.RenderTransformOrigin = new Point(0.5, 0.5);

                gameCard.MouseEnter += (s, e) =>
                {
                    var scaleAnimation = new DoubleAnimation
                    {
                        To = 1.05,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                };

                gameCard.MouseLeave += (s, e) =>
                {
                    var scaleAnimation = new DoubleAnimation
                    {
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                };

                // Tıklama eventi
                gameCard.MouseLeftButtonDown += (s, e) =>
                {
                    txtLog.AppendText($"🎯 {game.Name} seçildi!\n");
                    txtLog.AppendText($"📂 Kategori: {game.Category} | Boyut: {game.Size ?? "Bilinmiyor"}\n");
                    if (game.IsInstalled)
                    {
                        txtLog.AppendText($"✅ Kurulu - Son oynama: {game.LastPlayed}\n");
                    }
                    else
                    {
                        txtLog.AppendText($"📥 Kurulum gerekiyor - Setup: {game.SetupPath ?? "Belirtilmemiş"}\n");
                    }
                };

                return gameCard;
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ CreateAdvancedGameCard hatası: {ex.Message}\n");
                
                // Hata durumunda basit kart döndür
                var errorCard = new Border
                {
                    Width = 140,
                    Height = 80,
                    Background = new SolidColorBrush(Color.FromRgb(80, 30, 30)),
                    BorderBrush = Brushes.Red,
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = "HATA",
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                return errorCard;
            }
        }

        /// <summary>
        /// ✅ ÇÖZÜM - CreateEmergencyGameCards metodu  
        /// Acil durum oyun kartları oluşturur
        /// </summary>
        private async Task CreateEmergencyGameCards(Panel container)
        {
            try
            {
                txtLog.AppendText("🆘 Acil durum oyun kartları oluşturuluyor...\n");
                
                if (container == null)
                {
                    txtLog.AppendText("❌ Container null, acil durum kartları oluşturulamadı\n");
                    return;
                }

                // Mevcut kartları temizle
                container.Children.Clear();

                // Acil durum oyun listesi
                var emergencyGames = new List<Yafes.Models.GameData>
                {
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_steam",
                        Name = "Steam",
                        ImageName = "steam.png",
                        Category = "Platform",
                        Size = "150 MB",
                        IsInstalled = false,
                        Description = "PC Gaming Platform"
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_epic",
                        Name = "Epic Games Store",
                        ImageName = "epic_games.png",
                        Category = "Platform", 
                        Size = "200 MB",
                        IsInstalled = false,
                        Description = "Epic Games Store"
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_origin",
                        Name = "Origin",
                        ImageName = "origin.png",
                        Category = "Platform",
                        Size = "120 MB", 
                        IsInstalled = false,
                        Description = "EA Games Platform"
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_uplay",
                        Name = "Ubisoft Connect",
                        ImageName = "ubisoft_connect.png",
                        Category = "Platform",
                        Size = "110 MB",
                        IsInstalled = false,
                        Description = "Ubisoft Gaming Platform"
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_battlenet",
                        Name = "Battle.net",
                        ImageName = "battle_net.png", 
                        Category = "Platform",
                        Size = "90 MB",
                        IsInstalled = false,
                        Description = "Blizzard Games Platform"
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_gog",
                        Name = "GOG Galaxy",
                        ImageName = "gog_galaxy.png",
                        Category = "Platform",
                        Size = "80 MB", 
                        IsInstalled = false,
                        Description = "DRM-Free Gaming Platform"
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_rockstar",
                        Name = "Rockstar Launcher",
                        ImageName = "rockstar.png",
                        Category = "Platform",
                        Size = "85 MB",
                        IsInstalled = false,
                        Description = "Rockstar Games Launcher"
                    },
                    new Yafes.Models.GameData
                    {
                        Id = "emergency_xbox",
                        Name = "Xbox App",
                        ImageName = "xbox_app.png",
                        Category = "Platform",
                        Size = "95 MB",
                        IsInstalled = false,
                        Description = "Xbox Gaming Platform"
                    }
                };

                // Kartları oluştur ve ekle
                int addedCount = 0;
                foreach (var game in emergencyGames)
                {
                    try
                    {
                        var gameCard = CreateAdvancedGameCard(game);
                        container.Children.Add(gameCard);
                        addedCount++;

                        // Her 4 kartta bir küçük delay
                        if (addedCount % 4 == 0)
                        {
                            await Task.Delay(10);
                        }
                    }
                    catch (Exception ex)
                    {
                        txtLog.AppendText($"❌ Acil kart oluşturma hatası: {game.Name} - {ex.Message}\n");
                    }
                }

                txtLog.AppendText($"✅ {addedCount} acil durum oyun kartı oluşturuldu!\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"❌ CreateEmergencyGameCards kritik hatası: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Oyun kategorisine göre emoji ikonu döndürür
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
                "platform" => "🎯",
                "horror" => "👻",
                "puzzle" => "🧩",
                "general" => "🎮",
                _ => "🎮"
            };
        }
    }
}
