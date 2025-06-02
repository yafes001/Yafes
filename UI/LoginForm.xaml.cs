using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace Yafes
{
    public partial class LoginForm : Window
    {
        // Boss'un bilgisayarının HWID'si - ilk çalıştıran bilgisayar otomatik olarak boss olur
        private const string BossHardwareID = "7eb17b2de09928e2c1895e835d0ee8c2d42c7fa1e717a03d9a63cd197985b615";

        private string currentHWID = "";
        private Brush? originalButtonBackground;
        private Brush? originalButtonForeground;

        public LoginForm()
        {
            InitializeComponent();

            // Form yüklendiğinde işlemler
            Loaded += LoginForm_Loaded;
        }

        private async void LoginForm_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // GIF arka planını yükle - 66. satır civarındaki sorunu düzelttim
                LoadGifBackground();

                // Butonun orijinal renklerini kaydet
                originalButtonBackground = btnLogin.Background;
                originalButtonForeground = btnLogin.Foreground;

                // Donanım kimliğini al
                currentHWID = CreateHardwareID();

                // HWID değerini bir dosyaya yaz (isteğe bağlı, gerekirse kullanılabilir)
                try
                {
                    File.WriteAllText("hwid.txt", currentHWID);
                }
                catch
                {
                    // Dosyaya yazma hatası önemli değil, görmezden gel
                }

                // Auto-HWID Modu: Eğer BossHardwareID "AUTO-HWID-PLACEHOLDER" ise,
                // ilk çalıştırmada otomatik olarak mevcut bilgisayarın HWID'sini Boss olarak kabul et
                bool isBossComputer = false;

                if (BossHardwareID == "AUTO-HWID-PLACEHOLDER")
                {
                    // Otomatik mod: Bu bilgisayar Boss olarak kabul edilir
                    isBossComputer = true;
                }
                else
                {
                    // Normal mod: Boss'un HWID'si ile karşılaştır
                    isBossComputer = (currentHWID == BossHardwareID);
                }

                // Boss bilgisayarı ise otomatik giriş yap
                if (isBossComputer)
                {
                    await PerformBossLogin();
                }
                else
                {
                    // Normal login için UI hazırla
                    txtUsername.Focus();
                }
            }
            catch (Exception ex)
            {
                // Hata varsa sessizce normal login'e devam et
                Console.WriteLine("Login form yükleme hatası: " + ex.Message);
                txtUsername.Focus();
            }
        }

        // GIF arka planını yükle - basitleştirilmiş versiyon
        private void LoadGifBackground()
        {
            try
            {
                // XAML dosyasından <Image> elementi zaten yüklenmiş olmalı
                // Sadece bu elementi bulup, GIF kaynağını ayarlayalım

                // MainGrid'in ilk elementi bir Image mi?
                if (MainGrid != null && MainGrid.Children.Count > 0 && MainGrid.Children[0] is Image backgroundImage)
                {
                    // Gömülü kaynak olarak GIF'i yükle
                    string resourceName = "Yafes.Resources.Loading.gif";
                    var assembly = Assembly.GetExecutingAssembly();
                    Stream? stream = assembly.GetManifestResourceStream(resourceName);

                    if (stream != null)
                    {
                        // GIF'i yükle ve mevcut Image'e ata
                        var gifSource = new BitmapImage();
                        gifSource.BeginInit();
                        gifSource.StreamSource = stream;
                        gifSource.CacheOption = BitmapCacheOption.OnLoad;
                        gifSource.EndInit();

                        // WPF Animated GIF kütüphanesiyle GIF'i göster
                        ImageBehavior.SetAnimatedSource(backgroundImage, gifSource);
                        Console.WriteLine("[SUCCESS] GIF başarıyla yüklendi");
                    }
                    else
                    {
                        // Resource bulunamadı - dosyadan yüklemeyi dene
                        Console.WriteLine("[INFO] Resource bulunamadı, dosyadan yüklemeyi deneyeceğim");

                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        string gifPath = Path.Combine(baseDir, "Loading.gif");

                        if (File.Exists(gifPath))
                        {
                            var fileSource = new BitmapImage(new Uri(gifPath, UriKind.Absolute));
                            ImageBehavior.SetAnimatedSource(backgroundImage, fileSource);
                            Console.WriteLine("[SUCCESS] GIF dosyadan yüklendi");
                        }
                    }
                }
                else
                {
                    // MainGrid veya Image bulunamadı - yeni bir Image oluştur
                    if (MainGrid != null)
                    {
                        // Yeni bir Image oluştur
                        var newImage = new Image
                        {
                            Stretch = Stretch.UniformToFill,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };

                        // En arkaya yerleştir
                        Panel.SetZIndex(newImage, -1);

                        // GIF kaynağını yükle
                        try
                        {
                            // Önce embedded resource dene
                            string resourceName = "Yafes.Resources.Loading.gif";
                            var assembly = Assembly.GetExecutingAssembly();
                            Stream? stream = assembly.GetManifestResourceStream(resourceName);

                            if (stream != null)
                            {
                                var gifSource = new BitmapImage();
                                gifSource.BeginInit();
                                gifSource.StreamSource = stream;
                                gifSource.CacheOption = BitmapCacheOption.OnLoad;
                                gifSource.EndInit();

                                // WPF Animated GIF kütüphanesiyle GIF'i göster
                                ImageBehavior.SetAnimatedSource(newImage, gifSource);
                                MainGrid.Children.Insert(0, newImage);
                                Console.WriteLine("[SUCCESS] Yeni Image oluşturuldu ve GIF yüklendi");
                            }
                            else
                            {
                                // Resource bulunamadı - dosyadan yüklemeyi dene
                                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                                string gifPath = Path.Combine(baseDir, "Loading.gif");

                                if (File.Exists(gifPath))
                                {
                                    var fileSource = new BitmapImage(new Uri(gifPath, UriKind.Absolute));
                                    ImageBehavior.SetAnimatedSource(newImage, fileSource);
                                    MainGrid.Children.Insert(0, newImage);
                                    Console.WriteLine("[SUCCESS] Yeni Image oluşturuldu ve GIF dosyadan yüklendi");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] GIF yüklenirken hata: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GIF arka plan yükleme hatası: {ex.Message}");
            }
        }

        // Boss girişi animasyonu ve işlemi
        private async Task PerformBossLogin()
        {
            // Input kontrollerini devre dışı bırak
            txtUsername.IsEnabled = false;
            txtPassword.IsEnabled = false;

            // Boss kullanıcı bilgilerini göster
            txtUsername.Text = "Boss";
            txtPassword.Password = "********";

            // Buton rengini değiştir (yeşil)
            btnLogin.Background = new SolidColorBrush(Colors.ForestGreen);
            btnLogin.Foreground = new SolidColorBrush(Colors.White);
            btnLogin.FontWeight = FontWeights.Bold;

            // Animasyon sırasında butonun metinlerini değiştir
            btnLogin.Content = "Boss'un Bilgisayarı Tanındı!";

            // Buton parıldama efekti
            DoubleAnimation pulseAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.7,
                Duration = TimeSpan.FromMilliseconds(400),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3) // 3 kez tekrarla
            };

            btnLogin.BeginAnimation(OpacityProperty, pulseAnimation);

            // Kısa bir gecikme ekle
            await Task.Delay(1000);

            // Buton metnini değiştirerek animasyon göster
            btnLogin.Content = "Boss Giriş Yapılıyor...";
            await Task.Delay(900);

            btnLogin.Content = "Auto İşlem Başlatılıyor...";
            await Task.Delay(900);

            btnLogin.Content = "Login Pass Geçiliyor...";
            await Task.Delay(4000);

            // Ana ekranı aç
          //  var overlay = new OverlayWindows();
            //overlay.Show();
            Main mainForm = new Main();
            mainForm.Show();
          


            // Login formunu kapat
            this.Close();
        }

        // System.Management kullanmadan benzersiz donanım kimliği oluştur
        private string CreateHardwareID()
        {
            StringBuilder sb = new StringBuilder();

            // Windows kurulum bilgisi
            sb.Append(Environment.OSVersion.ToString());

            // Bilgisayar adı
            sb.Append(Environment.MachineName);

            // Kullanıcı adı 
            sb.Append(Environment.UserName);

            // İşlemci sayısı
            sb.Append(Environment.ProcessorCount);

            // Sistem klasörü (genellikle sabit diskin seri numarasıyla ilişkilidir)
            sb.Append(Environment.SystemDirectory);

            // Disk bilgisi
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        sb.Append(drive.Name);
                        sb.Append(drive.DriveFormat);
                        sb.Append(drive.TotalSize);
                    }
                }
            }
            catch { /* Disk bilgisi alınamazsa devam et */ }

            // Hash oluştur
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));

                // Hash'i hexadecimal string'e dönüştür
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text;
                string password = txtPassword.Password;

                // Basit doğrulama
                if (ValidateLogin(username, password))
                {
                    // Ana formu oluştur
                    Main mainForm = new Main();
                    mainForm.Show();

                    // Login formunu kapat
                    this.Close();
                }
                else
                {
                    // Hata mesajı göster
                    txtError.Foreground = Brushes.Red;
                    txtError.FontWeight = FontWeights.Normal;
                    txtError.Text = "Kullanıcı adı veya şifre yanlış!";
                    txtError.Visibility = Visibility.Visible;
                    txtPassword.Password = "";
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Giriş işlemi sırasında hata: " + ex.Message,
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateLogin(string username, string password)
        {
            // Basit doğrulama - gerçek projede daha güvenli yöntemler kullanılmalı
            return (username == "1" && password == "1") ||
                   (username == "boss" && password == "1");
        }

        // Test butonu için olay işleyicisi
        private void btnTestRuntime_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Runtimes artık StartupScreen üzerinde otomatik olarak kontrol edilmektedir. " +
                "Bu buton kaldırılabilir.",
                "Bilgi",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}