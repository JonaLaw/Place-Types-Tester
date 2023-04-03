using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Device.Location; // outdated, but works fine

/// <summary>
/// This code can generate GeoLocations through either direct user input or via the device.
/// Using a GeoLocation, it can then make a Google Place API request to get a list of all nearby places.
/// From that list of places, all place types are extracted for easy viewing.
/// </summary>
public class PlaceTypesTester
{
    static void Main()
    {
        while (true)
        {
            Console.WriteLine("\nPress a key to select an option:");
            Console.WriteLine("1: Output current location");
            Console.WriteLine("2: Output nearby place types using current location");
            Console.WriteLine("3: Output nearby place types using inputted location");
            Console.WriteLine("4: Exit");
            Console.Write("Input: ");
            char input = Console.ReadKey().KeyChar;
            Console.WriteLine("\n");
            switch (input)
            {
                case '1':
                    OutputCurrentLocation();
                    break;
                case '2':
                    OutputNearbyPlaceTypes(GetCurrentDeviceLocation());
                    break;
                case '3':
                    OutputNearbyPlaceTypes(GetLocationFromUser());
                    break;
                case '4':
                    return;
                default:
                    break;
            }
        }
    }
    
    /// <summary>
    /// Tries to output the device's current geolocation.
    /// If successful, asks the user if they want to view the location in their browser.
    /// </summary>
    private static void OutputCurrentLocation()
    {
        GeoCoordinate? gc = GetCurrentDeviceLocation();
        if (gc == null) return;
        Console.WriteLine("Latitude: {0}, Longitude {1}", gc.Latitude, gc.Longitude);
        Console.WriteLine("y/n: Would you like to view this location in your browser?");
        Console.Write("Input: ");
        char input = Console.ReadKey().KeyChar;
        Console.WriteLine();
        if (input == 'y')
        {
            string googleMapsURL = $"https://www.google.com/maps/search/?api=1&query={gc.Latitude}%2C{gc.Longitude}";
            try
            {
                Process.Start(new ProcessStartInfo() { FileName = googleMapsURL, UseShellExecute = true });
            }
            catch
            {
                Console.WriteLine($"Could not view this URL in a browser: {googleMapsURL}");
            }
        }
    }

    /// <summary>
    /// Tries to output the nearby place types around the given coordinate.
    /// </summary>
    /// <param name="coordinate">Coordinate to search around</param>
    public static void OutputNearbyPlaceTypes(GeoCoordinate? coordinate)
    {
        if (coordinate == null) return;
        
        List<KeyValuePair<string, int>>? nearbyPlaceTypes = GetNearbyPlaceTypes(coordinate);
        if (nearbyPlaceTypes == null) return;

        Console.WriteLine("\nNearby Place Types:");
        foreach (var placeType in nearbyPlaceTypes)
        {
            Console.WriteLine($"Count: {placeType.Value}, Type: {placeType.Key}");
        }
    }
    
    /// <summary>
    /// Tries to request the device's geolocation and returns the result.
    /// </summary>
    /// <returns>GeoCoordinate or null</returns>
    private static GeoCoordinate? GetCurrentDeviceLocation()
    {
        const int waitTimeMilliseconds = 5000;
        // subscribing to PositionChanged events would be faster but more complicated
        GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();

        Console.WriteLine($"Waiting {waitTimeMilliseconds} milliseconds for the geolocator to start.");
        watcher.TryStart(false, TimeSpan.FromMilliseconds(waitTimeMilliseconds));
        Thread.Sleep(waitTimeMilliseconds);

        if (watcher.Permission != GeoPositionPermission.Granted)
        {
            Console.WriteLine($"Geolocation permission has not been granted.");
            return null;
        }

        if (watcher.Status != GeoPositionStatus.Ready)
        {
            string? msg = watcher.Status switch
            {
                GeoPositionStatus.Disabled => "Geolocation is disabled.",
                GeoPositionStatus.Initializing => "Could not start the geolocator in time.",
                GeoPositionStatus.NoData => "No location data available.",
                _ => $"The value of {watcher.Status} was not expected as a status."
            };
            Console.WriteLine(msg);
            return null;
        }
        
        GeoCoordinate coord = watcher.Position.Location;
        if (!coord.IsUnknown)
        {
            Console.WriteLine("Got the device's current location.");
            return coord;
        }
        else
        {
            Console.WriteLine("Could not get the device's current location.");
            return null;
        }
    }

    /// <summary>
    /// Asks the user for a GeoCoordinate via the command line.
    /// Tries to parse the inputted coordinate strings into a GeoCoordinate before returning the result.
    /// </summary>
    /// <returns>GeoCoordinate or null</returns>
    private static GeoCoordinate? GetLocationFromUser()
    {
        Console.Write("Enter a Latitude: ");
        string? latStr = Console.ReadLine();
        Console.Write("Enter a Longitude: ");
        string? lonStr = Console.ReadLine();

        if (latStr == null || lonStr == null) 
        {
            Console.Write("An empty input was given.");
            return null;
        }
        if (!double.TryParse(latStr, out double lat) || lat < -90 || lat > 90)
        {
            Console.WriteLine("Invalid Latitude Entry. The Latitude entry must be a double and between -90 and 90.");
            return null;
        }
        if (!double.TryParse(lonStr, out double lon) || lon < -180 || lon > 180)
        {
            Console.WriteLine("Invalid Longitude Entry. The Longitude entry must be a double and between -180 and 180.");
            return null;
        }

        return new GeoCoordinate(lat, lon);
    }

