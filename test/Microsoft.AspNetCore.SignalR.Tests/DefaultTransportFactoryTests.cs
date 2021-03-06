// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DefaultTransportFactoryTests
    {
        [Theory]
        [InlineData((HttpTransportType)0)]
        [InlineData(HttpTransportType.All + 1)]
        public void DefaultTransportFactoryCannotBeCreatedWithInvalidTransportType(HttpTransportType transportType)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DefaultTransportFactory(transportType, new LoggerFactory(), new HttpClient(), httpOptions: null));
        }

        [Theory]
        [InlineData(HttpTransportType.All)]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        [InlineData(HttpTransportType.LongPolling | HttpTransportType.WebSockets)]
        [InlineData(HttpTransportType.ServerSentEvents | HttpTransportType.WebSockets)]
        public void DefaultTransportFactoryCannotBeCreatedWithoutHttpClient(HttpTransportType transportType)
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DefaultTransportFactory(transportType, new LoggerFactory(), httpClient: null, httpOptions: null));

            Assert.Equal("httpClient", exception.ParamName);
        }

        [Fact]
        public void DefaultTransportFactoryCanBeCreatedWithoutHttpClientIfWebSocketsTransportRequestedExplicitly()
        {
            new DefaultTransportFactory(HttpTransportType.WebSockets, new LoggerFactory(), httpClient: null, httpOptions: null);
        }

        [ConditionalTheory]
        [InlineData(HttpTransportType.WebSockets, typeof(WebSocketsTransport))]
        [InlineData(HttpTransportType.ServerSentEvents, typeof(ServerSentEventsTransport))]
        [InlineData(HttpTransportType.LongPolling, typeof(LongPollingTransport))]
        [WebSocketsSupportedCondition]
        public void DefaultTransportFactoryCreatesRequestedTransportIfAvailable(HttpTransportType requestedTransport, Type expectedTransportType)
        {
            var transportFactory = new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null);
            Assert.IsType(expectedTransportType,
                transportFactory.CreateTransport(HttpTransportType.All));
        }

        [Theory]
        [InlineData(HttpTransportType.WebSockets)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.All)]
        public void DefaultTransportFactoryThrowsIfItCannotCreateRequestedTransport(HttpTransportType requestedTransport)
        {
            var transportFactory =
                new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null);
            var ex = Assert.Throws<InvalidOperationException>(
                () => transportFactory.CreateTransport(~requestedTransport));

            Assert.Equal("No requested transports available on the server.", ex.Message);
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public void DefaultTransportFactoryCreatesWebSocketsTransportIfAvailable()
        {
            Assert.IsType<WebSocketsTransport>(
                new DefaultTransportFactory(HttpTransportType.All, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null)
                    .CreateTransport(HttpTransportType.All));
        }

        [Theory]
        [InlineData(HttpTransportType.All, typeof(ServerSentEventsTransport))]
        [InlineData(HttpTransportType.ServerSentEvents, typeof(ServerSentEventsTransport))]
        [InlineData(HttpTransportType.LongPolling, typeof(LongPollingTransport))]
        public void DefaultTransportFactoryCreatesRequestedTransportIfAvailable_Win7(HttpTransportType requestedTransport, Type expectedTransportType)
        {
            if (!TestHelpers.IsWebSocketsSupported())
            {
                var transportFactory = new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null);
                Assert.IsType(expectedTransportType,
                    transportFactory.CreateTransport(HttpTransportType.All));
            }
        }

        [Theory]
        [InlineData(HttpTransportType.WebSockets)]
        public void DefaultTransportFactoryThrowsIfItCannotCreateRequestedTransport_Win7(HttpTransportType requestedTransport)
        {
            if (!TestHelpers.IsWebSocketsSupported())
            {
                var transportFactory =
                    new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null);
                var ex = Assert.Throws<InvalidOperationException>(
                    () => transportFactory.CreateTransport(HttpTransportType.All));

                Assert.Equal("No requested transports available on the server.", ex.Message);
            }
        }
    }
}
