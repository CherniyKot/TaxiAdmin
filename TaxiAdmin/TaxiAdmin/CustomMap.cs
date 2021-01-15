using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms.Maps;

namespace TaxiAdmin
{
    public class CustomMap: Map
    {
        //public new List<CustomPin> Pins { get; set; }

        public CustomMap(MapSpan region):base(region)
        {
            //Pins = new List<CustomPin>();
        }
        public CustomMap() : base()
        {
            //Pins = new List<CustomPin>();
        }
    }
    public class CustomPin : Pin
    {
        public int Id { get; set; }
        //public new CustomPinType Type { get; set; }
    }
    public enum CustomPinType
    {
        FreeDriver,
        DriverWithUser,
        User,
        Destination
    }
}
