using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Core.Updates;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class UpdateServiceBehaviorTests
{
    [Fact]
    public async Task CheckAsync_ShouldFallbackToSecondSourceReleaseMetadata()
    {
        var handler = new StubHttpMessageHandler();
        handler.AddJson("https://mirror.example/info.json", HttpStatusCode.OK, """
            {"version":"2026.5.7.1","changes":["Mirror first"]}
            """);
        handler.AddString("https://mirror.example/releases/latest", HttpStatusCode.BadGateway, string.Empty);
        handler.AddJson("https://fallback.example/releases/latest", HttpStatusCode.OK, """
            {
              "assets": [
                {
                  "name": "TopSpeed-windows-x64-Release-v-2026.5.7.1.zip",
                  "browser_download_url": "https://fallback.example/download/TopSpeed-windows-x64-Release-v-2026.5.7.1.zip",
                  "size": 321
                }
              ]
            }
            """);

        var config = new UpdateConfig(
            new[]
            {
                new UpdateSource("https://mirror.example/info.json", "https://mirror.example/releases/latest"),
                new UpdateSource("https://fallback.example/info.json", "https://fallback.example/releases/latest")
            },
            "TopSpeed-{runtime}-Release-v-{version}{ext}",
            "windows-x64",
            "Updater",
            "TopSpeed");
        var service = new UpdateService(config, new HttpClient(handler));

        var result = await service.CheckAsync(new GameVersion(2026, 5, 1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Update.Should().NotBeNull();
        result.Update!.DownloadUrls.Should().ContainInOrder(
            "https://fallback.example/download/TopSpeed-windows-x64-Release-v-2026.5.7.1.zip");
        result.Update.AssetSizeBytes.Should().Be(321);
    }

    [Fact]
    public async Task DownloadAsync_ShouldRetryNextUrlWhenFirstUrlFails()
    {
        var handler = new StubHttpMessageHandler();
        handler.AddString("https://mirror.example/download/update.zip", HttpStatusCode.BadGateway, string.Empty);
        handler.AddBytes("https://fallback.example/download/update.zip", HttpStatusCode.OK, Encoding.UTF8.GetBytes("fallback payload"));

        var config = new UpdateConfig(
            new[]
            {
                new UpdateSource("https://mirror.example/info.json", "https://mirror.example/releases/latest"),
                new UpdateSource("https://fallback.example/info.json", "https://fallback.example/releases/latest")
            },
            "TopSpeed-{runtime}-Release-v-{version}{ext}",
            "windows-x64",
            "Updater",
            "TopSpeed");
        var service = new UpdateService(config, new HttpClient(handler));
        var update = new UpdateInfo
        {
            VersionText = "2026.5.7.1",
            DownloadUrl = "https://mirror.example/download/update.zip",
            DownloadUrls = new[]
            {
                "https://mirror.example/download/update.zip",
                "https://fallback.example/download/update.zip"
            },
            AssetSizeBytes = "fallback payload"u8.Length
        };

        var targetDirectory = Path.Combine(Path.GetTempPath(), "topspeed-update-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(targetDirectory);
        try
        {
            var result = await service.DownloadAsync(update, targetDirectory, _ => { }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            File.Exists(result.ZipPath).Should().BeTrue();
            File.ReadAllText(result.ZipPath).Should().Be("fallback payload");
        }
        finally
        {
            if (Directory.Exists(targetDirectory))
                Directory.Delete(targetDirectory, true);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, Func<HttpResponseMessage>> _responses = new(StringComparer.OrdinalIgnoreCase);

        public void AddJson(string url, HttpStatusCode statusCode, string json)
        {
            AddString(url, statusCode, json, "application/json");
        }

        public void AddString(string url, HttpStatusCode statusCode, string content, string mediaType = "text/plain")
        {
            _responses[url] = () =>
            {
                var response = new HttpResponseMessage(statusCode);
                response.Content = new StringContent(content, Encoding.UTF8, mediaType);
                return response;
            };
        }

        public void AddBytes(string url, HttpStatusCode statusCode, byte[] content, string mediaType = "application/octet-stream")
        {
            _responses[url] = () =>
            {
                var response = new HttpResponseMessage(statusCode);
                response.Content = new ByteArrayContent(content);
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
                response.Content.Headers.ContentLength = content.Length;
                return response;
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri != null && _responses.TryGetValue(request.RequestUri.ToString(), out var factory))
                return Task.FromResult(factory());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(request.RequestUri?.ToString() ?? "missing")
            });
        }
    }
}
