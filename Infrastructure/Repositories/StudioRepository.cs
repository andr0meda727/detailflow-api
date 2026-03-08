using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class StudioRepository(ApplicationDbContext context) : Repository<Studio>(context), IStudioRepository
{

}