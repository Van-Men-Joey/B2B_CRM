using Customer_Relationship_Management.Services.Interfaces;
using Customer_Relationship_Management.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IContractService _contractService;

        public PaymentWebhookController(IContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook([FromBody] WebhookDto data)
        {
            // API này sẽ được Casso/SePay gọi vào
            if (data == null) return BadRequest();

            try
            {
                await _contractService.ProcessPaymentWebhookAsync(data);
                return Ok(new { success = true });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}