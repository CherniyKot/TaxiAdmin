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
        int OrderID;
        int DriverID;
        DriverLocation[] drivers;
        OrderLocation[] orders;
        DriverLocation driver;
        OrderLocation order;
        double UserPathLength;
        double UserPathPrice;
        public MainPage()
        {
            InitializeComponent();

            RefreshPositions(null, null);
            RP = new Timer(1000);
            RP.Elapsed += RefreshPositions;
            RP.Start();
        }
        public async Task<double> DrawPolyline(Polyline polyline, SimpleWaypoint from, SimpleWaypoint to)
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
            else DisplayAlert("Routing error", string.Concat(response.ErrorDetails??new[]{ "Unknown error occured",""}), "Ok");

            return response.ResourceSets.First().Resources.OfType<Route>().First().TravelDistance;
        }

        public void RefreshPositions(object sender, ElapsedEventArgs e)
        {
            RP?.Stop();
            drivers = Server.GetDriversLocations();
            orders = Server.GetOrders();
            Geocoder geo = new Geocoder();
            foreach (var driver in drivers)
            {
                if (map.Pins.Any(d => d != null && d.Type==PinType.SavedPin && d.Label == driver.driverID.ToString()))
                {
                    Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Where(d => d.Type == PinType.SavedPin && d.Label == driver.driverID.ToString()).First().Position = new Position(driver.latitude ?? 0, driver.longitude ?? 0));
                }
                else
                {
                    Dispatcher.BeginInvokeOnMainThread(() => {
                        var pin = new Pin()
                        {
                            Position = new Position(driver.latitude ?? 0, driver.longitude ?? 0),
                            Label = driver.driverID.ToString(),
                            Type = PinType.SavedPin
                        };
                        map.Pins.Add(pin);
                        pin.MarkerClicked += Pin_MarkerClicked;
                    });
                }
            }
            foreach (var pin in map.Pins.Where(p => p !=null && p.Type == PinType.SavedPin))
            {
                if (!drivers.Select(d => d.driverID.ToString()).Contains(pin.Label)) Dispatcher.BeginInvokeOnMainThread(()=>map.Pins.Remove(pin));
            }
            foreach (var order in orders)
            {
                if (map.Pins.Any(d => d != null && d.Type == PinType.SearchResult && d.Label == order.orderID.ToString()))
                {
                    Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Where(d => d.Type == PinType.SearchResult && d.Label == order.orderID.ToString()).First().Position = new Position(order.latitudeFrom, order.longitudeFrom));
                }
                else
                {
                    Dispatcher.BeginInvokeOnMainThread(() => {
                        var pin= new Pin()
                        {
                            Position = new Position(order.latitudeFrom, order.longitudeFrom),
                            Label = order.orderID.ToString(),
                            Type = PinType.SearchResult
                        };
                        map.Pins.Add(pin);
                        pin.MarkerClicked += Pin_MarkerClicked;
                    });
                }
            }
            foreach (var pin in map.Pins.Where(p => p!=null && p.Type == PinType.SearchResult))
            {
                if (!orders.Select(d => d.orderID.ToString()).Contains(pin.Label)) Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Remove(pin));
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

        private async void Pin_MarkerClicked(object sender, PinClickedEventArgs e)
        {
            var pin = sender as Pin;
            if (pin.Type==PinType.SavedPin)
            {
                var nDriverID = int.Parse(pin.Label);
                if (DriverID == nDriverID)
                {
                    nDriverID = 0;
                    polylineDriver.Geopath.Clear();
                    chosenDriver.Text = "";
                }
                else
                {
                    DriverID = nDriverID;
                    driver = drivers.Where(d => d.driverID == DriverID).First();
                    chosenDriver.Text = $"{driver.longitude} {driver.latitude}";
                }
            }
            else if(pin.Type==PinType.SearchResult)
            {
                var nOrderID = int.Parse(pin.Label);
                if (OrderID == nOrderID)
                {
                    OrderID = 0;
                    polylineOrder.Geopath.Clear();
                    polylineDriver.Geopath.Clear();
                    chosenUser.Text = "";
                    UserPathLength = 0;
                    CalculatePrice(null, null);
                }
                else
                {
                    OrderID = nOrderID;
                    order = orders.Where(o => o.orderID == OrderID).First();
                    chosenUser.Text = $"{order.longitudeFrom} {order.latitudeFrom} - {order.longitudeTo} {order.latitudeTo} ";

                    UserPathLength = await DrawPolyline(polylineOrder, new SimpleWaypoint(order.latitudeFrom, order.longitudeFrom), new SimpleWaypoint(order.latitudeTo, order.longitudeTo));
                    CalculatePrice(null, null);
                }
            }
            if (OrderID == 0 || DriverID == 0) return;
            DrawPolyline(polylineDriver, new SimpleWaypoint(driver.latitude.GetValueOrDefault(), driver.longitude.GetValueOrDefault()), new SimpleWaypoint(order.latitudeFrom, order.longitudeFrom));
        }

        private void CalculatePrice(object sender, ValueChangedEventArgs e)
        {
            var price = KmPrice.Value;
            UserPathPrice = (price * UserPathLength);
            TotalPrice.Text = UserPathPrice.ToString("N2");
            distance.Text = UserPathLength.ToString("N1");
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Server.Connect(OrderID, DriverID);
            OrderID = 0;
            DriverID = 0;
            polylineDriver.Geopath.Clear();
            polylineOrder.Geopath.Clear();
            chosenDriver.Text = "";
            chosenUser.Text = "";
        }
    }


}

