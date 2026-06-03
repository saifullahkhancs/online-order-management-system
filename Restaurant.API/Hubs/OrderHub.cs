using Microsoft.AspNetCore.SignalR;

namespace Restaurant.API.Hubs
{
    public class OrderHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        // Client (agent) calls this after connecting
        public async Task JoinBranchGroup(string branchId)
        {
            string groupName = $"branch-{branchId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            Console.WriteLine($"Branch {branchId} joined group: {groupName}");

            await Clients.Caller.SendAsync("BranchRegistered", groupName);
        }
    }
}
