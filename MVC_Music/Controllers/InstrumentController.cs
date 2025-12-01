using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MVC_Music.CustomControllers;
using MVC_Music.Data;
using MVC_Music.Models;
using MVC_Music.Utilities;
using MVC_Music.ViewModels;

namespace MVC_Music.Controllers
{
    public class InstrumentController : ElephantController
    {
        private readonly MusicContext _context;

        public InstrumentController(MusicContext context)
        {
            _context = context;
        }

        // GET: Instrument
        public async Task<IActionResult> Index(int? page, int? pageSizeID)
        {
            var instruments = _context.Instruments
                .Include(i => i.Musicians)
                .Include(i => i.Plays).ThenInclude(p => p.Musician)
                .OrderBy(i=>i.Name)
                .AsNoTracking();

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Instrument>.CreateAsync(instruments.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Instrument/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instrument = await _context.Instruments
                .Include(i => i.Musicians)
                .Include(i => i.Plays).ThenInclude(p => p.Musician)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (instrument == null)
            {
                return NotFound();
            }

            return View(instrument);
        }

        // GET: Instrument/Create
        public async Task<IActionResult> Create()
        {
            var instrument = new Models.Instrument();
            await PopulatePlaysInstrumentData(instrument);
            return View();
        }

        // POST: Instrument/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name")] Instrument instrument, string[] selectedOptions)
        {
            try
            {
                await UpdatePlaysAsync(selectedOptions, instrument);
                if (ModelState.IsValid)
                {
                    _context.Add(instrument);
                    await _context.SaveChangesAsync();
                    var returnUrl = ViewData["returnURL"]?.ToString();
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    return Redirect(returnUrl);
                }
            }
            catch (RetryLimitExceededException /* dex */)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            await PopulatePlaysInstrumentData(instrument);
            return View(instrument);
        }

        // GET: Instrument/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instrument = await _context.Instruments
                .Include(i => i.Plays).ThenInclude(p => p.Musician)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (instrument == null)
            {
                return NotFound();
            }

            await PopulatePlaysInstrumentData(instrument);
            return View(instrument);
        }

        // POST: Instrument/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            string[] selectedOptions)
        {
            //Go get the Instrument to update
            var instrumentToUpdate = await _context.Instruments
                .Include(i => i.Plays).ThenInclude(p => p.Musician)
                .FirstOrDefaultAsync(p => p.ID == id);

            //Check that you got it or exit with a not found error
            if (instrumentToUpdate == null)
            {
                return NotFound();
            }

            await UpdatePlaysAsync(selectedOptions, instrumentToUpdate);

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Instrument>(instrumentToUpdate, "",
                d => d.Name))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    var returnUrl = ViewData["returnURL"]?.ToString();
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    return Redirect(returnUrl);
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstrumentExists(instrumentToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            await PopulatePlaysInstrumentData(instrumentToUpdate);
            return View(instrumentToUpdate);
        }

        // GET: Instrument/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instrument = await _context.Instruments
                .FirstOrDefaultAsync(m => m.ID == id);
            if (instrument == null)
            {
                return NotFound();
            }

            return View(instrument);
        }

        // POST: Instrument/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var instrument = await _context.Instruments
                .FirstOrDefaultAsync(m => m.ID == id);

            try
            {
                if (instrument != null)
                {
                    _context.Instruments.Remove(instrument);
                }

                await _context.SaveChangesAsync();
                var returnUrl = ViewData["returnURL"]?.ToString();
                if (string.IsNullOrEmpty(returnUrl))
                {
                    return RedirectToAction(nameof(Index));
                }
                return Redirect(returnUrl);
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to Delete Instrument. Remember, you cannot delete a " +
                        "Instrument that is played by any Musician.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem" +
                        " persists see your system administrator.");
                }
            }
            return View(instrument);

        }

        private async Task PopulatePlaysInstrumentData(Instrument instrument)
        {
            var allMusicians = await _context.Musicians 
                .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
                .ToListAsync();

            var assignedMusicianIds = new HashSet<int>(
                instrument.Plays?.Select(ds => ds.MusicianID) ?? Enumerable.Empty<int>()
            );

            var selected = new List<ListOptionVM>();
            var available = new List<ListOptionVM>();

            foreach (var musician in allMusicians)
            {
                var option = new ListOptionVM
                {
                    ID = musician.ID,
                    DisplayText = musician.FormalName
                };

                if (assignedMusicianIds.Contains(musician.ID))
                {
                    selected.Add(option);
                }
                else
                {
                    available.Add(option);
                }
            }

            ViewData["selOpts"] = new MultiSelectList(selected, "ID", "DisplayText");
            ViewData["availOpts"] = new MultiSelectList(available, "ID", "DisplayText");
        }

        private async Task UpdatePlaysAsync(string[] selectedOptions, Instrument instrumentToUpdate)
        {
            if (selectedOptions == null || selectedOptions.Length == 0)
            {
                instrumentToUpdate.Plays = new List<Play>();
                return;
            }

            var selectedIds = selectedOptions.Select(int.Parse).ToHashSet();
            var currentIds = instrumentToUpdate.Plays.Select(ds => ds.MusicianID).ToHashSet();

            var allMusicians = await _context.Musicians.ToListAsync();

            foreach (var musician in allMusicians)
            {
                var musicianId = musician.ID;
                var isSelected = selectedIds.Contains(musicianId);
                var isCurrentlyAssigned = currentIds.Contains(musicianId);

                if (isSelected && !isCurrentlyAssigned)
                {
                    instrumentToUpdate.Plays.Add(new Play
                    {
                        MusicianID = musicianId,
                        InstrumentID = instrumentToUpdate.ID
                    });
                }

                if (!isSelected && isCurrentlyAssigned)
                {
                    var toRemove = instrumentToUpdate.Plays
                        .FirstOrDefault(ds => ds.MusicianID == musicianId);

                    if (toRemove != null)
                    {
                        _context.Plays.Remove(toRemove);
                    }
                }
            }
        }


        private bool InstrumentExists(int id)
        {
            return _context.Instruments.Any(e => e.ID == id);
        }
    }
}
