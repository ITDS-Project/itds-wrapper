using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using DiscUtils.Fat;
using DiscUtils.Streams;
using Steamworks;

namespace SteamworksHelper;

public static class Steamworks
{
    extension(NamedPipeClientStream clientStream)
    {
        public void SendResponse(string response)
        {
            clientStream.Write(Encoding.UTF8.GetBytes(response).ToList()
                .Concat(new byte[512 - Encoding.UTF8.GetByteCount(response)]).ToArray(), 0, 512);
        }

        public void SendResponse(byte[] response)
        {
            clientStream.Write(response.ToList().Concat(new byte[512 - response.Length]).ToArray());
        }
    }

    public static string GameLanguage()
    {
        return SteamApps.GameLanguage;
    }

    public static string Username()
    {
        return SteamClient.Name;
    }

    public static void InputInit()
    {
        SteamInput.Init();
    }

    public static void InputShutdown()
    {
        SteamInput.Shutdown();
    }

    public static void SetRichPresence(string[] rp)
    {
        SteamFriends.SetRichPresence(rp[0], rp[1]);
        SteamFriends.SetRichPresence("steam_display", "#Status");
    }

    public static void TimelineInstantaneous(string[] timelineEvent)
    {
        SteamTimeline.AddInstantaneousTimelineEvent(timelineEvent[0], timelineEvent[1], timelineEvent[2],
            uint.Parse(timelineEvent[3]), float.Parse(timelineEvent[4]), TimelineEventClipPriority.Standard);
    }

    public static void UnlockAchievement(string achievementName)
    {
        SteamUserStats.SetAchievement(achievementName);
        while (!SteamUserStats.StoreStats()) ;
    }

    public static void UploadCloudSave(string saveDir, string saveFileName, string sdCardPath)
    {
        if (OperatingSystem.IsWindows())
        {
            string sdCardCopyPath = Path.Combine(saveDir, "melonDS DS", "dldi_sd_card_copy.bin");
            Thread.Sleep(500); // Sleep for just a sec before copying the SD card to get write updates
            File.Copy(sdCardPath, sdCardCopyPath, true);
            sdCardPath = sdCardCopyPath;
        }

        byte[] sdCardBytes = File.ReadAllBytes(sdCardPath);
        using MemoryStream sdCardStream = new(sdCardBytes[0x7E00..]);
        using FatFileSystem sdCardFat = new(sdCardStream, Ownership.None);
        if (!sdCardFat.FileExists(saveFileName))
        {
            return;
        }

        using SparseStream savStream = sdCardFat.OpenFile(saveFileName, FileMode.Open);
        byte[] savFile = new byte[savStream.Length];
        savStream.ReadExactly(savFile);
        if (!SteamRemoteStorage.FileWrite(saveFileName, savFile))
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "cloud_save_fail.log"),
                "Failed to upload save to steam cloud.");
        }

        if (OperatingSystem.IsWindows())
        {
            File.Delete(sdCardPath);
        }
    }

    public static bool DownloadCloudSave(string saveFileName, string sdCardPath)
    {
        if (!SteamRemoteStorage.FileExists(saveFileName))
        {
            return false;
        }

        if (!File.Exists(sdCardPath))
        {
            return true;
        }

        byte[]? saveFile = SteamRemoteStorage.FileRead(saveFileName);
        if (saveFile is null)
        {
            return false;
        }

        byte[] sdCardBytes = File.ReadAllBytes(sdCardPath);
        using SparseMemoryStream sdCardStream = new();
        sdCardStream.Write(sdCardBytes[0x7E00..]);
        sdCardStream.Seek(0, SeekOrigin.Begin);

        using FatFileSystem sdCardFat = new(sdCardStream, Ownership.None);
        if (sdCardFat.FileExists(saveFileName))
        {
            using SparseStream savStream =
                sdCardFat.OpenFile(saveFileName, FileMode.Open, FileAccess.Write);
            savStream.Write(saveFile, 0, saveFile.Length);
            savStream.Flush();
        }
        else
        {
            using SparseStream savStream =
                sdCardFat.OpenFile(saveFileName, FileMode.CreateNew, FileAccess.Write);
            savStream.Write(saveFile, 0, saveFile.Length);
            savStream.Flush();
            byte[] oldSdCardBytes = sdCardBytes;
            sdCardBytes = new byte[0x7E00 + sdCardStream.Length];
            Array.Copy(oldSdCardBytes, sdCardBytes, oldSdCardBytes.Length);
        }

        byte[] sdCardStreamBytes = new byte[sdCardStream.Length];
        sdCardStream.Seek(0, SeekOrigin.Begin);
        sdCardStream.ReadExactly(sdCardStreamBytes);
        Array.Copy(sdCardStreamBytes, 0, sdCardBytes, 0x7E00, sdCardStream.Length);
        File.WriteAllBytes(sdCardPath, sdCardBytes);

        return false;
    }

    public static void ClearSteamCloud()
    {
        string[] files = SteamRemoteStorage.Files.ToArray();
        foreach (string file in files)
        {
            SteamRemoteStorage.FileDelete(file);
        }
    }
}