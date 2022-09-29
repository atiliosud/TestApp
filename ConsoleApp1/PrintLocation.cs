using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWeatherServiceLibrary;

namespace TestApp
{
    public class PrintLocation
    {
        /// <summary>
        /// A collection of request guids and associated weather infor responses.
        /// </summary>
        static Dictionary<Guid, List<WeatherInfo>> responses = new Dictionary<Guid, List<WeatherInfo>>();

        static List<IntPtr> channels = new List<IntPtr>();

        public int printLocation()
        {
            try
            {
                //1. Get a list of all supported locations.
                var locations = WeatherService.GetSupportedLocations();

                //2. Communication with WeatherService is performed via channels, that need to be opened first.
                //WeatherService is currently limited to 3 channels. If you try to open more than 3 channels,
                //the service will respond with TooManyChannels result.
                if (!OpenChannel())
                    return 1;

                //3. Setup channels callbacks to receive WeatherInfoReady notifications.
                //The same callback will be used for all channels.
                foreach (var channel in channels)
                {
                    if (WeatherService.RegisterNotificationCallback(channel, WeatherServiceNotificationKind.WeatherInfoReady, OnWeatherInfoReady) != ResultKind.Ok)
                        return 1;
                }

                //4. Request weather information for all locations in a background task.
                var requestsTask = Task.Run(() =>
                {
                    foreach (var location in locations)
                    {
                        Guid request;
                        ResultKind result = ResultKind.TooManyRequests;
                        do
                        {
                            //4.1 A WeatherSevice channel can process only few requests simultaneously.
                            //Try sending the request to the first channel. If it is full, try sending to the second and the third channels.
                            //If they are also full, then just wait a little.
                            foreach (var channel in channels)
                            {
                                result = WeatherService.SendWeatherRequest(channel, location, null, out request);
                                if (result != ResultKind.TooManyRequests)
                                    break;
                            }

                            if (result == ResultKind.TooManyRequests)
                                Thread.Sleep(100); //wait a little.
                        }
                        while (result == ResultKind.TooManyRequests);
                    }
                });

                //5. Print the weather info.
                printLocation(locations);

                //Wait for background task to complete.
                requestsTask.Wait();

                //Close the channels.
                CloseChannel();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred:\n" + ex.ToString());
                return 1;
            }
        }

        static void printLocation(IEnumerable<string> locations)
        {
            int numberOfRequests = locations.Count();
            int numberOfResponsesPrinted = 0;
            //5.1 Print the header.
            Console.WriteLine($"{"Location",20} | {"Date",10} | {"Temperature",10}");
            Console.WriteLine($"{"".PadLeft(20, '-')}-+-{"".PadLeft(10, '-')}-+-{"".PadLeft(11, '-')}");

            while (numberOfResponsesPrinted != numberOfRequests)
            {
                //5.2 Print all responses available at the moment.
                foreach (var responseId in responses.Keys.ToArray())
                {
                    //Remove the response from the responses to avoid printing it again.
                    var response = responses[responseId];

                    if (response == null)
                    {
                        ++numberOfResponsesPrinted;
                        continue;
                    }

                    responses.Remove(responseId);

                    //Print each weather info record. Format the string according to the header columns.
                    foreach (var wi in response)
                    {
                        //            Location |       Date | Temperature
                        //---------------------+------------+------------
                        Console.WriteLine($"{wi.StationCity,20} | {wi.DateFull.ToShortDateString(),10} | {wi.TemperatureAvg,11}");
                    }
                    //Increase the number of already printed responses.
                    ++numberOfResponsesPrinted;
                }
                //Wait a litle for the next weather info responses to come in.
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// A callback to receive the WeatherInfoReady notification.
        /// </summary>
        static void OnWeatherInfoReady(WeatherServiceNotificationKind notificationKind, object notificationData)
        {
            var data = (WeatherInfoReadyNotification)notificationData;

            //Keep calling the WeatherService.GetNextWeatherInfoPage method until we get all records for the requested weather info. 
            List<WeatherInfo> weatherInfo = new List<WeatherInfo>();
            bool hasMoreData = true;
            while (hasMoreData)
            {
                var result = WeatherService.GetNextWeatherInfoPage(data.ChannelHandle, data.RequestId, out var weatherInfoPage, out hasMoreData);
                if (result == ResultKind.Ok)
                    weatherInfo.AddRange(weatherInfoPage);
            }

            //Save the response to be printed later.
            responses[data.RequestId] = weatherInfo;
        }

        static bool OpenChannel()
        {
            for (int i = 0; i < 3; i++)
            {
                if (WeatherService.OpenChannel(out var channel) == ResultKind.Ok)
                    channels.Add(channel);
                else
                    return false;
            }
            return true;
        }

        static void CloseChannel()
        {
            foreach (var channel in channels)
            {
                WeatherService.CloseChannel(channel);
            }
        }
    }
}
