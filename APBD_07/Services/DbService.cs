using APBD_07.Exceptions;
using APBD_07.Models;
using Microsoft.Data.SqlClient;

namespace APBD_07.Services;

public interface IDbService
{
    public Task<IEnumerable<TripWithCountriesGetDTO>> GetAllTripsAsync();
    public Task<IEnumerable<TripWithClientDetailsGetDTO>> GetTripsByClientIdAsync(int id);
    public Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO clientCreateDto);
    public Task<Client_TripGetDTO> PutClientOnTrip(int clientId, int tripId);
    public Task<Client_TripDeleteDTO> DeleteClientOnTrip(int clientId, int tripId);
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
            int idTrip = reader.GetInt32(0);
            // Jeśli takiego tripu jeszcze nie ma w słowniku to dodajemy nowy obiekt 'TripWithCountriesGetDTO'.
            if (!result.ContainsKey(idTrip))
            {
                result.Add(idTrip, new TripWithCountriesGetDTO
                {
                    Id = idTrip,
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<string>()
                });
            }
            
            // Do takiego obiektu w liście krajów dodajemy odczytany kraj
            result[idTrip].Countries.Add(reader.GetString(6));
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

    

    
    public async Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO clientCreateDto)
    {
        // Pobieramy wszystkie atrybuty (poza ID) z ciała JSON żądania HTTP
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);
        await using var sqlCommand = new SqlCommand(@"INSERT INTO Client (FirstName, LastName,
                    Email, Telephone, Pesel) VALUES (@FirstName, @LastName,
                    @Email, @Telephone, @Pesel); SELECT scope_identity();", connection);
        sqlCommand.Parameters.AddWithValue("@FirstName", clientCreateDto.FirstName);
        sqlCommand.Parameters.AddWithValue("@LastName", clientCreateDto.LastName);
        sqlCommand.Parameters.AddWithValue("@Email", clientCreateDto.Email);
        sqlCommand.Parameters.AddWithValue("@Telephone", clientCreateDto.Telephone);
        sqlCommand.Parameters.AddWithValue("@Pesel", clientCreateDto.Pesel);
        await connection.OpenAsync();
        
        var newId = Convert.ToInt32(await sqlCommand.ExecuteScalarAsync());

        return new ClientGetDTO()
        {
            IdClient = newId,
            FirstName = clientCreateDto.FirstName,
            LastName = clientCreateDto.LastName,
            Email = clientCreateDto.Email,
            Telephone = clientCreateDto.Telephone,
            Pesel = clientCreateDto.Pesel
        };
    }


    

    public async Task<Client_TripGetDTO> PutClientOnTrip(int clientId, int tripId)
    {
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);
        
        // Najpierw sprawdzamy czy wgl taki klient istnieje
        var checkClientCommand = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @id", connection);
        checkClientCommand.Parameters.AddWithValue("@id", clientId);
        await connection.OpenAsync();
        var clientExists = await checkClientCommand.ExecuteScalarAsync() != null;
        // Jeśli nie to 404 błąd
        if (!clientExists)
        {
            throw new NotFoundException($"Client with id {clientId} does not exist.");
        }
        
        //Potem sprawdzamy czy istnieje taka wycieczka
        var checkTripCommand = new SqlCommand("SELECT 1 FROM Trip WHERE IdTrip = @id", connection);
        checkTripCommand.Parameters.AddWithValue("@id", tripId);
        var tripExists = await checkTripCommand.ExecuteScalarAsync() != null;
        // Jeśli nie to 404 błąd
        if (!tripExists)
        {
            throw new NotFoundException($"Trip with id {tripId} does not exist.");
        }
        
        //Pobieramy ilość zarejestrowanych klientów na wycieczke
        var checkClientsCountCommand = new SqlCommand(@"SELECT COUNT(*) FROM Trip t LEFT JOIN Client_Trip ct 
                                                                ON t.IdTrip = ct.IdTrip WHERE t.IdTrip = @id", connection);
        checkClientsCountCommand.Parameters.AddWithValue("@id", tripId);
        var clientsCount = Convert.ToInt32(await checkClientsCountCommand.ExecuteScalarAsync());
        
        //Pobieramy max ilość miejsc na danej wycieczce
        var checkMaxSeatsCommand = new SqlCommand(@"SELECT MaxPeople FROM Trip WHERE IdTrip = @id", connection);
        checkMaxSeatsCommand.Parameters.AddWithValue("@id", tripId);
        var maxSeats = Convert.ToInt32(await checkMaxSeatsCommand.ExecuteScalarAsync());

        // Jeśli nie ma już miejsca dla nowego klienta to wywalamy błąd
        if (clientsCount >= maxSeats)
        {
            throw new TripIsFullException($"Trip with id {tripId} is full.");
        }

        // Jeśli wszystko spoko to zwaracamy nową rejestrację klienta
        var clientTrip = new Client_TripGetDTO()
        {
            IdClient = clientId,
            IdTrip = tripId,
            RegisteredAt = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd")),
            PaymentDate = null
        };

        var insertClientOnTripCommand = new SqlCommand(
            @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                                                VALUES (@IdClient, @IdTrip, @RegisteredAt, @PaymentDate);", connection);
        insertClientOnTripCommand.Parameters.AddWithValue("@IdClient", clientTrip.IdClient);
        insertClientOnTripCommand.Parameters.AddWithValue("@IdTrip", clientTrip.IdTrip);
        insertClientOnTripCommand.Parameters.AddWithValue("@RegisteredAt", clientTrip.RegisteredAt);
        insertClientOnTripCommand.Parameters.AddWithValue("@PaymentDate", DBNull.Value);
        await insertClientOnTripCommand.ExecuteNonQueryAsync();
        
        return clientTrip;
    }


    
    

    public async Task<Client_TripDeleteDTO> DeleteClientOnTrip(int clientId, int tripId)
    {
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);
        
        //Sprawdzamy czy istnieje taka rezerwacja klienta
        var checkClientOnTripCommand = new SqlCommand(@"SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection);
        checkClientOnTripCommand.Parameters.AddWithValue("@IdClient", clientId);
        checkClientOnTripCommand.Parameters.AddWithValue("@IdTrip", tripId);
        await connection.OpenAsync();
        var clientReservationExists = await checkClientOnTripCommand.ExecuteScalarAsync() != null;
        if (!clientReservationExists)
        {
            throw new NotFoundException($"Client with id {clientId} is not registered on Trip with id {tripId}.");
        }

        var clientTripToDelete = new Client_TripDeleteDTO()
        {
            IdClient = clientId,
            IdTrip = tripId
        };
        // Usuwamy taką rejestrację
        var deleteClientReservationCommand = new SqlCommand(@"DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection);
        deleteClientReservationCommand.Parameters.AddWithValue("@IdClient", clientTripToDelete.IdClient);
        deleteClientReservationCommand.Parameters.AddWithValue("@IdTrip", clientTripToDelete.IdTrip);
        await deleteClientReservationCommand.ExecuteNonQueryAsync();

        return clientTripToDelete;
    }
}