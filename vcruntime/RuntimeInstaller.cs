using Microsoft.Win32;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Yafes
{
    /// <summary>
    /// VC Runtime yükleyicisi için yardımcı sınıf
    /// </summary>
    public class RuntimeInstaller
    {
        // Kurulum klasörleri ve dosyaları
        private static readonly string VCRedistX64Url = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
        private static readonly string VCRedistX86Url = "https://aka.ms/vs/17/release/vc_redist.x86.exe";

        // Uygulama klasörü içinde Runtime dosyalarının bulunduğu klasör
        private static readonly string RuntimesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vcruntime");

        /// <summary>
        /// Runtime installer'ı oluştur ve kurulum klasörünü hazırla
        /// </summary>
        public static void PrepareRuntimeInstaller()
        {
            try
            {
                // VCRuntime klasörünü oluştur (yoksa)
                if (!Directory.Exists(RuntimesFolder))
                {
                    Directory.CreateDirectory(RuntimesFolder);

                    // Burada ayrıca, ilk kurulumda runtime dosyalarını indirebilirsiniz
                    // DownloadVCRuntimes();
                }
            }
            catch
            {
                // Hata durumunda sessizce devam et
            }
        }

        /// <summary>
        /// Gerekli VC++ Runtime'ları internet üzerinden indir
        /// (Bu işlev isteğe bağlıdır, kurulum paketine dahil edilirse kullanılmayabilir)
        /// </summary>
        public static async Task DownloadVCRuntimes()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5); // Zaman aşımı ayarı
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 Yafes Driver Tool");

                    // x64 VC++ Runtime'ı indir
                    string x64Path = Path.Combine(RuntimesFolder, "VC_redist.x64.exe");
                    if (!File.Exists(x64Path))
                    {
                        var responseX64 = await client.GetAsync(VCRedistX64Url);
                        responseX64.EnsureSuccessStatusCode();

                        using (var fileStream = new FileStream(x64Path, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await responseX64.Content.CopyToAsync(fileStream);
                        }

                        Console.WriteLine("[DEBUG] x64 VC++ Runtime indirildi");
                    }

                    // x86 VC++ Runtime'ı indir
                    string x86Path = Path.Combine(RuntimesFolder, "VC_redist.x86.exe");
                    if (!File.Exists(x86Path))
                    {
                        var responseX86 = await client.GetAsync(VCRedistX86Url);
                        responseX86.EnsureSuccessStatusCode();

                        using (var fileStream = new FileStream(x86Path, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await responseX86.Content.CopyToAsync(fileStream);
                        }

                        Console.WriteLine("[DEBUG] x86 VC++ Runtime indirildi");
                    }
                }
            }
            catch (Exception ex)
            {
                // İndirme hatası - hata mesajını logla ama sessizce devam et
                Console.WriteLine($"[ERROR] VC++ Runtime indirme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// VC++ Runtime'ların yüklü olup olmadığını kontrol et
        /// </summary>
        public static bool AreVCRuntimesInstalled()
        {
            try
            {
                // VC++ 2015-2022 x64 kontrolü
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64"))
                {
                    if (key != null)
                    {
                        return true;
                    }
                }

                // VC++ 2015-2022 x86 kontrolü
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86"))
                {
                    if (key != null)
                    {
                        return true;
                    }
                }

                // Alternatif Registry yolu kontrolü (yeni sürümler için)
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\VisualStudio\VC\Runtimes\x64"))
                {
                    if (key != null)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                // Hata durumunda yüklü olmadığını varsay
                return false;
            }
        }

        /// <summary>
        /// Uygulama ilk kez çalıştırıldığında kontrol et
        /// </summary>
        public static bool IsFirstRun()
        {
            try
            {
                // Registry'de bir anahtar oluşturup kontrol etme
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\YafesDriverTool"))
                {
                    return key == null;
                }
            }
            catch
            {
                // Hata durumunda ilk çalıştırma olarak kabul et
                return true;
            }
        }

        /// <summary>
        /// İlk çalıştırma işaretini kaydet
        /// </summary>
        public static void MarkFirstRunCompleted()
        {
            try
            {
                // İlk çalıştırma bayrağını ayarla
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\YafesDriverTool"))
                {
                    key.SetValue("FirstRunCompleted", 1);
                }
            }
            catch
            {
                // Registry hatası - sessizce devam et
            }
        }
    }
}