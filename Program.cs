// puvsHttpClient
//
// Dies ist ein simpler Client für einen WebService Aufruf.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        const string url = "https://randomuser.me/api/?nat=de&inc=gender,name,nat,location,picture&noinfo";

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

            Result result = data.Results[0];
            Console.WriteLine(result.ToString());

            string imageFile = await LoadPicture(result.Picture.Large);
            ProcessStartInfo startInfo = new ProcessStartInfo(imageFile);
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);

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

    static async Task<string> LoadPicture(string imageUrl)
    {
        string fileName = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.jpg");

        try
        {
            using (var client = new HttpClient())
            {
                var imageData = await client.GetByteArrayAsync(imageUrl);
                using (var fs = new FileStream(fileName, FileMode.Create))
                {
                    await fs.WriteAsync(imageData, 0, imageData.Length);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error downloading image: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error writing file: {ex.Message}");
        }

        return fileName;
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
        public Picture Picture { get; set; } = new Picture();

        public override string ToString()
        {
            return $"""
                    Name       : {this.Name} ({this.Gender}, {this.Nat})
                    {this.Location}

                    {this.Picture}
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
            return $"{this.Title} {this.First} {this.Last}";
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
                    Address    : {this.Street}, {this.Postcode} {this.City}, {this.State}, {this.Country}
                    Coordinates: {this.Coordinates}
                    Timezone   : {this.Timezone}
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

    public class Picture
    {
        public string Large { get; set; } = string.Empty; 
        public string Medium { get; set; } = string.Empty; 
        public string Thumbnail { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"""
                    Large      : {this.Large}
                    Medium     : {this.Medium}
                    Thumbnail  : {this.Thumbnail}
                    """;
        }
    }
}
