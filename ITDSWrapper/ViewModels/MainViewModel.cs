using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ITDSWrapper.Assets;
using ITDSWrapper.Audio;
using ITDSWrapper.Core;
using ITDSWrapper.Graphics;
using ITDSWrapper.Haptics;
using ITDSWrapper.Input;
using ITDSWrapper.ViewModels.Controls;
using ITDSWrapper.Views;
using Libretro.NET;
using Libretro.NET.Bindings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ITDSWrapper.ViewModels;

public class MainViewModel : ViewModelBase
{
    public static bool IsMobile => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public Control? Top { get; set; }

    private enum Setting
    {
        SCREEN_LAYOUT,
    }

    [Reactive] public WindowState WindowState { get; set; } = WindowState.FullScreen;
    [Reactive] public WindowDecorations Decorations { get; set; } = WindowDecorations.Full;
    [Reactive] public bool ExtendClientArea { get; set; }
    [Reactive] public string WindowingModeDesc { get; set; } = Strings.WindowStateFullScreen;

    public int WindowingModeIdx
    {
        get => (int)WrapperSettings.WindowingMode;
        set
        {
            WrapperSettings.WindowingMode = (WindowingMode)value;
            if (value >= 0 && value < Enum.GetNames<WindowingMode>().Length)
            {
                WindowingModeDesc = new[]
                {
                    Strings.WindowStateFullScreen, Strings.WindowStateBorderless, Strings.WindowStateWindowed
                }[value];
            }

            switch (WrapperSettings.WindowingMode)
            {
                default:
                case WindowingMode.FULL_SCREEN:
                    var window = (MainWindow?)Top;
                    Screen? currentScreen = window?.Screens.ScreenFromWindow(window);
                    if (currentScreen is not null)
                    {
                        // Keeps us on the right screen when we're full screening
                        window?.Position = new(currentScreen.WorkingArea.X, currentScreen.WorkingArea.Y);
                    }

                    WindowState = WindowState.FullScreen;
                    Decorations = WindowDecorations.None;
                    ExtendClientArea = true;
                    break;

                case WindowingMode.BORDERLESS:
                    WindowState = WindowState.Maximized;
                    Decorations = WindowDecorations.None;
                    ExtendClientArea = true;
                    break;

                case WindowingMode.WINDOWED:
                    WindowState = WindowState.Maximized;
                    Decorations = WindowDecorations.Full;
                    ExtendClientArea = false;
                    break;
            }
        }
    }

    [Reactive] public string TargetScreenLayoutDesc { get; set; } = Strings.ScreenLayoutTopBottom;

    public int TargetScreenLayoutIdx
    {
        get;
        set
        {
            field = value;
            if (value >= 0 && value < Enum.GetNames<ScreenLayout>().Length)
            {
                TargetScreenLayoutDesc = new[]
                {
                    Strings.ScreenLayoutTopBottom, Strings.ScreenLayoutLeftRight, Strings.ScreenLayoutRightLeft
                }[value];
            }
        }
    }

    [Reactive] public string LinuxRenderingModeDesc { get; set; } = Strings.LinuxRenderingModeGlx;

    public int LinuxRenderingModeIdx
    {
        get;
        set
        {
            field = value;
            if (value >= 0 && value < Enum.GetNames<LinuxRenderingMode>().Length)
            {
                LinuxRenderingModeDesc = new[]
                {
                    Strings.LinuxRenderingModeGlx, Strings.LinuxRenderingModeEgl,
                    Strings.LinuxRenderingModeVulkan, Strings.LinuxRenderingModeSoftware,
                }[value];
            }
        }
    }

    [Reactive] public string MacOsRenderingModeDesc { get; set; } = Strings.MacOsRenderingModeMetal;

    public int MacOsRenderingModeIdx
    {
        get;
        set
        {
            field = value;
            if (value >= 0 && value < Enum.GetNames<MacOsRenderingMode>().Length)
            {
                MacOsRenderingModeDesc = new[]
                {
                    Strings.MacOsRenderingModeMetal, Strings.MacOsRenderingModeOpenGl,
                    Strings.MacOsRenderingModeSoftware,
                }[value];
            }
        }
    }

    [Reactive] public string WindowsRenderingModeDesc { get; set; } = Strings.WindowsRenderingModeWgl;

    public int WindowsRenderingModeIdx
    {
        get;
        set
        {
            field = value;
            if (value >= 0 && value < Enum.GetNames<WindowsRenderingMode>().Length)
            {
                WindowsRenderingModeDesc = new[]
                {
                    Strings.WindowsRenderingModeWgl, Strings.WindowsRenderingModeVulkan,
                    Strings.WindowsRenderingModeAngleEgl, Strings.WindowsRenderingModeSoftware,
                }[value];
            }
        }
    }

    [Reactive] public string BordersSettingDesc { get; set; }
    [Reactive] public string AudioDeviceSettingsDesc { get; set; }
    [Reactive] public string ScreenReaderSettingDesc { get; set; }
    [Reactive] public string VirtualButtonsLayoutSettingDesc { get; set; }
    [Reactive] public string VirtualButtonsHapticsSettingDesc { get; set; }

