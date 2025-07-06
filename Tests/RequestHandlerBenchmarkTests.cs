using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncNetClient;
using AsyncNetClient.Requests;
using AsyncNetClient.Serialization;
using AsyncNetClient.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AsyncNetClient.Tests
{
    [TestFixture]
    public class RequestHandlerBenchmarkTests
    {
        private const string TestApiUrl = "https://dummyjson.com";
        private const string EndpointUsers = "/users/1";
        private const string EndpointLogin = "/users/login";

        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        [Serializable]
        public struct LoginRequest
        {
            public string username;
            public string password;
        }

        public struct BenchmarkResult
        {
            public string clientName;
            public int totalRequests;
            public int successfulRequests;
            public int failedRequests;
            public double averageLatencyMs;
            public double minLatencyMs;
            public double maxLatencyMs;
            public double requestsPerSecond;
            public double totalTimeMs;
        }

        [UnityTest]
        public IEnumerator Given_AsyncNetworkClients_When_Benchmarked_Then_ComparePerformance() =>
            TaskUtils.ToCoroutine(async () =>
            {
                // Arrange
                var requestCount = 20;
                var concurrentRequests = 3;

                var unityClient = new AsyncNetworkClientBuilder()
                    .WithBasePath(TestApiUrl)
                    .WithTimeout(_timeout)
                    .WithSerializer(SerializationFactory.Create())
                    .WithRequestHandler(new UnityWebRequestHandlerHandler()).Build();

                var httpClient = new AsyncNetworkClientBuilder()
                    .WithBasePath(TestApiUrl)
                    .WithTimeout(_timeout)
                    .WithSerializer(SerializationFactory.Create())
                    .WithRequestHandler(new HttpClientRequestHandler()).Build();

                var clients = new List<(IAsyncNetworkClient client, string name)>
                {
                    (unityClient, "UnityWebRequestHandler"),
                    (httpClient, "HttpClientRequestHandler")
                };

                var testRequests = new List<(HttpMethod method, string path, object value)>
                {
                    (HttpMethod.Get, EndpointUsers, null),
                    (HttpMethod.Post, EndpointLogin, new LoginRequest { username = "michaelw", password = "michaelwpass" })
                };

                var results = new List<BenchmarkResult>();

                // Act
                foreach (var (client, name) in clients)
                {
                    Debug.Log($"Benchmarking {name}...");

                    var result = await BenchmarkClientAsync(client, name, testRequests,
                        requestCount, concurrentRequests, CancellationToken.None);

                    results.Add(result);

                    // Small delay between client tests
                    await TaskUtils.Delay(1.0, CancellationToken.None);
                }

                // Assert and Log Results
                Assert.IsTrue(results.Count >= 2, "Should have results from at least 2 clients");

                LogBenchmarkResults(results);

                // Verify both clients completed some requests successfully
                foreach (var result in results)
                {
                    Assert.IsTrue(result.successfulRequests > 0,
                        $"{result.clientName} should have completed at least some requests successfully");
                    Assert.IsTrue(result.averageLatencyMs > 0,
                        $"{result.clientName} should have measurable latency");
                }

                // Cleanup
                if (httpClient is IDisposable disposableHttpClient)
                {
                    disposableHttpClient.Dispose();
                }
            });

        private async Task<BenchmarkResult> BenchmarkClientAsync(
    IAsyncNetworkClient client,
    string clientName,
    List<(HttpMethod method, string path, object value)> testRequests,
    int requestCount,
    int concurrentRequests,
    CancellationToken cancellationToken)
{
    var latencies = new List<double>();
    var successCount = 0;
    var failureCount = 0;

    var semaphore = new SemaphoreSlim(concurrentRequests, concurrentRequests);
    var tasks = new List<Task<bool>>();

    var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

    for (int i = 0; i < requestCount; i++)
    {
        var requestIndex = i % testRequests.Count;
        var (method, path, value) = testRequests[requestIndex];

        var task = ProcessRequestAsync(client, clientName, method, path, value, semaphore, latencies, cancellationToken);
        tasks.Add(task);
    }

    var results = await Task.WhenAll(tasks);
    totalStopwatch.Stop();

    // Count successes and failures from task results
    foreach (var success in results)
    {
        if (success)
            successCount++;
        else
            failureCount++;
    }

    // Calculate statistics
    var result = new BenchmarkResult
    {
        clientName = clientName,
        totalRequests = requestCount,
        successfulRequests = successCount,
        failedRequests = failureCount,
        totalTimeMs = totalStopwatch.Elapsed.TotalMilliseconds
    };

    if (latencies.Count > 0)
    {
        latencies.Sort();
        result.averageLatencyMs = latencies.Average();
        result.minLatencyMs = latencies.First();
        result.maxLatencyMs = latencies.Last();
        result.requestsPerSecond = successCount / totalStopwatch.Elapsed.TotalSeconds;
    }

    return result;
}

private async Task<bool> ProcessRequestAsync(
    IAsyncNetworkClient client,
    string clientName,
    HttpMethod method,
    string path,
    object value,
    SemaphoreSlim semaphore,
    List<double> latencies,
    CancellationToken cancellationToken)
{
    await semaphore.WaitAsync(cancellationToken);
    try
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await client.SendAsync(method, path, value, cancellationToken);
            stopwatch.Stop();

            lock (latencies)
            {
                latencies.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
            
            return true; // Success
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Debug.LogWarning($"{clientName} request failed: {ex.Message}");
            return false; // Failure
        }
    }
    finally
    {
        semaphore.Release();
    }
}

        private void LogBenchmarkResults(List<BenchmarkResult> results)
        {
            Debug.Log("=== ASYNCNETWORKCLIENT BENCHMARK RESULTS ===");

            foreach (var result in results)
            {
                Debug.Log($"\n--- {result.clientName} ---");
                Debug.Log($"Total Requests: {result.totalRequests}");
                Debug.Log($"Successful: {result.successfulRequests}");
                Debug.Log($"Failed: {result.failedRequests}");
                Debug.Log($"Success Rate: {(double)result.successfulRequests / result.totalRequests * 100:F1}%");
                Debug.Log($"Average Latency: {result.averageLatencyMs:F2} ms");
                Debug.Log($"Min Latency: {result.minLatencyMs:F2} ms");
                Debug.Log($"Max Latency: {result.maxLatencyMs:F2} ms");
                Debug.Log($"Requests/Second: {result.requestsPerSecond:F2}");
                Debug.Log($"Total Time: {result.totalTimeMs:F2} ms");
            }

            // Performance comparison
            if (results.Count >= 2)
            {
                var unity = results.FirstOrDefault(r => r.clientName.Contains("Unity"));
                var httpClient = results.FirstOrDefault(r => r.clientName.Contains("HttpClient"));

                if (unity.clientName != null && httpClient.clientName != null)
                {
                    Debug.Log($"\n=== PERFORMANCE COMPARISON ===");
                    Debug.Log($"HttpClient vs Unity Average Latency: {httpClient.averageLatencyMs / unity.averageLatencyMs:F2}x");
                    Debug.Log($"HttpClient vs Unity RPS: {httpClient.requestsPerSecond / unity.requestsPerSecond:F2}x");
                    Debug.Log($"HttpClient vs Unity Success Rate: {httpClient.successfulRequests / (double)unity.successfulRequests:F2}x");
                }
            }
        }
    }
}