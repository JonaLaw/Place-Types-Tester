# Place Types Tester

A simple command line tool for testing out Google Map's Nearby Search and Place Types.  
Uses a device's current location or a location provided by the user to make a [Google Maps Nearby Search request](https://developers.google.com/maps/documentation/places/web-service/search-nearby).  
Takes the request's result and extracts all [Place Types](https://developers.google.com/maps/documentation/places/web-service/supported_types) and their number of occurrences.  
Displays the devices current location and the all Place Types found in the search response.  

Requires the device to have location services enabled and have location access granted to apps.  
Requires the user to have their own [Places API Key](https://developers.google.com/maps/documentation/places/web-service/get-api-key).  
**Note that this usage of the API returns a large amount of unused data which will add up in costs over time.**

Example output for displaying a device's current location:
```
Press a key to select an option:
1: Output current location
2: Output nearby place types using current location
3: Output nearby place types using inputted location
4: Exit
Input: 1

Waiting 5000 milliseconds for the geolocator to start.
Got the device's current location.
Latitude: 22.44386558440614, Longitude -74.22038306360201
y/n: Would you like to view this location in your browser?
Input: y

```

Example output for displaying place types near a location specified by the user:
```
Press a key to select an option:
1: Output current location
2: Output nearby place types using current location
3: Output nearby place types using inputted location
4: Exit
Input: 3

Enter a Latitude: 40.75724143400579
Enter a Longitude: -73.9897854076933
Getting nearby place types.
Making a request with the URI:
https://maps.googleapis.com/maps/api/place/nearbysearch/json?location=40.75724,-73.98978&radius=100&key=YOUR_API_KEY

Nearby Place Types:
Count: 18, Type: point_of_interest
Count: 18, Type: establishment
Count: 3, Type: store
Count: 2, Type: political
Count: 2, Type: movie_theater
Count: 2, Type: electronics_store
Count: 2, Type: restaurant
Count: 2, Type: food
Count: 1, Type: locality
...
```
