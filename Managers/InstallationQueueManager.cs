using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;

namespace Yafes
{
    /// <summary>
    /// Kurulum kuyruğu yöneticisi - Maksimum 10 öğe gösterir, ScrollViewer ile
    /// </summary>
    public class InstallationQueueManager
    {
        private const int MAX_VISIBLE_ITEMS = 10; // 10 öğe gösterir
        private Queue<InstallationItem> installationQueue;
        private List<InstallationItem> visibleQueue;
        private InstallationItem? currentlyInstalling;
        private DispatcherTimer? queueUpdateTimer;

        // UI Elementleri
        private StackPanel originalActivePanel; // Orijinal panel referansı
        private StackPanel actualActivePanel;   // Gerçek içerik paneli  
        private ScrollViewer scrollViewer;     // Yeni ScrollViewer
        private TextBlock noActiveInstallationsText;
        private TextBox logTextBox;

        public InstallationQueueManager(StackPanel activePanel, TextBlock noActiveText, TextBox logBox)
        {
            installationQueue = new Queue<InstallationItem>();
            visibleQueue = new List<InstallationItem>();
            currentlyInstalling = null;

            // Orijinal paneli sakla ve yeni yapı oluştur
            originalActivePanel = activePanel;
            noActiveInstallationsText = noActiveText;
            logTextBox = logBox;

            CreateScrollableStructure();
        }

        /// <summary>
        /// YAFES temasına uygun ScrollViewer yapısı
        /// </summary>
        private void CreateScrollableStructure()
        {
            // Orijinal panel içeriğini temizle
            originalActivePanel.Children.Clear();

            // Sadece StackPanel - YAFES ListBox tarzı
            actualActivePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = Brushes.Transparent, // Transparent - parent arka planı göster
                Margin = new Thickness(0)
            };

            // Direkt olarak ana panele ekle
            originalActivePanel.Children.Add(actualActivePanel);

            LogMessage("[KUYRUK] YAFES temasına uygun yapı oluşturuldu");
        }

        /// <summary>
        /// Kuyruk yöneticisini başlatır
        /// </summary>
        public void Initialize()
        {
            // Timer KALDIRILDI - Gereksiz spam yapıyordu
            // Artık sadece manuel güncellemeler olacak:
            // - StartInstallation()
            // - CompleteInstallation() 
            // - CreateQueue()

            LogMessage("[KUYRUK] Yönetici başlatıldı - Timer devre dışı");
        }

        /// <summary>
        /// Kurulum kuyruğunu oluşturur - ESKİ LİSTEYİ KORUR!
        /// </summary>
        public void CreateQueue(List<Main.DriverInfo> drivers, List<Main.ProgramInfo> programs)
        {
            // SADECE ANA KUYRUĞU TEMİZLE - Görünür kuyrugu DOKUNMA!
            installationQueue.Clear();
            // visibleQueue.Clear(); ← BU SATIRI KALDIRDIK!

            // Önce sürücüleri ekle
            foreach (var driver in drivers)
            {
                var item = new InstallationItem
                {
                    Name = driver.Name,
                    Type = "Sürücü",
                    Status = InstallationStatus.Waiting,
                    OriginalItem = driver
                };
                installationQueue.Enqueue(item);
            }

            // Sonra programları ekle
            foreach (var program in programs)
            {
                var item = new InstallationItem
                {
                    Name = program.Name,
                    Type = "Program",
                    Status = InstallationStatus.Waiting,
                    OriginalItem = program
                };
                installationQueue.Enqueue(item);
            }

            LogMessage($"[KUYRUK] YENİ kuyruk eklendi - {drivers.Count} sürücü, {programs.Count} program (eski liste korundu)");

            // Görünür kuyruğa YENİ öğeleri ekle (eskiler kalır)
            int addedCount = 0;
            while (installationQueue.Count > 0 && visibleQueue.Count < MAX_VISIBLE_ITEMS && addedCount < 5)
            {
                var newItem = installationQueue.Dequeue();
                // Çift eklemeyi önle
                if (!visibleQueue.Any(x => x.Name == newItem.Name))
                {
                    visibleQueue.Add(newItem);
                    addedCount++;
                }
            }

            UpdateVisibleQueue();
        }

