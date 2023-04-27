// puvsHttpClient
//
// Dies ist ein simpler Client für einen WebService Aufruf.

using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        const string url = "https://randomuser.me/api/?nat=de&inc=gender,name,nat,location&noinfo";

        Response data = new Response();

        bool exit = false;

        Console.WriteLine("### puvsHttpClient ###");

        do
        {
            try
            {
                data = await GetDataFromWebService(url);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Exception: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unknown Exception: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine(data.Results[0].ToString());

            Console.WriteLine("\nPress any key to repreat or X to exit...");
            ConsoleKeyInfo key = Console.ReadKey(true);

            if(key.Key == ConsoleKey.X)
            {
                exit = true;
            }
        }
        while (!exit);

        Console.WriteLine("\nGoodbye!");
    }

    static async Task<Response> GetDataFromWebService(string url)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Response>(json, options) ?? new Response();
        }
        else
        {
            throw new HttpRequestException($"HTTP Error: {response.StatusCode}");
        }
    }

    public class Response
    {
        public Result[] Results { get; set; } = new Result[0];
    }

    public class Result
    {
        public string Gender { get; set; } = string.Empty;
        public Name Name { get; set; } = new Name();
        public string Nat { get; set; } = string.Empty;
        public Location Location { get; set; } = new Location();

        public override string ToString()
        {
            return $"""
                    Name       : {Name} ({Gender}, {Nat})
                    {Location}
                    """;
        }
    }

    public class Name
    {
        public string Title { get; set; } = string.Empty;
        public string First { get; set; } = string.Empty;
        public string Last { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Title} {First} {Last}";
        }
    }

    public class Location
    {
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Postcode { get; set; }
        public Street Street { get; set; } = new Street();
        public Coordinates Coordinates { get; set; } = new Coordinates();
        public Timezone Timezone { get; set; } = new Timezone();

        public override string ToString()
        {
            return $"""
                    Address    : {Street}, {Postcode} {City}, {State}, {Country}
                    Coordinates: {Coordinates}
                    Timezone   : {Timezone}
                    """;
        }
    }

    public class Street
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{this.Name} {this.Number}";
        }
    }

    public class Coordinates
    {
        public string Latitude { get; set; } = string.Empty;
        public string Longitude { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"(Lat: {this.Latitude}, Lon: {this.Longitude})";
        }
    }

    public class Timezone
    {
        public string Offset { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{this.Description} (GMT{this.Offset})";
        }
    }
}
