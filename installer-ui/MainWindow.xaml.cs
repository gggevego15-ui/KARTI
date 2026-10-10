using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace KartiGlassInstaller;

public partial class MainWindow : Window
{
    private bool _installing;
    private bool _syncingPaths;
    private bool _lightTheme;
    private string _installFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "KARTI");
    private const int AccentEnableAcrylic = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy { public int State; public int Flags; public int GradientColor; public int AnimationId; }
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData { public int Attribute; public IntPtr Data; public int SizeOfData; }
    [DllImport("user32.dll")] private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    public MainWindow()
    {
        InitializeComponent();
        ApplyWallpaperAccent();
        _syncingPaths = true;
        FolderBox.Text = _installFolder;
        PlacePathBox.Text = _installFolder;
        _syncingPaths = false;
        Loaded += (_, _) => EnableBackdrop();
    }

    // Sample the active Windows wallpaper and tint the glass glows to fit the user's desktop.
    // If the wallpaper is unavailable or unsupported, the default teal/violet palette remains.
    private void ApplyWallpaperAccent()
    {
        try
        {
            var wallpaper = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop")?.GetValue("Wallpaper") as string;
            if (string.IsNullOrWhiteSpace(wallpaper) || !File.Exists(wallpaper)) return;
            using var bitmap = new System.Drawing.Bitmap(wallpaper);
            long red = 0, green = 0, blue = 0, samples = 0;
            for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
            {
                var pixel = bitmap.GetPixel(Math.Clamp((int)((x + .5) * bitmap.Width / 7), 0, bitmap.Width - 1),
                                            Math.Clamp((int)((y + .5) * bitmap.Height / 7), 0, bitmap.Height - 1));
                red += pixel.R; green += pixel.G; blue += pixel.B; samples++;
            }
            var r = (byte)(red / samples);
            var g = (byte)(green / samples);
            var b = (byte)(blue / samples);
            var wallpaperTint = System.Windows.Media.Color.FromRgb(r, g, b);
            var teal = System.Windows.Media.Color.FromRgb(0x3D, 0xF4, 0xD8);
            var violet = System.Windows.Media.Color.FromRgb(0x8D, 0x78, 0xFF);
            var left = Blend(teal, wallpaperTint, .24);
            var right = Blend(violet, wallpaperTint, .18);
            GlowLeft.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x42, left.R, left.G, left.B));
            GlowRight.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x40, right.R, right.G, right.B));
            RootSurface.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x72, left.R, left.G, left.B));
        }
        catch
        {
            // Wallpaper sampling is a visual enhancement, never a setup requirement.
        }
    }

    private static System.Windows.Media.Color Blend(System.Windows.Media.Color foreground, System.Windows.Media.Color background, double amount)
    {
        byte Mix(byte a, byte b) => (byte)Math.Clamp((int)Math.Round(a * (1 - amount) + b * amount), 0, 255);
        return System.Windows.Media.Color.FromRgb(Mix(foreground.R, background.R), Mix(foreground.G, background.G), Mix(foreground.B, background.B));
    }

    private void EnableBackdrop()
    {
        try
        {
            var policy = new AccentPolicy { State = AccentEnableAcrylic, Flags = 2, GradientColor = unchecked((int)(_lightTheme ? 0xDCEAF4FA : 0x88101827)), AnimationId = 0 };
            var size = Marshal.SizeOf<AccentPolicy>();
            var pointer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(policy, pointer, false);
                var data = new WindowCompositionAttributeData { Attribute = 19, Data = pointer, SizeOfData = size };
                SetWindowCompositionAttribute(new WindowInteropHelper(this).Handle, ref data);
            }
            finally { Marshal.FreeHGlobal(pointer); }
        }
        catch { /* The decorative backdrop is optional. */ }
    }

    private void FolderBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncingPaths) return;
        _installFolder = FolderBox.Text.Trim();
        _syncingPaths = true;
        if (PlacePathBox is not null) PlacePathBox.Text = _installFolder;
        _syncingPaths = false;
        if (InstallButton is not null) InstallButton.IsEnabled = !_installing && !string.IsNullOrWhiteSpace(_installFolder);
    }

    private void PlacePathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncingPaths) return;
        _installFolder = PlacePathBox.Text.Trim();
        _syncingPaths = true;
        if (FolderBox is not null) FolderBox.Text = _installFolder;
        _syncingPaths = false;
        if (InstallButton is not null) InstallButton.IsEnabled = !_installing && !string.IsNullOrWhiteSpace(_installFolder);
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Выбери папку для KARTI",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_installFolder) ? _installFolder : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _installFolder = dialog.SelectedPath;
            _syncingPaths = true;
            FolderBox.Text = _installFolder;
            PlacePathBox.Text = _installFolder;
            _syncingPaths = false;
        }
    }

    private void SetTab(string tab)
    {
        WelcomePanel.Visibility = tab == "install" ? Visibility.Visible : Visibility.Collapsed;
        PlacePanel.Visibility = tab == "place" ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility = tab == "about" ? Visibility.Visible : Visibility.Collapsed;
        InstallTab.Background = tab == "install" ? new SolidColorBrush(Color.FromArgb(0x65, 0x45, 0xE8, 0xD0)) : new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
        PlaceTab.Background = tab == "place" ? new SolidColorBrush(Color.FromArgb(0x65, 0x45, 0xE8, 0xD0)) : new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
        AboutTab.Background = tab == "about" ? new SolidColorBrush(Color.FromArgb(0x65, 0x45, 0xE8, 0xD0)) : new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
    }
    private void TabInstall_Click(object sender, RoutedEventArgs e) => SetTab("install");
    private void TabPlace_Click(object sender, RoutedEventArgs e) => SetTab("place");
    private void TabAbout_Click(object sender, RoutedEventArgs e) => SetTab("about");

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _lightTheme = !_lightTheme;
        if (_lightTheme)
        {
            RootSurface.Background = new SolidColorBrush(Color.FromArgb(0xF2, 0xE9, 0xF4, 0xFB));
            RootSurface.BorderBrush = new SolidColorBrush(Color.FromArgb(0xD0, 0xFF, 0xFF, 0xFF));
            RootSurface.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(0x8A, 0xB7, 0xE8), BlurRadius = 42, ShadowDepth = 0, Opacity = .45 };
            Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x2A, 0x40));
            ThemeButton.Content = "☾  Тёмная тема";
            SetTextColors(this, Color.FromRgb(0x1A, 0x2A, 0x40));
            FolderBox.Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x2A, 0x40));
            PlacePathBox.Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x2A, 0x40));
            FolderBox.Background = new SolidColorBrush(Color.FromArgb(0xC8, 0xFF, 0xFF, 0xFF));
            PlacePathBox.Background = new SolidColorBrush(Color.FromArgb(0xC8, 0xFF, 0xFF, 0xFF));
        }
        else
        {
            RootSurface.Background = new SolidColorBrush(Color.FromArgb(0x78, 0x10, 0x18, 0x2A));
            RootSurface.BorderBrush = new SolidColorBrush(Color.FromArgb(0x6E, 0xFF, 0xFF, 0xFF));
            RootSurface.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(0x5B, 0x66, 0xFF), BlurRadius = 48, ShadowDepth = 0, Opacity = .48 };
            Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0xFB, 0xFF));
            ThemeButton.Content = "☼  Светлая тема";
            SetTextColors(this, Color.FromRgb(0xF5, 0xFB, 0xFF));
            FolderBox.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0xFB, 0xFF));
            PlacePathBox.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0xFB, 0xFF));
            FolderBox.Background = new SolidColorBrush(Color.FromArgb(0x1B, 0xFF, 0xFF, 0xFF));
            PlacePathBox.Background = new SolidColorBrush(Color.FromArgb(0x1B, 0xFF, 0xFF, 0xFF));
        }
        EnableBackdrop();
    }

    private static void SetTextColors(DependencyObject parent, Color color)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBlock text) text.Foreground = new SolidColorBrush(color);
            if (child is Button button) button.Foreground = new SolidColorBrush(color);
            SetTextColors(child, color);
        }
    }

    private void ShowStage(string stage)
    {
        WelcomePanel.Visibility = stage == "welcome" ? Visibility.Visible : Visibility.Collapsed;
        PlacePanel.Visibility = Visibility.Collapsed;
        AboutPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = stage == "progress" ? Visibility.Visible : Visibility.Collapsed;
        DonePanel.Visibility = stage == "done" ? Visibility.Visible : Visibility.Collapsed;
        ErrorPanel.Visibility = stage == "error" ? Visibility.Visible : Visibility.Collapsed;
        InstallTab.Visibility = stage == "welcome" ? Visibility.Visible : Visibility.Collapsed;
        PlaceTab.Visibility = stage == "welcome" ? Visibility.Visible : Visibility.Collapsed;
        AboutTab.Visibility = stage == "welcome" ? Visibility.Visible : Visibility.Collapsed;
        InstallButton.Visibility = stage == "welcome" ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = stage is "welcome" or "error" ? Visibility.Visible : Visibility.Collapsed;
        LaunchButton.Visibility = stage == "done" ? Visibility.Visible : Visibility.Collapsed;
        RetryButton.Visibility = stage == "error" ? Visibility.Visible : Visibility.Collapsed;
        FooterText.Text = stage == "progress" ? "KARTI • УСТАНОВКА" : "KARTI • LIQUID GLASS EXPERIENCE";
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (_installing) return;
        _installFolder = FolderBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(_installFolder))
        {
            MessageBox.Show("Укажи папку установки.", "KARTI", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _installing = true;
        InstallButton.IsEnabled = false;
        ShowStage("progress");
        StatusText.Text = "Подготавливаем файлы KARTI…";
        string? payloadPath = null;
        try
        {
            Directory.CreateDirectory(_installFolder);
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KartiGlassInstaller.Payload.SetupExe")
                ?? throw new FileNotFoundException("В установщик не встроен пакет KARTI.");
            payloadPath = Path.Combine(Path.GetTempPath(), "KARTI-Setup-internal-" + Guid.NewGuid().ToString("N") + ".exe");
            await using (var output = new FileStream(payloadPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await stream.CopyToAsync(output);

            StatusText.Text = "Устанавливаем приложение и ярлыки…";
            var start = new ProcessStartInfo
            {
                FileName = payloadPath,
                Arguments = "/S /D=" + _installFolder,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Не удалось запустить системный установщик.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Системный установщик завершился с кодом {process.ExitCode}. Попробуй выбрать другую папку или запусти установщик ещё раз.");
            var exe = Path.Combine(_installFolder, "KARTI.exe");
            if (!File.Exists(exe))
            {
                var candidates = new[] { Path.Combine(_installFolder, "KARTI", "KARTI.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "KARTI", "KARTI.exe") };
                exe = candidates.FirstOrDefault(File.Exists) ?? exe;
            }
            if (!File.Exists(exe))
                throw new FileNotFoundException("Установщик завершился, но файл KARTI.exe не найден в выбранной папке. Проверь путь и повтори установку.");
            _installFolder = Path.GetDirectoryName(exe)!;
            _installing = false;
            ShowStage("done");
        }
        catch (Exception ex)
        {
            _installing = false;
            ErrorText.Text = ex.Message;
            ShowStage("error");
        }
        finally
        {
            InstallButton.IsEnabled = true;
            if (payloadPath is not null) { try { File.Delete(payloadPath); } catch { } }
        }
    }

    private void Retry_Click(object sender, RoutedEventArgs e) { ShowStage("welcome"); SetTab("install"); }
    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        var exe = Path.Combine(_installFolder, "KARTI.exe");
        if (File.Exists(exe)) { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true }); Close(); }
        else { ErrorText.Text = "Не найден KARTI.exe. Попробуй установить приложение ещё раз."; ShowStage("error"); }
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) { if (!_installing) Close(); }
}