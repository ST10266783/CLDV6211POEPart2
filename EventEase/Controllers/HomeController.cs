using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.VenueCount = await _context.Venues.CountAsync();
                ViewBag.EventCount = await _context.Events.CountAsync();
                ViewBag.BookingCount = await _context.Bookings.CountAsync();
                ViewBag.AvailableVenueCount = await _context.Venues.CountAsync(v => v.IsAvailable);

                var recentBookings = await _context.Bookings
                    .Include(b => b.Venue)
                    .Include(b => b.Event)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(5)
                    .ToListAsync();

                return View(recentBookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard.");
                ViewBag.VenueCount = 0;
                ViewBag.EventCount = 0;
                ViewBag.BookingCount = 0;
                ViewBag.AvailableVenueCount = 0;
                return View(new List<Booking>());
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
