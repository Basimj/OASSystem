using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OAS.Print.Desktop.Models;
using OAS.Printing.Core.Models;
using OAS.Printing.Core.Services;

namespace OAS.Print.Desktop.Services;

public sealed class JobPollingService : IDisposable
{
    private readonly HttpClient _client = new();
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event Func<PrintJob, Task<bool>>? JobReceived;
    public event Action<string>? StatusChanged;

    public void Start(AppSettings settings)
    {
        Stop();

        if (!settings.PollEnabled ||
            string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = RunAsync(settings, _cts.Token);
    }

    public void Stop()
    {
        if (_cts is null)
            return;

        _cts.Cancel();
        _cts.Dispose();

        _cts = null;
        _loop = null;
    }

    private async Task RunAsync(
        AppSettings settings,
        CancellationToken token)
    {
        var baseUrl = settings.ApiBaseUrl.TrimEnd('/');

        while (!token.IsCancellationRequested)
        {
            try
            {
                var workstationCode =
                    Uri.EscapeDataString(settings.WorkstationCode);

                var url =
                    $"{baseUrl}/api/printing/desktop/jobs/next" +
                    $"?workstationCode={workstationCode}";

                using var request =
                    new HttpRequestMessage(HttpMethod.Get, url);

                AddApiKey(request, settings);

                using var response =
                    await _client.SendAsync(request, token);

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NoContent)
                {
                    StatusChanged?.Invoke(
                        "متصل - لا توجد مهام جديدة");
                }
                else if (response.IsSuccessStatusCode)
                {
                    var json =
                        await response.Content.ReadAsStringAsync(token);

                    var job =
                        JsonSerializer.Deserialize<PrintJob>(
                            json,
                            TemplateSerializer.Options);

                    if (job is not null)
                    {
                        var success = true;

                        if (JobReceived is not null)
                        {
                            success =
                                await JobReceived.Invoke(job);
                        }

                        await AcknowledgeAsync(
                            baseUrl,
                            settings,
                            job.JobId,
                            success,
                            success
                                ? null
                                : "تعذر تنفيذ مهمة الطباعة.",
                            token);
                    }
                }
                else
                {
                    StatusChanged?.Invoke(
                        $"API: {(int)response.StatusCode} " +
                        $"{response.ReasonPhrase}");
                }
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(
                    "غير متصل: " + ex.Message);
            }

            try
            {
                var seconds =
                    Math.Clamp(
                        settings.PollIntervalSeconds,
                        1,
                        60);

                await Task.Delay(
                    TimeSpan.FromSeconds(seconds),
                    token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task AcknowledgeAsync(
        string baseUrl,
        AppSettings settings,
        Guid jobId,
        bool success,
        string? error,
        CancellationToken token)
    {
        var action = success
            ? "complete"
            : "failed";

        var url =
            $"{baseUrl}/api/printing/desktop/jobs/" +
            $"{jobId}/{action}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                url);

        AddApiKey(request, settings);

        if (!success)
        {
            var json =
                JsonSerializer.Serialize(
                    new
                    {
                        error
                    });

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");
        }

        using var response =
            await _client.SendAsync(
                request,
                token);

        response.EnsureSuccessStatusCode();
    }

    private static void AddApiKey(
        HttpRequestMessage request,
        AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return;

        request.Headers.TryAddWithoutValidation(
            "X-OAS-Print-Key",
            settings.ApiKey);
    }

    public void Dispose()
    {
        Stop();
        _client.Dispose();

        GC.SuppressFinalize(this);
    }
}