using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using API = CitizenFX.Core.Native.API;
using CitizenFX.Core;

namespace Spotlight_client
{
    public static class Config
    {
        // Config keys
        public const string EMERGENCY_ONLY = "emergency_only";
        public const string REMOTE_CONTROL = "remote_control";
        public const string BRIGHTNESS_LEVEL = "brightness_level";
        public const string RANGE_LEFT = "aim_range_left";
        public const string RANGE_RIGHT = "aim_range_right";

        public static string GetValueString(string key, string defaultValue)
        {
            try
            {
                string value = API.GetResourceMetadata(API.GetCurrentResourceName(), key, 0).ToLower();
                if (value == null) return defaultValue;
                return value;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static bool GetValueBool(string key, bool defaultValue)
        {
            try
            {
                string value = API.GetResourceMetadata(API.GetCurrentResourceName(), key, 0).ToLower();
                if (value != "true" && value != "false") return defaultValue;
                return value == "true";
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static float GetValueFloat(string key, float defaultValue)
        {
            try
            {
                string value = API.GetResourceMetadata(API.GetCurrentResourceName(), key, 0);
                if (value == null) return defaultValue;
                return Convert.ToSingle(value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
