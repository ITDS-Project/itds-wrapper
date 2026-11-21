using System;
using System.IO;
using Android.App;
using Android.Content;
using ITDSWrapper.Graphics;

namespace ITDSWrapper.Android;

public class AndroidBorderLocator : IBorderLocator
{
    public MainActivity? Activity { get; set; }
    
    public string GetBorderPath(string name, BorderQuality quality)
    {
        string filename = $"{name}_{quality.ToString()[1..]}.mp4";
        string diskPath = Path.Combine(Application.Context.FilesDir!.AbsolutePath, filename);
        if (!File.Exists(diskPath))
        {
            try
            {
                using Stream? borderVideo = Activity?.Assets?.Open($"Borders/{filename}");
                Stream? fs = Application.Context.OpenFileOutput(filename, FileCreationMode.Private);
                if (fs is not null)
                    borderVideo?.CopyTo(fs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to copy border video file: {ex.Message}\n{ex.StackTrace}");
            }
        }

        return diskPath;
    }
}