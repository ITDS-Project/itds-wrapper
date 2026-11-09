using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ITDSWrapper.Playtest;

public class PlaytestApiClient
{
    private const string PlaytestApiUri = "http://playtest.intothedreamspring.com/api/ingest/";
    private readonly HttpClient _httpClient = new();
    private readonly string? _playtestApiToken;
    private readonly string _sessionGuid = Guid.NewGuid().ToString();
    private readonly string _platformId =
        $"{RuntimeInformation.RuntimeIdentifier} (v{Assembly.GetExecutingAssembly().GetName().Version})";

    private readonly List<PlaytestApiRequest> _requestsQueue = [];
    private bool _flushingQueue = false;
    
    public string? Username { get; set; }

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

    public void QueueLogs(List<string> logs)
    {
        _requestsQueue.Add(new(_sessionGuid, Username?.Replace("\0", "") ?? string.Empty, _platformId, string.Join("", logs)));
    }

    public async Task FlushLogs()
    {
        if (_flushingQueue)
            return;

        _flushingQueue = true;
        
        foreach (PlaytestApiRequest request in _requestsQueue)
        {
            try
            {
                for (int retries = 0; retries < 5; retries++)
                {
                    HttpResponseMessage response = await _httpClient.PostAsync("logs", JsonContent.Create(request));
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        await Task.Run(async () =>
                        {
                            await Task.Delay(response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(3));
                        });
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to send logs: {response.StatusCode} {response.ReasonPhrase}");
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send logs:\n{ex.Message}\n{ex.StackTrace}");
                // ignored
            }
        }

        _requestsQueue.Clear();
        _flushingQueue = false;
    }
}