using Avalonia.Input;
using ITDSWrapper.Assets;
using ITDSWrapper.ViewModels;
using ReactiveUI;

namespace ITDSWrapper.Input;

public class InputBinding : ViewModelBase
{
    public uint Action { get; set; }

    public ControllerBrand Controller { get; set; } = ControllerBrand.NONE;

    public ControllerButton? ControllerButton
    {
        get;
        set
        {
            field = value;
            this.RaisePropertyChanged(nameof(ButtonDescription));
        }
    }

    public PhysicalKey? KeyboardKey
    {
        get;
        set
        {
            field = value;
            this.RaisePropertyChanged(nameof(KeyDescription));
        }
    }

    public string ActionDescription => InputDescriptions.Descriptions[Action];
    public string KeyDescription => KeyboardKey?.ToString() ?? Strings.BindingNotMapped;
    public string ButtonDescription => InputDescriptions.GetInputGlyph(Controller, ControllerButton ?? Input.ControllerButton.Unknown);
}