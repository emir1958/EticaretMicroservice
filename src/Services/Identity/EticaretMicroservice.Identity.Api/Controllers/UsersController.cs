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
}

public record UserSummaryDto(string Id, string Username, string Email, string Role, bool IsSample);
