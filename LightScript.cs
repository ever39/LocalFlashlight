using BepInEx;
using GameNetcodeStuff;
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace localFlashlight
{
    internal class LightScript : MonoBehaviour
    {
        #region Values
        //get player controller
        private PlayerControllerB player_controller;

        //the gameobjects used
        private GameObject player, cameraObject, lightContainer, lightObject;

        //audioclips and audiosource
        public static AudioClip toggleon;
        public static AudioClip toggleoff;
        public static AudioClip denytoggle;
        public static AudioClip nochargetoggle;
        public AudioSource flashSource;

        //THE LIGHT.
        private Light locallight;

        //bool to check if light is positioned correctly so you avoid having a circle of holy light as your crosshair instead :)
        public static bool positioned = false;

        //flashlight state
        private bool flashState;
        public static bool publicFlashState;

        //battery configs
        public static float maxBatteryTime;
        public static float batteryTime = maxBatteryTime;
        private float batteryRegen;
        private float regenCool;
        private float batteryCooldown;
        private float burnOutCooldown;

        //battery values
        public static float BatteryPercent;
        public static float truePercentBattery;
        public static float BatteryClamped;

        //ui hide delay
        public static float UIHideTime;

        //shadow toggle
        public static bool enabledShadows;

        #endregion

        public void Update()
        {
            try
            {
                if (player == null)
                {

                    ///try to find local player gameobject, and if its found then proceed to the very important code
                    player = GameNetworkManager.Instance.localPlayerController.gameObject;

                    Plugin.mls.LogInfo("Found player gameobject, setting config values.");

                    #region setting values on the singular frame where the mod tries to find the player

                    positioned = false;

                    flashState = false;

                    maxBatteryTime = Plugin.BatteryLife.Value;

                    batteryTime = maxBatteryTime;

                    UIHideTime = 3f + Plugin.HideUIDelay.Value;

                    enabledShadows = Plugin.ShadowsEnabled.Value;

                    burnOutCooldown = Plugin.BurnOutCool.Value;

                    batteryCooldown = Plugin.BatteryCool.Value;

                    batteryRegen = Plugin.RechargeMult.Value;

                    if(Plugin.soundOption.Value == SoundOptions.Default)
                    {
                        toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton");
                        toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lightoff");
                        denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                        nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle");
                    }

                    if(Plugin.soundOption.Value == SoundOptions.ActualFlashlight)
                    {
                        toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton1");
                        toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lightoff1");
                        denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                        nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle1");
                    }

                    #endregion

                    if (player != null)
                    {
                        ///very important code, base of the entire flashlight

                        player_controller = player.GetComponent<PlayerControllerB>();

                        cameraObject = player.transform.Find("ScavengerModel/metarig/CameraContainer/MainCamera").gameObject;

                        //lightContainer = new GameObject();
                        //lightContainer.transform.SetParent(cameraObject.transform, false);

                        lightObject = new GameObject();

                        lightObject.transform.SetParent(cameraObject.transform, false);

                        //lightContainer.name = "LightContainer";
                        lightObject.name = "lightObject";

                        locallight = lightObject.AddComponent<Light>();
                        locallight.type = LightType.Spot;
                        locallight.shape = LightShape.Cone;
                        locallight.color = new Color((float)Plugin.FlashColorRed.Value / 255, (float)Plugin.FlashColorGreen.Value / 255, (float)Plugin.FlashColorBlue.Value / 255);
                        locallight.intensity = Plugin.Intensity.Value;
                        locallight.range = Plugin.Range.Value;
                        locallight.shadows = LightShadows.Hard;
                        locallight.spotAngle = Plugin.Angle.Value;

                        //FOR WHATEVER GOD AWFUL REASON, ADDING SHADOWS SCREWS UP THINGS WHEN YOU LOOK DOWN. DO NOT LOOK DOWN.
                        if (enabledShadows && lightObject != null)
                        {
                            var HDRPLight = lightObject.AddComponent<HDAdditionalLightData>();
                            HDRPLight.EnableShadows(true);
                        }
                        else Plugin.mls.LogInfo("shadow toggle not on, skipped creating them");

                        //flashlight sounds (why don't i just add the audiosource to the gameobject?)
                        flashSource = lightObject.AddComponent<AudioSource>();
                        flashSource.loop = false;
                        flashSource.playOnAwake = false;
                        flashSource.volume = Plugin.FlashVolume.Value;
                        flashSource.priority = 0;

                        if (!positioned)
                        {
                            lightObject.transform.localPosition = new Vector3(0, -0.55f, 0.5f);
                            lightObject.transform.Rotate(new Vector3(-12, 0, 0));
                            positioned = true;
                        }

                        locallight.enabled = false;
                        lightObject.SetActive(true);

                        Plugin.mls.LogInfo("light built, note that some values can't be updated in-game");
                        //i probably shouldn't spam logs with random values that the player probably already knows -> Plugin.mls.LogInfo($"Light has been made, unchangeable values in-game are: \nmaxBatteryTime: {maxBatteryTime}\nUIHideDelay: {UIHideTime}\nregenMultiplier: {batteryRegen}\nbattery cooldowns: {batteryCooldown}, {burnOutCooldown}\nshadows enabled: {enabledShadows}\nHUD style: {Plugin.batteryDisplay.Value}\nHUD text style: {Plugin.textDisplay.Value}\nNOTE:The values that are not listed here can be changed in-game.");
                    }
                }
            }
            //(nevermind its used now) catching errors :)
            catch (Exception e)
            {
                Plugin.mls.LogError($"if you see this, please report it to me on github with a screenshot of the error shown thank you\n{e}");
                return;
            }

            //handling death
            if (player_controller.isPlayerDead)
            {
                flashState = false;
                lightObject.SetActive(false);
                batteryTime = maxBatteryTime;
                regenCool = 0;
            }

            #region Resetting values to update alongside config changes

            locallight.color = new Color((float)Plugin.FlashColorRed.Value / 255, (float)Plugin.FlashColorGreen.Value / 255, (float)Plugin.FlashColorBlue.Value / 255);

            locallight.intensity = Plugin.Intensity.Value;
            locallight.range = Plugin.Range.Value;
            locallight.spotAngle = Plugin.Angle.Value;

            flashSource.volume = Plugin.FlashVolume.Value;

            #endregion

            #region Previously in LateUpdate, now in Update. Still used for battery management.

            //handling battery regen!
            if (!flashState)
            {
                if (batteryTime <= maxBatteryTime - 0.001)
                {
                    if (regenCool < 0) batteryTime += batteryRegen * Time.deltaTime;
                }

                if (batteryTime >= maxBatteryTime)
                {
                    batteryTime = maxBatteryTime;
                }
            }

            //handling toggling the flashlight back off when it is under 0
            if (flashState)
            {
                batteryTime -= Time.deltaTime;
                if (batteryTime < 0)
                {
                    Toggle();
                }
            }

            //handling UI hiding

            if (BatteryPercent <= 99 | flashState)
            {
                UIHideTime = Plugin.HideUIDelay.Value;
            }
            if (BatteryPercent > 99 && !flashState)
            {
                UIHideTime -= Time.deltaTime;
            }

            //updating values

            publicFlashState = flashState;

            regenCool -= Time.deltaTime;

            BatteryPercent = (int)(Math.Ceiling(batteryTime / maxBatteryTime * 100));

            BatteryClamped = batteryTime / maxBatteryTime;

            truePercentBattery = BatteryClamped * 100;
            #endregion

            //calling toggle script

            if (Plugin.flashlightToggleInstance.toggleKey.triggered && !(player_controller.quickMenuManager.isMenuOpen || player_controller.isPlayerDead || player_controller.inTerminalMenu || player_controller.isTypingChat))
            {
                if (batteryTime > 0)
                {
                    Toggle();
                }
                else flashSource.PlayOneShot(denytoggle);
            }
        }

        //the toggle void!
        public void Toggle()
        {
            if (!(player == null || cameraObject == null))
            {
                flashState = !flashState;
                locallight.enabled = flashState;
                if (flashState)
                {
                    flashSource.PlayOneShot(toggleon);
                    Plugin.mls.LogInfo("toggled light on");
                }
                else
                {
                    if (batteryTime <= 0 && Plugin.BatteryBurnOut.Value)
                    {
                        regenCool = burnOutCooldown;
                        Plugin.mls.LogInfo("No battery, setting cooldown to the burnt out value");
                    }
                    else
                    {
                        regenCool = batteryCooldown;
                        Plugin.mls.LogInfo($"Battery at {BatteryPercent}%, setting cooldown to normal");
                    }
                    if (batteryTime <= 0) flashSource.PlayOneShot(nochargetoggle);
                    else flashSource.PlayOneShot(toggleoff);
                    Plugin.mls.LogInfo("toggled light off");
                }
            }
        }
    }
}
