using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace CryptoMarket.UnitTests.Common.Helpers;

public static class MockHubContext
{
    public static (IHubContext<THub> HubContext, IClientProxy ClientProxy) Create<THub>() where THub : Hub
    {
        var hubContext = Substitute.For<IHubContext<THub>>();
        var hubClients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();

        hubContext.Clients.Returns(hubClients);
        hubClients.User(Arg.Any<string>()).Returns(clientProxy);
        hubClients.All.Returns(clientProxy);

        return (hubContext, clientProxy);
    }
}
