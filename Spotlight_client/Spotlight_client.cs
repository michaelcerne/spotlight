﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CitizenFX.Core;
using API = CitizenFX.Core.Native.API;
using Config = Spotlight_client.Config;

namespace Spotlight_client
{
    public class Spotlight_client : BaseScript
    {
        public const string DECOR_NAME_STATUS = "SpotlightStatus";
        public const string DECOR_NAME_XY = "SpotlightDirXY";
        public const string DECOR_NAME_Z = "SpotlightDirZ";
        public const string DECOR_NAME_BRIGHTNESS = "SpotlightDirLevel";
        public static readonly string[] TRACKER_BONES = {
            "weapon_1barrel",
            "turret_1base"
        };

        public Spotlight_client()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);

            Tick += OnTick;
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (API.GetCurrentResourceName() != resourceName) return;

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
                    string baseBone = GetBaseBone(handle);
                    Vector3 baseCoords = GetBaseCoordinates(handle, baseBone);
                    Vector3 directionalCoords = GetDirectionalCoordinates(handle, baseBone);

                    SetSpotlightDefaultsIfNull(handle);

                    API.DrawSpotLight(
                        baseCoords.X, baseCoords.Y, baseCoords.Z,
                        directionalCoords.X, directionalCoords.Y, directionalCoords.Z,
                        221, 221, 221,
                        VehicleHasRotatableTargetBone(handle) ? 200f : 70f, // rotatable spotlights have longer max reach
                        API.DecorGetFloat(handle, DECOR_NAME_BRIGHTNESS),
                        4.3f,
                        15.0f,
                        28.6f
                    );
                }

            }
            await Task.FromResult(0);
        }

        private async Task ToggleSpotlight()
        {
            var vehicle = API.GetVehiclePedIsIn(API.GetPlayerPed(-1), false);
            var vehicleClass = API.GetVehicleClass(vehicle);

            if (!Config.GetValueBool(Config.EMERGENCY_ONLY, true) || (vehicleClass == 18 || VehicleHasRotatableTargetBone(vehicle)))
            {
                if (IsSpotlightEnabled(vehicle))
                {
                    API.DecorSetBool(vehicle, DECOR_NAME_STATUS, false);
                    API.DecorSetFloat(vehicle, DECOR_NAME_BRIGHTNESS, 0f);
                } else
                {
                    API.DecorSetBool(vehicle, DECOR_NAME_STATUS, true);
                    await TranslateDecorSmoothly(vehicle, DECOR_NAME_BRIGHTNESS, 0f, Config.GetValueFloat(Config.BRIGHTNESS_LEVEL, 30f), 30);
                }
            }
            await Task.FromResult(0);
        }

        private async Task MoveSpotlightVertical(bool up)
        {
            var vehicle = API.GetVehiclePedIsIn(API.GetPlayerPed(-1), Config.GetValueBool(Config.REMOTE_CONTROL, true));
            var current = API.DecorGetFloat(vehicle, DECOR_NAME_Z);

            if (up)
            {
                if (current <= 0.3f) await TranslateDecorSmoothly(vehicle, DECOR_NAME_Z, current, current + 0.1f, 10);
            } else
            {
                if (current >= -1.2f) await TranslateDecorSmoothly(vehicle, DECOR_NAME_Z, current, current - 0.1f, 10);
            }
        }

        private async Task MoveSpotlightHorizontal(bool left)
        {
            var vehicle = API.GetVehiclePedIsIn(API.GetPlayerPed(-1), Config.GetValueBool(Config.REMOTE_CONTROL, true));
            var current = API.DecorGetFloat(vehicle, DECOR_NAME_XY);

            if (left)
            {
                if (current <= Config.GetValueFloat(Config.RANGE_LEFT, 90f)) await TranslateDecorSmoothly(vehicle, DECOR_NAME_XY, current, current + 10f, 10);
            }
            else
            {
                if (current >= -Config.GetValueFloat(Config.RANGE_RIGHT, 30f)) await TranslateDecorSmoothly(vehicle, DECOR_NAME_XY, current, current - 10f, 10);
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

        private static string GetBaseBone(int handle)
        {
            foreach (string bone in TRACKER_BONES)
            {
                var boneIndex = API.GetEntityBoneIndexByName(handle, bone);
                if (boneIndex != -1)
                {
                    return bone;
                }
            }
            return "door_dside_f";
        }

        private static Vector3 GetBaseCoordinates(int handle, string bone)
        {
            return API.GetWorldPositionOfEntityBone(handle, API.GetEntityBoneIndexByName(handle, bone));
        }

        private static Vector3 GetDirectionalCoordinates(int handle, string bone)
        {
            if (bone == "door_dside_f") // target bone is not rotatable, use default orientation
            {
                Vector2 vehicleHeading = (Vector2)API.GetEntityForwardVector(handle);
                double vehicleHeadingAngle = AngleConverter(Convert.ToDouble(vehicleHeading.X), Convert.ToDouble(vehicleHeading.Y));

                return new Vector3(
                    new Vector2(
                        Convert.ToSingle(Math.Cos((vehicleHeadingAngle + API.DecorGetFloat(handle, DECOR_NAME_XY)) / 57.2957795131)),
                        Convert.ToSingle(Math.Sin((vehicleHeadingAngle + API.DecorGetFloat(handle, DECOR_NAME_XY)) / 57.2957795131))
                    ),
                    API.DecorGetFloat(handle, DECOR_NAME_Z)
                );
            }
            else // target bone is rotatable, convert to direction
            {
                Vector3 boneHeading = API.GetWorldRotationOfEntityBone(handle, API.GetEntityBoneIndexByName(handle, bone));

                return RotationToDirection(boneHeading);
            }
        }

        private static bool VehicleHasRotatableTargetBone(int handle)
        {
            if (GetBaseBone(handle) != "door_dside_f") return true;
            return false;
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

        public static Vector3 RotationToDirection(Vector3 Rotation) // credit to LETSPLAYORDY
        {
            float z = Rotation.Z;
            float num = z * 0.0174532924f;
            float x = Rotation.X;
            float num2 = x * 0.0174532924f;
            float num3 = Math.Abs((float)Math.Cos((double)num2));
            return new Vector3
            {
                X = (float)((double)((float)(-(float)Math.Sin((double)num))) * (double)num3),
                Y = (float)((double)((float)Math.Cos((double)num)) * (double)num3),
                Z = (float)Math.Sin((double)num2)
            };
        }
    }

}
