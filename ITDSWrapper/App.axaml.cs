using System.Collections.Generic;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ITDSWrapper.Accessibility;
using ITDSWrapper.Audio;
using ITDSWrapper.Core;
using ITDSWrapper.Graphics;
using ITDSWrapper.Haptics;
using ITDSWrapper.Input;
using ITDSWrapper.ViewModels;
using ITDSWrapper.Views;

namespace ITDSWrapper;

public partial class App : Application
{
    public IAudioBackend? AudioBackend { get; set; }
    public List<IInputDriver>? InputDrivers { get; set; }
    public IHapticsBackend? HapticsBackend { get; set; }
    public IUpdater? Updater { get; set; }
    public LogInterpreter? LogInterpreter { get; set; }
    public PauseDriver? PauseDriver { get; set; }
    public IBatteryMonitor? BatteryMonitor { get; set; }
    public IScreenReader? ScreenReader { get; set; }
    public InputSwitcher? InputSwitcher { get; set; }
    public Action<TopLevel>? DesktopTopLevelOpened { get; set; }
    public IDualScreenBackend? DualScreenBackend { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        MainViewModel vm = new();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
            vm.Top = desktop.MainWindow;
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activityLifetime)
        {
            activityLifetime.MainViewFactory = () =>
            {
                MainView view = new() { DataContext = vm };
                vm.Top = view;
                return view;
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = vm,
            };
            vm.Top = singleViewPlatform.MainView;
        }

        base.OnFrameworkInitializationCompleted();
    }
}