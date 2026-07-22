using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CVApplicationAPI.Services
{
    /// <summary>
    /// Background service that periodically refreshes the CV content cache every 24 hours.
    /// Fetches content from the remote URL and updates the cache via CvCacheService.
    /// Handles network exceptions gracefully without crashing the application.
    /// </summary>
    public class CvRefreshWorker : BackgroundService
    {
        private readonly CvCacheService _cvCacheService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CvRefreshWorker> _logger;
        private readonly string _fileUrl;

        public CvRefreshWorker(
            CvCacheService cvCacheService,
            IHttpClientFactory httpClientFactory,
            ILogger<CvRefreshWorker> logger,
            IConfiguration configuration)
        {
            _cvCacheService = cvCacheService ?? throw new ArgumentNullException(nameof(cvCacheService));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Read the AI Context URL from configuration (e.g., user secrets or appsettings.json)
            // Expected path: configuration["Links:AIContext"]
            _fileUrl = configuration?["Links:AIContext"] 
                ?? Environment.GetEnvironmentVariable("Links__AIContext")
                ?? throw new InvalidOperationException("Configuration key 'Links:AIContext' is missing. Ensure it is set in user secrets or appsettings.json");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CvRefreshWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("CvRefreshWorker: Fetching CV content from {Url}", _fileUrl);

                    // Fetch the latest content from the remote URL
                    var content = await FetchContentFromUrlAsync(_fileUrl, stoppingToken);

                    if (!string.IsNullOrEmpty(content))
                    {
                        // Update the cache with the fetched content
                        _cvCacheService.UpdateCache(content);
                        _logger.LogInformation("CvRefreshWorker: Cache updated successfully. Content length: {Length} chars", content.Length);
                    }
                    else
                    {
                        _logger.LogWarning("CvRefreshWorker: Fetched empty content from {Url}", _fileUrl);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stoppingToken is cancelled during shutdown
                    _logger.LogInformation("CvRefreshWorker: Operation cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    // Log the exception but don't crash the application
                    _logger.LogError(ex, "CvRefreshWorker: Error occurred while fetching/updating cache. Will retry in 24 hours.");
                }

                // Wait for 24 hours before the next refresh
                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("CvRefreshWorker: Delay cancelled during shutdown.");
                    break;
                }
            }

            _logger.LogInformation("CvRefreshWorker stopped.");
        }

        /// <summary>
        /// Fetches content from the remote URL using HttpClient.
        /// </summary>
        private async Task<string> FetchContentFromUrlAsync(string url, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30); // 30-second timeout for network requests
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }
}
