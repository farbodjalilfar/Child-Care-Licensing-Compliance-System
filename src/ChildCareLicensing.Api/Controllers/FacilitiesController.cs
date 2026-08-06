using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacilitiesController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var facilities = await dbContext.Facilities
            .AsNoTracking()
            .Include(f => f.Rooms)
            .OrderBy(f => f.Name)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.City,
                f.Province,
                RoomCount = f.Rooms.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(facilities);
    }
}
