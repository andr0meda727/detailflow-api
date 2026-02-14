using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<List<Booking>> GetAllWithDetailsAsync();
}
