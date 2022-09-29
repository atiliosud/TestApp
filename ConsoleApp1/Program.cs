using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ThirdPartyWeatherServiceLibrary;

namespace TestApp
{
    internal class Program
    {

        /// <summary>
        /// The program uses a third party library called WeatherService to display the average temperature by different locations and dates.
        /// Output example:
        ///            Location |       Date | Temperature
        ///---------------------+------------+------------
        ///              Alpena |   1/3/2016 |          27
        ///              Alpena |  1/10/2016 |          24
        ///              Alpena |  1/17/2016 |          18
        ///              ...
        /// </summary>
        static int Main(string[] args)
        {
            return new PrintLocation().printLocation();
        }

    }
}