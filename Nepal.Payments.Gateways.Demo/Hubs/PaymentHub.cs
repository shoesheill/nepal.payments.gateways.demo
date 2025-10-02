using Microsoft.AspNetCore.SignalR;

namespace Nepal.Payments.Gateways.Demo.Hubs
{
    public class PaymentHub : Hub
    {
        public async Task JoinPaymentGroup(string prn)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"payment-{prn}");
        }

        public async Task LeavePaymentGroup(string prn)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"payment-{prn}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}