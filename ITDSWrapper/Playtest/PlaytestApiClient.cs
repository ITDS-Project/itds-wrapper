using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ITDSWrapper.Playtest;

public class PlaytestApiClient
{
    private const string PlaytestApiUri = "https://localhost:8081/api/ingest";
    private readonly HttpClient _httpClient = new();
    private readonly string? _playtestApiToken;
    private readonly string _sessionGuid = Guid.NewGuid().ToString();
    private readonly string _platformId =
        $"{RuntimeInformation.RuntimeIdentifier} (v{Assembly.GetExecutingAssembly().GetName().Version})";

    public PlaytestApiClient()
    {
        foreach (var attr in Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attr.Key == "PlaytestApiToken")
            {
                _playtestApiToken = attr.Value;
            }
        }

        _httpClient.BaseAddress = new(PlaytestApiUri);
        _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _playtestApiToken);
    }

    public async Task SendLogs(List<string> logs)
    {
        PlaytestApiRequest request = new(_sessionGuid, string.Empty, _platformId, string.Join('\n', logs));
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync("/logs", JsonContent.Create(request));
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(3));
                    await SendLogs(logs);
                });
            }
        }
        catch
        {
            Console.WriteLine("oh no");
        }
    }
}