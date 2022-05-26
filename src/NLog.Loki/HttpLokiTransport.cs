using System.Collections.Generic;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NLog.Common;
using NLog.Loki.Model;

namespace NLog.Loki;

/// <remarks>
/// See https://grafana.com/docs/loki/latest/api/#examples-4
/// </remarks>
internal sealed class HttpLokiTransport : ILokiTransport
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILokiHttpClient _lokiHttpClient;
    private readonly CompressionLevel _gzipLevel;

    public HttpLokiTransport(
        ILokiHttpClient lokiHttpClient,
        bool orderWrites,
        CompressionLevel gzipLevel)
    {
        _lokiHttpClient = lokiHttpClient;
        _gzipLevel = gzipLevel;
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new LokiEventsSerializer(orderWrites));
        _jsonOptions.Converters.Add(new LokiEventSerializer());
    }

    public async Task WriteLogEventsAsync(IEnumerable<LokiEvent> lokiEvents)
    {
        using var jsonStreamContent = CreateContent(lokiEvents);
        using var response = await _lokiHttpClient.PostAsync("loki/api/v1/push", jsonStreamContent).ConfigureAwait(false);
        await ValidateHttpResponse(response).ConfigureAwait(false);
    }

    public async Task WriteLogEventsAsync(LokiEvent lokiEvent)
    {
        using var jsonStreamContent = CreateContent(lokiEvent);
        using var response = await _lokiHttpClient.PostAsync("loki/api/v1/push", jsonStreamContent).ConfigureAwait(false);
        await ValidateHttpResponse(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Prepares the HttpContent for the loki event(s).
    /// If gzip compression is enabled, prepares a gzip stream with the appropriate headers.
    /// </summary>
    private HttpContent CreateContent<T>(T lokiEvent)
    {
        var jsonContent = JsonContent.Create(lokiEvent, options: _jsonOptions);

        // If no compression required
        if(_gzipLevel == CompressionLevel.NoCompression)
            return jsonContent;

        return new CompressedContent(jsonContent, _gzipLevel);
    }

    private static async ValueTask ValidateHttpResponse(HttpResponseMessage response)
    {
        if(response.IsSuccessStatusCode)
            return;

        // Read the response's content
        var content = response.Content == null ? null :
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        InternalLogger.Error("Failed pushing logs to Loki. Code: {Code}. Reason: {Reason}. Message: {Message}.",
            response.StatusCode, response.ReasonPhrase, content);

#if NET6_0_OR_GREATER
                throw new HttpRequestException("Failed pushing logs to Loki.", inner: null, response.StatusCode);
#else
        throw new HttpRequestException("Failed pushing logs to Loki.");
#endif
    }

    public void Dispose()
    {
        _lokiHttpClient.Dispose();
    }
}