    /// <summary>
    /// Uses the given GeoCoordinate to return a list of nearby place types using Google's Place API.
    /// Can request to search for only certain types.
    /// </summary>
    /// <param name="coordinate">Location to search around</param>
    /// <param name="typesToInclude">Place Types for Google to include in its search results</param>
    /// <returns>A list of found place types and their number of occurrences sorted by occurrences, or null</returns>
    private static List<KeyValuePair<string, int>>? GetNearbyPlaceTypes(GeoCoordinate coordinate, string[]? typesToInclude = null)
    {
        Console.WriteLine("Getting nearby place types.");
        string requestURL = NearbySearch.CreateURL(coordinate, typesToInclude);
        NearbySearch.Response? placeTypeResponse = NearbySearch.GetPlaceAPIResponse(requestURL);
        if (placeTypeResponse == null || placeTypeResponse.results == null)
        {
            Console.WriteLine("Got an empty response.");
            return null;
        }

        Dictionary<string, int> placeTypes = new Dictionary<string, int>(150);
        foreach (NearbySearch.Result result in placeTypeResponse.results)
        {
            if (result.types == null) continue;
            foreach (string placeType in result.types)
            {
                if (placeTypes.ContainsKey(placeType))
                {
                    placeTypes[placeType] += 1;
                }
                else
                {
                    placeTypes.Add(placeType, 1);
                }
            }
        }
        
        if (placeTypes.Count == 0)
        {
            Console.WriteLine("No place types found in the response.");
            return null;
        }
        List<KeyValuePair<string, int>> sortedPlaceTypes = placeTypes.ToList();
        sortedPlaceTypes.Sort((x, y) => y.Value.CompareTo(x.Value));
        return sortedPlaceTypes;
    }

    private static class NearbySearch
    {
        // API request URL inputs
        private const string GOOGLE_API_LINK = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?location=";
        private const int PLACE_SEARCH_RADIUS = 100; // in meters, don't go too crazy as there are limits

        //https://developers.google.com/maps/documentation/places/web-service/get-api-key
        private const string API_KEY = "YOUR_API_KEY_HERE";

        /// <summary>
        /// Uses the given GeoCoordinate to create a URL for a Google Maps Places Nearby Search request.
        /// Can specify to only include certain place types.
        /// </summary>
        /// <param name="coordinate">Location to search around</param>
        /// <param name="typesToInclude">Place Types for Google to include in its search results</param>
        /// <returns>URL</returns>
        public static string CreateURL(GeoCoordinate coordinate, string[]? typesToInclude)
        {
            // place types documentation
            // https://developers.google.com/maps/documentation/places/web-service/supported_types#table1
            // URL format documentation
            // https://developers.google.com/maps/documentation/places/web-service/search-nearby#nearby-search-example
            string URL = typesToInclude switch
            {
                null => string.Format("{0}{1},{2}&radius={3}&key={4}",
                    GOOGLE_API_LINK,
                    coordinate.Latitude,
                    coordinate.Longitude,
                    PLACE_SEARCH_RADIUS,
                    API_KEY),
                _ => string.Format("{0}{1},{2}&radius={3}&type={4}&key={5}",
                    GOOGLE_API_LINK,
                    coordinate.Latitude,
                    coordinate.Longitude,
                    PLACE_SEARCH_RADIUS,
                    string.Join(",", typesToInclude),
                    API_KEY)
            };
            return URL;
        }

        /// <summary>
        /// Makes an API request using the given URI and handles any errors.
        /// </summary>
        /// <returns>Response object or null</returns>
        public static Response? GetPlaceAPIResponse(string URI)
        {
            Console.WriteLine($"Making a request with the URI: {URI}");
            try
            {
                using (var client = new HttpClient())
                {
                    string response = client.GetStringAsync(URI).Result;
                    Response? responseObject = JsonSerializer.Deserialize<Response>(response); 
                    if (responseObject != null && responseObject.status != null && responseObject.status.Equals("OK"))
                    {
                        return responseObject;
                    }
                    Console.WriteLine("Got a bad responce.");
                }
            }
            catch (Exception ex)
            {
                string errorMSG = ex switch
                {
                    InvalidOperationException => "Bad HttpClient",
                    UriFormatException => "Bad URI",
                    HttpRequestException => "Network Failure",
                    System.Threading.Tasks.TaskCanceledException => "Wait Timeout",
                    JsonException => "Invalid JSON",
                    NotSupportedException => "JSON Error",
                    ArgumentNullException => "Unexpected Null Arg",
                    _ => "Unknown Exception"
                };
                Console.WriteLine($"Error encountered when making the request: {errorMSG}");
            }
            return null;
        }

        // response format documentation
        // https://developers.google.com/maps/documentation/places/web-service/search-nearby#PlacesNearbySearchResponse
        // all unused fluff is not included
        public class Result
        {
            public IList<string>? types { get; set; }
        }

        public class Response
        {
            public IList<Result>? results { get; set; }
            public string? status { get; set; }
        }
    }
}
