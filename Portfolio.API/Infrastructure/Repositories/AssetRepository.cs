using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Infrastructure.Repositories;

public class AssetRepository(ApplicationDbContext context) : Repository<Asset>(context), IAssetRepository
{
}
