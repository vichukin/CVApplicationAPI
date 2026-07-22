using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CVApplicationAPI.Services
{
    /// <summary>
    /// Singleton service that manages in-memory lazy-loaded cache of the CV content.
    /// Fetches content from a remote URL on first access and stores it in memory.
    /// Supports background updates via UpdateCache method.
    /// </summary>
    public class CvCacheService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _fileUrl;
        private string? _cachedContent;
        private readonly object _lockObject = new();

        public CvCacheService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

            // Read the AI Context URL from configuration (e.g., user secrets or appsettings.json)
            // Expected path: configuration["Links:AIContext"]
            _fileUrl = configuration["Links:AIContext"]
                ?? Environment.GetEnvironmentVariable("Links__AIContext")
                ?? throw new InvalidOperationException("Configuration key 'Links:AIContext' is missing. Ensure it is set in user secrets or appsettings.json");
        }

        /// <summary>
        /// Gets the cached CV content. If the cache is empty, fetches from the remote URL.
        /// Subsequent calls return the cached content immediately.
        /// </summary>
        public async Task<string> GetContentAsync(CancellationToken cancellationToken = default)
        {
            // Fast path: if cache is populated, return immediately
            if (!string.IsNullOrEmpty(_cachedContent))
            {
                return _cachedContent;
            }

            // Slow path: fetch from remote URL under lock to prevent multiple simultaneous fetches
            lock (_lockObject)
            {
                // Double-check after acquiring lock
                if (!string.IsNullOrEmpty(_cachedContent))
                {
                    return _cachedContent;
                }

                // Fetch content from URL (synchronous call within lock; consider refactoring if blocking is a concern)
                _cachedContent = FetchContentFromUrlAsync(_fileUrl, cancellationToken).GetAwaiter().GetResult();
            }

            return _cachedContent ?? string.Empty;
        }

        /// <summary>
        /// Updates the cached content. Used by the background refresh worker.
        /// </summary>
        public void UpdateCache(string newContent)
        {
            lock (_lockObject)
            {
                _cachedContent = newContent;
            }
        }

        /// <summary>
        /// Fetches content from the remote URL using HttpClient.
        /// </summary>
        private async Task<string> FetchContentFromUrlAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to fetch CV content from URL: {url}",
                    ex);
            }
        }
    }
}
