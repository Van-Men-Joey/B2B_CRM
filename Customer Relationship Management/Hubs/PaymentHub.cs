using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Hubs
{
    public class PaymentHub : Hub
    {
        // Hàm này để client (JS) gọi lên nếu cần (hiện tại chưa cần, chỉ cần Server gọi xuống)
        public async Task SendPaymentUpdate(string contractId, string status)
        {
            await Clients.All.SendAsync("ReceivePaymentUpdate", contractId, status);
        }
    }
}