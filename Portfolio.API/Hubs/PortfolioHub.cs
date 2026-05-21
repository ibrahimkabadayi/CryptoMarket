namespace Portfolio.API.Hubs;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class PortfolioHub : Hub
{
    public override Task OnConnectedAsync()
    {
        // SignalR'ın eşleştirme için baz aldığı ana ID değeri
        var userId = Context.UserIdentifier;

        Console.WriteLine("\n--- YENİ SİGNALR BAĞLANTISI ---");
        Console.WriteLine($"SignalR'ın Gördüğü UserIdentifier: {userId ?? "NULL GELDİ!"}");

        // Token içindeki tüm claimleri yazdıralım ki neye dönüştüklerini görelim
        if (Context.User?.Claims != null)
        {
            foreach (var claim in Context.User.Claims)
            {
                Console.WriteLine($"Claim Tipi: {claim.Type} | Değer: {claim.Value}");
            }
        }
        Console.WriteLine("-------------------------------\n");

        return base.OnConnectedAsync();
    }
}
