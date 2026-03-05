using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class StudioMemberRepository(ApplicationDbContext context) : Repository<StudioMember>(context), IStudioMemberRepository
{

}