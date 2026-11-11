using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace SteamworksHelper;

public static class Program
{
    private const string ResetAchievementsEnvironmentVariable = "RESET_ACHIEVEMENTS";
    private const string GamePathEnvironmentVariable = "ITDS_PATH";
    
    private static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        SteamClient.Init(4026050);
        if (Environment.GetEnvironmentVariable(ResetAchievementsEnvironmentVariable)
                ?.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            SteamUserStats.ResetAll(includeAchievements: true); // TODO: DELETE THIS
        }

        string? gamePath = Environment.GetEnvironmentVariable(GamePathEnvironmentVariable);
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEBUG_IPC")) && !string.IsNullOrEmpty(gamePath))
        {
            if (OperatingSystem.IsMacOS())
            {
                ProcessStartInfo psi = new(gamePath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = false,
                };
                Process.Start(psi);
            }
            else
            {
                Process.Start(gamePath);
            }
        }
        else if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEBUG_IPC")))
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(".\\ITDSWrapper.Desktop.exe");
            }
            else if (OperatingSystem.IsMacOS())
            {
                ProcessStartInfo psi = new("./ITDSWrapper.Desktop")
                {
                    UseShellExecute = false,
                    CreateNoWindow = false,
                };
                Process.Start(psi);
            }
            else
            {
                Process.Start("./ITDSWrapper.Desktop");
            }
        }
        
        NamedPipeServerStream steamworksServer = new("SteamworksHelperPipe");
        NamedPipeClientStream steamworksClient = new("SteamworksReturnPipe");
        await steamworksClient.ConnectAsync();
        bool loop = true;
        await steamworksServer.WaitForConnectionAsync();
        
        while (loop)
        {
            try
            {
                if (!steamworksServer.CanRead)
                {
                    continue;
                }
                byte[] utf8Bytes = new byte[512];
                await steamworksServer.ReadExactlyAsync(utf8Bytes);
                string[] cmd = Encoding.UTF8.GetString(utf8Bytes, 0, utf8Bytes.Length).Split(' ').Select(s => s.Replace('\u0000', ' ').TrimEnd()).ToArray();

                switch (cmd[0])
                {
                    case "ACHIEVEMENT":
                        Console.WriteLine("Unlocking achievement...");
                        Steamworks.UnlockAchievement(cmd[1]);
                        break;
                    
                    case "CLOUD_SAVE_CLEAR":
                        Console.WriteLine("Clearing cloud saves...");
                        Steamworks.ClearSteamCloud();
                        break;
                    
                    case "CLOUD_SAVE_DOWNLOAD":
                        Console.WriteLine("Downloading cloud save...");
                        Steamworks.DownloadCloudSave(cmd[1], cmd[2]);
                        break;
                    
                    case "CLOUD_SAVE_UPLOAD":
                        Console.WriteLine("Uploading cloud save...");
                        Steamworks.UploadCloudSave(cmd[1], cmd[2], cmd[3]);
                        break;
                    
                    case "DIE":
                        Console.WriteLine("Exiting...");
                        loop = false;
                        break;
                    
                    case "GAME_LANGUAGE":
                        Console.WriteLine("Fetching game language...");
                        steamworksClient.SendResponse(Steamworks.GameLanguage());
                        break;
                    
                    case "RICH_PRESENCE":
                        Console.WriteLine("Emitting rich presence...");
                        Steamworks.SetRichPresence(cmd[1..]);
                        break;
                    
                    case "TIMELINE_INST":
                        Console.WriteLine("Emitting instantaneous timeline event...");
                        Steamworks.TimelineInstantaneous(cmd[1..]);
                        break;
                    
                    case "USERNAME":
                        Console.WriteLine("Emitting username...");
                        steamworksClient.SendResponse(Steamworks.Username());
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}