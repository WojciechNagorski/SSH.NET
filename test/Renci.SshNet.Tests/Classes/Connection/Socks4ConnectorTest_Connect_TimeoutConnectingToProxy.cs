﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Renci.SshNet.Common;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

namespace Renci.SshNet.Tests.Classes.Connection
{
    [TestClass]
    public class Socks4ConnectorTest_Connect_TimeoutConnectingToProxy : Socks4ConnectorTestBase
    {
        private ConnectionInfo _connectionInfo;
        private SshOperationTimeoutException _actualException;
        private Socket _clientSocket;
        private Stopwatch _stopWatch;

        protected override void SetupData()
        {
            base.SetupData();

            var random = new Random();

            _connectionInfo = CreateConnectionInfo("proxyUser", "proxyPwd");
            _connectionInfo.Timeout = TimeSpan.FromMilliseconds(random.Next(50, 200));
            _stopWatch = new Stopwatch();
            _clientSocket = SocketFactory.Create(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _actualException = null;
        }

        protected override void SetupMocks()
        {
            _ = SocketFactoryMock.Setup(p => p.Create(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                                 .Returns(_clientSocket);
        }

        protected override void TearDown()
        {
            base.TearDown();

            _clientSocket?.Dispose();
        }

        protected override void Act()
        {
            _stopWatch.Start();

            try
            {
                _ = Connector.Connect(_connectionInfo);
                Assert.Fail();
            }
            catch (SshOperationTimeoutException ex)
            {
                _actualException = ex;
            }
            finally
            {
                _stopWatch.Stop();
            }
        }

        [TestMethod]
        public void ConnectShouldHaveThrownSshOperationTimeoutException()
        {
            Assert.IsNull(_actualException.InnerException);
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, "Connection failed to establish within {0} milliseconds.", _connectionInfo.Timeout.TotalMilliseconds), _actualException.Message);
        }

        [TestMethod]
        public void ConnectShouldHaveRespectedTimeout()
        {
            var errorText = string.Format("Elapsed: {0}, Timeout: {1}",
                                          _stopWatch.ElapsedMilliseconds,
                                          _connectionInfo.Timeout.TotalMilliseconds);

            // Compare elapsed time with configured timeout, allowing for a margin of error
            Assert.IsTrue(_stopWatch.ElapsedMilliseconds >= _connectionInfo.Timeout.TotalMilliseconds - 10, errorText);
            Assert.IsTrue(_stopWatch.ElapsedMilliseconds < _connectionInfo.Timeout.TotalMilliseconds + 100, errorText);
        }

        [TestMethod]
        public void ClientSocketShouldHaveBeenDisposed()
        {
            try
            {
                _ = _clientSocket.Receive(new byte[0]);
                Assert.Fail();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        [TestMethod]
        public void CreateOnSocketFactoryShouldHaveBeenInvokedOnce()
        {
            SocketFactoryMock.Verify(p => p.Create(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                                     Times.Once());
        }
    }
}
