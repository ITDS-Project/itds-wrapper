using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ITDSWrapper.Assets;
using ITDSWrapper.Core;
using ITDSWrapper.Input;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ITDSWrapper.ViewModels.Controls;

public partial class ControllerDiagramViewModel : ViewModelBase
{
    private static readonly Dictionary<ControllerButton, Point> XboxControllerPoints = new()
    {
        { ControllerButton.Y, new(1840, 345) },
        { ControllerButton.X, new(1840, 410) },
        { ControllerButton.B, new(1840, 475) },
        { ControllerButton.A, new(1840, 540) },
        { ControllerButton.DPadUp, new(65, 665) },
        { ControllerButton.DPadLeft, new(65, 730) },
        { ControllerButton.DPadRight, new(65, 795) },
        { ControllerButton.DPadDown, new(65, 860) },
        { ControllerButton.LeftTrigger, new(190, 130) },
        { ControllerButton.LeftBumper, new(190, 200) },
        { ControllerButton.LeftStickClick, new(190, 310) },
        { ControllerButton.LeftStickUp, new(190, 370) },
        { ControllerButton.LeftStickRight, new(190, 430) },
        { ControllerButton.LeftStickLeft, new(190, 490) },
        { ControllerButton.LeftStickDown, new(190, 550) },
        { ControllerButton.RightTrigger, new(1745, 130) },
        { ControllerButton.RightBumper, new(1745, 200) },
        { ControllerButton.RightStickClick, new(1745, 655) },
        { ControllerButton.RightStickUp, new(1745, 715) },
        { ControllerButton.RightStickRight, new(1745, 775) },
        { ControllerButton.RightStickLeft, new(1745, 835) },
        { ControllerButton.RightStickDown, new(1745, 895) },
        { ControllerButton.Start, new(700, 920) },
        { ControllerButton.Select, new(700, 1000) },
    };

    private static readonly Dictionary<ControllerButton, Point> AynThorControllerPoints = new()
    {
        { ControllerButton.X, new(1825, 260) },
        { ControllerButton.Y, new(1825, 335) },
        { ControllerButton.A, new(1825, 410) },
        { ControllerButton.B, new(1825, 485) },
        { ControllerButton.DPadUp, new(80, 665) },
        { ControllerButton.DPadLeft, new(80, 740) },
        { ControllerButton.DPadRight, new(80, 805) },
        { ControllerButton.DPadDown, new(80, 870) },
        { ControllerButton.LeftTrigger, new(80, 95) },
        { ControllerButton.LeftBumper, new(80, 100) },
        { ControllerButton.LeftStickClick, new(80, 255) },
        { ControllerButton.LeftStickUp, new(80, 330) },
        { ControllerButton.LeftStickRight, new(80, 400) },
        { ControllerButton.LeftStickLeft, new(80, 470) },
        { ControllerButton.LeftStickDown, new(80, 540) },
        { ControllerButton.RightTrigger, new(1825, 95) },
        { ControllerButton.RightBumper, new(1825, 100) },
        { ControllerButton.RightStickClick, new(1825, 595) },
        { ControllerButton.RightStickUp, new(1825, 670) },
        { ControllerButton.RightStickRight, new(1825, 745) },
        { ControllerButton.RightStickLeft, new(1825, 820) },
        { ControllerButton.RightStickDown, new(1825, 895) },
        { ControllerButton.Start, new(1015, 1010) },
        { ControllerButton.Select, new(1015, 1070) },
    };

    private static readonly Dictionary<ControllerButton, Point> PlayStationControllerPoints = new()
    {
        { ControllerButton.Y, new(1815, 380) },
        { ControllerButton.X, new(1815, 445) },
        { ControllerButton.B, new(1815, 510) },
        { ControllerButton.A, new(1815, 575) },
        { ControllerButton.DPadUp, new(75, 380) },
        { ControllerButton.DPadRight, new(75, 445) },
        { ControllerButton.DPadLeft, new(75, 510) },
        { ControllerButton.DPadDown, new(75, 575) },
        { ControllerButton.LeftTrigger, new(180, 145) },
        { ControllerButton.LeftBumper, new(180, 215) },
        { ControllerButton.LeftStickClick, new(75, 685) },
        { ControllerButton.LeftStickUp, new(75, 755) },
        { ControllerButton.LeftStickRight, new(75, 825) },
        { ControllerButton.LeftStickLeft, new(75, 895) },
        { ControllerButton.LeftStickDown, new(75, 965) },
        { ControllerButton.RightTrigger, new(1705, 145) },
        { ControllerButton.RightBumper, new(1705, 215) },
        { ControllerButton.RightStickClick, new(1815, 685) },
        { ControllerButton.RightStickUp, new(1815, 755) },
        { ControllerButton.RightStickRight, new(1815, 825) },
        { ControllerButton.RightStickLeft, new(1815, 895) },
        { ControllerButton.RightStickDown, new(1815, 965) },
        { ControllerButton.Start, new(980, 925) },
        { ControllerButton.Select, new(980, 990) },
    };

