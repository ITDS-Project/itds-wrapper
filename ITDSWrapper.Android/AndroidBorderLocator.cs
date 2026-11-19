using System.IO;
using Android.App;
using Android.Content;
using ITDSWrapper.Graphics;

namespace ITDSWrapper.Android;

public class AndroidBorderLocator : IBorderLocator
{
    public MainActivity? Activity { get; set; }
    
    public string GetBorderPath(string name)
    {
        string diskPath = Path.Combine(Application.Context.FilesDir!.AbsolutePath, $"{name}.mp4");
        if (!File.Exists(diskPath))
        {
            using Stream? borderVideo = Activity?.Assets?.Open($"Borders/{name}.mp4");
            Stream? fs = Application.Context.OpenFileOutput($"{name}.mp4", FileCreationMode.Private);
            if (fs is not null)
                borderVideo?.CopyTo(fs);
        }

        return diskPath;
    }
}