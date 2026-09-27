using System.Text;
using System.Text.Json;
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
        if (!settings.PollEnabled)
        {
            StatusChanged?.Invoke("استقبال المهام متوقف");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
        {
            StatusChanged?.Invoke("غير متصل: عنوان API غير محدد");
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = RunAsync(settings, _cts.Token);
    }

    public void Stop()
    {
        if (_cts is null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    private async Task RunAsync(AppSettings settings, CancellationToken token)
    {
        var baseUrl = settings.ApiBaseUrl.TrimEnd('/');
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{baseUrl}/api/printing/desktop/jobs/next?workstationCode={Uri.EscapeDataString(settings.WorkstationCode)}");
                if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                    request.Headers.TryAddWithoutValidation("X-OAS-Print-Key", settings.ApiKey);

                using var response = await _client.SendAsync(request, token);
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    StatusChanged?.Invoke("متصل - لا توجد مهام جديدة");
                }
                else if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(token);
                    var job = JsonSerializer.Deserialize<PrintJob>(json, TemplateSerializer.Options);
                    if (job is not null)
                    {
                        var ok = JobReceived is null || await JobReceived.Invoke(job);
                        await AcknowledgeAsync(baseUrl, settings, job.JobId, ok, ok ? null : "تعذر تنفيذ مهمة الطباعة.", token);
                    }
                }
                else
                {
                    StatusChanged?.Invoke($"API: {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke("غير متصل: " + ex.Message);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(settings.PollIntervalSeconds, 1, 60)), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task AcknowledgeAsync(string baseUrl, AppSettings settings, Guid jobId, bool success, string? error, CancellationToken token)
    {
        var suffix = success ? "complete" : "failed";
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/printing/desktop/jobs/{jobId}/{suffix}");
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            request.Headers.TryAddWithoutValidation("X-OAS-Print-Key", settings.ApiKey);
        if (!success)
            request.Content = new StringContent(JsonSerializer.Serialize(new { error }), Encoding.UTF8, "application/json");
        using var _ = await _client.SendAsync(request, token);
    }

    public void Dispose()
    {
        Stop();
        _client.Dispose();
    }
}
