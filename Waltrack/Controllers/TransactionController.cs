using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Waltrack.Models;

namespace Waltrack.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Transaction
        public async Task<IActionResult> Index()
        {
            PopulateCategories();

            // Get the ID of the currently logged-in user
            string userId = _userManager.GetUserId(User);


            // Filter transactions to only those belonging to the current user
            var applicationDbContext = _context.Transactions
               .Include(t => t.Category)
               .Where(t => t.UserId == userId);
            return View(await applicationDbContext.ToListAsync());
        }

        

        // GET: Transaction/Create
        public IActionResult AddOrEdit(int id=0)
        {
            PopulateCategories();
            if (id == 0)
                return View(new Transaction());
            else
                return View(_context.Transactions.Find(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,Amount,Note,Date")] Transaction transaction)
        {
            // check ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine("Validation errors: " + string.Join(", ", errors));
                PopulateCategories();
                return View(transaction);
            }

            // assign user id
            transaction.UserId = _userManager.GetUserId(User);

            Console.WriteLine("UserId: " + transaction.UserId);
            Console.WriteLine("TransactionId: " + transaction.TransactionId);

            if (transaction.TransactionId == 0)
            {
                _context.Add(transaction);
            }
            else
            {
                _context.Update(transaction);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        // POST: Transaction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [NonAction]
        public void PopulateCategories()
        {
            var CategoryCollection = _context.Categories.ToList();
            Category DefaultCategory = new Category { CategoryId = 0, Title = "Choose a Category" };
            CategoryCollection.Insert(0, DefaultCategory);
            ViewBag.Categories = CategoryCollection;
        }
        
    }
}
