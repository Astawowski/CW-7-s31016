using APBD_07.Exceptions;
using APBD_07.Models;
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
    
    
    // Post dodający do bazy nowego klienta i zwracający jego ID
    [HttpPost]
    public async Task<IActionResult> InsertClientAsync([FromBody] ClientCreateDTO client)
    {
        var createdClient = await dbservice.CreateClientAsync(client);
        return Accepted(new { message = $"Added Client with ID: {createdClient.IdClient}" });
    }
    
    
    // Put rejestrujący klienta na wycieczkę
    [HttpPut("{cId}/trips/{tripId}")]
    public async Task<IActionResult> PutClientOnTripAsync([FromRoute] int cId, [FromRoute] int tripId)
    {
        var createdReservation = new Client_TripGetDTO();
        try
        {
            createdReservation = await dbservice.PutClientOnTrip(cId, tripId);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (TripIsFullException e)
        {
            return Conflict(e.Message);
        }
        return Accepted(new { message = $"Registered Client with Id {createdReservation.IdClient} on Trip with Id {createdReservation.IdTrip}" });
    }

    
    // Delete do usuwania rejestracji klienta
    [HttpDelete("{cId}/trips/{tripId}")]
    public async Task<IActionResult> DeleteClientOnTripAsync([FromRoute] int cId, [FromRoute] int tripId)
    {
        var deletedReservation = new Client_TripDeleteDTO();
        try
        {
            deletedReservation = await dbservice.DeleteClientOnTrip(cId, tripId);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        return Accepted(new { message = $"UnRegistered Client with Id {deletedReservation.IdClient} from Trip with Id {deletedReservation.IdTrip}" });
    }
}