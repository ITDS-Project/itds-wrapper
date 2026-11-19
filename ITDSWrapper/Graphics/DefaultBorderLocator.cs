using System;
using System.IO;

namespace ITDSWrapper.Graphics;

public class DefaultBorderLocator : IBorderLocator
{
    public string GetBorderPath(string name)
    {
        return Path.Combine(
            OperatingSystem.IsMacOS()
                ? Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.FullName, "MonoBundle")
                : AppDomain.CurrentDomain.BaseDirectory, "Borders", $"{name}.mp4");
    }
}