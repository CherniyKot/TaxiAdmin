using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace TaxiAdmin
{
    public static class Server
    {
        public static DriverLocation[] GetDriversLocations()
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
            var request = WebRequest.Create("https://localhost:44368/Api/DriverLocations");
            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch
            {
                return new DriverLocation[] { };
            }

            DriverLocation[] result;
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string r = reader.ReadLine();
                try
                {
                    result = JsonConvert.DeserializeObject<DriverLocation[]>(r);
                }
                catch
                {
                    return null;
                }
            }
            return result;
        }

        public static OrderLocation[] GetOrders()
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
            var request = WebRequest.Create("https://localhost:44368/Api/OrderLocations");
            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch
            {
                return new OrderLocation[] { };
            }

            OrderLocation[] result;
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string r = reader.ReadLine();
                try
                {
                    result = JsonConvert.DeserializeObject<OrderLocation[]>(r);
                }
                catch
                {
                    return null;
                }
            }
            return result;
        }
    }

    public class DriverLocation
    {
        public int driverID { get; set; }
        public string driverName { get; set; }
        public string driverCar { get; set; }
        public double? longitude { get; set; }
        public double? latitude { get; set; }
    }


    public class OrderLocation
    {
        public int orderID { get; set; }
        public string userName { get; set; }
        public double longitudeFrom { get; set; }
        public double latitudeFrom { get; set; }
        public double longitudeTo { get; set; }
        public double latitudeTo { get; set; }
    }
}
