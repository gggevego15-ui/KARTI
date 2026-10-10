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
        _syncingPaths = true;
        FolderBox.Text = _installFolder;
        PlacePathBox.Text = _installFolder;
        _syncingPaths = false;
        Loaded += (_, _) => EnableBackdrop();
    }

    private void EnableBackdrop()
    {
        try
        {
            var policy = new AccentPolicy { State = AccentEnableAcrylic, Flags = 2, GradientColor = unchecked((int)(_lightTheme ? 0xDCEAF4FA : 0xCC101827)), AnimationId = 0 };
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
            RootSurface.Background = new SolidColorBrush(Color.FromArgb(0xEC, 0x07, 0x11, 0x1F));
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