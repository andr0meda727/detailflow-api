using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository(ApplicationDbContext context) 
    : Repository<Booking>(context), IBookingRepository
{
    public async Task<List<Booking>> GetAllWithDetailsAsync()
    {
        return await DbSet
            .Include(b => b.Vehicle)
            .Include(b => b.Client)
            .Include(b => b.Status)
            .ToListAsync();
    }
}