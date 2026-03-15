using Microsoft.AspNetCore.Mvc;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Models;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WalletController(IWalletService walletService) : ControllerBase
{
    [HttpPost("deposit_id")]
    public async Task<IActionResult> DepositMoney([FromBody] DepositMoneyRequestId request)
    {
        var result = await walletService.DepositMoney(request.WalletId, request.Amount);

        if(result.StartsWith("Success"))
            return Ok(new {Message = $"Transfered {request.Amount} into your account."});
        else 
            return BadRequest(result);
    }

    [HttpPost("deposit_address")]
    public async Task<IActionResult> DepositMoney([FromBody] DepositMoneyRequestAdress request)
    {
        var result = await walletService.DepositMoney(request.WalletAddress, request.Amount);

        if (result.StartsWith("Success"))
            return Ok(new { Message = $"Transfered {request.Amount} into your account." });
        else
            return BadRequest(result);
    }

    [HttpPost("buy_asset")]
    public async Task<IActionResult> BuyAsset([FromBody] BuyAssetRequest request)
    {
        try
        {
            var result = await walletService.BuyAsset(request.WalletId, request.Symbol, request.BuyingPrice, request.Amount);

            if (result.StartsWith("Success"))
                return Ok(new { Message = result });
            else
                return BadRequest(new { Message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("transfer_asset")]
    public async Task<IActionResult> TransferAsset([FromBody] TransferAssetRequest request)
    {
        var transferDto = new TransferAssetDto 
        { 
            FromWalletId = request.FromWalletId,
            AssetAmount = request.AssetAmount,
            Symbol = request.Symbol,
            TargetWalletAddress = request.TargetWalletAddress
        };

        var result = await walletService.TransferAsset(transferDto);

        if (result.StartsWith("Success"))
            return Ok(new { Message = "Transfer is successfull" });
        else       
            return BadRequest(result);       
    }

    [HttpPost("create_limit_order")]
    public async Task<IActionResult> CreateLimitOrder([FromBody] CreateLimitOrderRequest request)
    {

    }
}
