using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace xamarinforms_demo
{
    public partial class MainPage : ContentPage
    {
        static readonly HttpClient client = new HttpClient();
        public MainPage()
        {
            InitializeComponent();
        }
        private async void CurrentLocationButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                // clear pins
                map.Pins.Clear();
                // get current location
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                var location = await Geolocation.GetLocationAsync(request);
                if (location != null)
                {
                    // set current location to gpslabel at the bottom of the screen
                    GPSlabel.Text = $"{location.Latitude},{location.Longitude}";
                    // enable IsShowingUser botton in the map
                    map.IsShowingUser = true;
                    // move the map to current location
                    Position position = new Position(Convert.ToDouble(location.Latitude), Convert.ToDouble(location.Longitude));
                    MapSpan mapSpan = new MapSpan(position, 0.015, 0.015);
                    map.MoveToRegion(mapSpan);
                    // get records from azure search
                    HttpResponseMessage res = await client.GetAsync(Constants.SearchRequestURL + "*" + "&api-key=" + Constants.SearchRequestKey);
                    string content = await res.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(content);
                    // get records nearby
                    var nearRecords = GetNearRecords(position, json);
                    // add pins nearby
                    foreach (JObject item in nearRecords)
                    {
                        AddPin(Convert.ToString(item.GetValue("NAME")), Convert.ToString(item.GetValue("DESCRIPTION")), new Position(Convert.ToDouble(item.GetValue("LAT")), Convert.ToDouble(item.GetValue("LONG"))));
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Location is not obtained.");
            }
        }
        private async void SearchLocationButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                // clear pins
                map.Pins.Clear();
                // get coordinates geocoded from entry.Text 
                Geocoder geoCoder = new Geocoder();
                IEnumerable<Position> approximateLocations = await geoCoder.GetPositionsForAddressAsync(entry.Text);
                Position position = approximateLocations.FirstOrDefault();
                if (position != null)
                {
                    // move the map to current location
                    MapSpan mapSpan = new MapSpan(position, 0.015, 0.015);
                    map.MoveToRegion(mapSpan);
                    // if keyphrase is not specified
                    if (string.IsNullOrEmpty(keyphrase.Text))
                    {
                        // set current location to gpslabel at the bottom of the screen
                        GPSlabel.Text = $"{position.Latitude},{position.Longitude}";
                        // get records from azure search
                        HttpResponseMessage res = await client.GetAsync(Constants.SearchRequestURL + "*" + "&api-key=" + Constants.SearchRequestKey);
                        string content = await res.Content.ReadAsStringAsync();
                        JObject json = JObject.Parse(content);
                        // get records nearby
                        var nearRecords = GetNearRecords(position, json);
                        // get trend keyphrase
                        Trend.Text = getTrendKeyphrase(nearRecords);
                        // add pins nearby
                        foreach (JObject item in nearRecords)
                        {
                            AddPin(item.GetValue("NAME").ToString(), item.GetValue("DESCRIPTION").ToString(), new Position(Convert.ToDouble(item.GetValue("LAT")), Convert.ToDouble(item.GetValue("LONG"))));
                        }
                    }
                    else // if keyphrase s specified
                    {
                        // set current location to gpslabel at the bottom of the screen
                        GPSlabel.Text = $"{position.Latitude},{position.Longitude}";
                        // get records from azure search with keyphrase specified 
                        HttpResponseMessage res = await client.GetAsync(Constants.SearchRequestURL + keyphrase.Text + "searchFields=keyphrases&api-key=" + Constants.SearchRequestKey);
                        string content = await res.Content.ReadAsStringAsync();
                        JObject json = JObject.Parse(content);
                        // get records nearby
                        var nearRecords = GetNearRecords(position, json);
                        // add pins nearby
                        foreach (JObject item in nearRecords)
                        {
                            AddPin(item.GetValue("NAME").ToString(), item.GetValue("DESCRIPTION").ToString(), new Position(Convert.ToDouble(item.GetValue("LAT")), Convert.ToDouble(item.GetValue("LONG"))));
                        }
                    }
                }

            }
            catch (Exception)
            {
                Console.WriteLine("Location cannot be obtained.");
            }
        }
        private List<Object> GetNearRecords(Position position, JObject json)
        {
            var nearRecords = new List<Object>();
            foreach (JObject item in json["value"])
            {
                if ((Math.Abs(position.Latitude - Convert.ToDouble(item.GetValue("LAT"))) <= 0.05) && (Math.Abs(position.Longitude - Convert.ToDouble(item.GetValue("LONG"))) <= 0.05))
                {
                    nearRecords.Add(item);
                }
            }
            return nearRecords;
        }
        private string getTrendKeyphrase(List<object> nearRecords)
        {
            var listKeyphrase = new List<string>();
            foreach (JObject item in nearRecords)
            {
                foreach (var kp in item["keyphrases"])
                {
                    listKeyphrase.Add(kp.ToString());
                }
            }
            var mostCommon = (from item in listKeyphrase
                              group item by item into g
                              orderby g.Count() descending
                              select g.Key).First();
            return mostCommon;
        }
        private void AddPin(string name, string desc, Position position)
        {
            Pin pin = new Pin
            {
                Label = name,
                Address = desc,
                Type = PinType.Place,
                Position = position
            };
            map.Pins.Add(pin);
        }
    }
}
