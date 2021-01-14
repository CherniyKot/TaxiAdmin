using BingMapsRESTToolkit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using System.Globalization;
using System.Windows;
using System.Timers;

namespace TaxiAdmin
{
    public partial class MainPage : ContentPage
    {
        Timer RP;
        public MainPage()
        {
            InitializeComponent();

            CreateMap();

            RefreshPositions(null, null);
            RP = new Timer(1000);
            RP.Elapsed += RefreshPositions;
            RP.Start();
        }
        public async void CreateMap()
        {
            Geocoder geo = new Geocoder();
            var rus = await geo.GetPositionsForAddressAsync("St. Petersburg");

            /*map.Pins.Add(new Pin()
            {
                Label = "a",
                Position = (await geo.GetPositionsForAddressAsync("st. petersburg, budapestskaya 8")).First(),
                Type = PinType.Generic
            });
            map.Pins.Add(new Pin()
            {
                Label = "b",
                Position = (await geo.GetPositionsForAddressAsync("st. petersburg, budapestskaya 9")).First(),
                Type = PinType.Place
            });
            map.Pins.Add(new Pin()
            {
                Label = "c",
                Position = (await geo.GetPositionsForAddressAsync("st. petersburg, budapestskaya 10")).First(),
                Type = PinType.SavedPin
            });
            map.Pins.Add(new Pin()
            {
                Label = "d",
                Position = (await geo.GetPositionsForAddressAsync("st. petersburg, budapestskaya 11")).First(),
                Type = PinType.SearchResult
            });*/

            DrawPolyline(polylineOrder,
                         map.Pins.Count != 0 ? new SimpleWaypoint() { Coordinate = new Coordinate(map.Pins.FirstOrDefault().Position.Latitude, map.Pins.FirstOrDefault().Position.Longitude) } :
                         new SimpleWaypoint() { Address = "St.Petersburg" },
                         new SimpleWaypoint() { Address = "St. petersburg, hermitage" });*/

        }
        public async void DrawPolyline(Polyline polyline, SimpleWaypoint from, SimpleWaypoint to)
        {
            polyline.Geopath.Clear();
            RouteRequest request = new RouteRequest()
            {
                RouteOptions = new RouteOptions()
                {
                    Avoid = new List<AvoidType>()
                    {
                        AvoidType.MinimizeTolls
                    },
                    TravelMode = TravelModeType.Driving,
                    DistanceUnits = DistanceUnitType.Kilometers,
                    RouteAttributes = new List<RouteAttributeType>()
                    {
                        RouteAttributeType.RoutePath
                    },
                    Optimize = RouteOptimizationType.TimeWithTraffic
                },
                Waypoints = new List<SimpleWaypoint>() { from, to },
                BingMapsKey = "IL4KJLeeFpfhl9mqdvw8~QuoEqNN-cICujvx87S4zcg~AgVzx_pxd7tdbOkilsKFwk5B-2iNInOs1OC1HXhm810TqteSR1TzPN87K6czdFAL"
            };
            var response = await request.Execute();
            if (response.StatusCode == 200)
                foreach (var pos in response.ResourceSets.First().Resources.OfType<Route>().First().RoutePath.Line.Coordinates.Select(e => new Position(e[0], e[1])).ToList())
                {
                    polyline.Geopath.Add(pos);
                }
            else DisplayAlert("Routing error", string.Concat(response.ErrorDetails), "Ok");
        }

        public void RefreshPositions(object sender, ElapsedEventArgs e)
        {
            RP?.Stop();
            var drivers = Server.GetDriversLocations();
            var orders = Server.GetOrders();
            Geocoder geo = new Geocoder();
            foreach (var driver in drivers)
            {
                if (map.Pins.Any(d => d != null && d.Label == driver.driverID.ToString()))
                {
                    Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Where(d => d.Label == driver.driverID.ToString()).First().Position = new Position(driver.latitude ?? 0, driver.longitude ?? 0));
                }
                else
                {
                    Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Add(new Pin()
                    {
                        Position = new Position(driver.latitude ?? 0, driver.longitude ?? 0),
                        Label = driver.driverID.ToString(),
                        Type = PinType.SavedPin
                    }));
                }
            }
            foreach (var order in orders)
            {
                if (map.Pins.Any(d => d.Label == order.orderID.ToString()))
                {
                    Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Where(d => d.Label == order.orderID.ToString()).First().Position = new Position(order.latitudeFrom, order.longitudeFrom));
                }
                else
                {
                    Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Add(new Pin()
                    {
                        Position = new Position(order.latitudeFrom, order.longitudeFrom),
                        Label = order.orderID.ToString(),
                        Type = PinType.SearchResult
                    }));
                }
            }
            Dispatcher.BeginInvokeOnMainThread(() =>
            {
                foreach (var pin in map.Pins.Where(p => p.Type == PinType.SearchResult))
                {
                    Dispatcher.BeginInvokeOnMainThread(async () => pin.Address = (await geo.GetAddressesForPositionAsync(pin.Position)).FirstOrDefault());
                }
            });
            RP?.Start();
        }
    }


}