    private readonly Dictionary<Setting, Action> _settingsChangesBuffer = [];

    public RetroWrapper Wrapper { get; }
    [Reactive] public EmuImageSource? CurrentFrame { get; set; }

    [Reactive] public bool DisplaySettingsMenuOpen { get; set; }
    [Reactive] public bool AudioSettingsMenuOpen { get; set; }
    [Reactive] public bool ControllerSettingsMenuOpen { get; set; }
    [Reactive] public bool AccessibilitySettingsMenuOpen { get; set; }
    [Reactive] public bool LegalMenuOpen { get; set; }
    [Reactive] public bool ShowSidebar { get; set; }

    public Settings WrapperSettings { get; }
    private int _currentLangBit;
    private int _langInputsSent;

    public double EmuRenderRatio { get; private set; } = 256.0 / 384.0;

    public double EmuRenderWidth
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            _pointerState?.Width = value;
        }
    } = 256;

    public double EmuRenderHeight
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            _pointerState?.Height = value;
        }
    } = 384;

    public int TopPadding
    {
        get => IsMobile ? field : 0;
        set;
    } = 45;

    public int BottomPadding
    {
        get => IsMobile ? field : 0;
        set;
    } = 10;

    public static int MenuMargins => IsMobile ? 30 : 50;

    public bool DisplayVirtualControls => IsMobile && CurrentInputDriver == 0;

    [Reactive] public Bitmap? CurrentBorder { get; set; }
    [Reactive] public Bitmap? NextBorder { get; set; }
    [Reactive] public double CurrentBorderOpacity { get; set; } = 1.0;
    [Reactive] public double NextBorderOpacity { get; set; }
    private System.Timers.Timer? _borderTimer;
    private string _currentBorder = "BLACK_BG";
    private int _currentBorderFrame;
    private string _nextBorder = string.Empty;
    private int _nextBorderFrame;

    public bool Closing { get; set; }

    private IUpdater? _updater;

    private readonly PauseDriver _pauseDriver;
    private readonly LogInterpreter? _logInterpreter;

    private readonly IAudioBackend _audioBackend;
    private readonly IHapticsBackend? _hapticsBackend;

    private readonly List<IInputDriver> _inputDrivers;
    public int NumInputDrivers => _inputDrivers.Count;

    public int CurrentInputDriver
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(DisplayVirtualControls));
        }
    }

    private readonly PointerState? _pointerState;

    private readonly InputSwitcher _inputSwitcher;

    private readonly System.Timers.Timer _batteryTimer;

    private readonly System.Timers.Timer _menuInputTimer;
    private bool _acceptingMenuInput = true;

    public ICommand CloseMenuOverlayCommand { get; }
    public ICommand OpenDisplaySettingsMenuCommand { get; }
    public ICommand OpenAudioSettingsMenuCommand { get; }
    public ICommand OpenControllerSettingsMenuCommand { get; }
    public ICommand OpenAccessibilitySettingsMenuCommand { get; }
    public ICommand OpenSurveyUrlCommand { get; }
    public ICommand OpenLegalMenuCommand { get; }
    public ICommand OpenDiscordUrlCommand { get; }
    public ICommand QuitToDesktopCommand { get; }

    public ICommand ChangeWindowingSettingsCommand { get; }
    public ICommand ChangeScreenLayoutCommand { get; }
    public ICommand ChangeRenderingModeCommand { get; }
    public ICommand ChangeBorderSettingsCommand { get; }
    public ICommand ChangeAudioDeviceSettingsCommand { get; }
    public ICommand ChangeScreenReaderSettingsCommand { get; }
    public ICommand ChangeControlPadHapticsSettingsCommand { get; }
    public ICommand ChangeVirtualButtonsLayoutSettingsCommand { get; }

    public ICommand SettingsBackCommand { get; }

    public VirtualButtonViewModel? AButton { get; set; }
    public VirtualButtonViewModel? BButton { get; set; }
    public VirtualButtonViewModel? XButton { get; set; }
    public VirtualButtonViewModel? YButton { get; set; }
    public VirtualButtonViewModel? LButton { get; set; }
    public VirtualButtonViewModel? RButton { get; set; }
    public VirtualDPadViewModel Dpad { get; set; } = new(150, 150);

    public VirtualButtonViewModel? StartButton { get; set; }
    public VirtualButtonViewModel? SelectButton { get; set; }
    public VirtualButtonViewModel? MenuButton { get; set; }

    [Reactive] public bool DisplayMenuOverlay { get; set; }
    [Reactive] public IEffect? ScreenEffect { get; set; }
    [Reactive] public bool IsFullLayout { get; set; }

    public MainViewModel()
    {
        WrapperSettings = Settings.Load(RetroWrapper.GetDirectoryForPlatform("settings"));
        WindowingModeIdx = (int)WrapperSettings.WindowingMode;
        TargetScreenLayoutIdx = IsMobile ? (int)ScreenLayout.TOP_BOTTOM : (int)WrapperSettings.CurrentScreenLayout;
        LinuxRenderingModeIdx = (int)WrapperSettings.LinuxRenderingMode;
        MacOsRenderingModeIdx = (int)WrapperSettings.MacOsRenderingMode;
        WindowsRenderingModeIdx = (int)WrapperSettings.WindowsRenderingMode;
        BordersSettingDesc = WrapperSettings.BordersEnabled
            ? Strings.SettingSwitchEnabled
            : Strings.SettingSwitchDisabled;
        ScreenReaderSettingDesc =
            WrapperSettings.ScreenReaderEnabled ? Strings.SettingSwitchOn : Strings.SettingSwitchOff;
        VirtualButtonsHapticsSettingDesc = WrapperSettings.VirtualButtonHaptics
            ? Strings.SettingSwitchOn
            : Strings.SettingSwitchDisabled;
        IsFullLayout = WrapperSettings.VirtualButtonFullLayout;
        VirtualButtonsLayoutSettingDesc = IsFullLayout
            ? Strings.SettingsVirtualButtonLayoutFull
            : Strings.SettingsVirtualButtonLayoutRecommended;
        Wrapper = new();
        _logInterpreter = ((App)Application.Current!).LogInterpreter ?? new();
        _logInterpreter.SetNextBorder = border =>
        {
            _nextBorder = border;
            SetNextBorder();
        };
        if (WrapperSettings.ScreenReaderEnabled)
        {
            StartScreenReader();
        }

        Wrapper.OnReceiveLog = HandleLog;
        Wrapper.LoadCore();
        using Stream ndsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ITDSWrapper.itds.nds")!;
        byte[] data = new byte[ndsStream.Length];
        ndsStream.ReadExactly(data);
        Wrapper.LoadGame(data);

        if (WrapperSettings.BordersEnabled)
        {
            StartBorder();
        }

        if (((App)Application.Current).AudioBackend is not null)
        {
            _audioBackend = ((App)Application.Current).AudioBackend!;
            _audioBackend.Initialize(Wrapper.SampleRate);
        }
        else
        {
            _audioBackend = new SilkNetOpenALBackend(Wrapper.SampleRate, 32);
        }

        AudioDeviceSettingsDesc =
            _audioBackend.GetDeviceList().FirstOrDefault(d => d.InternalName == WrapperSettings.AudioDevice)
                ?.DisplayName ?? Strings.SettingsAudioDeviceDefault;

        if (((App)Application.Current).HapticsBackend is not null)
        {
            _hapticsBackend = ((App)Application.Current).HapticsBackend!;
            _hapticsBackend.Initialize();
        }

        _pauseDriver = ((App)Application.Current).PauseDriver ?? new();
        _pauseDriver.AudioBackend = _audioBackend;

        _inputDrivers = ((App)Application.Current).InputDrivers ?? [];
        _inputDrivers.Insert(0, new DefaultInputDriver(IsMobile, ToggleMenuOverlay));
        for (int i = 1; i < _inputDrivers.Count; i++)
        {
            _inputDrivers[i].SetSpecialAction(RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, ToggleMenuOverlay);
        }

        _pointerState = new(EmuRenderWidth, EmuRenderHeight);
        if (IsMobile)
        {
            AssignVirtualBindings();
        }

        _inputSwitcher = ((App)Application.Current).InputSwitcher ?? new();
        _inputSwitcher.Wrapper = Wrapper;

        IBatteryMonitor? batteryMonitor = ((App)Application.Current).BatteryMonitor;
        Wrapper.BatteryLevel = batteryMonitor?.GetBatteryLevel() ?? 100;
        _batteryTimer = new(TimeSpan.FromMinutes(1)) { AutoReset = true };
        _batteryTimer.Elapsed += (_, _) => Wrapper.BatteryLevel = batteryMonitor?.GetBatteryLevel() ?? 100;
        _batteryTimer.Start();

        _menuInputTimer = new(TimeSpan.FromMilliseconds(300)) { AutoReset = false };
        _menuInputTimer.Elapsed += (_, _) => _acceptingMenuInput = true;

        CloseMenuOverlayCommand = ReactiveCommand.Create(ToggleMenuOverlay);
        OpenDisplaySettingsMenuCommand = ReactiveCommand.Create(OpenDisplaySettings);
        OpenAudioSettingsMenuCommand = ReactiveCommand.Create(OpenAudioSettings);
        OpenControllerSettingsMenuCommand = ReactiveCommand.Create(OpenControllerSettings);
        OpenAccessibilitySettingsMenuCommand = ReactiveCommand.Create(OpenAccessibilitySettings);
        OpenSurveyUrlCommand = ReactiveCommand.Create(() => OpenUrl("https://google.com"));
        OpenLegalMenuCommand = ReactiveCommand.Create(OpenLegal);
        OpenDiscordUrlCommand = ReactiveCommand.Create(() => OpenUrl("https://discord.com"));
        QuitToDesktopCommand = ReactiveCommand.Create(CloseApplication);

        ChangeWindowingSettingsCommand = ReactiveCommand.Create<bool>(ChangeWindowingSettings);
        ChangeScreenLayoutCommand = ReactiveCommand.Create<bool>(ChangeScreenLayout);
        ChangeRenderingModeCommand = ReactiveCommand.Create<bool>(ChangeRenderingMode);
        ChangeBorderSettingsCommand = ReactiveCommand.Create(ToggleBorderSettings);
        ChangeAudioDeviceSettingsCommand = ReactiveCommand.Create<bool>(ChangeAudioDevice);
        ChangeScreenReaderSettingsCommand = ReactiveCommand.Create(ToggleScreenReader);
        ChangeVirtualButtonsLayoutSettingsCommand = ReactiveCommand.Create(ToggleVirtualButtonsLayout);
        ChangeControlPadHapticsSettingsCommand = ReactiveCommand.Create(ToggleControlPadHaptics);

        SettingsBackCommand = ReactiveCommand.Create(CloseSubMenu);

        Wrapper.OnFrame = DisplayFrame;
        Wrapper.OnSample = PlaySample;
        _inputSwitcher.SetDefaultInputDelegate(HandleInput);
        _inputSwitcher.SetInputDelegate(new("startup", HandleStartupInput));
        Wrapper.OnRumble = DoRumble;
        ThreadPool.QueueUserWorkItem(_ => Run());
    }

    private void CloseApplication()
    {
        // todo a modal warning?
        _updater?.Die();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
    }

    private void OpenDisplaySettings()
    {
        DisplaySettingsMenuOpen = true;
        AudioSettingsMenuOpen = LegalMenuOpen = ControllerSettingsMenuOpen = AccessibilitySettingsMenuOpen = false;
        ShowSidebar = !IsMobile;
    }

    private void OpenAudioSettings()
    {
        AudioSettingsMenuOpen = true;
        DisplaySettingsMenuOpen = LegalMenuOpen = ControllerSettingsMenuOpen = AccessibilitySettingsMenuOpen = false;
        ShowSidebar = !IsMobile;
    }

    private void OpenControllerSettings()
    {
        ControllerSettingsMenuOpen = true;
        AudioSettingsMenuOpen = LegalMenuOpen = DisplaySettingsMenuOpen = AccessibilitySettingsMenuOpen = false;
        ShowSidebar = !IsMobile;
    }

    private void OpenAccessibilitySettings()
    {
        AccessibilitySettingsMenuOpen = true;
        AudioSettingsMenuOpen = DisplaySettingsMenuOpen = ControllerSettingsMenuOpen = LegalMenuOpen = false;
        ShowSidebar = !IsMobile;
    }

    private void OpenLegal()
    {
        LegalMenuOpen = true;
        AudioSettingsMenuOpen = DisplaySettingsMenuOpen = ControllerSettingsMenuOpen = AccessibilitySettingsMenuOpen = false;
        ShowSidebar = !IsMobile;
    }

    private void CloseSubMenu()
    {
        AudioSettingsMenuOpen = DisplaySettingsMenuOpen = ControllerSettingsMenuOpen = AccessibilitySettingsMenuOpen = LegalMenuOpen = false;
        ShowSidebar = true;
    }

    private void ToggleMenuOverlay()
    {
        DisplayMenuOverlay = !DisplayMenuOverlay;
        _pauseDriver.PushPauseState(DisplayMenuOverlay);
        AudioSettingsMenuOpen = DisplaySettingsMenuOpen = ControllerSettingsMenuOpen = LegalMenuOpen = false;
        ScreenEffect = ScreenEffect is null ? new BlurEffect { Radius = 50 } : null;
        ShowSidebar = true;

        if (!DisplayMenuOverlay)
        {
            foreach (Setting setting in _settingsChangesBuffer.Keys)
            {
                _settingsChangesBuffer[setting].Invoke();
            }

            _settingsChangesBuffer.Clear();
            WrapperSettings.Save(RetroWrapper.GetDirectoryForPlatform("settings"));
        }
    }

    private void OpenUrl(string url)
    {
        TopLevel.GetTopLevel(Top)?.Launcher.LaunchUriAsync(new(url));
    }

    private void ChangeWindowingSettings(bool forward)
    {
        if (!forward && WindowingModeIdx == 0)
        {
            WindowingModeIdx = 2;
        }
        else
        {
            WindowingModeIdx = (WindowingModeIdx + (forward ? 1 : -1)) % 3;
        }
    }

    private void ChangeScreenLayout(bool forward)
    {
        if (!forward && TargetScreenLayoutIdx == 0)
        {
            TargetScreenLayoutIdx = Enum.GetValues<ScreenLayout>().Length - 1;
        }
        else
        {
            TargetScreenLayoutIdx =
                (TargetScreenLayoutIdx + (forward ? 1 : -1)) % Enum.GetValues<ScreenLayout>().Length;
        }

        if (TargetScreenLayoutIdx == (int)WrapperSettings.CurrentScreenLayout)
        {
            _settingsChangesBuffer.Remove(Setting.SCREEN_LAYOUT);
        }
        else
        {
            _settingsChangesBuffer[Setting.SCREEN_LAYOUT] = () =>
            {
                int numPresses = TargetScreenLayoutIdx - (int)WrapperSettings.CurrentScreenLayout;
                numPresses = numPresses < 0 ? Enum.GetValues<ScreenLayout>().Length + numPresses : numPresses;

                ChangeEmulatedScreenLayout();
                SendLayoutChangeToCore(numPresses);
            };
        }
    }

    private void ChangeRenderingMode(bool forward)
    {
        if (OperatingSystem.IsLinux())
        {
            if (!forward && LinuxRenderingModeIdx == 0)
            {
                LinuxRenderingModeIdx = Enum.GetValues<LinuxRenderingMode>().Length - 1;
            }
            else
            {
                LinuxRenderingModeIdx = (LinuxRenderingModeIdx +
                                         (forward ? 1 : -1)) % Enum.GetValues<LinuxRenderingMode>().Length;
            }

            WrapperSettings.LinuxRenderingMode = (LinuxRenderingMode)LinuxRenderingModeIdx;
        }
        else if (OperatingSystem.IsMacOS())
        {
            if (!forward && MacOsRenderingModeIdx == 0)
            {
                MacOsRenderingModeIdx = Enum.GetValues<MacOsRenderingMode>().Length - 1;
            }
            else
            {
                MacOsRenderingModeIdx = (MacOsRenderingModeIdx +
                                         (forward ? 1 : -1)) % Enum.GetValues<MacOsRenderingMode>().Length;
            }

            WrapperSettings.MacOsRenderingMode = (MacOsRenderingMode)MacOsRenderingModeIdx;
        }
        else if (OperatingSystem.IsWindows())
        {
            if (!forward && WindowsRenderingModeIdx == 0)
            {
                WindowsRenderingModeIdx = Enum.GetValues<WindowsRenderingMode>().Length - 1;
            }
            else
            {
                WindowsRenderingModeIdx = (WindowsRenderingModeIdx +
                                           (forward ? 1 : -1)) % Enum.GetValues<WindowsRenderingMode>().Length;
            }

            WrapperSettings.WindowsRenderingMode = (WindowsRenderingMode)WindowsRenderingModeIdx;
        }
    }

    private void ToggleBorderSettings()
    {
        if (WrapperSettings.BordersEnabled)
        {
            StopBorder();
            BordersSettingDesc = Strings.SettingSwitchDisabled;
        }
        else
        {
            StartBorder();
            BordersSettingDesc = Strings.SettingSwitchEnabled;
        }

        WrapperSettings.BordersEnabled = !WrapperSettings.BordersEnabled;
    }

    public void ChangeAudioDevice(bool forward)
    {
        List<WrapperAudioDevice> deviceList = [.. _audioBackend.GetDeviceList()];
        if (deviceList.Count <= 1)
            return;

        int devIndex = deviceList.FindIndex(d => d.InternalName == WrapperSettings.AudioDevice);
        devIndex = (devIndex + (forward ? 1 : -1)) % deviceList.Count;
        devIndex = devIndex < 0 ? deviceList.Count - 1 : devIndex;

        bool changeSuccess = false;
        if (forward)
        {
            for (int i = devIndex; i < deviceList.Count;)
            {
                try
                {
                    _audioBackend.SetDevice(deviceList[i].InternalName);
                    changeSuccess = true;
                    break;
                }
                catch
                {
                    devIndex = ++i;
                }
            }
        }
        else
        {
            for (int i = devIndex; i >= 0;)
            {
                try
                {
                    _audioBackend.SetDevice(deviceList[i].InternalName);
                    changeSuccess = true;
                    break;
                }
                catch
                {
                    devIndex = --i;
                }
            }
        }

        if (changeSuccess)
        {
            WrapperSettings.AudioDevice = deviceList[devIndex].InternalName;
            AudioDeviceSettingsDesc = deviceList[devIndex].DisplayName;
        }
    }

    private void ToggleScreenReader()
    {
        if (WrapperSettings.ScreenReaderEnabled)
        {
            StopScreenReader();
            ScreenReaderSettingDesc = Strings.SettingSwitchOff;
        }
        else
        {
            StartScreenReader();
            ScreenReaderSettingDesc = Strings.SettingSwitchOn;
        }

        WrapperSettings.ScreenReaderEnabled = !WrapperSettings.ScreenReaderEnabled;
    }

    private void ToggleVirtualButtonsLayout()
    {
        WrapperSettings.VirtualButtonFullLayout = !WrapperSettings.VirtualButtonFullLayout;
        VirtualButtonsLayoutSettingDesc = WrapperSettings.VirtualButtonFullLayout
            ? Strings.SettingsVirtualButtonLayoutFull
            : Strings.SettingsVirtualButtonLayoutRecommended;
        IsFullLayout = WrapperSettings.VirtualButtonFullLayout;
    }

    private void ToggleControlPadHaptics()
    {
        VirtualButtonsHapticsSettingDesc = WrapperSettings.VirtualButtonHaptics
            ? Strings.SettingSwitchOff
            : Strings.SettingSwitchOn;
        WrapperSettings.VirtualButtonHaptics = !WrapperSettings.VirtualButtonHaptics;
    }

    public void ChangeEmulatedScreenLayout()
    {
        WrapperSettings.CurrentScreenLayout = (ScreenLayout)TargetScreenLayoutIdx;
        switch (WrapperSettings.CurrentScreenLayout)
        {
            default:
            case ScreenLayout.TOP_BOTTOM:
                CurrentFrame = new(CurrentFrame?.Frame ?? [], 256, 384);
                EmuRenderRatio = 256.0 / 384.0;
                break;

            case ScreenLayout.LEFT_RIGHT:
            case ScreenLayout.RIGHT_LEFT:
                CurrentFrame = new(CurrentFrame?.Frame ?? [], 512, 192);
                EmuRenderRatio = 512.0 / 192.0;
                break;
        }

        ResizeEmuScreen(Top?.Bounds.Width ?? 0, Top?.Bounds.Height ?? 0);
    }

    public void SendLayoutChangeToCore(int numPresses)
    {
        int currentPress = 0;
        bool pressed = false;
        _inputSwitcher.SetInputDelegate(new(nameof(ChangeScreenLayout), (_, _, _, id) =>
        {
            if (currentPress == numPresses)
            {
                _inputSwitcher.ResetInputDelegate();
                return 0;
            }

            switch (id)
            {
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_R3:
                {
                    if (!pressed)
                    {
                        pressed = true;
                        return 1;
                    }
                    else
                    {
                        pressed = false;
                        currentPress++;
                    }

                    break;
                }
            }

            return 0;
        }));
    }

    public void ResizeEmuScreen(double width, double height)
    {
        EmuRenderWidth = Math.Min(width, height * EmuRenderRatio);
        EmuRenderHeight = Math.Min(height, 1 / (EmuRenderRatio / width));
    }

    public void HandleKey<T>(T input, bool pressed)
    {
        for (int i = 0; i < _inputDrivers.Count; i++)
        {
            if (_inputDrivers[i] is DefaultInputDriver)
            {
                CurrentInputDriver = i;
                break;
            }
        }

        if (_inputDrivers[CurrentInputDriver] is not DefaultInputDriver)
        {
            return;
        }

        if (pressed)
        {
            _inputDrivers[CurrentInputDriver].Push(input);
        }
        else
        {
            _inputDrivers[CurrentInputDriver].Release(input);
        }
    }

    public void HandlePointer(Visual relativeTo,
        PointerPressedEventArgs? pressedArgs = null,
        PointerReleasedEventArgs? releasedArgs = null,
        PointerEventArgs? movedArgs = null)
    {
        if (pressedArgs is not null)
        {
            Point pos = pressedArgs.GetCurrentPoint(relativeTo).Position;
            _pointerState?.Press(pos.X, pos.Y, WrapperSettings.CurrentScreenLayout);
        }
        else if (releasedArgs is not null)
        {
            _pointerState?.Release();
        }
        else if (movedArgs is not null && (_pointerState?.Pressed ?? false))
        {
            Point pos = movedArgs.GetCurrentPoint(relativeTo).Position;
            _pointerState.Press(pos.X, pos.Y, WrapperSettings.CurrentScreenLayout);
        }
    }

    private void Run()
    {
        TimeSpan interval = TimeSpan.FromSeconds(1 / Wrapper.Fps);
        DateTime nextTick = DateTime.Now + interval;
        _updater = ((App)Application.Current!).Updater;

        while (!Closing)
        {
            int nextInputDriver = _updater?.Update() ?? -1;
            if (nextInputDriver >= 0)
            {
                if (nextInputDriver != CurrentInputDriver)
                {
                    _inputDrivers[nextInputDriver].RequestInputUpdate = true;
                }

                CurrentInputDriver = nextInputDriver;
            }

            if (!_pauseDriver.IsPaused())
            {
                Wrapper.Run();

                while (DateTime.Now < nextTick)
                {
                    TimeSpan sleep = nextTick - DateTime.Now;
                    Thread.Sleep(sleep > TimeSpan.Zero ? sleep : TimeSpan.Zero);
                }

                nextTick += interval;
            }
            else
            {
                if (_inputDrivers[CurrentInputDriver] is not DefaultInputDriver && _acceptingMenuInput)
                {
                    int currentInputDriver = CurrentInputDriver;
                    if (_inputDrivers[CurrentInputDriver].QueryInput(RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN))
                    {
                        Dispatcher.UIThread.Post(() =>
                            TopLevel.GetTopLevel(Top)?.RaiseEvent(new KeyEventArgs
                            {
                                RoutedEvent = InputElement.KeyDownEvent,
                                Key = Key.Down,
                                Source = TopLevel.GetTopLevel(Top),
                            }));
                        _acceptingMenuInput = false;
                        _menuInputTimer.Stop();
                        _menuInputTimer.Start();
                    }
                    else if (_inputDrivers[CurrentInputDriver].QueryInput(RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP))
                    {
                        Dispatcher.UIThread.Post(() =>
                            TopLevel.GetTopLevel(Top)?.RaiseEvent(new KeyEventArgs
                            {
                                RoutedEvent = InputElement.KeyDownEvent,
                                Key = Key.Up,
                                Source = TopLevel.GetTopLevel(Top),
                            }));
                        _acceptingMenuInput = false;
                        _menuInputTimer.Stop();
                        _menuInputTimer.Start();
                    }
                    else if (_inputDrivers[CurrentInputDriver].QueryInput(RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT))
                    {
                        Dispatcher.UIThread.Post(() =>
                            TopLevel.GetTopLevel(Top)?.RaiseEvent(new KeyEventArgs
                            {
                                RoutedEvent = InputElement.KeyDownEvent,
                                Key = Key.Left,
                                Source = TopLevel.GetTopLevel(Top),
                            }));
                        _acceptingMenuInput = false;
                        _menuInputTimer.Stop();
                        _menuInputTimer.Start();
                    }
                    else if (_inputDrivers[CurrentInputDriver].QueryInput(RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT))
                    {
                        Dispatcher.UIThread.Post(() =>
                            TopLevel.GetTopLevel(Top)?.RaiseEvent(new KeyEventArgs
                            {
                                RoutedEvent = InputElement.KeyDownEvent,
                                Key = Key.Right,
                                Source = TopLevel.GetTopLevel(Top),
                            }));
                        _acceptingMenuInput = false;
                        _menuInputTimer.Stop();
                        _menuInputTimer.Start();
                    }
                    else if (_inputDrivers[CurrentInputDriver].QueryInput(RetroBindings.RETRO_DEVICE_ID_JOYPAD_A))
                    {
                        IInputElement? focused = TopLevel.GetTopLevel(Top)?.FocusManager.GetFocusedElement();
                        Dispatcher.UIThread.Post(() =>
                        {
                            focused?.RaiseEvent(new KeyEventArgs
                            {
                                RoutedEvent = InputElement.KeyDownEvent,
                                Key = Key.Enter,
                                Source = focused,
                            });
                            focused?.RaiseEvent(new KeyEventArgs
                            {
                                RoutedEvent = InputElement.KeyUpEvent,
                                Key = Key.Enter,
                                Source = focused,
                            });
                        });
                        Dispatcher.UIThread.Post(() => TopLevel.GetTopLevel(Top)?.FocusManager.Focus(focused));
                        _acceptingMenuInput = false;
                        _menuInputTimer.Stop();
                        _menuInputTimer.Start();
                    }
                    else if (_inputDrivers[CurrentInputDriver].QueryInput(RetroBindings.RETRO_DEVICE_ID_JOYPAD_B))
                    {
                        if (DisplaySettingsMenuOpen || ControllerSettingsMenuOpen || AccessibilitySettingsMenuOpen ||
                            LegalMenuOpen)
                            CloseSubMenu();
                        else
                            ToggleMenuOverlay();
                        _acceptingMenuInput = false;
                        _menuInputTimer.Stop();
                        _menuInputTimer.Start();
                    }

                    // Sending a key press changes the input driver, so we have to reset it manually
                    CurrentInputDriver = currentInputDriver;
                }

                nextTick = DateTime.Now + interval;
            }
        }

        foreach (IInputDriver inputDriver in _inputDrivers)
        {
            inputDriver.Shutdown();
        }

        _updater?.Die();
        Wrapper.Dispose();
        _logInterpreter?.Dispose();
    }

    private void DisplayFrame(byte[] frame, uint width, uint height)
    {
        CurrentFrame ??= new(frame, width, height);
        CurrentFrame.SetFrame(frame);
    }

    private void PlaySample(byte[] sample)
    {
        _audioBackend.PlaySamples(sample);
    }

    private short HandleInput(uint port, uint device, uint index, uint id)
    {
        if (device == RetroBindings.RETRO_DEVICE_JOYPAD)
        {
            // Press the debug button when we've changed controller bindings
            if (id == RetroBindings.RETRO_DEVICE_ID_JOYPAD_L3 && _inputDrivers[CurrentInputDriver].RequestInputUpdate)
            {
                _inputDrivers[CurrentInputDriver].RequestInputUpdate = false;
                return 1;
            }

            return _inputDrivers[CurrentInputDriver].QueryInput(id) ? (short)1 : (short)0;
        }

        if (device == RetroBindings.RETRO_DEVICE_POINTER)
        {
            switch (id)
            {
                case RetroBindings.RETRO_DEVICE_ID_POINTER_X:
                    return _pointerState?.RetroX ?? 0;

                case RetroBindings.RETRO_DEVICE_ID_POINTER_Y:
                    return _pointerState?.RetroY ?? 0;

                case RetroBindings.RETRO_DEVICE_ID_POINTER_PRESSED:
                    return (_pointerState?.Pressed ?? false) ? (short)1 : (short)0;
            }
        }

        return 0;
    }

    private short HandleStartupInput(uint port, uint device, uint index, uint id)
    {
        if ((_logInterpreter?.StartupReceived ?? false) && device == RetroBindings.RETRO_DEVICE_JOYPAD)
        {
            switch (id)
            {
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP:
                    if (_langInputsSent < 4)
                    {
                        _langInputsSent++;
                    }
                    else
                    {
                        return 0;
                    }

                    return (WrapperSettings.LanguageIndex & (0x1 << (6 - _currentLangBit))) != 0 ? (short)1 : (short)0;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN:
                    if (_langInputsSent < 4)
                    {
                        _langInputsSent++;
                    }
                    else
                    {
                        return 0;
                    }

                    return (WrapperSettings.LanguageIndex & (0x1 << (6 - _currentLangBit))) == 0 ? (short)1 : (short)0;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT:
                    if (_langInputsSent is >= 4 and < 6)
                    {
                        _langInputsSent++;
                        return 1;
                    }

                    if (_langInputsSent >= 6)
                    {
                        _langInputsSent = 0;
                        _currentLangBit++;
                    }

                    return 0;
            }
        }

        if (_logInterpreter?.LangReceived ?? false)
        {
            _inputSwitcher.ResetInputDelegate();
            return 0;
        }

        if (device == RetroBindings.RETRO_DEVICE_JOYPAD)
        {
            if (id is RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP or RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN)
            {
                return 1;
            }
        }

        return 0;
    }

    private bool DoRumble(uint port, uint type, ushort strength)
    {
        _inputDrivers[CurrentInputDriver].DoRumble(strength);
        return true;
    }

    private void HandleLog(string line)
    {
        _logInterpreter?.InterpretLog(line);
    }

    private void StartScreenReader()
    {
        _logInterpreter!.ScreenReader = ((App)Application.Current!).ScreenReader;
    }

    private void StopScreenReader()
    {
        _logInterpreter!.ScreenReader = null;
    }

    private void StartBorder()
    {
        SetBorder();
        _borderTimer = new(TimeSpan.FromMilliseconds(100)) { AutoReset = true };
        _borderTimer.Elapsed += (_, _) =>
        {
            _currentBorderFrame = (_currentBorderFrame + 1) % 600;
            SetBorder();
        };
        _borderTimer.Start();
    }

    private void StopBorder()
    {
        CurrentBorder = null;
        _borderTimer?.Stop();
    }

    private void SetBorder()
    {
        if (string.IsNullOrEmpty(_currentBorder))
            return;
        
        using Stream borderStream =
            AssetLoader.Open(
                new($"avares://ITDSWrapper/Assets/Borders/{_currentBorder}/{_currentBorderFrame + 1:0000}.jpg"));
        CurrentBorder = new(borderStream);

        if (NextBorder is not null)
        {
            SetNextBorder();
            FadeBorder();
        }
    }

    private void SetNextBorder()
    {
        if (string.IsNullOrEmpty(_nextBorder))
            return;
        
        using Stream borderStream =
            AssetLoader.Open(new($"avares://ITDSWrapper/Assets/Borders/{_nextBorder}/{_nextBorderFrame + 1:0000}.jpg"));
        NextBorder = new(borderStream);
    }

    private void FadeBorder()
    {
        if (CurrentBorderOpacity < 0.02)
        {
            CurrentBorder = NextBorder;
            NextBorder = null;
            CurrentBorderOpacity = 1.0;
            NextBorderOpacity = 0.0;
            _currentBorder = _nextBorder;
            _currentBorderFrame = _nextBorderFrame;
            _nextBorder = string.Empty;
            _nextBorderFrame = 0;
            return;
        }

        CurrentBorderOpacity -= 0.02;
        NextBorderOpacity += 0.02;
    }

    private void AssignVirtualBindings()
    {
        int defaultInputDriverIndex = 0;
        foreach (uint inputKey in _inputDrivers[defaultInputDriverIndex].GetInputKeys())
        {
            VirtualButtonInput? button = new();
            switch (inputKey)
            {
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_A:
                    AButton = new("A", button, 50, 50, _hapticsBackend, WrapperSettings);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_B:
                    BButton = new("B", button, 50, 50, _hapticsBackend, WrapperSettings);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_X:
                    XButton = new("X", button, 50, 50, _hapticsBackend, WrapperSettings);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y:
                    YButton = new("Y", button, 50, 50, _hapticsBackend, WrapperSettings);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_L:
                    LButton = new("L", button, 67, 40, _hapticsBackend, WrapperSettings);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_R:
                    RButton = new("R", button, 67, 40, _hapticsBackend, WrapperSettings);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP:
                    Dpad?.Up = new("UP", button, 0, 0, _hapticsBackend);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT:
                    Dpad?.Right = new("RIGHT", button, 0, 0, _hapticsBackend);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN:
                    Dpad?.Down = new("DOWN", button, 0, 0, _hapticsBackend);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT:
                    Dpad?.Left = new("LEFT", button, 0, 0, _hapticsBackend);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_START:
                    StartButton = new("START", button, 65, 40, _hapticsBackend, WrapperSettings);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT:
                    SelectButton = new("SELECT", button, 65, 40, _hapticsBackend, WrapperSettings);
                    break;
                case RetroBindings.RETRO_DEVICE_ID_JOYPAD_R2:
                    button.SpecialAction = ToggleMenuOverlay;
                    MenuButton = new("MENU", button, 65, 40, _hapticsBackend, WrapperSettings);
                    break;
                default:
                    button = null;
                    break;
            }

            _inputDrivers[defaultInputDriverIndex].SetBinding(inputKey, button);
        }
    }
}