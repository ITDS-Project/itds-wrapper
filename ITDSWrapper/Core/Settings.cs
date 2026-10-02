using System.IO;
using System.Text.Json;
using DynamicData;

namespace ITDSWrapper.Core;

public class Settings
{
    public bool FirstLaunch { get; set; } = true;
    public bool BordersEnabled { get; set; } = true;
    public bool ScreenReaderEnabled { get; set; } = false;
    public bool VirtualButtonHaptics { get; set; } = true;
    public bool VirtualButtonFullLayout { get; set; } = false;
    public string LanguageCode { get; set; } = "en";

    private static string[] _langCodeArray = ["en", "ja"];
    public byte LanguageIndex => (byte)_langCodeArray.IndexOf(LanguageCode);
    
    public WindowingMode WindowingMode { get; set; } = WindowingMode.FULL_SCREEN;
    public ScreenLayout CurrentScreenLayout { get; set; } = ScreenLayout.TOP_BOTTOM;
    public WindowsRenderingMode WindowsRenderingMode { get; set; } = WindowsRenderingMode.WINDOWS_GL;
    public MacOsRenderingMode MacOsRenderingMode { get; set; } = MacOsRenderingMode.METAL;
    public LinuxRenderingMode LinuxRenderingMode { get; set; } = LinuxRenderingMode.GLX;

    public string AudioDevice { get; set; } = "default";

    public void Save(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        File.WriteAllText(Path.Combine(path, "settings.json"), JsonSerializer.Serialize(this));
    }

    public static Settings Load(string? path)
    {
        return !string.IsNullOrEmpty(path) && Path.Exists(Path.Combine(path, "settings.json"))
            ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(path, "settings.json"))) ?? new()
            : new();
    }
}

public enum WindowingMode
{
    FULL_SCREEN,
    BORDERLESS,
    WINDOWED,
}

public enum ScreenLayout
{
    TOP_BOTTOM,
    LEFT_RIGHT,
    RIGHT_LEFT,
}

public enum WindowsRenderingMode
{
    WINDOWS_GL,
    VULKAN,
    ANGLE_EGL,
    SOFTWARE,
}

public enum MacOsRenderingMode
{
    METAL,
    OPENGL,
    SOFTWARE,
}

public enum LinuxRenderingMode
{
    GLX,
    EGL,
    VULKAN,
    SOFTWARE,
}