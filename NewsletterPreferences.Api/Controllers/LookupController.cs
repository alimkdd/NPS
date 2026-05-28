using Microsoft.AspNetCore.Mvc;
using NewsletterPreferences.Application.Interfaces;

namespace NewsletterPreferences.Api.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupController(ILookupService lookupService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await lookupService.GetAllLookupsAsync(cancellationToken);
        return Ok(result);
    }
}