using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(ApplicationDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Bookings  — consolidated view with search
        public async Task<IActionResult> Index(string? searchTerm)
        {
            try
            {
                var query = _context.Bookings
                    .Include(b => b.Venue)
                    .Include(b => b.Event)
                    .AsQueryable();

                // Search by BookingReference (e.g. "BK000001" or "1") or Event Name or Customer Name
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var trimmed = searchTerm.Trim();

                    // Attempt to resolve a numeric BookingId from the search string
                    int? searchId = null;
                    if (trimmed.StartsWith("BK", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(trimmed[2..].TrimStart('0'), out int parsed))
                            searchId = parsed;
                    }
                    else if (int.TryParse(trimmed.TrimStart('0'), out int parsed))
                    {
                        searchId = parsed;
                    }

                    if (searchId.HasValue)
                    {
                        query = query.Where(b =>
                            b.BookingId == searchId.Value ||
                            b.Event!.Name.Contains(trimmed) ||
                            b.CustomerName.Contains(trimmed));
                    }
                    else
                    {
                        query = query.Where(b =>
                            b.Event!.Name.Contains(trimmed) ||
                            b.CustomerName.Contains(trimmed) ||
                            b.CustomerEmail.Contains(trimmed));
                    }
                }

                var bookings = await query
                    .OrderByDescending(b => b.BookingDate)
                    .Select(b => new BookingDetailsViewModel
                    {
                        BookingId       = b.BookingId,
                        BookingDate     = b.BookingDate,
                        StartDate       = b.StartDate,
                        EndDate         = b.EndDate,
                        CustomerName    = b.CustomerName,
                        CustomerEmail   = b.CustomerEmail,
                        CustomerPhone   = b.CustomerPhone,
                        Notes           = b.Notes,
                        VenueId         = b.VenueId,
                        VenueName       = b.Venue!.Name,
                        VenueLocation   = b.Venue.Location,
                        VenueCapacity   = b.Venue.Capacity,
                        VenueImageUrl   = b.Venue.ImageUrl,
                        EventId         = b.EventId,
                        EventName       = b.Event!.Name,
                        EventDescription = b.Event.Description,
                        EventStartDate  = b.Event.StartDate,
                        EventEndDate    = b.Event.EndDate,
                        EventImageUrl   = b.Event.ImageUrl
                    })
                    .ToListAsync();

                ViewBag.SearchTerm = searchTerm;
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bookings list");
                TempData["Error"] = "An error occurred while loading bookings.";
                return View(new List<BookingDetailsViewModel>());
            }
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // GET: Bookings/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropDownsAsync();
            return View(new Booking
            {
                StartDate   = DateTime.Today,
                EndDate     = DateTime.Today.AddDays(1),
                BookingDate = DateTime.Now
            });
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            ModelState.Remove(nameof(Booking.Venue));
            ModelState.Remove(nameof(Booking.Event));

            // Date range validation
            if (booking.EndDate <= booking.StartDate)
                ModelState.AddModelError(nameof(Booking.EndDate), "End date must be after the start date.");

            // Venue availability check
            if (booking.VenueId > 0)
            {
                var venue = await _context.Venues.FindAsync(booking.VenueId);
                if (venue != null && !venue.IsAvailable)
                    ModelState.AddModelError(nameof(Booking.VenueId), "The selected venue is currently not available for booking.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropDownsAsync(booking.VenueId, booking.EventId);
                return View(booking);
            }

            // Double-booking check — overlap: newStart < existEnd AND newEnd > existStart
            var conflict = await _context.Bookings
                .Where(b => b.VenueId == booking.VenueId
                         && b.StartDate < booking.EndDate
                         && b.EndDate   > booking.StartDate)
                .Include(b => b.Event)
                .FirstOrDefaultAsync();

            if (conflict != null)
            {
                var msg = $"Booking conflict: \"{conflict.Event?.Name ?? "another booking"}\" already occupies this venue from " +
                          $"{conflict.StartDate:dd MMM yyyy} to {conflict.EndDate:dd MMM yyyy}.";
                ModelState.AddModelError(string.Empty, msg);
                TempData["Error"] = msg;
                await PopulateDropDownsAsync(booking.VenueId, booking.EventId);
                return View(booking);
            }

            try
            {
                booking.BookingDate = DateTime.Now;
                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Booking {booking.BookingReference} was created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await PopulateDropDownsAsync(booking.VenueId, booking.EventId);
                return View(booking);
            }
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            await PopulateDropDownsAsync(booking.VenueId, booking.EventId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.BookingId) return NotFound();

            ModelState.Remove(nameof(Booking.Venue));
            ModelState.Remove(nameof(Booking.Event));

            if (booking.EndDate <= booking.StartDate)
                ModelState.AddModelError(nameof(Booking.EndDate), "End date must be after the start date.");

            if (!ModelState.IsValid)
            {
                await PopulateDropDownsAsync(booking.VenueId, booking.EventId);
                return View(booking);
            }

            // Double-booking check — exclude the current booking from the check
            var conflict = await _context.Bookings
                .Where(b => b.VenueId    == booking.VenueId
                         && b.BookingId  != booking.BookingId
                         && b.StartDate  < booking.EndDate
                         && b.EndDate    > booking.StartDate)
                .Include(b => b.Event)
                .FirstOrDefaultAsync();

            if (conflict != null)
            {
                var msg = $"Booking conflict: \"{conflict.Event?.Name ?? "another booking"}\" already occupies this venue from " +
                          $"{conflict.StartDate:dd MMM yyyy} to {conflict.EndDate:dd MMM yyyy}.";
                ModelState.AddModelError(string.Empty, msg);
                TempData["Error"] = msg;
                await PopulateDropDownsAsync(booking.VenueId, booking.EventId);
                return View(booking);
            }

            try
            {
                _context.Update(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Booking {booking.BookingReference} was updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(booking.BookingId)) return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking {BookingId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await PopulateDropDownsAsync(booking.VenueId, booking.EventId);
                return View(booking);
            }
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            try
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Booking BK{id:D6} has been cancelled and removed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting booking {BookingId}", id);
                TempData["Error"] = "An error occurred while deleting the booking. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private bool BookingExists(int id) =>
            _context.Bookings.Any(b => b.BookingId == id);

        private async Task PopulateDropDownsAsync(int selectedVenueId = 0, int selectedEventId = 0)
        {
            var venues = await _context.Venues
                .OrderBy(v => v.Name)
                .Select(v => new SelectListItem
                {
                    Value    = v.VenueId.ToString(),
                    Text     = $"{v.Name} — {v.Location} (Cap: {v.Capacity})",
                    Selected = v.VenueId == selectedVenueId,
                    Disabled = !v.IsAvailable
                })
                .ToListAsync();

            var events = await _context.Events
                .OrderBy(e => e.StartDate)
                .Select(e => new SelectListItem
                {
                    Value    = e.EventId.ToString(),
                    Text     = $"{e.Name}  [{e.StartDate:dd MMM yyyy} – {e.EndDate:dd MMM yyyy}]",
                    Selected = e.EventId == selectedEventId
                })
                .ToListAsync();

            ViewBag.Venues = venues;
            ViewBag.Events = events;
        }
    }
}
