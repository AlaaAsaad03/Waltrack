using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.HeatMap;
using Syncfusion.EJ2.HeatMap;
using System.Globalization;
using Waltrack.Models;


namespace Waltrack.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;

        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {

            // Get logged-in user's ID
            var userId = _userManager.GetUserId(User);

            // Step 1: Base query
            var query = _context.Transactions
                .Include(t => t.Category)
                 .Where(t => t.UserId == userId)
                 .AsQueryable();

            // Step 2: Apply date range filter
            if (startDate.HasValue && endDate.HasValue)
            {
                if (startDate > endDate)
                {
                    ModelState.AddModelError("", "Start Date cannot be after End Date.");
                    return View(new List<Transaction>());
                }

                DateTime start = startDate.Value.Date;
                DateTime end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date >= start && t.Date <= end);
            }

            var transactions = await query.OrderByDescending(t => t.Date).ToListAsync();

            // Step 3: Totals
            ViewBag.TotalIncome = transactions.Where(t => t.Category.Type == "Income").Sum(t => t.Amount);
            ViewBag.TotalExpense = transactions.Where(t => t.Category.Type == "Expense").Sum(t => t.Amount);
            ViewBag.Balance = ViewBag.TotalIncome - ViewBag.TotalExpense;

            // Save filter values
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            // 🔹 Insights Calculations

            // 1. Top Expense Category
            var topCategory = transactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.Category.Title)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(g => g.Total)
                .FirstOrDefault();

            ViewBag.TopCategory = topCategory != null
                ? $"{topCategory.Category} ({topCategory.Total.ToString("C0", CultureInfo.CreateSpecificCulture("en-US"))})"
                : "No expenses";

            // 2. Expense Trend vs Last Month
            DateTime currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime lastMonthStart = currentMonthStart.AddMonths(-1);
            DateTime lastMonthEnd = currentMonthStart.AddDays(-1);

            var currentMonthExpense = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Category.Type == "Expense" && t.Date >= currentMonthStart && t.Date <= DateTime.Today)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var lastMonthExpense = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Category.Type == "Expense" && t.Date >= lastMonthStart && t.Date <= lastMonthEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            if (lastMonthExpense == 0 && currentMonthExpense == 0)
                ViewBag.ExpenseTrend = "no change";
            else if (lastMonthExpense == 0)
                ViewBag.ExpenseTrend = "increased significantly";
            else
            {
                decimal change = ((currentMonthExpense - lastMonthExpense) / lastMonthExpense) * 100;
                ViewBag.ExpenseTrend = change >= 0
                    ? $"increased by {change:F1}%"
                    : $"decreased by {Math.Abs(change):F1}%";
            }

            // 3. Average Transaction Size
            ViewBag.AvgTransaction = transactions.Any()
                ? transactions.Average(t => t.Amount).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"))
                : "$0";

            // 4. Expense by Category (for doughnut chart - optional, can be removed)
            ViewBag.DoughnutChartData = transactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.Category.Title)
                .Select(g => new
                {
                    categoryTitleWithIcon = g.Key,
                    amount = g.Sum(x => x.Amount)
                })
                .ToList();

            // 5. Income vs Expense Trend (for line chart)
            var trendData = transactions
                .GroupBy(t => t.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    day = g.Key.ToString("yyyy-MM-dd"),
                    income = g.Where(t => t.Category.Type == "Income").Sum(t => t.Amount),
                    expense = g.Where(t => t.Category.Type == "Expense").Sum(t => t.Amount)
                })
                .ToList();

            ViewBag.SplineChartData = trendData;
            ViewBag.IncomeTrendData = trendData.Select(d => new { day = d.day, income = d.income }).ToList();
            ViewBag.ExpenseTrendData = trendData.Select(d => new { day = d.day, expense = d.expense }).ToList();

            // Expense Heatmap Data

            var expenseCategories = transactions
                .Where(t => t.Category.Type == "Expense")
                .Select(t => t.Category.Title)
                .Distinct()
                .ToList();

            var days = transactions.Select(t => t.Date.Date).Distinct().OrderBy(d => d).ToList();

            var heatmapData = new List<List<decimal>>();

            foreach (var day in days)
            {
                var row = new List<decimal>();
                foreach (var category in expenseCategories)
                {
                    var total = transactions
                        .Where(t => t.Category.Title == category && t.Date.Date == day)
                        .Sum(t => t.Amount);
                    row.Add(total);
                }
                heatmapData.Add(row);
            }


            //  Expense Heatmap Data

            // X-axis: Expense categories
            ViewBag.HeatmapXAxis = new HeatMapAxis
            {
                Labels = expenseCategories.ToArray(), // array of category names
                LabelRotation = 45                     // optional, rotates labels for readability
            };

            // Y-axis: Dates
            ViewBag.HeatmapYAxis = new HeatMapAxis
            {
                Labels = days.Select(d => d.ToString("dd-MMM")).ToArray()
            };

            // Heatmap data
            ViewBag.HeatmapData = heatmapData;

            // Palette settings
            ViewBag.HeatmapPalette = new HeatMapPaletteSettings
            {
                Type = PaletteType.Gradient,
                Palette = new List<HeatMapPalette>
                {
                    new HeatMapPalette { Color = "#28a745" },  // Green
                    new HeatMapPalette { Color = "#dc3545" }   // Red
                }
            };

            // Return view
            return View(transactions);
        }
    }
}

