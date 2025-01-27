// <copyright file="TelemetryHostedServiceTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.Extensions.Hosting.Tests;

public class TelemetryHostedServiceTests
{
    [Fact]
    public async Task StartWithoutProvidersDoesNotThrow()
    {
        var builder = new HostBuilder().ConfigureServices(services =>
        {
            services.AddOpenTelemetry()
                .StartWithHost();
        });

        var host = builder.Build();

        await host.StartAsync().ConfigureAwait(false);

        await host.StopAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task StartWithExceptionsThrows()
    {
        bool expectedInnerExceptionThrown = false;

        var builder = new HostBuilder().ConfigureServices(services =>
        {
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    if (builder is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
                    {
                        deferredTracerProviderBuilder.Configure((sp, sdkBuilder) =>
                        {
                            try
                            {
                                // Note: This throws because services cannot be
                                // registered after IServiceProvider has been
                                // created.
                                sdkBuilder.SetSampler<MySampler>();
                            }
                            catch (NotSupportedException)
                            {
                                expectedInnerExceptionThrown = true;
                                throw;
                            }
                        });
                    }
                })
                .StartWithHost();
        });

        var host = builder.Build();

        await Assert.ThrowsAsync<NotSupportedException>(() => host.StartAsync()).ConfigureAwait(false);

        await host.StopAsync().ConfigureAwait(false);

        Assert.True(expectedInnerExceptionThrown);
    }

    private sealed class MySampler : Sampler
    {
        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
            => new SamplingResult(SamplingDecision.RecordAndSample);
    }
}