        /// <summary>
        /// Bir kurulum başladığında çağrılır
        /// </summary>
        public void StartInstallation(string itemName)
        {
            // ÖNCE YER AÇMA KONTROLÜ - Eğer kuyruk dolu ise EN ALTTAKİ tamamlananı sil
            if (visibleQueue.Count >= MAX_VISIBLE_ITEMS)
            {
                // EN ALTTAKİ "Yüklendi" durumundaki öğeyi bul ve sil (LIFO mantığı)
                for (int i = visibleQueue.Count - 1; i >= 0; i--)
                {
                    if (visibleQueue[i].Status == InstallationStatus.Completed)
                    {
                        var removedItem = visibleQueue[i];
                        visibleQueue.RemoveAt(i);
                        LogMessage($"[KUYRUK] {removedItem.Name} kuyruktan çıkarıldı (en alttaki tamamlanan)");
                        break;
                    }
                }
            }

            // Ana kuyruktan yeni öğe ekle
            while (visibleQueue.Count < MAX_VISIBLE_ITEMS && installationQueue.Count > 0)
            {
                var nextItem = installationQueue.Peek();
                if (!visibleQueue.Any(x => x.Name == nextItem.Name))
                {
                    visibleQueue.Add(installationQueue.Dequeue());
                }
                else
                {
                    break;
                }
            }

            // Başlatılacak öğeyi bul ve güncelle
            var item = visibleQueue.FirstOrDefault(x => x.Name == itemName);
            if (item != null)
            {
                item.Status = InstallationStatus.Installing;
                item.StartTime = DateTime.Now;
                currentlyInstalling = item;

                UpdateVisibleQueue();
                LogMessage($"[KUYRUK] {item.Name} kurulumu başladı");
            }
        }

        /// <summary>
        /// Kurulum tamamlandığında çağrılır
        /// </summary>
        public void CompleteInstallation(string itemName)
        {
            var item = visibleQueue.FirstOrDefault(x => x.Name == itemName);
            if (item != null)
            {
                item.Status = InstallationStatus.Completed;
                item.IsCompleted = true;
                item.CompletionTime = DateTime.Now;

                if (currentlyInstalling?.Name == itemName)
                {
                    currentlyInstalling = null;
                }

                UpdateVisibleQueue();
                LogMessage($"[KUYRUK] {item.Name} kurulumu tamamlandı");
            }
        }



        /// <summary>
        /// Görünür kuyruğu günceller
        /// </summary>
        private void UpdateVisibleQueue()
        {
            actualActivePanel.Children.Clear();

            foreach (var item in visibleQueue)
            {
                var itemControl = CreateInstallationItemControl(item);
                actualActivePanel.Children.Add(itemControl);
            }

            // Boş durum kontrolü
            if (visibleQueue.Count == 0)
            {
                noActiveInstallationsText.Visibility = Visibility.Visible;
            }
            else
            {
                noActiveInstallationsText.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// YAFES ListBox tarzı kurulum öğesi - programla uyumlu
        /// </summary>
        private Border CreateInstallationItemControl(InstallationItem item)
        {
            // YAFES ListBox item tarzı
            var itemBorder = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 245, 255)), // #00F5FF (YAFES cyan)
                BorderThickness = new Thickness(0, 0, 0, 1), // Alt border
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(0, 1, 0, 1),
                Height = 22
            };

            // İç panel
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Durum ikonu - YAFES emoji style
            var statusIcon = new TextBlock
            {
                Width = 16,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 8,
                Margin = new Thickness(0, 0, 5, 0)
            };

