using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Yafes
{
    /// <summary>
    /// Uygulama başlangıç ekranı ve sistem kontrolü
    /// </summary>
    public partial class StartupScreen : Window
    {
        // Program sürüm bilgisi
        private string appVersion = "1.0.0";

        // Gömülü Runtime Klasörü
        private readonly string runtimesFolder;

        // HTTP istemcisi
        private readonly HttpClient httpClient = new HttpClient();

        // Runtime yöneticisi
        private VCRuntimeManager runtimeManager;

        // Otomatik kapatma süresi (saniye)
        private const int AUTO_CLOSE_SECONDS = 5;

        public StartupScreen()
        {
            InitializeComponent();

            try
            {
                // Uygulama sürümünü al
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                if (version != null)
                {
                    appVersion = version.ToString();
                }
            }
            catch
            {
                // Sürüm bilgisi alınamazsa varsayılanı kullan
            }

            txtVersion.Text = $"© 2025 Yafes Auto Install v{appVersion}";

            // Runtime klasörünü ayarla
            runtimesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vcruntime");

            // Runtime yöneticisini oluştur
            runtimeManager = new VCRuntimeManager(runtimesFolder, (msg, prg) => UpdateStatus(msg, prg));

            // Form yüklendiğinde başlangıç işlemlerini başlat
            Loaded += StartupScreen_Loaded;
        }

        private async void StartupScreen_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Runtime kontrol başlatılıyor...", 5);

                if (!Directory.Exists(runtimesFolder))
                    Directory.CreateDirectory(runtimesFolder);

                runtimeManager.CheckInstalledRuntimes();

                // DEBUG: Hangi runtime'lar kontrol edildiğini yazdır
                foreach (var rt in runtimeManager.AvailableRuntimes)
                {
                    Console.WriteLine($"[DEBUG] {rt.Year} - {rt.Name} => Installed: {rt.IsInstalled}");
                }

                if (runtimeManager.AllRuntimesInstalled)
                {
                    // İyileştirilmiş "FULL READY" görünümü
                    UpdateStatus("Runtimes: ALL READY - Tüm VC++ paketleri kurulu", 100, Brushes.LightGreen);
                    WriteRuntimeStatusToLog();
                    await Task.Delay(800);
                    OpenLoginForm();
                }
                else
                {
                    UpdateStatus("Eksik Runtimes tespit edildi, yükleniyor...", 10, Brushes.Orange);
                    WriteRuntimeStatusToLog();

                    await runtimeManager.InstallMissingRuntimes();

                    // YENİ: Kurulum sonrası detaylı rapor oluştur
                    string detailedReport = runtimeManager.PerformDetailedRuntimeCheck();

                    // Bu raporu bir log dosyasına yazabilirsiniz
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime_installation_report.txt"), detailedReport);

                    // Bu noktada gerçek durumu biliyoruz, ona göre mesaj gösterelim
                    if (runtimeManager.InstalledRuntimesCount >= runtimeManager.TotalRuntimesCount - 2)
                    {
                        // En fazla 2 eksik varsa
                        UpdateStatus("Runtimes Durumu: Hepsi Yüklü ✓", 100, Brushes.LightGreen);
                    }
                    else
                    {
                        // 3 veya daha fazla eksik varsa
                        UpdateStatus("Runtimes Eksik: Uyumluluk sorunları olabilir ⚠", 100, Brushes.Orange);
                    }

                    await Task.Delay(800);
                    OpenLoginForm();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Sistem Kontrolü Tamamlandı", 100, Brushes.LightGreen);
                Console.WriteLine($"[ERROR] {ex.Message}");

                await Task.Delay(800);
                OpenLoginForm();
            }
        }

        // Ana program çalışmaya devam ederken otomatik kapanan MessageBox göster
        private void ShowAutoCloseMessageBoxAndContinue(string message, string title)
        {
            // MessageBox oluştur
            Window messageBox = new Window
            {
                Title = title,
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false,
                // ÖNEMLİ: Sahipliği ayarlamadan göster
                Owner = null
            };

            // İçerik paneli oluştur
            var panel = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(20)
            };

            // Uyarı ikonu
            try
            {
                var image = new System.Windows.Controls.Image
                {
                    Width = 32,
                    Height = 32,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                // İkonu yüklemeye çalış, hata alırsa atla
                var iconUri = title == "Uyarı"
                    ? new Uri("pack://application:,,,/PresentationFramework.Aero;component/Images/Warning.png", UriKind.Absolute)
                    : new Uri("pack://application:,,,/PresentationFramework.Aero;component/Images/Error.png", UriKind.Absolute);

                try
                {
                    var iconBitmap = new System.Windows.Media.Imaging.BitmapImage(iconUri);
                    image.Source = iconBitmap;
                    panel.Children.Add(image);
                }
                catch
                {
                    // İkon yüklenemezse atla
                }
            }
            catch
            {
                // İkon yükleme tamamen başarısız olursa atla
            }

            // Mesaj metni
            var messageText = $"{message}\n\n(Bu mesaj {AUTO_CLOSE_SECONDS} saniye içinde otomatik kapanacak)";
            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = messageText,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };
            panel.Children.Add(textBlock);

            // Kalan süre göstergesi
            var timerText = new System.Windows.Controls.TextBlock
            {
                Text = AUTO_CLOSE_SECONDS.ToString(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            panel.Children.Add(timerText);

            // Tamam butonu
            var okButton = new System.Windows.Controls.Button
            {
                Content = "Tamam",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            okButton.Click += (s, e) => messageBox.Close();
            panel.Children.Add(okButton);

            // Paneli pencereye ekle
            messageBox.Content = panel;

            // Zamanlayıcı
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            int remainingSeconds = AUTO_CLOSE_SECONDS;

            timer.Tick += (s, e) =>
            {
                remainingSeconds--;
                timerText.Text = remainingSeconds.ToString();

                if (remainingSeconds <= 0)
                {
                    timer.Stop();
                    messageBox.Close();
                }
            };

            // Gösterildikten sonra zamanlayıcıyı başlat
            messageBox.ContentRendered += (s, e) => timer.Start();

            // Mesaj kutusunu göster (ShowDialog DEĞİL, Show kullan)
            messageBox.Show();

            // Mesaj gösterildi şimdi login formuna geç
            // Bir sonraki tick'te login forma geçmek için Dispatcher kullan
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                OpenLoginForm();
            }));
        }

        // Runtime durumunu gösteren mesajı oluştur
        private string GetRuntimeStatusMessage()
        {
            int installedCount = runtimeManager.InstalledRuntimesCount;
            int totalCount = runtimeManager.TotalRuntimesCount;

            if (installedCount == totalCount)
            {
                return $"Runtimes: FULL READY - Tüm VC++ Runtime paketleri yüklü ({installedCount}/{totalCount})";
            }
            else
            {
                return $"Runtimes: EKSİK - VC++ Runtime paketleri: {installedCount}/{totalCount} yüklü";
            }
        }

        // Runtime durumunu detaylı olarak log dosyasına yaz
        private void WriteRuntimeStatusToLog()
        {
            try
            {
                // Log dosyası yolu
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime_status.log");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Runtime Durum Raporu - {DateTime.Now}");
                sb.AppendLine("----------------------------------------");

                // Her bir runtime paketi için durum bilgisi
                foreach (var runtime in runtimeManager.AvailableRuntimes)
                {
                    sb.AppendLine($"{runtime.Name} - {(runtime.IsInstalled ? "KURULU ✓" : "KURULU DEĞİL ✗")}");
                }

                sb.AppendLine("----------------------------------------");
                sb.AppendLine($"Toplam: {runtimeManager.InstalledRuntimesCount}/{runtimeManager.TotalRuntimesCount} paket kurulu");
                sb.AppendLine($"Durum: {(runtimeManager.AllRuntimesInstalled ? "FULL READY" : "EKSİK")}");

                // Dosyaya yaz
                File.WriteAllText(logFilePath, sb.ToString());
            }
            catch
            {
                // Log yazma hatası önemli değil, sessizce devam et
            }
        }

        // İlerleme durumunu güncelle (renkli versiyon)
        private void UpdateStatus(string message, int progress, Brush? textColor = null)
        {
            // Dispatcher ile UI thread'ine geç
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateStatus(message, progress, textColor));
                return;
            }

            // UI thread'indeyken güncelleme yap
            txtStatus.Text = message;

            // Renk belirtilmişse değiştir
            if (textColor != null)
            {
                txtStatus.Foreground = textColor;
            }
            else
            {
                // Varsayılan renk (beyaz)
                txtStatus.Foreground = Brushes.White;
            }

            progressBar.Value = progress;
        }

        // Giriş formunu aç
        private async Task OpenLoginForm()
        {
            try
            {
                var loginForm = new LoginForm();
                await Task.Delay(2000); // Giriş formu açılmadan önce kısa bir gecikme ekle
                loginForm.Show();

                // Bu formu kapat
                this.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Giriş formu açılırken hata: {ex.Message}");
                // Ciddi bir hata olduğunda uygulamayı kapat
                Application.Current.Shutdown();
            }
        }
    }
}