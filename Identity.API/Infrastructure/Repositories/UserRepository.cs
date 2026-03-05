using Identity.API.Domain.Entities;
using Identity.API.Domain.Interfaces;
using Identity.API.Infrastructure.Context;

namespace Identity.API.Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext context) : Repository<AppUser>(context), IUserRepository
{
}