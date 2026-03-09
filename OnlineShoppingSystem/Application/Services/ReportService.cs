using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Presentation.Helpers;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Generates analytical reports for administrators using LINQ aggregations.
/// Depends on IOrderRepository and IProductRepository — never accesses DataStore directly.
/// </summary>
public class ReportService : IReportService
{
    private readonly IOrderRepository   _orders;
    private readonly IProductRepository _products;

    public ReportService(IOrderRepository orders, IProductRepository products)
    {
        _orders   = orders;
        _products = products;
    }

    /// <summary>Displays total revenue, order counts, and per-status breakdowns.</summary>
    public void GenerateSalesReport()
    {
        ConsoleHelper.WriteHeader("Sales Report");

        var allOrders       = _orders.GetAll();
        var completedOrders = allOrders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var totalRevenue    = completedOrders.Sum(o => o.TotalAmount);

        var ordersByStatus = allOrders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .OrderBy(g => g.Status)
            .ToList();

        Console.WriteLine($"Total Orders    : {allOrders.Count}");
        Console.WriteLine($"Total Revenue   : R{totalRevenue:F2}");
        Console.WriteLine($"Average Order   : R{(completedOrders.Count > 0 ? completedOrders.Average(o => o.TotalAmount) : 0):F2}");
        Console.WriteLine();
        Console.WriteLine($"{"Status",-15} {"Count",8} {"Revenue",14}");
        Console.WriteLine(new string('-', 40));

        foreach (var row in ordersByStatus)
            Console.WriteLine($"{row.Status,-15} {row.Count,8} R{row.Revenue,12:F2}");
    }

    /// <summary>Lists the top 5 most-sold products by units and by revenue.</summary>
    public void GenerateTopProductsReport()
    {
        ConsoleHelper.WriteHeader("Top Products Report");

        var topByUnits = _orders.GetAll()
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                g.Key.ProductName,
                UnitsSold = g.Sum(i => i.Quantity),
                Revenue   = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(p => p.UnitsSold)
            .Take(5)
            .ToList();

        if (!topByUnits.Any())
        {
            ConsoleHelper.WriteWarning("No sales data available yet.");
            return;
        }

        Console.WriteLine($"{"Product",-30} {"Units Sold",12} {"Revenue",14}");
        Console.WriteLine(new string('-', 58));

        foreach (var row in topByUnits)
            Console.WriteLine($"{row.ProductName,-30} {row.UnitsSold,12} R{row.Revenue,12:F2}");
    }

    /// <summary>Lists all products at or below the low-stock threshold.</summary>
    public void GenerateLowStockReport()
    {
        ConsoleHelper.WriteHeader("Low Stock Report");

        var lowStock = _products.GetActive()
            .Where(p => p.IsLowStock)
            .OrderBy(p => p.StockQuantity)
            .ToList();

        if (!lowStock.Any())
        {
            ConsoleHelper.WriteSuccess("All products are adequately stocked.");
            return;
        }

        Console.WriteLine($"{"ID",4} {"Product",-28} {"Category",-15} {"Stock",7}");
        Console.WriteLine(new string('-', 57));

        foreach (var p in lowStock)
        {
            var stockLabel = p.StockQuantity == 0 ? "OUT" : p.StockQuantity.ToString();
            Console.ForegroundColor = p.StockQuantity == 0 ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.WriteLine($"{p.Id,4} {p.Name,-28} {p.Category,-15} {stockLabel,7}");
            Console.ResetColor();
        }
    }

    /// <summary>Shows each customer's order count and total spend, sorted by spend.</summary>
    public void GenerateCustomerOrderReport()
    {
        ConsoleHelper.WriteHeader("Customer Order Report");

        var report = _orders.GetAll()
            .GroupBy(o => new { o.CustomerId, o.CustomerName })
            .Select(g => new
            {
                g.Key.CustomerName,
                OrderCount = g.Count(),
                TotalSpend = g.Where(o => o.Status != OrderStatus.Cancelled)
                              .Sum(o => o.TotalAmount)
            })
            .OrderByDescending(c => c.TotalSpend)
            .ToList();

        if (!report.Any())
        {
            ConsoleHelper.WriteWarning("No orders placed yet.");
            return;
        }

        Console.WriteLine($"{"Customer",-25} {"Orders",8} {"Total Spend",14}");
        Console.WriteLine(new string('-', 50));

        foreach (var row in report)
            Console.WriteLine($"{row.CustomerName,-25} {row.OrderCount,8} R{row.TotalSpend,12:F2}");
    }
}
