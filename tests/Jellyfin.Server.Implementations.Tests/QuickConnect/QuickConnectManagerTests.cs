using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.QuickConnect;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.QuickConnect
{
    public class QuickConnectManagerTests
    {
        private static readonly AuthorizationInfo _quickConnectAuthInfo = new AuthorizationInfo
        {
            Device = "Device",
            DeviceId = "DeviceId",
            Client = "Client",
            Version = "1.0.0"
        };

        private readonly Fixture _fixture;
        private readonly ServerConfiguration _config;
        private readonly QuickConnectManager _quickConnectManager;

        public QuickConnectManagerTests()
        {
            _config = new ServerConfiguration();
            var configManager = new Mock<IServerConfigurationManager>();
            configManager.Setup(x => x.Configuration).Returns(_config);

            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization
            {
                ConfigureMembers = true
            }).Inject(configManager.Object);

            // User object contains circular references.
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _quickConnectManager = _fixture.Create<QuickConnectManager>();
        }

        [Fact]
        public void IsEnabled_QuickConnectUnavailable_False()
            => Assert.False(_quickConnectManager.IsEnabled);

        [Fact]
        public void TryConnect_QuickConnectUnavailable_ThrowsAuthenticationException()
            => Assert.Throws<AuthenticationException>(() => _quickConnectManager.TryConnect(_quickConnectAuthInfo));

        [Fact]
        public void CheckRequestStatus_QuickConnectUnavailable_ThrowsAuthenticationException()
            => Assert.Throws<AuthenticationException>(() => _quickConnectManager.CheckRequestStatus(string.Empty));

        [Fact]
        public void AuthorizeRequest_QuickConnectUnavailable_ThrowsAuthenticationException()
            => Assert.ThrowsAsync<AuthenticationException>(() => _quickConnectManager.AuthorizeRequest(Guid.Empty, string.Empty));

        [Fact]
        public void IsEnabled_QuickConnectAvailable_True()
        {
            _config.QuickConnectAvailable = true;
            Assert.True(_quickConnectManager.IsEnabled);
        }

        [Fact]
        public void CheckRequestStatus_QuickConnectAvailable_Success()
        {
            _config.QuickConnectAvailable = true;
            var res1 = _quickConnectManager.TryConnect(_quickConnectAuthInfo);
            var res2 = _quickConnectManager.CheckRequestStatus(res1.Secret);
            Assert.Equal(res1, res2);
        }

        [Fact]
        public async Task AuthorizeRequest_QuickConnectAvailable_Success()
        {
            _config.QuickConnectAvailable = true;
            var res = _quickConnectManager.TryConnect(_quickConnectAuthInfo);
            Assert.True(await _quickConnectManager.AuthorizeRequest(Guid.Empty, res.Code));
        }
    }
}
