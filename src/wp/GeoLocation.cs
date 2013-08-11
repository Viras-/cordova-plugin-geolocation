/*  
	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at
	
	http://www.apache.org/licenses/LICENSE-2.0
	
	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Device.Location;
using WPCordovaClassLib.Cordova;
using WPCordovaClassLib.Cordova.Commands;
using WPCordovaClassLib.Cordova.JSON;

namespace WPCordovaClassLib.Cordova.Commands
{
    /// <summary>
    /// This class provides the GeoLocation API implementation. It is required since the built in API does not support various properties (e.g. altitude)
    /// </summary>
    public class Geolocation : BaseCommand
    {
        protected GeoCoordinateWatcher watcher;
        protected Dictionary<string, string> watchIds;
        protected static int watcher_timeout = 5000;

        public Geolocation()
        {
            watchIds = new Dictionary<string, string>();

            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_PositionStatusChanged);
        }

        protected void startWatcher()
        {
            watcher.TryStart(true, TimeSpan.FromMilliseconds(watcher_timeout));
        }

        protected void stopWatcher()
        {
            if (watchIds.Count <= 0)
            {
                watcher.Stop();
            }
        }

        /// <summary>
        /// Helper function for forcing a valid number and preventing NaN as result
        /// </summary>
        /// <param name="input">value to check</param>
        /// <returns>double value, but NaN as 0.0</returns>
        protected double forceNumber(double input)
        {
            if (double.IsNaN(input))
            {
                return 0.0;
            }
            else
            {
                return input;
            }
        }

        protected string getLocationJSON(GeoCoordinate coord)
        {
            string res = String.Format("\"latitude\":{0},\"longitude\":{1},\"altitude\":{2},\"accuracy\":{3},\"heading\":{4},\"velocity\":{5},\"altitudeAccuracy\":{6}",
                forceNumber(coord.Latitude),
                forceNumber(coord.Longitude),
                forceNumber(coord.Altitude),
                forceNumber(coord.HorizontalAccuracy),
                forceNumber(coord.Course),
                forceNumber(coord.Speed),
                forceNumber(coord.VerticalAccuracy)
                );
            res = "{" + res + "}";

            return res;
        }

        public void getLocation(string options)
        {
            startWatcher();
            GeoCoordinate coord = watcher.Position.Location;
            stopWatcher();

            PluginResult pluginResult;
            if (!coord.IsUnknown)
            {
                pluginResult = new PluginResult(PluginResult.Status.OK, getLocationJSON(coord));
            }
            else
            {
                pluginResult = new PluginResult(PluginResult.Status.ERROR);
            }
            pluginResult.KeepCallback = true;

            DispatchCommandResult(pluginResult);
        }

        public void addWatch(string options)
        {
            string watchId = JsonHelper.Deserialize<string[]>(options)[0];
            string callbackId = JsonHelper.Deserialize<string[]>(options)[2];

            watchIds.Add(watchId, callbackId);

            startWatcher();

            /*PluginResult pluginResult = new PluginResult(PluginResult.Status.OK);
            pluginResult.KeepCallback = true;
            DispatchCommandResult(pluginResult);*/
        }

        public void clearWatch(string options)
        {
            string watchId = JsonHelper.Deserialize<string[]>(options)[0];

            watchIds.Remove(watchId);

            stopWatcher();

            DispatchCommandResult();
        }

        public void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            string res = getLocationJSON(e.Position.Location);
            PluginResult pluginResult = new PluginResult(PluginResult.Status.OK, res);
            pluginResult.KeepCallback = true;

            foreach (KeyValuePair<string, string> entry in watchIds)
            {
                DispatchCommandResult(pluginResult, entry.Value);
            }
        }

        public void watcher_PositionStatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
        }
    }
}
