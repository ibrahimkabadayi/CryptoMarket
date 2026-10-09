using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Models;
using System.Security.Claims;

namespace Portfolio.API.Controllers;

[Route("api/wallet")]
[Authorize]
[ApiController]
public class WalletController(IWalletService walletService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("Invalid or broken token.");
        }

        var result = await walletService.GetPortfolioDashboardAsync(userId);
        return Ok(new { result });
    }

    [HttpPost("withdrawals")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawMoneyRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest();
        }

        await walletService.WithdrawMoney(userId, request.Amount);
        return Ok();
    }

    [HttpPost("deposits")]
    public async Task<IActionResult> DepositMoney([FromBody] DepositMoneyRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest();
        }

        await walletService.DepositMoney(userId, request.Amount);

        return Ok(new { Message = $"Transfered {request.Amount} into your account." });
    }

    [HttpPost("assets/{symbol}/purchases")]
    public async Task<IActionResult> BuyAsset(string symbol, [FromBody] BuyAssetRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest();
        }

        await walletService.BuyAsset(userId, symbol, request.BuyingPrice, request.Amount, false);

        return Ok($"Succesfully bought {request.Amount} {symbol}s");

    }

    [HttpPost("assets/{symbol}/transfers")]
    public async Task<IActionResult> TransferAsset(string symbol, [FromBody] TransferAssetRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest();
        }

        var transferDto = new TransferAssetDto
        {
            UserId = userId,
            AssetAmount = request.AssetAmount,
            Symbol = symbol,
            TargetWalletAddress = request.TargetWalletAddress
        };

        await walletService.TransferAsset(transferDto);

        return Ok(new { Message = "Transfer is successfull" });
    }

    [HttpPost("assets/{symbol}/sales")]
    public async Task<IActionResult> SellAsset(string symbol, [FromBody] SellAssetRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest();
        }

        await walletService.SellAsset(userId, symbol, request.Price, request.Amount, false);
        return Ok();
    }
}
