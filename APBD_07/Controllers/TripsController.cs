using APBD_07.Models;
using APBD_07.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_07.Controllers;


[ApiController]
[Route("api/[controller]")]
public class TripsController(IDbService dbService) : ControllerBase
{
    // GET zwracający wszystkie Tripy z listą nazw krajów, które odwiedza
    [HttpGet]
    public async Task<IActionResult> GetAllTripsAsync()
    {
        return Ok(await dbService.GetAllTripsAsync());
    }
}