using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.ReactiveUI;
using ITDSWrapper.Core;
#if IS_LINUX
using ITDSWrapper.Desktop.Linux;
#endif
#if MACOS
using AvFoundationBackend;
#endif
using ITDSWrapper.Desktop.Steam;
#if IS_WINDOWS
using ITDSWrapper.Desktop.Windows;
#endif

namespace ITDSWrapper.Desktop;

sealed class Program
{
    private const string DebugIpcEnvironmentVariable = "DEBUG_IPC";
    private const string NoSteamEnvironmentVariable = "NOSTEAM";
    private const string ClearSteamCloudEnvironmentVariable = "CLEAR_CLOUD";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"),
                $"CRASH: {ex.Message}\n\n{ex.StackTrace}");
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace()
            .With(() =>
            {
                Win32RenderingMode[] defaultModes =
                [
                    Win32RenderingMode.Wgl, Win32RenderingMode.Vulkan, Win32RenderingMode.AngleEgl,
                    Win32RenderingMode.Software,
                ];
                if (!Path.Exists(Path.Combine(AppContext.BaseDirectory, "settings", "settings.json")))
                    return new() { RenderingMode = defaultModes };

                var wrapperSettings = JsonSerializer.Deserialize<Settings>(
                    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "settings",
                        "settings.json")))!;
                return new Win32PlatformOptions
                {
                    RenderingMode = wrapperSettings.WindowsRenderingMode switch
                    {
                        WindowsRenderingMode.WINDOWS_GL => [Win32RenderingMode.Wgl],
                        WindowsRenderingMode.VULKAN => [Win32RenderingMode.Vulkan],
                        WindowsRenderingMode.ANGLE_EGL => [Win32RenderingMode.AngleEgl],
                        _ => [Win32RenderingMode.Software],
                    },
                };
            })
            .With(() =>
            {
                AvaloniaNativeRenderingMode[] defaultModes =
                [
                    AvaloniaNativeRenderingMode.Metal, AvaloniaNativeRenderingMode.OpenGl,
                    AvaloniaNativeRenderingMode.Software,
                ];
                if (!Path.Exists(Path.Combine(AppContext.BaseDirectory, "settings", "settings.json")))
                    return new() { RenderingMode = defaultModes };

                var wrapperSettings = JsonSerializer.Deserialize<Settings>(
                    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "settings",
                        "settings.json")))!;
                return new AvaloniaNativePlatformOptions
                {
                    RenderingMode = wrapperSettings.MacOsRenderingMode switch
                    {
                        MacOsRenderingMode.METAL => [AvaloniaNativeRenderingMode.Metal],
                        MacOsRenderingMode.OPENGL => [AvaloniaNativeRenderingMode.OpenGl],
                        _ => [AvaloniaNativeRenderingMode.Software],
                    },
                };
            })
            .With(() =>
            {
                X11RenderingMode[] defaultModes =
                [
                    X11RenderingMode.Glx, X11RenderingMode.Egl, X11RenderingMode.Vulkan, X11RenderingMode.Software,
                ];
                if (!Path.Exists(Path.Combine(AppContext.BaseDirectory, "settings", "settings.json")))
                    return new() { RenderingMode = defaultModes };

                var wrapperSettings = JsonSerializer.Deserialize<Settings>(
                    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "settings",
                        "settings.json")))!;
                return new X11PlatformOptions()
                {
                    RenderingMode = wrapperSettings.LinuxRenderingMode switch
                    {
                        LinuxRenderingMode.EGL => [X11RenderingMode.Egl],
                        LinuxRenderingMode.VULKAN => [X11RenderingMode.Vulkan],
                        LinuxRenderingMode.GLX => [X11RenderingMode.Glx],
                        _ => [X11RenderingMode.Software],
                    },
                };
            })
            .AfterSetup(b =>
            {
                string? ipcPath = Environment.GetEnvironmentVariable(DebugIpcEnvironmentVariable);
                if (!string.IsNullOrEmpty(ipcPath))
                {
                    Process.Start(ipcPath);
                }

                SteamHelperIpc ipc = new();
                if (!Environment.GetEnvironmentVariable(NoSteamEnvironmentVariable)
                        ?.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ?? true)
                {
                    try
                    {
                        if (Environment.GetEnvironmentVariable(ClearSteamCloudEnvironmentVariable)
                                ?.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ?? false)
                        {
                            SteamSaveManager.ClearSteamCloud(ipc);
                        }

                        SdlInputContextHost inputContextHost = new();
                        SdlInputDriver inputDriver = new(inputContextHost);
                        ((App)b.Instance!).InputDrivers = [inputDriver];
                        ((App)b.Instance).DesktopTopLevelOpened = inputContextHost.Attach;
                        ((App)b.Instance).Updater = new SteamUpdater(inputDriver, ipc);
                        ((App)b.Instance).InputSwitcher = new();
                        SteamLogInterpreter logInterpreter = new(inputDriver, ((App)b.Instance).InputSwitcher!, ipc)
                        {
                            AchievementManager = new SteamAchievementManager(ipc),
                            WatchForSdCreate = SteamSaveManager.DownloadCloudSave(ipc),
                        };
                        ((App)b.Instance).LogInterpreter = logInterpreter;
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup_crash.log"),
                            $"{ex.Message}\n{ex.StackTrace}");
                        throw;
                    }
                }

                ((App)b.Instance!).BatteryMonitor = new BatteryMonitor();
#if MACOS
                ((App)b.Instance).AudioBackend = new AvFoundationAudioBackend();
#elif IS_WINDOWS
                ((App)b.Instance).AudioBackend = new WasapiAudioBackend();
#elif IS_LINUX
                ((App)b.Instance).AudioBackend = new AlsaAudioBackend();
#endif
                ipc.SendCommand("GAME_LANGUAGE");
#if MACOS
                ((App)b.Instance).ScreenReader =
 new AvFoundationScreenReader(DesktopScreenReader.GetPlatformSpecificLanguageCode(Encoding.UTF8.GetString(ipc.ReceiveResponse())));
#else
                ((App)b.Instance).ScreenReader =
                    DesktopScreenReader.Instantiate(Encoding.UTF8.GetString(ipc.ReceiveResponse()));
#endif
            });
}