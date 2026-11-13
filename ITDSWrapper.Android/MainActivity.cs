using System;
using System.Globalization;
using System.Linq;
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Display;
using Android.OS;
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

    private AndroidControllerInputDriver? ControllerInputDriver =>
        App?.InputDrivers?.OfType<AndroidControllerInputDriver>().FirstOrDefault();

    private AndroidUpdater? Updater => App?.Updater as AndroidUpdater;
    private AndroidDualScreenBackend? DualScreenBackend => App?.DualScreenBackend as AndroidDualScreenBackend;

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        if (DualScreenBackend?.IsActive != true)
        {
            PauseDriver?.PushPauseState(!hasFocus);
        }

        base.OnWindowFocusChanged(hasFocus);
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        if (e?.DeviceId != ControllerInputDriver?.Controller?.Id)
        {
            return base.OnKeyDown(keyCode, e);
        }

        ControllerInputDriver?.Push(new AndroidInputContainer(AndroidInputType.KEY, keyCode, null));
        if (Updater is { } updater)
            updater.RetValue = 1;
        return true;
    }

    public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
    {
        if (e?.DeviceId != ControllerInputDriver?.Controller?.Id)
        {
            return base.OnKeyUp(keyCode, e);
        }

        ControllerInputDriver?.Release(new AndroidInputContainer(AndroidInputType.KEY, keyCode, null));
        return true;
    }

    public override View? OnCreateView(View? parent, string name, Context context, IAttributeSet attrs)
    {
        DisplayManager? displayManager = (DisplayManager?)context.GetSystemService(DisplayService);

        if (displayManager?.GetDisplays(DisplayManager.DisplayCategoryPresentation)?.Length > 0)
        {
            DualScreenBackend?.Initialize(context,
                displayManager.GetDisplays(DisplayManager.DisplayCategoryPresentation)![0]!);
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

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

        if (savedInstanceState is not null)
            return;

        AccountManager? manager = AccountManager.Get(this);
        Account? existingAccount = manager?.GetAccountsByType("com.google").FirstOrDefault();

        if (existingAccount is not null)
        {
            if (App?.LogInterpreter is { } logInterpreter)
                logInterpreter.Username = existingAccount.Name;

            return;
        }

        Intent? intent = AccountManager.NewChooseAccountIntent(null, null, ["com.google"],
            "Choose an account for playtest logs", null, null, null);

        StartActivityForResult(intent, 1001);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode != 1001 || resultCode != Result.Ok || data is null)
        {
            return;
        }

        string? accountName = data.GetStringExtra(AccountManager.KeyAccountName);
        string? accountType = data.GetStringExtra(AccountManager.KeyAccountType);

        if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(accountType))
        {
            return;
        }

        Account? account = AccountManager.Get(this)?.GetAccountsByType(accountType)
            .FirstOrDefault(a => a.Name == accountName);

        if (account is null)
            return;

        if (App?.LogInterpreter is { } logInterpreter)
            logInterpreter.Username = account.Name;
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
                ((App)b.Instance!).AudioBackend = new AndroidAudioBackend();
                ((App)b.Instance).PauseDriver = new();
                ((App)b.Instance).HapticsBackend = new AndroidHapticsBackend();
                ((App)b.Instance).BatteryMonitor = new AndroidBatteryMonitor();
                ((App)b.Instance).LogInterpreter = new();
                ((App)b.Instance).ScreenReader = new AndroidScreenReader(this,
                    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
                    {
                        _ => "en-GB",
                    });
                ((App)b.Instance).InputDrivers = [controllerInputDriver];
                ((App)b.Instance).Updater = updater;
                ((App)b.Instance).DualScreenBackend = new AndroidDualScreenBackend();
            });
    }
}