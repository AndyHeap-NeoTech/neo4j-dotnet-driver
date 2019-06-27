// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Tests;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Reactive.Testing.ReactiveAssert;
using static Neo4j.Driver.Tests.Assertions;

namespace Neo4j.Driver.Reactive.Internal
{
    public static class InternalRxSessionTests
    {
        public class Run : AbstractRxTest
        {
            [Fact]
            public void ShouldReturnInternalRxResult()
            {
                var rxSession = new InternalRxSession(Mock.Of<IInternalAsyncSession>(), Mock.Of<IRxRetryLogic>());

                rxSession.Run("RETURN 1").Should().BeOfType<InternalRxResult>();
            }

            [Fact]
            public void ShouldInvokeSessionRunAsyncOnKeys()
            {
                VerifyLazyRunAsync(r => r.Keys().WaitForCompletion());
            }

            [Fact]
            public void ShouldInvokeSessionRunAsyncOnRecords()
            {
                VerifyLazyRunAsync(r => r.Records().WaitForCompletion());
            }

            [Fact]
            public void ShouldInvokeSessionRunAsyncOnSummary()
            {
                VerifyLazyRunAsync(r => r.Summary().WaitForCompletion());
            }

            [Fact]
            public void ShouldInvokeSessionRunAsyncOnlyOnce()
            {
                VerifyLazyRunAsync(r =>
                {
                    r.Keys().WaitForCompletion();
                    r.Records().WaitForCompletion();
                    r.Summary().WaitForCompletion();
                });
            }

            private static void VerifyLazyRunAsync(Action<IRxResult> action)
            {
                var asyncSession = new Mock<IInternalAsyncSession>();
                asyncSession.Setup(x => x.RunAsync(It.IsAny<Statement>(), It.IsAny<TransactionConfig>()))
                    .ReturnsAsync(new ListBasedRecordCursor(new[] {"x"}, Enumerable.Empty<IRecord>,
                        Mock.Of<IResultSummary>));
                var session = new InternalRxSession(asyncSession.Object, Mock.Of<IRxRetryLogic>());
                var result = session.Run("RETURN 1");

                asyncSession.Verify(
                    x => x.RunAsync(It.IsAny<Statement>(), It.IsAny<TransactionConfig>()), Times.Never);

                action(result);

                asyncSession.Verify(
                    x => x.RunAsync(It.IsAny<Statement>(), It.IsAny<TransactionConfig>()), Times.Once);
            }
        }

        public class BeginTransaction : AbstractRxTest
        {
            [Fact]
            public void ShouldReturnObservable()
            {
                var session = new Mock<IInternalAsyncSession>();
                session.Setup(x => x.BeginTransactionAsync(It.IsAny<TransactionConfig>()))
                    .ReturnsAsync(Mock.Of<IAsyncTransaction>());

                var rxSession = new InternalRxSession(session.Object, Mock.Of<IRxRetryLogic>());

                rxSession.BeginTransaction().WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, Matches<IRxTransaction>(t => t.Should().BeOfType<InternalRxTransaction>())),
                        OnCompleted<IRxTransaction>(0));
                session.Verify(x => x.BeginTransactionAsync(It.IsAny<TransactionConfig>()), Times.Once);
            }
        }

        public class TransactionFunctions : AbstractRxTest
        {
            [Theory]
            [MemberData(nameof(AccessModes))]
            public void ShouldBeginTransactionAndCommit(AccessMode mode)
            {
                var rxSession = CreateSession(mode, TransactionConfig.Empty, out var session, out var txc);

                rxSession
                    .RunTransaction(mode, t => Observable.Return(1), TransactionConfig.Empty)
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, 1),
                        OnCompleted<int>(0));

                session.Verify(x => x.BeginTransactionAsync(mode, TransactionConfig.Empty), Times.Once);
                session.VerifyNoOtherCalls();

                txc.Verify(x => x.CommitAsync(), Times.Once);
                txc.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(AccessModes))]
            public void ShouldBeginTransactionAndRollback(AccessMode mode)
            {
                var error = new ClientException();
                var rxSession = CreateSession(mode, TransactionConfig.Empty, out var session, out var txc);

                rxSession
                    .RunTransaction(mode, t => Observable.Throw<int>(error), TransactionConfig.Empty)
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<int>(0, error));

                session.Verify(x => x.BeginTransactionAsync(mode, TransactionConfig.Empty), Times.Once);
                session.VerifyNoOtherCalls();

                txc.Verify(x => x.RollbackAsync(), Times.Once);
                txc.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(AccessModes))]
            public void ShouldBeginTransactionAndRollbackOnSynchronousException(AccessMode mode)
            {
                var error = new ClientException();
                var rxSession = CreateSession(mode, TransactionConfig.Empty, out var session, out var txc);

                rxSession
                    .RunTransaction<int>(mode, t => throw error, TransactionConfig.Empty)
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<int>(0, error));

                session.Verify(x => x.BeginTransactionAsync(mode, TransactionConfig.Empty), Times.Once);
                session.VerifyNoOtherCalls();

                txc.Verify(x => x.RollbackAsync(), Times.Once);
                txc.VerifyNoOtherCalls();
            }

            private static InternalRxSession CreateSession(AccessMode mode, TransactionConfig txConfig,
                out Mock<IInternalAsyncSession> session, out Mock<IAsyncTransaction> txc)
            {
                txc = new Mock<IAsyncTransaction>();
                txc.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
                txc.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);

                session = new Mock<IInternalAsyncSession>();
                session.Setup(x => x.BeginTransactionAsync(mode, txConfig)).ReturnsAsync(txc.Object);

                return new InternalRxSession(session.Object, new SingleRetryLogic());
            }

            public static TheoryData<AccessMode> AccessModes()
            {
                return new TheoryData<AccessMode>
                {
                    AccessMode.Read,
                    AccessMode.Write
                };
            }


            private class SingleRetryLogic : IRxRetryLogic
            {
                public IObservable<T> Retry<T>(IObservable<T> work)
                {
                    return work;
                }
            }
        }

        public class Close : AbstractRxTest
        {
            [Fact]
            public void ShouldInvokeSessionCloseAsync()
            {
                var asyncSession = new Mock<IInternalAsyncSession>();
                var rxSession = new InternalRxSession(asyncSession.Object, Mock.Of<IRxRetryLogic>());

                var close = rxSession.Close<Unit>();

                asyncSession.Verify(x => x.CloseAsync(), Times.Never);

                close.WaitForCompletion();

                asyncSession.Verify(x => x.CloseAsync(), Times.Once);
            }
        }
    }
}