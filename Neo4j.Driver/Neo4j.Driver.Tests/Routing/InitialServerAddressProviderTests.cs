// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests.Routing;

public class InitialServerAddressProviderTests
{
    [Fact]
    public void ShouldUsePortFromResolvedAddress()
    {
        // Driver URI has port 9999, resolver returns localhost:7687.
        // The resolved port (7687) must be used — not the driver URI's port.
        var initUri = new Uri("neo4j://example.com:9999");
        var resolver = new ListAddressResolver(ServerAddress.From("localhost", 7687));
        var provider = new InitialServerAddressProvider(initUri, resolver);

        var uris = provider.Get();

        uris.Should().ContainSingle(because: "resolver returned one address")
            .Which.Port.Should().Be(7687, because: "the resolver specified port 7687");
    }

    [Fact]
    public void ShouldUseHostFromResolvedAddress()
    {
        var initUri = new Uri("neo4j://example.com:9999");
        var resolver = new ListAddressResolver(ServerAddress.From("localhost", 7687));
        var provider = new InitialServerAddressProvider(initUri, resolver);

        var uris = provider.Get();

        uris.Should().ContainSingle()
            .Which.Host.Should().Be("localhost", because: "the resolver specified localhost");
    }
}

// Regression tests for DRIVERS-199: custom domain resolver ignores port.
//
// Scenario: driver is configured with neo4j://example.com:9999 and a resolver maps that
// address to localhost:7687. The resolver is only invoked for the initial seed address;
// addresses returned in the routing table come directly from the server. On a standalone
// server the routing table echoes back the address from the routing context, which is the
// *original* driver URI address (example.com:9999). Without applying the resolver to those
// routing table entries, the driver then tries to connect to example.com:9999 directly —
// bypassing the resolver and failing even though localhost:7687 is reachable.
//
// The fix: after discovery returns a routing table, every address in it must be passed
// through the IServerAddressResolver before the driver tries to open connections to them.
public class RoutingTableResolverTests
{
    private static RoutingTable TableWith(params string[] addresses)
    {
        var uris = addresses.Select(a => new Uri($"neo4j://{a}/")).ToList();
        return new RoutingTable("neo4j", uris, uris, uris, 30);
    }

    [Fact]
    public void ShouldResolveRouterAddressesAfterDiscovery()
    {
        // Standalone server echoes back the driver URI address (example.com:9999) in its
        // routing table. The resolver must map this to the actual reachable address.
        var table = TableWith("example.com:9999");
        var resolver = new ListAddressResolver(ServerAddress.From("localhost", 7687));

        var resolved = RoutingTableAddressResolver.Resolve(table, resolver);

        resolved.Routers.Should().ContainSingle()
            .Which.Authority.Should().Be(
                "localhost:7687",
                because: "the resolver mapped example.com:9999 to localhost:7687");
    }

    [Fact]
    public void ShouldResolveReaderAndWriterAddressesAfterDiscovery()
    {
        var table = TableWith("example.com:9999");
        var resolver = new ListAddressResolver(ServerAddress.From("localhost", 7687));

        var resolved = RoutingTableAddressResolver.Resolve(table, resolver);

        resolved.Readers.Should().ContainSingle().Which.Authority.Should().Be("localhost:7687");
        resolved.Writers.Should().ContainSingle().Which.Authority.Should().Be("localhost:7687");
    }

    [Fact]
    public void ShouldPreserveAddressesNotCoveredByResolver()
    {
        // A resolver that only maps the seed address; cluster member addresses pass through.
        var table = new RoutingTable(
            "neo4j",
            routers: [new Uri("neo4j://cluster-member-1:7687/")],
            readers: [new Uri("neo4j://cluster-member-2:7687/")],
            writers: [new Uri("neo4j://cluster-member-3:7687/")],
            expireAfterSeconds: 30);

        // Resolver maps the seed (example.com:9999 → localhost:7687) but returns all
        // other addresses unchanged, as a typical user-provided resolver would.
        var resolver = new CustomResolver(
            known: ServerAddress.From("example.com", 9999),
            mappedTo: ServerAddress.From("localhost", 7687));

        var resolved = RoutingTableAddressResolver.Resolve(table, resolver);

        // cluster-member-* addresses are unknown to the resolver and must be preserved.
        resolved.Routers.Should().ContainSingle().Which.Authority.Should().Be("cluster-member-1:7687");
        resolved.Readers.Should().ContainSingle().Which.Authority.Should().Be("cluster-member-2:7687");
        resolved.Writers.Should().ContainSingle().Which.Authority.Should().Be("cluster-member-3:7687");
    }

    private sealed class CustomResolver(ServerAddress known, ServerAddress mappedTo) : IServerAddressResolver
    {
        public ISet<ServerAddress> Resolve(ServerAddress address) =>
            address == known
                ? new HashSet<ServerAddress> { mappedTo }
                : new HashSet<ServerAddress> { address };
    }

    [Fact]
    public void ShouldPreserveSchemeWhenResolving()
    {
        var table = new RoutingTable(
            "neo4j",
            routers: [new Uri("neo4j://example.com:9999/")],
            readers: new List<Uri>(),
            writers: new List<Uri>(),
            expireAfterSeconds: 30);

        var resolver = new ListAddressResolver(ServerAddress.From("localhost", 7687));

        var resolved = RoutingTableAddressResolver.Resolve(table, resolver);

        resolved.Routers.Should().ContainSingle()
            .Which.Scheme.Should().Be("neo4j", because: "the original scheme must be preserved");
    }
}
