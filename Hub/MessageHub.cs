using Microsoft.AspNetCore.SignalR;

namespace FoosballApi.Hub
{
    public interface IMessageHub
    {
        Task SendLiveMatches(List<string> message);
    }

    public class MessageHub : Hub<IMessageHub>
{
    // Method to send live match updates to all users in a specific organization group
    public async Task SendLiveMatches(List<string> message)
    {
        string currentOrganisationId = Context.User?.FindFirst("CurrentOrganisationId")?.Value;

        if (!string.IsNullOrEmpty(currentOrganisationId))
        {
            await Clients.Group(currentOrganisationId).SendLiveMatches(message);
        }
    }

    // Method to join a group
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    // Automatically add users to their organization's group when they connect
    public override async Task OnConnectedAsync()
    {
        string currentOrganisationId = Context.User?.FindFirst("CurrentOrganisationId")?.Value;

        if (!string.IsNullOrEmpty(currentOrganisationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, currentOrganisationId);
        }

        await base.OnConnectedAsync();
    }

    // Automatically remove users from their organization's group when they disconnect
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        string currentOrganisationId = Context.User?.FindFirst("CurrentOrganisationId")?.Value;

        if (!string.IsNullOrEmpty(currentOrganisationId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, currentOrganisationId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

}
