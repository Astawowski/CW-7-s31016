using APBD_07.Exceptions;
using APBD_07.Models;
using Microsoft.Data.SqlClient;

namespace APBD_07.Services;

public interface IDbService
{
    public Task<IEnumerable<TripWithCountriesGetDTO>> GetAllTripsAsync();
    public Task<IEnumerable<TripWithClientDetailsGetDTO>> GetTripsByClientIdAsync(int id);
}

public class DbService(IConfiguration config) : IDbService
{
    
    
    public async Task<IEnumerable<TripWithCountriesGetDTO>> GetAllTripsAsync()
    {
        // Zapisujemy do słownika gdzie kluczem jest ID Tripu
        var result = new Dictionary<int,TripWithCountriesGetDTO>();
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);
        var command = new SqlCommand(@"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               c.Name AS CountryName
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
        ORDER BY t.IdTrip", connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            int IdTrip = reader.GetInt32(0);
            
            // Jeśli takiego tripu jeszcze nie ma w słowniku to dodajemy nowy obiekt 'TripWithCountriesGetDTO'.
            if (!result.ContainsKey(IdTrip))
            {
                result.Add(IdTrip, new TripWithCountriesGetDTO
                {
                    Id = IdTrip,
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<string>()
                });
            }
            
            // Do takiego obiektu w liście krajów dodajemy odczytany kraj
            result[IdTrip].Countries.Add(reader.GetString(6));
        }
        // Zwracamy same obiekty typu 'TripWithCountriesGetDTO'
        return result.Values;
    }


    public async Task<IEnumerable<TripWithClientDetailsGetDTO>> GetTripsByClientIdAsync(int id)
    {
        var result = new List<TripWithClientDetailsGetDTO>();
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);
        
        // Najpierw sprawdzamy czy wgl taki klient istnieje
        var checkClientCommand = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @id", connection);
        checkClientCommand.Parameters.AddWithValue("@id", id);
        await connection.OpenAsync();
        var clientExists = await checkClientCommand.ExecuteScalarAsync() != null;
        // Jeśli nie to 404 błąd
        if (!clientExists)
        {
            throw new NotFoundException($"Client with id {id} does not exist.");
        }
        
        // Potem pobieramy wszystkie Tripy od tego klienta z detalami
        var command = new SqlCommand(@"SELECT t.IdTrip,t.Name,t.Description,t.DateFrom,t.DateTo,t.MaxPeople,
                                                    ct.RegisteredAt,ct.PaymentDate
                                                FROM Trip t LEFT JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
                                                WHERE ct.IdClient = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            result.Add(new TripWithClientDetailsGetDTO()
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    RegisteredAt = reader.GetInt32(6),
                    PaymentDate = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                }
            );
        }
        
        // Jeśli nie ma żadnych tripów to też wywalam 404
        if (result.Count == 0)
        {
            throw new NotFoundException($"Client with id {id} does not have any trips.");
        }
        return result;
    }
}