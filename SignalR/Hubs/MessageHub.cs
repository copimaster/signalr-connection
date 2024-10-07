using Microsoft.AspNetCore.SignalR;

namespace SignalR.Hubs
{
    /// <summary>
    /// SignalR uses a concept called "hubs" to communicate between clients and servers. A hub is a high-level pipeline that allows the client and server to call methods on each other. 
    /// </summary>
    public class MessageHub : Hub<IMessageHubClient>
    {
        public async Task SendOffersToUser(List<string> message)
        {
            await Clients.All.SendOffersToUser(message);
        }
    }
}
