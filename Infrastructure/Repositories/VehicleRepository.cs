using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class VehicleRepository(ApplicationDbContext context) : Repository<Vehicle>(context), IVehicleRepository
{

}