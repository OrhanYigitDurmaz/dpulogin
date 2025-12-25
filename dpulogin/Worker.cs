using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation; // Add this for Ping
using System.Net.Sockets;            // For SocketError

namespace dpulogin
{
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("DPU Auto Login service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!await HasInternetAsync())
                    {
                        logger.LogWarning("Internet restricted or DNS down. Attempting login...");
                        await DoLoginAsync();
                    }
                    else
                    {
                        logger.LogInformation("Internet is available.");
                    }
                }
                catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: SocketError.HostNotFound })
                {
                    // This catches the "No such host is known" specifically
                    logger.LogCritical("Login server 'giris.dpu.edu.tr' is not resolvable yet.");
                }
                catch (Exception ex)
                {
                    logger.LogError("Unexpected error: {msg}", ex.Message);
                }

                // Wait 5 seconds before next check to avoid overwhelming the system
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task<bool> HasInternetAsync()
        {
            try
            {
                // 1. Level One: Basic IP Connectivity
                // Check if we can reach a public IP directly (Cloudflare DNS)
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("1.1.1.1", 2000);

                if (reply.Status != IPStatus.Success)
                {
                    logger.LogTrace("Cannot ping 1.1.1.1. Offline.");
                    return false;
                }

                // 2. Level Two: DNS Resolution
                // If we can reach IPs, can we resolve names?
                var addresses = await Dns.GetHostAddressesAsync("www.google.com");

                // 3. Level Three: Captive Portal Check
                // If DNS works, are we redirected?
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var response = await client.GetAsync("http://www.msftconnecttest.com/connecttest.txt");

                var content = await response.Content.ReadAsStringAsync();
                return response.IsSuccessStatusCode && content.Contains("Microsoft");
            }
            catch (Exception ex)
            {
                logger.LogTrace("Internet check failed: {msg}", ex.Message);
                return false;
            }
        }

        private async Task DoLoginAsync()
        {
            var cookies = new CookieContainer();

            var handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All
            };

            using var client = new HttpClient(handler);

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            var loginUrl =
                "https://giris.dpu.edu.tr:6082/php/uid.php?vsys=1&rule=1&url=http://www.msftconnecttest.com%2fredirect";

            var username = Environment.GetEnvironmentVariable("DPU_USER");
            var password = Environment.GetEnvironmentVariable("DPU_PASS");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Missing DPU credentials.");

            var form = new Dictionary<string, string>
            {
                ["inputStr"] = "",
                ["escapeUser"] = "",
                ["preauthid"] = "",
                ["user"] = username,
                ["passwd"] = password,
                ["ok"] = "Login"
            };

            using var content = new FormUrlEncodedContent(form);
            content.Headers.ContentType =
                new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.PostAsync(loginUrl, content);

            logger.LogInformation(
                "Login HTTP Status: {status}",
                (int)response.StatusCode
            );

            if (response.Headers.Location != null)
            {
                logger.LogInformation(
                    "Redirected to: {location}",
                    response.Headers.Location
                );
            }
        }
    }
}
