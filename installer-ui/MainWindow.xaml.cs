using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace KartiGlassInstaller;

public partial class MainWindow : Window
{
    private bool _installing;
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
        FolderBox.Text = _installFolder;
        Loaded += (_, _) => EnableBackdrop();
    }

    private void EnableBackdrop()
    {
        try
        {
            var policy = new AccentPolicy { State = AccentEnableAcrylic, Flags = 2, GradientColor = unchecked((int)0xCC101827), AnimationId = 0 };
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
        catch { /* Decorative backdrop is optional; installation must still work. */ }
    }

    private void FolderBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (FolderBox is not null) _installFolder = FolderBox.Text.Trim();
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
            FolderBox.Text = dialog.SelectedPath;
            _installFolder = dialog.SelectedPath;
        }
    }

    private void ShowStage(string stage)
    {
        WelcomePanel.Visibility = stage == "welcome" ? Visibility.Visible : Visibility.Collapsed;
        ProgressPanel.Visibility = stage == "progress" ? Visibility.Visible : Visibility.Collapsed;
        DonePanel.Visibility = stage == "done" ? Visibility.Visible : Visibility.Collapsed;
        ErrorPanel.Visibility = stage == "error" ? Visibility.Visible : Visibility.Collapsed;
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
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("KartiGlassInstaller.Payload.SetupExe")
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
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Не удалось запустить системный установщик.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Системный установщик завершился с кодом {process.ExitCode}. Попробуй выбрать другую папку или запусти установщик ещё раз.");
            var exe = Path.Combine(_installFolder, "KARTI.exe");
            if (!File.Exists(exe))
            {
                // electron-builder may normalize a selected destination depending on NSIS install mode.
                var candidates = new[] { Path.Combine(_installFolder, "KARTI", "KARTI.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "KARTI", "KARTI.exe") };
                exe = candidates.FirstOrDefault(File.Exists) ?? exe;
            }
            if (!File.Exists(exe))
                throw new FileNotFoundException("Установщик завершился, но файл KARTI.exe не найден в выбранной папке. Проверьте папку и повторите установку.");
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

    private void Retry_Click(object sender, RoutedEventArgs e) => ShowStage("welcome");
    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        var exe = Path.Combine(_installFolder, "KARTI.exe");
        if (File.Exists(exe)) { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true }); Close(); }
        else { ErrorText.Text = "Не найден KARTI.exe. Попробуй установить приложение ещё раз."; ShowStage("error"); }
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) { if (!_installing) Close(); }
}
