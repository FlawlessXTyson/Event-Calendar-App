using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Wallet;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IWalletService
    {
        Task<WalletDTO> GetOrCreateWalletAsync(int userId);
        Task<WalletDTO> AddMoneyAsync(int userId, AddMoneyRequestDTO request);
        Task<PaymentResponseDTO> PayWithWalletAsync(int userId, WalletPaymentRequestDTO request);
        Task<IEnumerable<WalletTransactionDTO>> GetTransactionsAsync(int userId);
        Task CreditAsync(int userId, float amount, string source, string description);
        Task DebitAsync(int userId, float amount, string source, string description);
        Task DebitStrictAsync(int userId, float amount, string source, string description);
    }
}
