using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AsyncNetClient;
using AsyncNetClient.Decorators;
using AsyncNetClient.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AsyncNetClient.Tests
{
    [TestFixture]
    public class AsyncNetworkClientTests
    {
        private const string ThirdPartyAPI = "https://dummyjson.com";
        private const string FakeAPI = "https://fakeurl";
        private const string EndpointUsers = "/users";
        private const string EndpointUsersLogin = "/users/login";
        private const string EndpointSingleUser = "/users/2";
        private const string EndpointUsersDelayed = "/users?delay=3000";
        
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _timeoutLarge = TimeSpan.FromSeconds(10);
        
        [Serializable]
        public struct LoginRequest
        {
            public string username;
            public string password;
        }

        private struct LoginResponse
        {
            public int id;
            public string username;
            public string email;
            public string firstName;
            public string lastName;
            public string gender;
            public string image;
            public string accessToken;
            public string refreshToken;
        }
        
        public struct UserData
        {
            
        }

        public struct UserResponse
        {
            public int id;
            public string username;
            public string password;
            public string email;
            public string firstName;
            public string lastName;
            public string gender;
            public string image;
        }
        
        [UnityTest]
        public IEnumerator Given_MockDecorator_When_PostAsync_Then_ResultEqualsMockResponse() => 
            TaskUtils.ToCoroutine(async () =>
            {
                // Arrange
                var loginRequest = new LoginRequest
                {
                    username = "michaelw",
                    password = "michaelwpass"
                };
                var mock = new Dictionary<string, IAsyncNetDecorator.NextDecorator>
                {
                    { EndpointUsers, (context, _) => Task.FromResult(new ResponseContext(context, context.Value, (long)HttpStatusCode.OK,
                        new Dictionary<string, string>()))
                    }
                        
                };
                
                var mockDecorator = new MockDecorator(mock);
                var client = new AsyncNetworkClient(FakeAPI, _timeout, mockDecorator);
            
                // Act
                var response = await client.SendAsync(HttpMethod.Post, EndpointUsers, loginRequest);
                var result = response.GetResponseAs<LoginResponse>();
            
                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(loginRequest.username, result.username);
            });
        
        [UnityTest]
        public IEnumerator Given_NetworkClient_When_ProperPostAsync_Then_ReturnsProperResponse() => 
            TaskUtils.ToCoroutine(async () =>
            {
                // Arrange
                var loginRequest = new LoginRequest
                {
                    username = "michaelw",
                    password = "michaelwpass"
                };
                var client = new AsyncNetworkClient(ThirdPartyAPI, _timeout);

                // Act
                var response = await client.SendAsync(HttpMethod.Post, EndpointUsersLogin, loginRequest);
                var result = response.GetResponseAs<LoginResponse>();
                
                // Assert
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.id);
                Assert.AreEqual(result.id, 2);
                Assert.IsNotNull(result.username);
                Assert.IsNotEmpty(result.username);
                Assert.IsNotNull(result.accessToken);
                Assert.IsNotEmpty(result.accessToken);
                Assert.IsNotNull(result.refreshToken);
                Assert.IsNotEmpty(result.refreshToken);
            });
        
        [UnityTest]
        public IEnumerator Given_NetworkClient_When_ProperGetAsync_Then_ReturnsProperResponse() => 
            TaskUtils.ToCoroutine(async () =>
            {
                // Arrange
                var client = new AsyncNetworkClient(ThirdPartyAPI, _timeout, new LoggingDecorator());

                // Act
                var response = await client.SendAsync(HttpMethod.Get, EndpointSingleUser);
                var result = response.GetResponseAs<UserResponse>();
                
                // Assert
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.username);
                Assert.IsNotEmpty(result.username);
            });
        
        [UnityTest]
        public IEnumerator Given_NetworkClient_When_ProperGetAsyncRateLimited_Then_ReturnsProperQueuedResponses() => 
            TaskUtils.ToCoroutine(async () =>
            {
                // Arrange
                var client = new AsyncNetworkClient(ThirdPartyAPI, _timeoutLarge, 
                    new RateLimitRequestDecorator(1), new LoggingDecorator());

                // Act
                var responses = await Task.WhenAll(client.SendAsync(HttpMethod.Get, EndpointUsersDelayed),
                    client.SendAsync(HttpMethod.Get, EndpointUsersDelayed));

                var result1 = responses[0].Timestamp;
                var result2 = responses[1].Timestamp;
                
                var diffInSeconds = (result2 - result1).TotalSeconds;
                
                // Assert
                Assert.IsTrue(diffInSeconds > 3);
            });
        
        [UnityTest]
        public IEnumerator Given_NetworkClient_When_GetAsyncDelayed_Then_RaisesTimeoutException() => 
            TaskUtils.ToCoroutine(async () =>
            {
                // Arrange
                var client = new AsyncNetworkClient(ThirdPartyAPI, _timeout);
                var hasTimeout = false;

                // Act and Assert
                try
                {
                    await client.SendAsync(HttpMethod.Get, EndpointUsersDelayed);
                }
                catch (TimeoutException)
                {
                    hasTimeout = true;
                }
                finally
                {
                    Assert.IsTrue(hasTimeout);
                }
            });
        
        [UnityTest]
        public IEnumerator Given_BackoffDecorator_When_GetAsyncDelayed_Then_MaxRetriesExceeded() => 
            TaskUtils.ToCoroutine(async () =>
            {
                // Arrange
                var backoffRetries = 2;
                var minBackoff = 0.5f;
                var maxBackoff = 5f;

                var maxRetriesExceeded = false;
                var loggingDecorator = new LoggingDecorator();
                var backoffDecorator = new BackoffDecorator(backoffRetries, minBackoff, maxBackoff);
                var client = new AsyncNetworkClient(ThirdPartyAPI, _timeout, backoffDecorator, loggingDecorator);

                // Act and Assert
                try
                {
                    await client.SendAsync(HttpMethod.Get, EndpointUsersDelayed);
                }
                catch (MaxRetriesExceededException)
                {
                    maxRetriesExceeded = true;
                }
                finally
                {
                    Assert.IsTrue(maxRetriesExceeded);
                }
            });
    }
}