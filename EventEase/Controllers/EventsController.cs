using EventEase.Data;
using EventEase.Models;
using EventEase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<EventsController> _logger;

        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB

        public EventsController(
            ApplicationDbContext context,
            IBlobStorageService blobStorageService,
            ILogger<EventsController> logger)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        // GET: Events
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .OrderBy(e => e.StartDate)
                .ToListAsync();
            return View(events);
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();

            return View(ev);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            return View(new Event
            {
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(8)
            });
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event ev, IFormFile? imageFile)
        {
            ModelState.Remove(nameof(Event.ImageUrl));
            ModelState.Remove(nameof(Event.ImageFile));
            ModelState.Remove(nameof(Event.Bookings));

            // Custom date range validation
            if (ev.EndDate <= ev.StartDate)
                ModelState.AddModelError(nameof(Event.EndDate), "End date must be after the start date.");

            if (!ModelState.IsValid) return View(ev);

            if (imageFile != null && imageFile.Length > 0)
            {
                var (valid, error) = ValidateImage(imageFile);
                if (!valid)
                {
                    ModelState.AddModelError(nameof(Event.ImageFile), error!);
                    return View(ev);
                }

                try
                {
                    ev.ImageUrl = await _blobStorageService.UploadImageAsync(imageFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Blob upload failed for new event '{Name}'", ev.Name);
                    TempData["Warning"] = "Image could not be saved to storage — a placeholder has been used instead. Ensure Azurite is running.";
                    ev.ImageUrl = "https://placehold.co/600x400/7c3aed/white?text=Event+Image";
                }
            }
            else
            {
                ev.ImageUrl = "https://placehold.co/600x400/7c3aed/white?text=Event+Image";
            }

            try
            {
                _context.Add(ev);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Event \"{ev.Name}\" was created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving event");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(ev);
            }
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            return View(ev);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event ev, IFormFile? imageFile)
        {
            if (id != ev.EventId) return NotFound();

            ModelState.Remove(nameof(Event.ImageFile));
            ModelState.Remove(nameof(Event.Bookings));

            if (ev.EndDate <= ev.StartDate)
                ModelState.AddModelError(nameof(Event.EndDate), "End date must be after the start date.");

            if (!ModelState.IsValid) return View(ev);

            if (imageFile != null && imageFile.Length > 0)
            {
                var (valid, error) = ValidateImage(imageFile);
                if (!valid)
                {
                    ModelState.AddModelError(nameof(Event.ImageFile), error!);
                    return View(ev);
                }

                try
                {
                    if (!string.IsNullOrEmpty(ev.ImageUrl) && IsBlobUrl(ev.ImageUrl))
                        await _blobStorageService.DeleteImageAsync(ev.ImageUrl);

                    ev.ImageUrl = await _blobStorageService.UploadImageAsync(imageFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Blob upload failed editing event {EventId}", id);
                    TempData["Warning"] = "Image could not be updated in storage — existing image kept. Ensure Azurite is running.";
                }
            }

            try
            {
                _context.Update(ev);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Event \"{ev.Name}\" was updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(ev.EventId)) return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(ev);
            }
        }

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound();

            ViewBag.HasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);

            return View(ev);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (await _context.Bookings.AnyAsync(b => b.EventId == id))
            {
                TempData["Error"] = "Cannot delete this event — it has existing bookings. Please remove all associated bookings first.";
                return RedirectToAction(nameof(Index));
            }

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            try
            {
                if (!string.IsNullOrEmpty(ev.ImageUrl) && IsBlobUrl(ev.ImageUrl))
                    await _blobStorageService.DeleteImageAsync(ev.ImageUrl);

                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Event \"{ev.Name}\" was deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", id);
                TempData["Error"] = "An error occurred while deleting the event. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private bool EventExists(int id) =>
            _context.Events.Any(e => e.EventId == id);

        private static bool IsBlobUrl(string url) =>
            url.Contains("127.0.0.1:10000") || url.Contains("blob.core.windows.net");

        private static (bool IsValid, string? Error) ValidateImage(IFormFile file)
        {
            if (file.Length > MaxImageSize)
                return (false, "Image file size must not exceed 5 MB.");

            if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
                return (false, "Only JPEG, PNG, GIF, and WebP images are allowed.");

            return (true, null);
        }
    }
}
