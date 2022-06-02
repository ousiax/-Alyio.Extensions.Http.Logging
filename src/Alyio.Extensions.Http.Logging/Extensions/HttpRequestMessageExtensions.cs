﻿using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Alyio.Extensions;

/// <summary>
/// Extension mehtods for <see cref="HttpRequestMessage"/>.
/// </summary>
public static class HttpRequestMessageExtensions
{
    /// <summary>
    /// Gets HTTP response message with headers and body.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="ignoreContent">A <see cref="bool"/> value that indicates to ignore the request content. The default is false.</param>
    /// <param name="ignoreHeaders">The specified <see cref="string"/> array to ignore the specified headers of <see cref="HttpRequestMessage.Headers"/>.</param>
    /// <returns>The raw http message of <see cref="HttpRequestMessage"/>.</returns>
    public static async Task<string> ReadRawMessageAsync(this HttpRequestMessage request, bool ignoreContent = false, params string[] ignoreHeaders)
    {
        StringBuilder strBuilder = new StringBuilder(128);
        strBuilder.Append($"{request.Method} {request.RequestUri} HTTP/{request.Version}\r\n");

        foreach (var header in request.Headers)
        {
            if (ignoreHeaders.Contains(header.Key)) { continue; }
            strBuilder.Append($"{header.Key}: {string.Join(",", header.Value)}\r\n");
        }
        if (!ignoreContent && request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                strBuilder.Append($"{header.Key}: {string.Join(",", header.Value)}\r\n");
            }
            strBuilder.Append("\r\n");

            var contentStream = await request.Content.ReadAsStreamAsync();
            var dumpContentStream = new MemoryStream();
            await contentStream.CopyToAsync(dumpContentStream);
            dumpContentStream.Position = 0;

            var reader = new StreamReader(dumpContentStream);
            strBuilder.Append(await reader.ReadToEndAsync());

            if (contentStream.CanSeek)
            {
                dumpContentStream.Dispose();
                contentStream.Position = 0;
            }
            else
            {
                contentStream.Dispose();

                dumpContentStream.Position = 0;
                var newContent = new StreamContent(dumpContentStream);
                foreach (var header in request.Content.Headers)
                {
                    newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                request.Content = newContent;
            }
        }
        return strBuilder.ToString();
    }
}
