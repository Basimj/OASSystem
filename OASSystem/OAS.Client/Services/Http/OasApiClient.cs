using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OAS.Client.Database.State;
using OAS.Contracts.Common.Errors;

namespace OAS.Client.Services.Http;

public sealed class OasApiClient(
    HttpClient httpClient,
    DatabaseProfileSelectionState databaseSelection)
{
    public async Task<T?> GetAsync<T>(
        string uri,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, uri);

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        return await ReadAsync<T>(
            response,
            cancellationToken);
    }

    public async Task<T?> PostAsync<TRequest, T>(
        string uri,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        using var request =
            CreateRequest(HttpMethod.Post, uri);

        request.Content =
            JsonContent.Create(payload);

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        return await ReadAsync<T>(
            response,
            cancellationToken);
    }

    public async Task<ApiCallResult<T>> PostResultAsync<TRequest, T>(
        string uri,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request =
                CreateRequest(HttpMethod.Post, uri);

            request.Content =
                JsonContent.Create(payload);

            using var response =
                await httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiCallResult<T>.Failure(
                    await ReadErrorAsync(
                        response,
                        cancellationToken));
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NoContent)
            {
                return ApiCallResult<T>.Success(default);
            }

            var value =
                await response.Content.ReadFromJsonAsync<T>(
                    cancellationToken: cancellationToken);

            return ApiCallResult<T>.Success(value);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<T>.Failure(
                new ApiError
                {
                    Status = 0,
                    Code = "network_error",
                    Message =
                        "Unable to connect to the server."
                });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ApiCallResult<T>.Failure(
                new ApiError
                {
                    Status = 0,
                    Code = "network_error",
                    Message =
                        "The request timed out before the server responded."
                });
        }
        catch (JsonException)
        {
            return ApiCallResult<T>.Failure(
                new ApiError
                {
                    Status = 0,
                    Code = "response_parse_error",
                    Message =
                        "The server returned an invalid response."
                });
        }
        catch (NotSupportedException)
        {
            return ApiCallResult<T>.Failure(
                new ApiError
                {
                    Status = 0,
                    Code = "response_parse_error",
                    Message =
                        "The server returned an unsupported response."
                });
        }
    }

    public async Task PostAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        using var request =
            CreateRequest(HttpMethod.Post, uri);

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        await EnsureSuccessAsync(
            response,
            cancellationToken);
    }

    public async Task<T?> PutAsync<TRequest, T>(
        string uri,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        using var request =
            CreateRequest(HttpMethod.Put, uri);

        request.Content =
            JsonContent.Create(payload);

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        return await ReadAsync<T>(
            response,
            cancellationToken);
    }

    public async Task<ApiCallResult<T>> PutResultAsync<TRequest, T>(
        string uri,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Put, uri);
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
        catch (JsonException)
        {
            return ApiCallResult<T>.Failure(new ApiError { Status = 0, Code = "response_parse_error", Message = "The server returned an invalid response." });
        }
    }

    public async Task DeleteAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        using var request =
            CreateRequest(HttpMethod.Delete, uri);

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        await EnsureSuccessAsync(
            response,
            cancellationToken);
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string uri)
    {
        var request =
            new HttpRequestMessage(
                method,
                uri);

        if (!string.IsNullOrWhiteSpace(
                databaseSelection.SelectedProfileKey))
        {
            request.Headers.TryAddWithoutValidation(
                DatabaseProfileSelectionState.HeaderName,
                databaseSelection.SelectedProfileKey);
        }

        return request;
    }

    private static async Task<T?> ReadAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(
            response,
            cancellationToken);

        if (response.StatusCode ==
            System.Net.HttpStatusCode.NoContent)
        {
            return default;
        }

        /*
         * String responses are handled explicitly.
         *
         * This supports both:
         *   "EMP-00003"
         *
         * and:
         *   EMP-00003
         *
         * without affecting normal JSON DTO responses.
         */
        if (typeof(T) == typeof(string))
        {
            var raw =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return default;
            }

            raw = raw.Trim();

            // JSON string:
            // "EMP-00003"
            if (raw.Length >= 2 &&
                raw.StartsWith('"') &&
                raw.EndsWith('"'))
            {
                try
                {
                    var parsed =
                        JsonSerializer.Deserialize<string>(raw);

                    return (T?)(object?)parsed;
                }
                catch (JsonException)
                {
                    // Fall through and use the raw value.
                }
            }

            // Plain text:
            // EMP-00003
            return (T?)(object)raw;
        }

        return await response.Content.ReadFromJsonAsync<T>(
            cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new ApiClientException(
            await ReadErrorAsync(
                response,
                cancellationToken));
    }

    private static async Task<ApiError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        ApiError? error = null;

        try
        {
            error =
                await response.Content.ReadFromJsonAsync<ApiError>(
                    cancellationToken: cancellationToken);
        }
        catch
        {
            // Use the fallback error below.
        }

        return error ??
               new ApiError
               {
                   Status = (int)response.StatusCode,
                   Code = "http_error",
                   Message =
                       $"Request failed with status {(int)response.StatusCode}."
               };
    }


    public async Task<ApiCallResult<T>> GetResultAsync<T>(
        string uri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, uri);
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
        catch (JsonException)
        {
            return ApiCallResult<T>.Failure(new ApiError { Status = 0, Code = "response_parse_error", Message = "The server returned an invalid response." });
        }
    }

    public async Task<ApiCallResult<bool>> UploadFilePutResultAsync(
        string uri,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Put, uri);
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);
            request.Content = content;
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ApiCallResult<bool>.Failure(await ReadErrorAsync(response, cancellationToken));
            return ApiCallResult<bool>.Success(true);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<bool>.Failure(new ApiError { Status = 0, Code = "network_error", Message = "Unable to connect to the server." });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiCallResult<bool>.Failure(new ApiError { Status = 0, Code = "network_error", Message = "The request timed out before the server responded." });
        }
    }

    public async Task<ApiCallResult<bool>> DeleteResultAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Delete, uri);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ApiCallResult<bool>.Failure(await ReadErrorAsync(response, cancellationToken));
            return ApiCallResult<bool>.Success(true);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<bool>.Failure(new ApiError { Status = 0, Code = "network_error", Message = "Unable to connect to the server." });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiCallResult<bool>.Failure(new ApiError { Status = 0, Code = "network_error", Message = "The request timed out before the server responded." });
        }
    }

    public async Task<T?> UploadFileAsync<T>(
        string uri,
        Stream fileStream,
        string fileName,
        string contentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        CancellationToken cancellationToken = default)
    {
        using var request =
            CreateRequest(
                HttpMethod.Post,
                uri);

        using var content =
            new MultipartFormDataContent();

        var fileContent =
            new StreamContent(fileStream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                contentType);

        content.Add(
            fileContent,
            "file",
            fileName);

        request.Content = content;

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        return await ReadAsync<T>(
            response,
            cancellationToken);
    }

    public async Task<byte[]> GetFileAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        using var request =
            CreateRequest(
                HttpMethod.Get,
                uri);

        using var response =
            await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        return await response.Content.ReadAsByteArrayAsync(
            cancellationToken);
    }
}