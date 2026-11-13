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
using ReactiveUI.Fody.Helpers;

namespace ITDSWrapper.ViewModels.Controls;

public class ControllerDiagramViewModel : ViewModelBase
{
    private static readonly Dictionary<ControllerButton, Point> XboxControllerPoints = new()
    {
        { ControllerButton.A, new(1837, 555) },
        { ControllerButton.B, new(1837, 490) },
        { ControllerButton.X, new(1837, 427) },
        { ControllerButton.Y, new(1837, 363) },
        { ControllerButton.DPadUp, new(30, 688) },
        { ControllerButton.DPadDown, new(30, 880) },
        { ControllerButton.DPadLeft, new(30, 818) },
        { ControllerButton.DPadRight, new(30, 751) },
        { ControllerButton.LeftBumper, new(121, 222) },
        { ControllerButton.RightBumper, new(1744, 222) },
        { ControllerButton.LeftStick, new(156, 427) },
        { ControllerButton.RightStick, new(1468, 1008) },
        { ControllerButton.Start, new(704, 938) },
        { ControllerButton.Select, new(704, 1008) },
    };

    [Reactive] public RenderTargetBitmap? DiagramImage { get; private set; }
    [Reactive] public bool HasDiagram { get; private set; }

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
        _logInterpreter.AddRecentLog($"[WRAPPER] CONTROLLER_CHG: {InputDriver?.GetControllerBrand()} ('{InputDriver?.GetControllerRawName()}')");
        Dispatcher.UIThread.Post(UpdateDiagram);
    }

    private void UpdateDiagram()
    {
        string? assetUri = InputDriver?.GetControllerBrand() switch
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
                foreach ((ControllerButton b, Point p) in XboxControllerPoints)
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