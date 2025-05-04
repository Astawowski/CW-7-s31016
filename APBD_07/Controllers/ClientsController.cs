using APBD_07.Exceptions;
using APBD_07.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_07.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ClientsController(IDbService dbservice) : ControllerBase
{
    // Get zwracający wszystkie Tripy powiązane z konkretnym ClientId
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetAllClientTripsAsync([FromRoute] int id)
    {
        try
        {
            return Ok(await dbservice.GetTripsByClientIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}