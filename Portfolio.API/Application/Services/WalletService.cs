using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;

namespace Portfolio.API.Application.Services;

public class WalletService(IWalletRepository walletRepository) : IWalletService
{
    public async Task<string> CreateWallet(Guid UserId)
    {
        var generatedAddress = "0x" + Guid.NewGuid().ToString("N");

        var newWallet = new Wallet
        {
            UserId = UserId,
            Address = generatedAddress,
        };

        await walletRepository.AddAsync(newWallet);

        return generatedAddress;
    }
}
