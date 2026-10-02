using System;
using System.Globalization;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;
using ITDSWrapper.Core;

namespace ITDSWrapper.Android;

[Activity(
    Label = "Into the Dream Spring",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private App? App => Avalonia.Application.Current as App;
    private PauseDriver? PauseDriver => App?.PauseDriver;
    private AndroidHapticsBackend? HapticsBackend => App?.HapticsBackend as AndroidHapticsBackend;
    private AndroidControllerInputDriver? ControllerInputDriver => App?.InputDrivers?[0] as AndroidControllerInputDriver;
    private AndroidUpdater? Updater => App?.Updater as AndroidUpdater;

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        PauseDriver?.PushPauseState(!hasFocus);
        base.OnWindowFocusChanged(hasFocus);
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        if (e?.DeviceId != ControllerInputDriver?.Controller?.Id)
        {
            return base.OnKeyDown(keyCode, e);
        }
        ControllerInputDriver?.Push(new AndroidInputContainer(AndroidInputType.KEY, keyCode, null));
        if (Updater is { } updater) updater.RetValue = 1;
        return true;
    }

    public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
    {
        if (e?.DeviceId != ControllerInputDriver?.Controller?.Id)
        {
            return base.OnKeyDown(keyCode, e);
        }
        ControllerInputDriver?.Release(new AndroidInputContainer(AndroidInputType.KEY, keyCode, null));
        return true;
    }

    public override View? OnCreateView(View? parent, string name, Context context, IAttributeSet attrs)
    {
        if (App!.AudioBackend is null)
        {
            App.AudioBackend =  new AndroidAudioBackend(context);
        }
        
        View? view = base.OnCreateView(parent, name, context, attrs);
        AndroidHapticsBackend? hapticsBackend = HapticsBackend;
        if (hapticsBackend is not null && view is not null)
        {
            hapticsBackend.View = view;
        }
        else if (hapticsBackend is not null && parent is not null)
        {
            hapticsBackend.View = parent;
        }
        return view;
    }
}

[Application]
public class AndroidApp : AvaloniaAndroidApplication<App>
{
    protected AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        AndroidControllerInputDriver controllerInputDriver = new();
        AndroidUpdater updater = new(controllerInputDriver);

        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseReactiveUI()
            .AfterSetup(b =>
            {
                ((App)b.Instance!).PauseDriver = new();
                ((App)b.Instance).HapticsBackend = new AndroidHapticsBackend();
                ((App)b.Instance).BatteryMonitor = new AndroidBatteryMonitor();
                ((App)b.Instance).ScreenReader = new AndroidScreenReader(this,
                    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
                    {
                        _ => "en-GB",
                    });
                ((App)b.Instance).InputDrivers = [controllerInputDriver];
                ((App)b.Instance).Updater = updater;
            });
    }
}