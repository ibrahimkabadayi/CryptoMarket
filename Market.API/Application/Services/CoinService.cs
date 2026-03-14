using AutoMapper;
using Market.API.Application.DTOs;
using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using MassTransit;
using Shared.Messages;

namespace Market.API.Application.Services;

public class CoinService(ICoinRepository coinRepository, IMapper mapper, IPublishEndpoint publishEndpoint, IRedisCacheService cacheService) : ICoinService
{
    public async Task<string> AddCoin(string Name, string Symbol, double Price, double MarketCap)
    {
        try
        {
            var coin = new Coin
            {
                Symbol = Symbol,
                Name = Name,
                CurrentPrice = Price,
                MarketCap = MarketCap
            };

            await coinRepository.AddAsync(coin);
            return "Success";
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
            return "Error: " + ex.Message;
        }
    }

    public void BuyCoin(BuyCoinDto buyCoinDto)
    {
        try
        {
            publishEndpoint.Publish(new BuyCoinEvent { BuyPrice = buyCoinDto.BuyPrice, Symbol = buyCoinDto.Symbol, UserId = buyCoinDto.UserId, BuyAmount = buyCoinDto.BuyAmount });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }

    public async Task<List<CoinDto>> GetAllCoins()
    {
        var coinsKey = "market:coins";

        var coins = await cacheService.GetAsync<List<Coin>>(coinsKey);

        if (coins is not null)
        {
            return mapper.Map<List<CoinDto>>(coins);
        }

        var allCoins = await coinRepository.GetAllAsync();

        await cacheService.SetAsync(coinsKey, allCoins, TimeSpan.FromSeconds(5));

        return mapper.Map<List<CoinDto>>(allCoins);
    }

    public async Task<CoinDto> GetCoinBySymbol(string Symbol)
    {
        var coin = await coinRepository.GetCoinAsync(Symbol);
        return mapper.Map<CoinDto>(coin);
    }
}
