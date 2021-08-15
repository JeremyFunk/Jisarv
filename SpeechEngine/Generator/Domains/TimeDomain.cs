using Jisarv.Data;
using Jisarv.SpeechEngine.Generator.Propagator.Grammer;
using System;
using System.Collections.Generic;

namespace Jisarv.SpeechEngine.Generator.Domains {
    class TimeDomain : Domain {
        


        public TimeDomain() : base("time", "en")
        {

        }

        public override Dictionary<string, Func<Dictionary<string, string>, InternalCallReturn>> LoadDomainFunctions()
        {
            var returnDic = new Dictionary<string, Func<Dictionary<string, string>, InternalCallReturn>>();
            returnDic.Add("BASIC_TIME", BasicTime);
            returnDic.Add("MAKE_APPOINTMENT", MakeAppointment);
            returnDic.Add("TEST", Test);

            return returnDic;
        }

        private InternalCallReturn BasicTime(Dictionary<string, string> variables)
        {
            var dateTime = DateTime.Now;
            var returnDic = new Dictionary<string, string>();


            if (variables.ContainsKey("REGION"))
            {
                var region = variables["REGION"];
                returnDic.Add("REGION", region);

                var timezone = GeoData.GetTimeZone(region);
                dateTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.FindSystemTimeZoneById(timezone));
            }


            returnDic.Add("TIME", dateTime.Hour + "");
            returnDic.Add("MINUTES", dateTime.Minute + "");

            return new InternalCallReturn{ Variables=returnDic, Call="BASIC_TIME" };
        }

        private InternalCallReturn MakeAppointment(Dictionary<string, string> variables)
        {
            var returnDic = new Dictionary<string, string>();

            var containsName = variables.ContainsKey("NAME");
            var containsPOI = variables.ContainsKey("POINT_IN_TIME");

            if (containsPOI && containsName)
            {
                return new InternalCallReturn { Variables = returnDic, Call = "MADE_APPOINTMENT" };
            }else if (containsPOI)
            {
                return new InternalCallReturn { Variables = returnDic, Call = "MAKE_APPOINTMENT_ASK_NAME" };
            }
            

            return new InternalCallReturn { Variables = returnDic, Call = "MAKE_APPOINTMENT_ASK_TIME" };
        }

        private InternalCallReturn Test(Dictionary<string, string> variables)
        {
            Console.WriteLine("Number was parsed as: " + variables["NUMBER"]);
            return new InternalCallReturn { Variables = new Dictionary<string, string>() };
        }
    }
}