using Jisarv.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoTimeZone;
using TimeZoneConverter;

namespace Jisarv.Data
{
    class CityType
    {
        public string City, City_ASCII, Country, Iso2, Iso3, Admin_Name, Capital;
        public float LAT, LNG;
        public int Population, ID;
    }

    class GeoData
    {
        private static Dictionary<string, CityType[]> cities = new Dictionary<string, CityType[]>();

        public static void LoadGeoData()
        {
            string[] worldcities = IO.LoadFileLines(@"\res\geo\worldcities.csv");

            for (var i = 1; i < worldcities.Length; i++)
            {
                string[] splitted = worldcities[i].Split("\",\"");

                var popu = 0;

                var city = new CityType
                {
                    City = splitted[0].Replace("\"", ""),
                    City_ASCII = splitted[1].Replace("\"", ""),
                    LAT = float.Parse(splitted[2].Replace("\"", "").Replace(".", ",")),
                    LNG = float.Parse(splitted[3].Replace("\"", "").Replace(".", ",")),
                    Country = splitted[4].Replace("\"", ""),
                    Iso2 = splitted[5].Replace("\"", ""),
                    Iso3 = splitted[6].Replace("\"", ""),
                    Admin_Name = splitted[7].Replace("\"", ""),
                    Capital = splitted[8].Replace("\"", ""),
                    Population = int.TryParse(splitted[9].Replace("\"", "").Replace(".", ","), out popu) ? popu : 0,
                    ID = int.Parse(splitted[10].Replace("\"", "")),
                };


                if (cities.ContainsKey(splitted[0]))
                {
                    var curArray = new List<CityType>(cities[splitted[0]]);
                    curArray.Add(city);
                    cities[splitted[0]] = curArray.ToArray();
                }
                else
                {
                    var curArray = new List<CityType>();
                    curArray.Add(city);
                    cities[splitted[0].Replace("\"", "").ToLower()] = curArray.ToArray();
                }
            }
        }

        public static string GetTimeZone(string city)
        {
            var cityT = cities[city.ToLower()];

            if (cityT == null)
            {
                return null;
            }

            string tz = TimeZoneLookup.GetTimeZone(cityT[0].LAT, cityT[0].LNG).Result;

            return TZConvert.IanaToWindows(tz);
        }
    }
}