            // Öğe adı - YAFES font (BÜYÜK)
            var nameText = new TextBlock
            {
                Text = item.Name,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Trebuchet MS"),
                FontSize = 11, // 8'den 11'e çıkarıldı
                FontWeight = FontWeights.Bold, // Bold yapıldı
                Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)) // #00F5FF (YAFES cyan)
            };

            // Durum metni - YAFES style (BÜYÜK)
            var statusText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                FontFamily = new FontFamily("Trebuchet MS"),
                FontSize = 10, // 7'den 10'a çıkarıldı
                FontWeight = FontWeights.Bold // Bold yapıldı
            };

            // YAFES temasına uygun durumlar
            switch (item.Status)
            {
                case InstallationStatus.Waiting:
                    statusIcon.Text = "⏸";
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // #CCCCCC
                    nameText.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // #CCCCCC
                    statusText.Text = "Bekliyor";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // #CCCCCC
                    itemBorder.Background = Brushes.Transparent;
                    break;

                case InstallationStatus.Installing:
                    statusIcon.Text = "⚡";
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // #FFA500 (YAFES orange)
                    nameText.Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)); // #00F5FF (YAFES cyan)
                    statusText.Text = "Yükleniyor...";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // #FFA500 (YAFES orange)
                    itemBorder.Background = new SolidColorBrush(Color.FromArgb(26, 0, 245, 255)); // Hafif cyan glow (Alpha, R, G, B)
                    break;

                case InstallationStatus.Completed:
                    statusIcon.Text = "✓";
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)); // #00F5FF (YAFES cyan)
                    nameText.Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)); // #00F5FF (YAFES cyan)
                    statusText.Text = "Yüklendi";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)); // #00F5FF (YAFES cyan)
                    itemBorder.Background = Brushes.Transparent;
                    break;
            }

            // Mouse over efekti - YAFES ListBox gibi
            itemBorder.MouseEnter += (s, e) =>
            {
                itemBorder.Background = new SolidColorBrush(Color.FromArgb(26, 0, 245, 255)); // #1A00F5FF (Alpha, R, G, B)
            };

            itemBorder.MouseLeave += (s, e) =>
            {
                if (item.Status != InstallationStatus.Installing)
                {
                    itemBorder.Background = Brushes.Transparent;
                }
                else
                {
                    itemBorder.Background = new SolidColorBrush(Color.FromArgb(26, 0, 245, 255)); // Installing durumunda cyan glow kalsın
                }
            };

            // Elementleri panele ekle
            itemPanel.Children.Add(statusIcon);
            itemPanel.Children.Add(nameText);
            itemPanel.Children.Add(statusText);

            // Paneli border'a ekle
            itemBorder.Child = itemPanel;

            return itemBorder;
        }

        /// <summary>
        /// Kuyruk yöneticisini durdurur - LİSTEYİ TEMİZLEMEZ!
        /// </summary>
        public void Stop()
        {
            // Timer zaten yok, sadece referansları temizle
            queueUpdateTimer = null;

            // Mevcut kurulum durumunu sonlandır
            currentlyInstalling = null;

            // ✅ Sadece ana kuyruğu temizle, görünür kuyruk kalacak (tamamlanan kurulumlar)
            installationQueue.Clear();

            LogMessage("[KUYRUK] Yönetici durduruldu - tamamlanan kurulumlar panelde kalıyor");

            // Panel görünümünü güncelle
            UpdateEmptyState();
        }

        /// <summary>
        /// Kurulum bitince paneli boş duruma çevirir ama listeyi korur
        /// </summary>
        private void UpdateEmptyState()
        {
            actualActivePanel.Children.Clear();

            // Tamamlanan kurulumları göster
            foreach (var item in visibleQueue)
            {
                var itemControl = CreateInstallationItemControl(item);
                actualActivePanel.Children.Add(itemControl);
            }

            // Eğer hiç tamamlanan yoksa boş mesaj göster
            if (visibleQueue.Count == 0)
            {
                noActiveInstallationsText.Visibility = Visibility.Visible;
            }
            else
            {
                noActiveInstallationsText.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Log mesajı yazar
        /// </summary>
        private void LogMessage(string message)
        {
            try
            {
                if (logTextBox != null)
                {
                    logTextBox.Dispatcher.Invoke(() =>
                    {
                        logTextBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}\n");
                        logTextBox.ScrollToEnd();
                    });
                }
            }
            catch
            {
                // Log yazma hatası önemli değil
            }
        }
    }

    // Kurulum durumu enum
    public enum InstallationStatus
    {
        Waiting,    // Bekliyor
        Installing, // Yükleniyor
        Completed   // Tamamlandı
    }

    // Kurulum öğesi sınıfı
    public class InstallationItem
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Driver" veya "Program"
        public InstallationStatus Status { get; set; } = InstallationStatus.Waiting;
        public DateTime StartTime { get; set; }
        public DateTime CompletionTime { get; set; }
        public bool IsCompleted { get; set; } = false;
        public object OriginalItem { get; set; } // DriverInfo veya ProgramInfo referansı

        // CanBeRemoved artık kullanılmıyor - tamamlananlar program kapanana kadar kalacak
        public bool CanBeRemoved => false; // Her zaman false - manuel silme olmayacak
    }
}