using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace localFlashlight
{
    [BepInPlugin("command.localFlashlight", "Local Flashlight", VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.0.0";

        //configs...........
        public static ConfigEntry<KeyCode> toggleKey;
        public static ConfigEntry<float> intensity;
        public static ConfigEntry<float> range;
        public static ConfigEntry<float> angle;
        public static ConfigEntry<float> batteryLife;
        public static ConfigEntry<float> rechargeMult;
        public static ConfigEntry<BatteryDisplayOptions> batteryDisplay;
        public static ConfigEntry<bool> hideUI;
        public static ConfigEntry<float> hideUIDelay;
        public static ConfigEntry<byte> colorRed;
        public static ConfigEntry<byte> colorGreen;
        public static ConfigEntry<byte> colorBlue;
        public static ConfigEntry<float> UIScale;
        public static ConfigEntry<float> UIHiddenAlpha;
        public static ConfigEntry<bool> UIDisabledLowBatteryWarning;
        public static ConfigEntry<float> LowBatteryWarningPercentage;

        public static AssetBundle bundle;

        private readonly Harmony har = new Harmony("command.localFlashlight");
        private void Awake()
        {
            base.Logger.LogInfo("Patching stuff and adding the light.....");

            //configs
            toggleKey = base.Config.Bind<KeyCode>("Keybinds", "Toggle Key", KeyCode.F);
            intensity = base.Config.Bind<float>("Flashlight", "Light Intensity", 225);
            range = base.Config.Bind<float>("Flashlight", "Light Range", 17);
            angle = base.Config.Bind<float>("Flashlight", "Light Angle", 55, "How much the light lets you see");
            batteryLife = base.Config.Bind<float>("Battery", "Battery Life", 8f, "The battery of the flashlight, only used if you have the battery toggle on");
            rechargeMult = base.Config.Bind<float>("Battery", "Recharge Multiplier", 0.75f, "The rate at which the flashlight battery recharges");
            batteryDisplay = base.Config.Bind<BatteryDisplayOptions>("Indicator", "Battery Details", BatteryDisplayOptions.Percentage, "How the mod should display the battery details on the indicator");
            hideUI = base.Config.Bind<bool>("Indicator", "Hide Battery Indicator?", true, "Should the mod hide the indicator when the flashlight is not used?");
            hideUIDelay = base.Config.Bind<float>("Indicator", "Battery Indicator Hide Delay", 1.5f);
            colorRed = base.Config.Bind<byte>("Colors (Flashlight and Indicator)", "Red Color Value", 255);
            colorGreen = base.Config.Bind<byte>("Colors (Flashlight and Indicator)", "Green Color Value", 255);
            colorBlue = base.Config.Bind<byte>("Colors (Flashlight and Indicator)", "Blue Color Value", 255);
            UIScale = base.Config.Bind<float>("Indicator", "Indicator Scale", 1);
            UIHiddenAlpha = base.Config.Bind<float>("Indicator", "Indicator Hidden Opacity", 0.2f, "The opacity of the indicator when the flashlight is not in use for a while");
            UIDisabledLowBatteryWarning = base.Config.Bind<bool>("Indicator", "Low Battery Warning Toggle", true, "Should the mod display a low battery warning when the indicator is disabled?");
            LowBatteryWarningPercentage = base.Config.Bind<float>("Indicator", "Low Battery Warning Percentage", 30, "The percentage at which the low battery warning shows up");

            //assetbundle usage
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("localFlashlight.bundle");
            bundle = AssetBundle.LoadFromStream(stream);
            if (bundle == null)
            {
                base.Logger.LogWarning("failed to get assets, as assetbundle is null :(");
            }

            //adding the gameobject
            GameObject gameObject = new GameObject("LightController");
            gameObject.AddComponent<LightScript>();
            Object.DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            //hud patching
            har.PatchAll(typeof(HUDPatch));

            base.Logger.LogInfo("Patched! you'll see it work in game");
        }

        //i couldn't load the audioclips with Resources.Load for some reason so i had to manually insert them in, but i still had to edit some of the sounds anyway
    }
}
