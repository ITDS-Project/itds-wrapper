using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ITDSWrapper.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Silk.NET.Input;

namespace ITDSWrapper.ViewModels.Controls;

public class ControllerDiagramViewModel : ViewModelBase
{

    private static Dictionary<ButtonName, Point> XboxControllerPoints = new()
    {
        {ButtonName.A, new Point(1837, 555)},
        {ButtonName.B, new Point(1837, 490)},
        {ButtonName.X, new Point(1837, 427)},
        {ButtonName.Y, new Point(1837, 363)},
        {ButtonName.DPadUp, new Point(30, 688)},
        {ButtonName.DPadDown, new Point(30, 880)},
        {ButtonName.DPadLeft, new Point(30, 818)},
        {ButtonName.DPadRight, new Point(30, 751)},
        {ButtonName.LeftBumper, new Point(121, 222)},
        {ButtonName.RightBumper, new Point(1744, 222)},
        {ButtonName.LeftStick, new Point(156, 427)},
        {ButtonName.RightStick, new Point(1468, 1008)},
        {ButtonName.Start, new Point(704, 938)},
        {ButtonName.Back, new Point(704, 1008)}
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

    public ControllerDiagramViewModel(IInputDriver inputDriver)
    {
        InputDriver = inputDriver;
    }

    private void OnControllerChanged()
    {
        Dispatcher.UIThread.Post(UpdateDiagram);
    }

    private void UpdateDiagram()
    {
        var assetUri = InputDriver?.GetControllerBrand() switch
        {
            IInputDriver.ControllerBrand.GENERIC or IInputDriver.ControllerBrand.XBOX =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/Xbox.png",
            IInputDriver.ControllerBrand.PLAYSTATION =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/PlayStation.png",
            IInputDriver.ControllerBrand.NINTENDO_SWITCH =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/SwitchPro.png",
            IInputDriver.ControllerBrand.STEAM_DECK =>
                "avares://ITDSWrapper/Assets/ControllerDiagrams/SteamDeck.png",
            _ => null
        };

        if (assetUri is not null)
        {
            var diagram = new Bitmap(AssetLoader.Open(new Uri(assetUri)));
            DiagramImage = new RenderTargetBitmap(diagram.PixelSize, new Vector(96, 96));
            using (var context = DiagramImage.CreateDrawingContext())
            {
                var rect = new Rect(0, 0, diagram.PixelSize.Width, diagram.PixelSize.Height);
                context.DrawImage(diagram, rect);
                
                var font = new Typeface(new FontFamily("avares://ITDSWrapper/Assets/Fonts#Gupter"));
                foreach (var (b, p) in XboxControllerPoints)
                {
                    // var binding = InputDriver.GetLocalisedBinding(b); - todo get what is bound here
                    var formattedText = new FormattedText(
                        "Lorem ipsum tibi",
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