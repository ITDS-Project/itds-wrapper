using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ITDSWrapper.ViewModels;

namespace ITDSWrapper.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void MainView_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (!MainViewModel.IsMobile || ((MainViewModel)DataContext!).DualScreenBackend?.IsActive == true)
            return;

        int currentLayout = ((MainViewModel)DataContext!).TargetScreenLayoutIdx;
        ((MainViewModel)DataContext).TargetScreenLayoutIdx = e.NewSize.Width > e.NewSize.Height ? 1 : 0;
        ((MainViewModel)DataContext).ChangeEmulatedScreenLayout();
        int numPresses = ((MainViewModel)DataContext!).TargetScreenLayoutIdx - currentLayout;
        numPresses = numPresses < 0 ? Enum.GetValues<Core.ScreenLayout>().Length + numPresses : numPresses;
        ((MainViewModel)DataContext).SendLayoutChangeToCore(numPresses);
    }

    private void ScreenGrid_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ((MainViewModel)DataContext!).ResizeEmuScreen(e.NewSize.Width, e.NewSize.Height);

        var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
        insetsManager?.DisplayEdgeToEdgePreference = true;
        insetsManager?.IsSystemBarVisible = false;
        ((MainViewModel) DataContext!).TopPadding = (int?)insetsManager?.SafeAreaPadding.Top ?? 0;
        ((MainViewModel) DataContext).BottomPadding = (int?)insetsManager?.SafeAreaPadding.Bottom ?? 0;
    }

    private void DsScreen_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ((MainViewModel)DataContext!).HandlePointer(DsScreen, pressedArgs: e);
    }

    private void DsScreen_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        ((MainViewModel)DataContext!).HandlePointer(DsScreen, releasedArgs: e);
    }

    private void DsScreen_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        ((MainViewModel)DataContext!).HandlePointer(DsScreen, movedArgs: e);
    }

    private void MainScreen_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Point pos = e.GetPosition(sender as Control);
        // If we touch anywhere except the bottom screen, reveal the virtual controls
        if (MainViewModel.IsMobile && (pos.X < DsScreen.Bounds.Left || pos.Y < DsScreen.Bounds.Top + DsScreen.Bounds.Height / 2 ||
                                       pos.X > DsScreen.Bounds.Right || pos.Y > DsScreen.Bounds.Bottom))
        {
            ((MainViewModel)DataContext!).CurrentInputDriver = 0;
        }
    }
}
