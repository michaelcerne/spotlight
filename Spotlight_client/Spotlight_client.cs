using System;
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
            int playerVehicle = API.GetVehiclePedIsIn(API.GetPlayerPed(-1), false);

            foreach (var vehicle in World.GetAllVehicles())
            {
                var handle = vehicle.Handle;
                if (IsSpotlightEnabled(handle))
                {
                    string baseBone = GetBaseBone(handle);
                    Vector3 baseCoords = GetBaseCoordinates(handle, baseBone);
                    Vector3 directionalCoords = GetDirectionalCoordinates(handle, baseBone);

                    if (API.GetVehicleEngineHealth(handle) <= -4000) return;

                    SetSpotlightDefaultsIfNull(handle);
                    if (handle == playerVehicle)
                    {
                        DrawSpotlightLabel(true);
                    }

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

            if (IsSpotlightUsageAllowed(vehicle))
            {
                if (!API.NetworkHasControlOfEntity(vehicle))
                {
                    ShowControlFailureMessage();
                    return;
                }

                if (IsSpotlightEnabled(vehicle))
                {
                    API.DecorSetBool(vehicle, DECOR_NAME_STATUS, false);
                    API.DecorSetFloat(vehicle, DECOR_NAME_BRIGHTNESS, 0f);
                    for (int i = 0; i < 250; i++)
                    {
                        if (IsSpotlightEnabled(vehicle)) break;
                        DrawSpotlightLabel(false);
                        await Delay(0);
                    }
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

            if (!IsPlayerNearbyVehicle(API.GetPlayerPed(-1), vehicle)) return;
            if (!API.NetworkHasControlOfEntity(vehicle))
            {
                ShowControlFailureMessage();
                return;
            }

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
            bool isHeli = API.GetVehicleClass(vehicle) == 15;

            if (!IsPlayerNearbyVehicle(API.GetPlayerPed(-1), vehicle)) return;
            if (!API.NetworkHasControlOfEntity(vehicle))
            {
                ShowControlFailureMessage();
                return;
            }

            if (left)
            {
                if (isHeli || current <= Config.GetValueFloat(Config.RANGE_LEFT, 90f)) await TranslateDecorSmoothly(vehicle, DECOR_NAME_XY, current, current + 10f, 10);
            }
            else
            {
                if (isHeli || current >= -Config.GetValueFloat(Config.RANGE_RIGHT, 30f)) await TranslateDecorSmoothly(vehicle, DECOR_NAME_XY, current, current - 10f, 10);
            }
        }

        private bool IsSpotlightEnabled(int vehicle)
        {
            if (!API.DecorExistOn(vehicle, DECOR_NAME_STATUS)) return false;
            return API.DecorGetBool(vehicle, DECOR_NAME_STATUS);
        }

        private static bool IsSpotlightUsageAllowed(int handle)
        {
            int vehicleClass = API.GetVehicleClass(handle);
            bool isHelicopter = vehicleClass == 15;
            bool isEmergency = vehicleClass == 18;

            if (isHelicopter)
            {
                if (Config.GetValueBool(Config.HELICOPTER_SUPPORT, true))
                {
                    if (Config.GetValueBool(Config.HELICOPTER_POLMAV_ONLY, false))
                    {
                        if (API.IsVehicleModel(handle, (uint)API.GetHashKey("polmav"))) return true;
                        return false;
                    } else return true;
                } else return false;
            }

            if (VehicleHasRotatableTargetBone(handle) && Config.GetValueBool(Config.TURRET_SUPPORT, true)) return true;

            if (isEmergency || !Config.GetValueBool(Config.EMERGENCY_ONLY, true)) return true;

            return false;
        }

        private static void SetSpotlightDefaultsIfNull(int handle)
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
                Vector2 vehicleHeading = (Vector2) API.GetEntityForwardVector(handle);
                double vehicleHeadingAngle = Utilities.DirectionToAngle(Convert.ToDouble(vehicleHeading.X), Convert.ToDouble(vehicleHeading.Y));

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

                if (API.GetVehicleClass(handle) == 15) // helicopters retain manual offset
                {
                    double angle = Utilities.DirectionToAngle((Vector2) Utilities.RotationToDirection(boneHeading));

                    return new Vector3(
                        new Vector2(
                            Convert.ToSingle(Math.Cos((angle + API.DecorGetFloat(handle, DECOR_NAME_XY)) / 57.2957795131)),
                            Convert.ToSingle(Math.Sin((angle + API.DecorGetFloat(handle, DECOR_NAME_XY)) / 57.2957795131))
                        ),
                        API.DecorGetFloat(handle, DECOR_NAME_Z)
                    );
                }

                return Utilities.RotationToDirection(boneHeading);
            }
        }

        private static bool VehicleHasRotatableTargetBone(int handle)
        {
            if (GetBaseBone(handle) != "door_dside_f") return true;
            return false;
        }

        private static async Task TranslateDecorSmoothly(int handle, string decorName, float from, float to, int timeMs)
        {
            for (int i = 0; i < 10; i++)
            {
                API.DecorSetFloat(handle, decorName, from + (to - from) * i/10);
                await Delay(timeMs);
            }
        }

        private static void DrawSpotlightLabel(bool status)
        {
            if (!Config.GetValueBool(Config.MESSAGE_ENABLED, true)) return;
            API.SetTextFont(0);
            API.SetTextProportional(true);
            API.SetTextScale(0.0f, 0.3f);
            if (status)
            {
                API.SetTextColour(144, 238, 144, 255);
            } else
            {
                API.SetTextColour(238, 144, 144, 255);
            }
            API.SetTextDropshadow(0, 0, 0, 0, 255);
            API.SetTextEdge(1, 0, 0, 0, 255);
            API.SetTextDropShadow();
            API.SetTextOutline();
            API.SetTextEntry("STRING");
            API.AddTextComponentString("Spotlight: " + (status ? "ON" : "OFF"));
            API.SetTextRightJustify(Config.GetValueBool(Config.MESSAGE_RIGHT_ALIGNED, false));
            API.DrawText(0.005f, 0.480f);
        }

        private static void ShowControlFailureMessage()
        {
            API.BeginTextCommandDisplayHelp("STRING");
            API.AddTextComponentSubstringPlayerName("Only the owner of this vehicle can control the spotlight.");
            API.EndTextCommandDisplayHelp(0, false, true, 3000);
        }

        private static bool IsPlayerNearbyVehicle(int ped, int vehicle)
        {
            Vector3 pedCoords = API.GetEntityCoords(ped, true);
            Vector3 vehicleCoords = API.GetEntityCoords(vehicle, true);
            float distance = API.GetDistanceBetweenCoords(pedCoords.X, pedCoords.Y, pedCoords.Z, vehicleCoords.X, vehicleCoords.Y, vehicleCoords.Z, true);

            if (distance >= 3.0f)
            {
                return false;
            }
            return true;
        }
    }

}
