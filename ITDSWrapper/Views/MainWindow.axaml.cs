using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ITDSWrapper.ViewModels;

namespace ITDSWrapper.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        ((MainViewModel)DataContext!).SetupWindowing();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ((App)Application.Current!).DesktopTopLevelOpened?.Invoke(this);
        ((MainViewModel)DataContext!).ChangeEmulatedScreenLayout();
        ((MainViewModel)DataContext).SendLayoutChangeToCore(((MainViewModel)DataContext).TargetScreenLayoutIdx);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ((MainViewModel)DataContext!).Closing = true;
        base.OnClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        ((MainViewModel)DataContext!).HandleKey(e.PhysicalKey, true);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        ((MainViewModel)DataContext!).HandleKey(e.PhysicalKey, false);
    }
}