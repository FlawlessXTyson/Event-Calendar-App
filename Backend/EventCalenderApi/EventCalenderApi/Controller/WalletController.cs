using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Wallet;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        public WalletController(IWalletService walletService) => _walletService = walletService;

        // GET /api/Wallet — get or create wallet for current user
        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _walletService.GetOrCreateWalletAsync(userId));
        }

        // POST /api/Wallet/add — add money to wallet
        [HttpPost("add")]
        public async Task<IActionResult> AddMoney([FromBody] AddMoneyRequestDTO request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _walletService.AddMoneyAsync(userId, request));
        }

        // GET /api/Wallet/transactions — transaction history
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _walletService.GetTransactionsAsync(userId));
        }

        // POST /api/Wallet/pay — pay for event using wallet
        [HttpPost("pay")]
        public async Task<IActionResult> PayWithWallet([FromBody] WalletPaymentRequestDTO request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _walletService.PayWithWalletAsync(userId, request));
        }
    }
}
