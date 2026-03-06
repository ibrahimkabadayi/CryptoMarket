namespace Portfolio.API.Application.Interfaces;

public interface IWalletService
{
    Task<string> CreateWallet(Guid UserId);
}
