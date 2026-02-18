using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingServicesRepository(ApplicationDbContext context)
    : Repository<BookingServices>(context), IBookingServicesRepository
{

}