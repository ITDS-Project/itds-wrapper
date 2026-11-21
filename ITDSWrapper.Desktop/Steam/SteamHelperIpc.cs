using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace ITDSWrapper.Desktop.Steam;

public class SteamHelperIpc
{
    private readonly NamedPipeClientStream _steamworksHelperPipe;
    private readonly NamedPipeServerStream _steamworksReturnPipe;

    public SteamHelperIpc()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                _steamworksHelperPipe = new("SteamworksHelperPipe");
                _steamworksReturnPipe = new("SteamworksReturnPipe");
                break;
            }
            catch (IOException)
            {
                Thread.Sleep(200 * (int)Math.Pow(i, 2));
            }
        }

        if (_steamworksHelperPipe is null || _steamworksReturnPipe is null)
        {
            throw new IOException("Named pipes couldn't be created!");
        }
        _steamworksHelperPipe.Connect();
        _steamworksReturnPipe.WaitForConnection();
    }
    
    public void SendCommand(string command)
    {
        try
        {
            _steamworksHelperPipe.Write(
                Encoding.UTF8.GetBytes(command).ToList().Concat(new byte[512 - Encoding.UTF8.GetByteCount(command)])
                    .ToArray(), 0, 512);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public byte[] ReceiveResponse()
    {
        try
        {
            byte[] buffer = new byte[512];
            _steamworksReturnPipe.ReadExactly(buffer);
            return buffer;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return [];
        }
    }
}