    private static readonly Dictionary<ControllerButton, Point> SwitchProControllerPoints = new()
    {
        { ControllerButton.X, new(1815, 340) },
        { ControllerButton.Y, new(1815, 400) },
        { ControllerButton.A, new(1815, 465) },
        { ControllerButton.B, new(1815, 528) },
        { ControllerButton.DPadUp, new(85, 655) },
        { ControllerButton.DPadRight, new(85, 715) },
        { ControllerButton.DPadLeft, new(85, 775) },
        { ControllerButton.DPadDown, new(85, 835) },
        { ControllerButton.LeftTrigger, new(205, 140) },
        { ControllerButton.LeftBumper, new(205, 205) },
        { ControllerButton.LeftStickClick, new(205, 310) },
        { ControllerButton.LeftStickUp, new(205, 370) },
        { ControllerButton.LeftStickRight, new(205, 430) },
        { ControllerButton.LeftStickLeft, new(205, 490) },
        { ControllerButton.LeftStickDown, new(205, 550) },
        { ControllerButton.RightTrigger, new(1705, 140) },
        { ControllerButton.RightBumper, new(1705, 205) },
        { ControllerButton.RightStickClick, new(1715, 635) },
        { ControllerButton.RightStickUp, new(1715, 695) },
        { ControllerButton.RightStickRight, new(1715, 755) },
        { ControllerButton.RightStickLeft, new(1715, 815) },
        { ControllerButton.RightStickDown, new(1715, 875) },
        { ControllerButton.Start, new(585, 935) },
        { ControllerButton.Select, new(585, 1000) },
    };

    private static readonly Dictionary<ControllerButton, Point> SteamDeckControllerPoints = new()
    {
        { ControllerButton.Y, new(1925, 310) },
        { ControllerButton.X, new(1925, 375) },
        { ControllerButton.B, new(1925, 435) },
        { ControllerButton.A, new(1925, 495) },
        { ControllerButton.DPadUp, new(20, 310) },
        { ControllerButton.DPadRight, new(20, 375) },
        { ControllerButton.DPadLeft, new(20, 435) },
        { ControllerButton.DPadDown, new(20, 495) },
        { ControllerButton.LeftTrigger, new(20, 115) },
        { ControllerButton.LeftBumper, new(20, 180) },
        { ControllerButton.LeftStickClick, new(20, 800) },
        { ControllerButton.LeftStickUp, new(20, 870) },
        { ControllerButton.LeftStickRight, new(20, 940) },
        { ControllerButton.LeftStickLeft, new(20, 1010) },
        { ControllerButton.LeftStickDown, new(20, 1080) },
        { ControllerButton.RightTrigger, new(1925, 115) },
        { ControllerButton.RightBumper, new(1925, 180) },
        { ControllerButton.RightStickClick, new(1925, 800) },
        { ControllerButton.RightStickUp, new(1925, 870) },
        { ControllerButton.RightStickRight, new(1925, 940) },
        { ControllerButton.RightStickLeft, new(1925, 1010) },
        { ControllerButton.RightStickDown, new(1925, 1080) },
        { ControllerButton.Start, new(865, 65) },
        { ControllerButton.Select, new(865, 130) },
    };

    private static Dictionary<ControllerButton, Point> GetControllerPoints(ControllerBrand? brand) => brand switch
    {
        ControllerBrand.XBOX or ControllerBrand.GENERIC => XboxControllerPoints,
        ControllerBrand.PLAYSTATION => PlayStationControllerPoints,
        ControllerBrand.NINTENDO_SWITCH => SwitchProControllerPoints,
        ControllerBrand.STEAM_DECK => SteamDeckControllerPoints,
        ControllerBrand.AYN_THOR => AynThorControllerPoints,

        _ => new Dictionary<ControllerButton, Point>()
    };

    [Reactive] public partial RenderTargetBitmap? DiagramImage { get; private set; }
    [Reactive] public partial bool HasDiagram { get; private set; }

    public IInputDriver? InputDriver
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
                return;

            if (field is not null)
                field.ControllerChanged -= OnControllerChanged;

            this.RaiseAndSetIfChanged(ref field, value);

            if (field is not null)
                field.ControllerChanged += OnControllerChanged;

            UpdateDiagram();
        }
    }

    private readonly LogInterpreter _logInterpreter;

    public ControllerDiagramViewModel(IInputDriver inputDriver, LogInterpreter logInterpreter)
    {
        _logInterpreter = logInterpreter;
        InputDriver = inputDriver;
    }

    private void OnControllerChanged()
    {
        _logInterpreter.AddRecentLog(
            $"[WRAPPER] CONTROLLER_CHG: {InputDriver?.GetControllerBrand()} ('{InputDriver?.GetControllerRawName()}')");
        Dispatcher.UIThread.Post(UpdateDiagram);
    }

    private void UpdateDiagram()
    {
        ControllerBrand? controllerBrand = InputDriver?.GetControllerBrand();
        string? assetUri = controllerBrand switch
        {
            ControllerBrand.GENERIC or ControllerBrand.XBOX =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/Xbox.png",
            ControllerBrand.PLAYSTATION =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/PlayStation.png",
            ControllerBrand.NINTENDO_SWITCH =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/SwitchPro.png",
            ControllerBrand.STEAM_DECK =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/SteamDeck.png",
            ControllerBrand.AYN_THOR =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/AynThor.png",
            _ => null,
        };

        if (!string.IsNullOrEmpty(assetUri))
        {
            Bitmap diagram = new(AssetLoader.Open(new(assetUri)));
            DiagramImage = new(diagram.PixelSize, new(96, 96));
            using (DrawingContext context = DiagramImage.CreateDrawingContext())
            {
                Rect rect = new(0, 0, diagram.PixelSize.Width, diagram.PixelSize.Height);
                context.DrawImage(diagram, rect);

                Typeface font = new(new FontFamily("avares://ITDSWrapper/Assets/Fonts#Gupter"));
                foreach ((ControllerButton b, Point p) in GetControllerPoints(controllerBrand))
                {
                    string? binding = InputDriver?.GetLocalisedBinding(b);
                    FormattedText formattedText = new(
                        binding ?? Strings.BindingUnknown,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        font,
                        32,
                        Brushes.White
                    );
                    context.DrawText(formattedText, p);
                }
            }

            HasDiagram = true;
        }
        else
        {
            HasDiagram = false;
            DiagramImage = null;
        }
    }
}