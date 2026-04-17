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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Tests.Connector;
using Xunit;

namespace Neo4j.Driver.Tests.Routing;

public class RoutingContextAddressTests
{
    // Regression test for DRIVERS-199:
    // When a custom resolver maps the driver URI to a different address, the routing context
    // sent to the server must use the *resolved* address — not the original driver URI.
    // The server echoes the routing context's "address" field back in the routing table.
    // If it contains the original (unresolved) address, the driver then tries to connect
    // to that address from the routing table without going through the resolver, causing
    // connection failures even when the resolved address is reachable.
    [Fact]
    public void RoutingContextAddressShouldReflectResolvedConnectionUri_NotOriginalDriverUri()
    {
        // Driver is configured to connect to example.com:9999 ...
        var driverUri = new Uri("neo4j://example.com:9999");
        var driverContext = new DriverContext(driverUri, AuthTokenManagers.None, Config.Builder.Build());

        // ... but a resolver maps example.com:9999 → localhost:7687.
        // The SocketConnection is therefore created with the resolved URI.
        var connectionUri = new Uri("neo4j://localhost:7687/");
        var connection = SocketConnectionTests.NewSocketConnection(
            server: new ServerInfo(connectionUri),
            context: driverContext);

        // The routing context's "address" must reflect the actual connection address,
        // because the server uses it to construct the routing table entries it returns.
        connection.RoutingContext["address"].Should().Be(
            "localhost:7687",
            because: "the server echoes this address back in the routing table; " +
                     "using the original driver URI causes the driver to attempt connections " +
                     "to the unresolved address from the routing table");
    }
}
