using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Models;

namespace Portfolio.API.Controllers;

[Route("api/wallets")]
[Authorize]
[ApiController]
public class WalletController(IWalletService walletService) : ControllerBase
{
    [HttpPost("{walletId}/transaction")]
    public async Task<IActionResult> DepositMoney(Guid walletId, [FromBody] DepositMoneyRequest request)
    {
        await walletService.DepositMoney(walletId, request.Amount);
       
        return Ok(new {Message = $"Transfered {request.Amount} into your account."});
    }

    [HttpPost("{walletId}/assets/{symbol}")]
    public async Task<IActionResult> BuyAsset(Guid walletId, string symbol, [FromBody] BuyAssetRequest request)
    {
        await walletService.BuyAsset(walletId, symbol, request.BuyingPrice, request.Amount, false);

        return Ok($"Succesfully bought {request.Amount} {symbol}s");
        
    }

    [HttpPost("{walletId}/transfers/{symbol}")]
    public async Task<IActionResult> TransferAsset(Guid walletId, string symbol, [FromBody] TransferAssetRequest request)
    {
        var transferDto = new TransferAssetDto 
        { 
            FromWalletId = walletId,
            AssetAmount = request.AssetAmount,
            Symbol = symbol,
            TargetWalletAddress = request.TargetWalletAddress
        };

        await walletService.TransferAsset(transferDto);
      
        return Ok(new { Message = "Transfer is successfull" });            
    }

    [HttpPatch("{walletId}")]
    public async Task<IActionResult> WithdrawMoney(Guid walletId, [FromBody] WithdrawMoneyRequest request)
    {
        await walletService.WithdrawMoney(walletId, request.Amount);
        return Ok();
    }

    [HttpPatch("{walletId}/assets/{symbol}")]
    public async Task<IActionResult> SellAsset(Guid walletId, string symbol, [FromBody] SellAssetRequest request)
    {
        await walletService.SellAsset(walletId, symbol, request.Price, request.Amount, false);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized("Invalid or broken token.");
        }

        var result = await walletService.GetPortfolioDashboardAsync(userId);
        return Ok(new { result });
    }
}
