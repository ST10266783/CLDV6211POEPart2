using EventEase.Data;
using EventEase.Models;
using EventEase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<VenuesController> _logger;

        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB

        public VenuesController(
            ApplicationDbContext context,
            IBlobStorageService blobStorageService,
            ILogger<VenuesController> logger)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        // GET: Venues
        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues
                .OrderBy(v => v.Name)
                .ToListAsync();
            return View(venues);
        }

        // GET: Venues/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .Include(v => v.Bookings)
                    .ThenInclude(b => b.Event)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();

            return View(venue);
        }

        // GET: Venues/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Venues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue, IFormFile? imageFile)
        {
            // Remove navigation/computed properties from validation
            ModelState.Remove(nameof(Venue.ImageUrl));
            ModelState.Remove(nameof(Venue.ImageFile));
            ModelState.Remove(nameof(Venue.Bookings));

            if (!ModelState.IsValid) return View(venue);

            // Validate and upload image
            if (imageFile != null && imageFile.Length > 0)
            {
                var (valid, error) = ValidateImage(imageFile);
                if (!valid)
                {
                    ModelState.AddModelError(nameof(Venue.ImageFile), error!);
                    return View(venue);
                }

                try
                {
                    venue.ImageUrl = await _blobStorageService.UploadImageAsync(imageFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Blob upload failed for new venue '{Name}'", venue.Name);
                    TempData["Warning"] = "Image could not be saved to storage — a placeholder has been used instead. Ensure Azurite is running.";
                    venue.ImageUrl = "https://placehold.co/600x400/4f46e5/white?text=Venue+Image";
                }
            }
            else
            {
                venue.ImageUrl = "https://placehold.co/600x400/4f46e5/white?text=Venue+Image";
            }

            try
            {
                _context.Add(venue);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Venue \"{venue.Name}\" was created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving venue");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(venue);
            }
        }

        // GET: Venues/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        // POST: Venues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venue, IFormFile? imageFile)
        {
            if (id != venue.VenueId) return NotFound();

            ModelState.Remove(nameof(Venue.ImageFile));
            ModelState.Remove(nameof(Venue.Bookings));

            if (!ModelState.IsValid) return View(venue);

            // Handle image update
            if (imageFile != null && imageFile.Length > 0)
            {
                var (valid, error) = ValidateImage(imageFile);
                if (!valid)
                {
                    ModelState.AddModelError(nameof(Venue.ImageFile), error!);
                    return View(venue);
                }

                try
                {
                    // Delete old blob image if it came from Azurite
                    if (!string.IsNullOrEmpty(venue.ImageUrl) && IsBlobUrl(venue.ImageUrl))
                        await _blobStorageService.DeleteImageAsync(venue.ImageUrl);

                    venue.ImageUrl = await _blobStorageService.UploadImageAsync(imageFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Blob upload failed editing venue {VenueId}", id);
                    TempData["Warning"] = "Image could not be updated in storage — existing image kept. Ensure Azurite is running.";
                }
            }
            // If no new file, ImageUrl keeps the value from the hidden field in the form

            try
            {
                _context.Update(venue);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Venue \"{venue.Name}\" was updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VenueExists(venue.VenueId))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating venue {VenueId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(venue);
            }
        }

        // GET: Venues/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.VenueId == id);
            if (venue == null) return NotFound();

            ViewBag.HasBookings = await _context.Bookings.AnyAsync(b => b.VenueId == id);

            return View(venue);
        }

        // POST: Venues/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Guard: refuse delete if bookings exist
            if (await _context.Bookings.AnyAsync(b => b.VenueId == id))
            {
                TempData["Error"] = "Cannot delete this venue — it has existing bookings. Please remove all associated bookings first.";
                return RedirectToAction(nameof(Index));
            }

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            try
            {
                // Clean up blob storage image
                if (!string.IsNullOrEmpty(venue.ImageUrl) && IsBlobUrl(venue.ImageUrl))
                    await _blobStorageService.DeleteImageAsync(venue.ImageUrl);

                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Venue \"{venue.Name}\" was deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting venue {VenueId}", id);
                TempData["Error"] = "An error occurred while deleting the venue. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private bool VenueExists(int id) =>
            _context.Venues.Any(v => v.VenueId == id);

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
