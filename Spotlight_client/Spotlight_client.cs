using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

using CitizenFX.Core;
using API = CitizenFX.Core.Native.API;

namespace Spotlight_client
{
    public class Spotlight_client : BaseScript
    {
        readonly string DECOR_NAME_STATUS = "SpotlightStatus";
        readonly string DECOR_NAME_XY = "SpotlightDirXY";
        readonly string DECOR_NAME_Z = "SpotlightDirZ";
        readonly string DECOR_NAME_BRIGHTNESS = "SpotlightDirLevel";

        public Spotlight_client()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);

            Tick += OnTick;
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (API.GetCurrentResourceName() != resourceName)
            {
                return;
            }

            API.DecorRegister(DECOR_NAME_STATUS, 3);
            API.DecorRegister(DECOR_NAME_XY, 3);
            API.DecorRegister(DECOR_NAME_Z, 3);
            API.DecorRegister(DECOR_NAME_BRIGHTNESS, 3);

            API.RegisterCommand("spotlight", new Action<int, List<object>, string>(async (src, args, raw) =>
            {
                await ToggleSpotlight();
            }), false);

            API.RegisterCommand("spotlightaim", new Action<int, List<object>, string>(async (src, args, raw) =>
            {
                var vehicle = API.GetVehiclePedIsIn(API.GetPlayerPed(-1), false);

                switch (args[0])
                {
                    case "up":
                        await MoveSpotlightVertical(true);
                        break;
                    case "down":
                        await MoveSpotlightVertical(false);
                        break;
                    case "left":
                        await MoveSpotlightHorizontal(true);
                        break;
                    case "right":
                        await MoveSpotlightHorizontal(false);
                        break;
                }
            }), false);

