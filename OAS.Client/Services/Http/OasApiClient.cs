using System.Net.Http.Json;
using OAS.Client.Database.State;
using OAS.Contracts.Common.Errors;

namespace OAS.Client.Services.Http;

public sealed class OasApiClient(HttpClient httpClient, DatabaseProfileSelectionState databaseSelection)
{
    public async Task<T?> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, uri);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    public async Task<T?> PostAsync<TRequest, T>(string uri, TRequest payload, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, uri);
        request.Content = JsonContent.Create(payload);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }


    public async Task<ApiCallResult<T>> PostResultAsync<TRequest, T>(string uri, TRequest payload, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Post, uri);
            request.Content = JsonContent.Create(payload);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ApiCallResult<T>.Failure(await ReadErrorAsync(response, cancellationToken));

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return ApiCallResult<T>.Success(default);

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return ApiCallResult<T>.Success(value);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<T>.Failure(new ApiError { Status = 0, Code = "network_error", Message = "Unable to connect to the server." });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiCallResult<T>.Failure(new ApiError { Status = 0, Code = "network_error", Message = "The request timed out before the server responded." });
        }
        catch (System.Text.Json.JsonException)
        {
            return ApiCallResult<T>.Failure(new ApiError { Status = 0, Code = "response_parse_error", Message = "The server returned an invalid response." });
        }
        catch (NotSupportedException)
        {
            return ApiCallResult<T>.Failure(new ApiError { Status = 0, Code = "response_parse_error", Message = "The server returned an unsupported response." });
        }
    }

    public async Task PostAsync(string uri, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, uri);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<T?> PutAsync<TRequest, T>(string uri, TRequest payload, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Put, uri);
        request.Content = JsonContent.Create(payload);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    public async Task DeleteAsync(string uri, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, uri);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        if (!string.IsNullOrWhiteSpace(databaseSelection.SelectedProfileKey))
            request.Headers.TryAddWithoutValidation(DatabaseProfileSelectionState.HeaderName, databaseSelection.SelectedProfileKey);
        return request;
    }

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        throw new ApiClientException(await ReadErrorAsync(response, cancellationToken));
    }

    private static async Task<ApiError> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiError? error = null;
        try { error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken); } catch { }
        return error ?? new ApiError
        {
            Status = (int)response.StatusCode,
            Code = "http_error",
            Message = $"Request failed with status {(int)response.StatusCode}."
        };
    }
}
