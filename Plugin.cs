using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace localFlashlight
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.1.1";
        public const string GUID = "command.localFlashlight";
        public const string NAME = "Local Flashlight";

        //configs...........
        public static ConfigEntry<KeyCode> toggleKey { get; private set; }

        public static ConfigEntry<float> intensity { get; private set; }
        public static ConfigEntry<float> range { get; private set; }
        public static ConfigEntry<float> angle { get; private set; }

        public static ConfigEntry<float> batteryLife { get; private set; }
        public static ConfigEntry<float> rechargeMult { get; private set; }
        public static ConfigEntry<float> burnOutCool { get; private set; }
        public static ConfigEntry<bool> batteryBurnOut { get; private set; }
        public static ConfigEntry<float> batteryCool { get; private set; }
        public static ConfigEntry<BatteryDisplayOptions> batteryDisplay { get; private set; }
        public static ConfigEntry<TextDisplayOptions> textDisplay { get; private set; }

        public static ConfigEntry<int> flashColorRed { get; private set; }
        public static ConfigEntry<int> flashColorGreen { get; private set; }
        public static ConfigEntry<int> flashColorBlue { get; private set; }

        public static ConfigEntry<int> UIColorRed { get; private set; }
        public static ConfigEntry<int> UIColorGreen { get; private set; }
        public static ConfigEntry<int> UIColorBlue { get; private set; }

        public static ConfigEntry<bool> hideUI { get; private set; }
        public static ConfigEntry<float> hideUIDelay { get; private set; }

        public static ConfigEntry<float> UIScale { get; private set; }
        public static ConfigEntry<float> UIHiddenAlpha { get; private set; }
        public static ConfigEntry<bool> UIDisabledLowBatteryWarning { get; private set; }
        public static ConfigEntry<int> LowBatteryWarningPercentage { get; private set; }

        public static ConfigEntry<float> flashVolume { get; private set; }

        public static ConfigEntry<float> UIPositionX { get; private set; }
        
        public static ConfigEntry<float> UIPositionY { get; private set; }

        public static ConfigEntry<bool> shadowsEnabled { get; private set; }

        public static AssetBundle bundle;

        public static ManualLogSource mls { get; private set; }

        private readonly Harmony har = new Harmony(GUID);
        private void Awake()
        {
            mls = Logger;
            mls.LogInfo("Patching stuff and adding the light.....");

            #region Configs
            toggleKey = Config.Bind<KeyCode>("Keybinds", "Toggle Key", KeyCode.F);
            
            intensity = Config.Bind<float>("Flashlight", "Light Intensity", 225, new ConfigDescription("Intensity of the flashlight", new AcceptableValueRange<float>(0, 5000)));
            range = Config.Bind<float>("Flashlight", "Light Range", 17);
            angle = Config.Bind<float>("Flashlight", "Light Angle", 55, "How much the light lets you see");
            
            batteryLife = Config.Bind<float>("Battery", "Battery Life", 8f, "The battery of the flashlight");
            rechargeMult = Config.Bind<float>("Battery", "Recharge Multiplier", 0.75f, "The rate at which the flashlight battery recharges");
            batteryDisplay = Config.Bind<BatteryDisplayOptions>("Indicator", "Battery Details", BatteryDisplayOptions.Percentage, "How the mod should display the battery details on the indicator");
            
            hideUI = Config.Bind<bool>("Indicator", "Hide Battery Indicator?", true, "Should the mod hide the indicator when the flashlight is not used?");
            hideUIDelay = Config.Bind<float>("Indicator", "Battery Indicator Hide Delay", 1.5f);
            
            flashColorRed = Config.Bind<int>("Flashlight Colors", "RedColorValue", 255, new ConfigDescription("Flashlight red color value",new AcceptableValueRange<int>(0, 255)));
            flashColorGreen = Config.Bind<int>("Flashlight Colors", "Green Color Value", 255, new ConfigDescription("Flashlight green color value", new AcceptableValueRange<int>(0, 255)));
            flashColorBlue = Config.Bind<int>("Flashlight Colors", "Blue Color Value", 255, new ConfigDescription("Flashlight blue color value", new AcceptableValueRange<int>(0, 255)));
            
            UIColorRed = Config.Bind<int>("HUD Colors", "RedColorValue", 255, new ConfigDescription("HUD red color value", new AcceptableValueRange<int>(0, 255)));
            UIColorGreen = Config.Bind<int>("HUD Colors", "GreenColorValue", 255, new ConfigDescription("HUD red color value", new AcceptableValueRange<int>(0, 255)));
            UIColorBlue = Config.Bind<int>("HUD Colors", "BlueColorValue", 255, new ConfigDescription("HUD red color value", new AcceptableValueRange<int>(0, 255)));
            
            UIScale = Config.Bind<float>("Indicator", "Indicator Scale", 1);
            UIHiddenAlpha = Config.Bind<float>("Indicator", "Indicator Hidden Opacity", 0.2f, new ConfigDescription("The opacity of the indicator when the flashlight is not in use for a while",new AcceptableValueRange<float>(0, 1)));
            UIDisabledLowBatteryWarning = Config.Bind<bool>("Indicator", "Low Battery Warning Toggle", true, "Should the mod display a low battery warning when the indicator is disabled?");
            LowBatteryWarningPercentage = Config.Bind<int>("Indicator", "Low Battery Warning Percentage", 30, new ConfigDescription("The percentage at which the low battery warning shows up", new AcceptableValueRange<int>(0, 100)));
            textDisplay = Config.Bind<TextDisplayOptions>("Indicator", "Text display", TextDisplayOptions.Percent);

            UIPositionX = Config.Bind<float>("Indicator", "IndicatorPositionX", 350, new ConfigDescription("The position of the UI on the X axis",new AcceptableValueRange<float>(-450, 450)));
            UIPositionY = Config.Bind<float>("Indicator", "IndicatorPositionY", -150, new ConfigDescription("The position of the UI on the Y axis", new AcceptableValueRange<float>(-280, 280)));
            
            flashVolume = Config.Bind<float>("Other", "Volume", 0.5f, "Volume of all the flashlight sounds");
            batteryCool = Config.Bind<float>("Battery", "Battery Recharge Cooldown", 1, "The cooldown before the battery starts recharging");
            batteryBurnOut = Config.Bind<bool>("Battery", "Battery Burnout", true, "Should the battery take a longer time to start recharging when the flashlight has no more battery?");
            burnOutCool = Config.Bind<float>("Battery", "Battery Recharge Cooldown (burnt out)", 3);

            shadowsEnabled = Config.Bind<bool>("Other", "Enable shadows (EXPERIMENTAL)", true, "left this as an experimental feature as shadows are very buggy with the playermodel, which is also the reason why helmetlights dont work properly when looking down or crouching, so i advise not looking down when using the light unless you really need to, other than that it's all good!");

            #endregion

            //assetbundle usage
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("localFlashlight.bundle");
            bundle = AssetBundle.LoadFromStream(stream);
            if (bundle == null)
            {
                mls.LogWarning("failed to get assets, as assetbundle is null :(");
            }

            ///detetcting scene, and adding the gameobject
            SceneManager.sceneLoaded += OnPlayingSceneLoaded;

            ///hud patching
            har.PatchAll(typeof(HUDPatch));

            mls.LogInfo("Patched! you'll see it work in game");
        }

        private void OnPlayingSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            //detect if in-game (MAY NOT WORK WITH LATECOMPANY IF IT IS SET TO ALLOW MID-ROUND JOINING, AS IT SKIPS THE SHIP SCENE!)
            if (scene.name == "SampleSceneRelay")
            {
                GameObject gameObject = new GameObject("LightController");
                gameObject.AddComponent<LightScript>();
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave;
                mls.LogInfo("joined game, made lightcontroller!");
            }

            //detect if in main menu
            if (scene.name == "MainMenu")
            {
                GameObject lightController = GameObject.Find("LightController");
                GameObject.Destroy(lightController);
                mls.LogInfo("in main menu, destroyed lightcontroller to prevent duplicates of it!");
            }
        }
    }
}
