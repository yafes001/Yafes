using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Animation;
using Yafes;
using Yafes.GameData;

namespace Yafes
{
    public partial class Main : Window
    {
        private ListBox _lstDrivers;
        private GamesManager gamesManager;
        private SystemInfoManager systemInfoManager;
        private bool _driversMessageShown = false;
        private bool _programsMessageShown = false;
        private readonly string driversFolder = "C:\\Drivers";
        private readonly string programsFolder = "C:\\Programs"; // Yeni programlar klasörü
        private readonly string alternativeDriversFolder = "F:\\MSI Drivers"; // Alternatif sürücü klasörü
        private readonly string alternativeProgramsFolder = "F:\\Programs"; // Alternatif programlar klasörü
        private readonly HttpClient httpClient = new HttpClient();
        private List<DriverInfo> drivers = new List<DriverInfo>();
        private List<ProgramInfo> programs = new List<ProgramInfo>(); // Yeni program listesi
        private int currentDriverIndex = 0;
        private int currentProgramIndex = 