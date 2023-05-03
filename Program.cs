// puvsHttpClient
//
// Dies ist ein Client für einen WebService Aufruf.

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Diagnostics;
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

            if (key.Key == ConsoleKey.X)
            {
                exit = true;
            }
        }
        while (!exit);

        Console.WriteLine("\nGoodbye!");
    }

    static async Task<Response> GetDataFromWebService(string url)
    {
        Response response = new Response();

        using var client = new HttpClient();

        HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        AsyncCircuitBreakerPolicy circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));

        AsyncRetryPolicy retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(16)
            }, (exception, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"Retry attempt {retryCount} failed. Waiting {timeSpan} before retrying...");
            });

        AsyncRetryPolicy retryPolicy2 = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                5, // number of retries
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // calculate delay based on retry count
                (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry attempt {retryCount} failed. Waiting {timeSpan} before retrying...");
                });

        AsyncTimeoutPolicy timeoutPolicy = Policy.TimeoutAsync(30);

        try
        {
            await circuitBreakerPolicy.WrapAsync(timeoutPolicy.WrapAsync(retryPolicy)).ExecuteAsync(async () =>
            {
                httpResponse = await client.GetAsync(url);
            });
        }
        catch (BrokenCircuitException ex)
        {
            Console.WriteLine($"There was a persistent Error downloading data. Requests are not being executed. ({ex.Message})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unknown Error downloading data: {ex.Message}");
        }

        if (httpResponse.IsSuccessStatusCode)
        {
            string json = await httpResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            response = JsonSerializer.Deserialize<Response>(json, options) ?? new Response();
        }

        return response;
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
