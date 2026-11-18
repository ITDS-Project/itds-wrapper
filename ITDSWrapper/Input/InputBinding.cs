using Avalonia.Input;
using ITDSWrapper.Assets;

namespace ITDSWrapper.Input;

public class InputBinding
{
    public uint Action { get; set; }
    public ControllerButton? ControllerButton { get; set; }
    public PhysicalKey? KeyboardKey { get; set; }
    
    public string ActionDescription => InputDescriptions.Descriptions[Action];
    public string KeyDescription => KeyboardKey?.ToString() ?? Strings.BindingNotMapped;
    public string ButtonDescription => ControllerButton?.ToString() ?? Strings.BindingNotMapped;
}