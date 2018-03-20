using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [Trait("feature", "sni")]
    public class SslStreamSNITest
    {
        [Theory]
        [InlineData("a")]
        [InlineData("test")]
        [InlineData("aaaaaaaaaaaaaaaaaaaabbbbbbbbbbbbbbbbbbbbbbbbbbbbccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc")]
        public void ClientSendsSNIServerReceivesIt(string hostName)
        {
            X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();

            WithVirtualConnection((server, client) =>
            {
                Task clientJob = Task.Run(() => {
                    client.AuthenticateAsClient(hostName);
                });

                SslServerAuthenticationOptions options = DefaultServerOptions();

                bool callbackCalled = false;
                options.ServerCertificateSelectionCallback = (sender, actualHostName) =>
                {
                    callbackCalled = true;
                    Assert.Equal(hostName, actualHostName);
                    return serverCert;
                };

                server.AuthenticateAsServer(options);

                Assert.True(callbackCalled);
                clientJob.Wait();
            },
            (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    Assert.Equal(serverCert, certificate);
                    return true;
                });
        }

        [Fact]
        public void ServerDoesNotKnowTheHostName()
        {
            WithVirtualConnection((server, client) =>
            {
                Task clientJob = Task.Run(() => {
                    Assert.Throws<VirtualNetwork.VirtualNetworkConnectionBroken>(()
                        => client.AuthenticateAsClient("test"));
                });

                bool callbackCalled = false;
                SslServerAuthenticationOptions options = DefaultServerOptions();
                options.ServerCertificateSelectionCallback = (sender, actualHostName) =>
                {
                    callbackCalled = true;
                    return null;
                };

                Assert.Throws<NotSupportedException>(() => {
                    server.AuthenticateAsServer(options);
                });
                server.Dispose();

                Assert.True(callbackCalled);

                clientJob.Wait();
            },
            (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                return true;
            });
        }

        private static SslServerAuthenticationOptions DefaultServerOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ClientCertificateRequired = false,
                EnabledSslProtocols = SslProtocols.Tls,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
            };
        }

        private void WithVirtualConnection(Action<SslStream, SslStream> serverClientConnection, RemoteCertificateValidationCallback clientCertValidate)
        {
            VirtualNetwork vn = new VirtualNetwork();
            using (VirtualNetworkStream serverStream = new VirtualNetworkStream(vn, isServer: true),
                                        clientStream = new VirtualNetworkStream(vn, isServer: false))
            using (SslStream server = new SslStream(serverStream, leaveInnerStreamOpen: false),
                             client = new SslStream(clientStream, leaveInnerStreamOpen: false, clientCertValidate))
            {
                serverClientConnection(server, client);
            }
        }
    }
}
