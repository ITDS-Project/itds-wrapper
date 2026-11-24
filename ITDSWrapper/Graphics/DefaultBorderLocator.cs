using System;
using System.IO;

namespace ITDSWrapper.Graphics;

public class DefaultBorderLocator : IBorderLocator
{
    public string GetBorderPath(string name, BorderQuality quality)
    {
        return Path.Combine(
            OperatingSystem.IsMacOS()
                ? Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.FullName, "MonoBundle")
                : AppDomain.CurrentDomain.BaseDirectory, "Borders", $"{name}_{quality.ToString()[1..]}.mp4");
    }
}