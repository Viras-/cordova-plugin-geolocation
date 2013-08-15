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

        /// <summary>
        /// Prepare GeoLocation watching
        /// </summary>
        public Geolocation()
        {
            watchIds = new Dictionary<string, string>();

            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_PositionStatusChanged);
        }

        /// <summary>
        /// Start watching the location
        /// </summary>
        protected void startWatcher()
        {
            watcher.TryStart(true, TimeSpan.FromMilliseconds(watcher_timeout));
        }

        /// <summary>
        /// Stop watching the location
        /// </summary>
        protected void stopWatcher()
        {
            if (watchIds.Count <= 0)
            {
                watcher.Stop();
            }
        }

        /// <summary>
        /// Helper function for forcing a valid number and preventing NaN as result, in addition it takes care of correct number formatting
        /// </summary>
        /// <param name="input">value to check</param>
        /// <returns>string value, but NaN as 0.0</returns>
        protected string formatNumber(double input)
        {
            string value = "";

            // we do not want NaN as a result
            if (double.IsNaN(input))
            {
                value = "0.0";
            }
            else
            {
                value = input.ToString();
            }

            // make sure we use a '.' as separator
            return value.Replace(',', '.');
        }


        /// <summary>
        /// Format a GeoCoordinate object into a JSON object for sending to the brower
        /// </summary>
        /// <param name="coord">Coordinate object to convert</param>
        /// <returns>string containing a JSON-formatted object representation</returns>
        protected string getLocationJSON(GeoCoordinate coord)
        {
            string res = String.Format("\"latitude\":{0},\"longitude\":{1},\"altitude\":{2},\"accuracy\":{3},\"heading\":{4},\"velocity\":{5},\"altitudeAccuracy\":{6}",
                formatNumber(coord.Latitude),
                formatNumber(coord.Longitude),
                formatNumber(coord.Altitude),
                formatNumber(coord.HorizontalAccuracy),
                formatNumber(coord.Course),
                formatNumber(coord.Speed),
                formatNumber(coord.VerticalAccuracy)
                );
            res = "{" + res + "}";

            return res;
        }

        /// <summary>
        /// Called from the JavaScript bridge when the location should be fetched
        /// </summary>
        /// <param name="options">JSON-parameters</param>
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

        /// <summary>
        /// Called from the JavaScript bridge when location changes should be watched
        /// </summary>
        /// <param name="options"></param>
        public void addWatch(string options)
        {
            string watchId = JsonHelper.Deserialize<string[]>(options)[0];
            string callbackId = JsonHelper.Deserialize<string[]>(options)[2];

            watchIds.Add(watchId, callbackId);

            startWatcher();
        }

        /// <summary>
        /// Stop watching the location
        /// </summary>
        /// <param name="options"></param>
        public void clearWatch(string options)
        {
            string watchId = JsonHelper.Deserialize<string[]>(options)[0];

            watchIds.Remove(watchId);

            stopWatcher();

            DispatchCommandResult();
        }

        /// <summary>
        /// Callback which is called from the GeoCoordinateWatcher class once the location has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Called when the status of the position tracking has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void watcher_PositionStatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
        }
    }
}