            API.RegisterKeyMapping("spotlight", "Toggle spotlight", "keyboard", "l");
            API.RegisterKeyMapping("spotlightaim up", "Aim spotlight up", "keyboard", "prior");
            API.RegisterKeyMapping("spotlightaim down", "Aim spotlight down", "keyboard", "pagedown");
            API.RegisterKeyMapping("spotlightaim left", "Aim spotlight left", "keyboard", "delete");
            API.RegisterKeyMapping("spotlightaim right", "Aim spotlight right", "keyboard", "end");
        }

        private async Task OnTick()
        {
            foreach (var vehicle in World.GetAllVehicles())
            {
                var handle = vehicle.Handle;
                if (IsSpotlightEnabled(handle))
                {
                    var door = API.GetEntityBoneIndexByName(handle, "door_dside_f");
                    var coords = API.GetWorldPositionOfEntityBone(handle, door);
                    var doorCoords = API.GetWorldPositionOfEntityBone(handle, door);
                    var carHeadingVector = (Vector2) API.GetEntityForwardVector(handle);
                    var carHeadingVectorAngle = AngleConverter(Convert.ToDouble(carHeadingVector.X), Convert.ToDouble(carHeadingVector.Y));

                    SetSpotlightDefaultsIfNull(handle);

                    var finalVector = new Vector3(
                        new Vector2(
                            Convert.ToSingle(Math.Cos((carHeadingVectorAngle + API.DecorGetFloat(handle, DECOR_NAME_XY)) / 57.2957795131)),
                            Convert.ToSingle(Math.Sin((carHeadingVectorAngle + API.DecorGetFloat(handle, DECOR_NAME_XY)) / 57.2957795131))
                        ),
                        API.DecorGetFloat(handle, DECOR_NAME_Z)
                    );

                    API.DrawSpotLight(coords.X, doorCoords.Y, coords.Z + 0.35f, finalVector.X, finalVector.Y, finalVector.Z, 221, 221, 221, 70.0f, API.DecorGetFloat(handle, DECOR_NAME_BRIGHTNESS), 4.3f, 15.0f, 28.6f);
                }

            }
            await Task.FromResult(0);
        }

        private async Task ToggleSpotlight()
        {
            var vehicle = API.GetVehiclePedIsIn(API.GetPlayerPed(-1), false);

            if (API.GetVehicleClass(vehicle) == 18)
            {
                if (IsSpotlightEnabled(vehicle))
                {
                    API.DecorSetBool(vehicle, DECOR_NAME_STATUS, false);
                    API.DecorSetFloat(vehicle, DECOR_NAME_BRIGHTNESS, 0f);
                } else
                {
                    API.DecorSetBool(vehicle, DECOR_NAME_STATUS, true);
                    await TranslateDecorSmoothly(vehicle, DECOR_NAME_BRIGHTNESS, 0f, 30f, 30);
                }
            }
            await Task.FromResult(0);
        }

        private async Task MoveSpotlightVertical(bool up)
        {
            var vehicle = API.GetVehiclePedIsIn(API.GetPlayerPed(-1), true);
            var current = API.DecorGetFloat(vehicle, DECOR_NAME_Z);

            if (up)
            {
                if (current <= 0.1f) await TranslateDecorSmoothly(vehicle, DECOR_NAME_Z, current, current + 0.1f, 10);
            } else
            {
                if (current >= -1.2f) await TranslateDecorSmoothly(vehicle, DECOR_NAME_Z, current, current - 0.1f, 10);
            }
        }

        private async Task MoveSpotlightHorizontal(bool left)
        {
            var vehicle = API.GetVehiclePedIsIn(API.GetPlayerPed(-1), true);
            var current = API.DecorGetFloat(vehicle, DECOR_NAME_XY);

            if (left)
            {
                if (current <= 90f) await TranslateDecorSmoothly(vehicle, DECOR_NAME_XY, current, current + 10f, 10);
            }
            else
            {
                if (current >= -30f) await TranslateDecorSmoothly(vehicle, DECOR_NAME_XY, current, current - 10f, 10);
            }
        }

        private bool IsSpotlightEnabled(int vehicle)
        {
            if (!API.DecorExistOn(vehicle, DECOR_NAME_STATUS)) return false;
            return API.DecorGetBool(vehicle, DECOR_NAME_STATUS);
        }

        private void SetSpotlightDefaultsIfNull(int handle)
        {
            if (!API.DecorExistOn(handle, DECOR_NAME_XY))
            {
                API.DecorSetFloat(handle, DECOR_NAME_XY, 0f);
            }
            if (!API.DecorExistOn(handle, DECOR_NAME_Z))
            {
                API.DecorSetFloat(handle, DECOR_NAME_Z, 0.05f);
            }
            if (!API.DecorExistOn(handle, DECOR_NAME_BRIGHTNESS))
            {
                API.DecorSetFloat(handle, DECOR_NAME_BRIGHTNESS, 0f);
            }
        }

        private static double AngleConverter(double x, double y) // credit to Aidan Ferry
        {
            double[] angles = new double[4];
            double newRad = Math.Sqrt(x * x + y * y);
            x /= newRad;
            y /= newRad;
            angles[0] = RadianToDegree(Math.Acos(x));
            angles[1] = ((x == 1 || x == -1) ? angles[0] : 360 - angles[0]);
            angles[2] = RadianToDegree(Math.Asin(y));
            angles[3] = ((y == 1 || y == -1) ? angles[2] : 180 - angles[2]);
            if (angles[2] < 0.0) angles[2] += 360.0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (i != j && (Approximates(angles[i], angles[j]))) return (angles[i] + angles[j]) / 2;
                }
            }
            return -1;
        }

        private static double RadianToDegree(double rad)
        {
            return rad / Math.PI * 180.0f;
        }

        private static bool Approximates(double a, double b)
        {
            return Math.Abs(a - b) < 0.5;
        }

        private static async Task TranslateDecorSmoothly(int handle, string decorName, float from, float to, int timeMs)
        {
            for (int i = 0; i < 10; i++)
            {
                API.DecorSetFloat(handle, decorName, from + (to - from) * i/10);
                await Delay(timeMs);
            }
        }
    }

}
