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
            // Assuming that clients send their organization ID as part of their SignalR connection context
            string currentOrganisationId = Context.User?.FindFirst("CurrentOrganisationId")?.Value;

            if (!string.IsNullOrEmpty(currentOrganisationId))
            {
                // Broadcast the message to the group representing the current organization
                await Clients.Group(currentOrganisationId).SendLiveMatches(message);
            }
        }

        // Automatically add users to their organization's group when they connect
        public override async Task OnConnectedAsync()
        {
            // Retrieve the organization ID from the user's claims (e.g., from a JWT token)
            string currentOrganisationId = Context.User?.FindFirst("CurrentOrganisationId")?.Value;

            if (!string.IsNullOrEmpty(currentOrganisationId))
            {
                // Add the user to the SignalR group representing their organization
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
                // Remove the user from the SignalR group representing their organization
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, currentOrganisationId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
