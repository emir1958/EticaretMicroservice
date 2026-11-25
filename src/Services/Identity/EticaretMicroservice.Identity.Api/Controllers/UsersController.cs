using System.ComponentModel.DataAnnotations;
using EticaretMicroservice.Identity.Api.Persistence;
using EticaretMicroservice.Shared.SampleData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Identity.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private const int MaxPageSize = 100;

    // Büyük/küçük harf ve aksan duyarsız: "caglar" → "Çağlar", "istanbul" → "İstanbul" (Order aramasıyla aynı kolasyon)
    private const string SearchCollation = "Latin1_General_100_CI_AI";

    private readonly AppIdentityDbContext _context;

    public UsersController(AppIdentityDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new { u.Id, u.Username, u.Email, u.Role })
            .ToListAsync(cancellationToken);

        return Ok(users.Select(u => new UserSummaryDto(u.Id, u.Username, u.Email, u.Role, SampleCustomers.IsSample(u.Id))));
    }

    // Admin hesapları hariç; her arama terimi ad veya e-postada geçmeli
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery, StringLength(100)] string? q,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, MaxPageSize)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var customers = _context.Users.AsNoTracking().Where(u => u.Role != "Admin");

        foreach (var term in (q ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            customers = customers.Where(u =>
                EF.Functions.Collate(u.Username, SearchCollation).Contains(term)
                || EF.Functions.Collate(u.Email, SearchCollation).Contains(term));
        }

        var totalCount = await customers.CountAsync(cancellationToken);

        var rows = await customers
            .OrderBy(u => u.Username)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.Username, u.Email, u.Role })
            .ToListAsync(cancellationToken);

        var items = rows.Select(u => new UserSummaryDto(u.Id, u.Username, u.Email, u.Role, SampleCustomers.IsSample(u.Id))).ToList();
        return Ok(new PagedUsersDto(items, totalCount, page, pageSize));
    }
}

public record UserSummaryDto(string Id, string Username, string Email, string Role, bool IsSample);

public record PagedUsersDto(IReadOnlyList<UserSummaryDto> Items, int TotalCount, int Page, int PageSize);
