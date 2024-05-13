using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalCompanyInputUtils.Api;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace localFlashlight
{
    #region Keybinds and inputs
    public class ToggleButton : LcInputActions
    {
        [InputAction("<Keyboard>/f", Name = "Light Toggle")]
        public InputAction toggleKey { get; set; }

        [InputAction("<Keyboard>/h", Name = "LightPosSwitch")]
        public InputAction switchLightPosKey { get; set; }

        [InputAction("<Keyboard>/q", Name = "Recharge Key")]
        public InputAction rechargeKey { get; set; }
    }
    #endregion

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.3.1";
        public const string GUID = "command.localFlashlight";
        public const string NAME = "LocalFlashlight";

        #region Config list
        public static ConfigEntry<float> Intensity { get; private set; }
        public static ConfigEntry<float> Range { get; private set; }
        public static ConfigEntry<float> Angle { get; private set; }

        public static ConfigEntry<float> BatteryLife { get; private set; }
        public static ConfigEntry<float> RechargeMult { get; private set; }
        public static ConfigEntry<float> BurnOutCool { get; private set; }
        public static ConfigEntry<bool> BatteryBurnOut { get; private set; }
        public static ConfigEntry<float> BatteryCool { get; private set; }

        public static ConfigEntry<BatteryDisplayOptions> BatteryDisplay { get; private set; }
        public static ConfigEntry<TextDisplayOptions> TextDisplay { get; private set; }
        public static ConfigEntry<SoundOptions> soundOption { get; private set; }
        public static ConfigEntry<RechargeOptions> rechargeOption { get; private set; }

        public static ConfigEntry<int> FlashColorRed { get; private set; }
        public static ConfigEntry<int> FlashColorGreen { get; private set; }
        public static ConfigEntry<int> FlashColorBlue { get; private set; }

        public static ConfigEntry<int> UIColorRed { get; private set; }
        public static ConfigEntry<int> UIColorGreen { get; private set; }
        public static ConfigEntry<int> UIColorBlue { get; private set; }

        public static ConfigEntry<bool> HideUI { get; private set; }
        public static ConfigEntry<float> HideUIDelay { get; private set; }

        public static ConfigEntry<float> UIScale { get; private set; }
        public static ConfigEntry<float> UIHiddenAlpha { get; private set; }
        public static ConfigEntry<bool> UIDisabledLowBatteryWarning { get; private set; }
        public static ConfigEntry<int> LowBatteryWarningPercentage { get; private set; }

        public static ConfigEntry<int> FlashVolume { get; private set; }

        public static ConfigEntry<float> UIPositionX { get; private set; }

        public static ConfigEntry<float> UIPositionY { get; private set; }

        public static ConfigEntry<bool> ShadowsEnabled { get; private set; }
        public static ConfigEntry<bool> soundAggros { get; private set; }
        public static ConfigEntry<bool> flashlightToggleModSynergyquestionmark { get; private set; }
        public static ConfigEntry<int> shakeStaminaConsume { get; private set; }
        public static ConfigEntry<float> shakeActionCooldown { get; private set; }
        public static ConfigEntry<float> apparaticeFlashlightIntensityMult { get; private set; }
        public static ConfigEntry<float> dynamoUseMoveMult { get; private set; }
        public static ConfigEntry<bool> dimEnabled { get; private set; }
        public static ConfigEntry<int> flashlightStopDimBatteryValue { get; private set; }
        public static ConfigEntry<bool> flickerOnBatteryBurn { get; private set; }
        public static ConfigEntry<bool> onlyShowErrorOnce { get; private set; }
        #endregion

        #region Other values
        public static AssetBundle bundle;
        public static ManualLogSource mls { get; private set; }
        internal static ToggleButton flashlightToggleInstance = new ToggleButton();
        private readonly Harmony har = new Harmony(GUID);
        #endregion

        #region Awake void
        private void Awake()
        {
            mls = Logger;

            #region Setting configs
            Intensity = Config.Bind<float>("Flashlight", "Light Intensity", 350, new ConfigDescription("The intensity of the light. Capped at a maximum of 5000 so you don't blind yourself", new AcceptableValueRange<float>(0, 5000)));
            Range = Config.Bind<float>("Flashlight", "Light Range", 17);
            Angle = Config.Bind<float>("Flashlight", "Light Angle", 55);

            BatteryLife = Config.Bind<float>("Battery", "Battery Life", 12f);
            RechargeMult = Config.Bind<float>("Battery", "Recharge Multiplier", 0.8f, "The rate at which the flashlight battery recharges (WORKS WITH almost ALL RECHARGE METHODS)");

            BatteryDisplay = Config.Bind<BatteryDisplayOptions>("Indicator", "Battery Details", BatteryDisplayOptions.Bar, "How the mod should display the battery details on the HUD");
            TextDisplay = Config.Bind<TextDisplayOptions>("Indicator", "Battery text display", TextDisplayOptions.Percent);

            HideUI = Config.Bind<bool>("Indicator", "Indicator hiding", true, "Should the mod hide the battery indicator if the light is at full battery?");
            HideUIDelay = Config.Bind<float>("Indicator", "Battery Indicator Hide Delay", 1.5f, "The delay time before hiding the indicator");
            UIHiddenAlpha = Config.Bind<float>("Indicator", "Indicator Hidden Opacity", 0.2f, new ConfigDescription("The opacity of the indicator when it is hidden", new AcceptableValueRange<float>(0, 1)));
            UIScale = Config.Bind<float>("Indicator", "Indicator Scale", 1);
            UIPositionX = Config.Bind<float>("Indicator", "IndicatorPositionX", 350, new ConfigDescription("The position of the UI on the X axis", new AcceptableValueRange<float>(-450, 450)));
            UIPositionY = Config.Bind<float>("Indicator", "IndicatorPositionY", -150, new ConfigDescription("The position of the UI on the Y axis", new AcceptableValueRange<float>(-280, 280)));
            UIDisabledLowBatteryWarning = Config.Bind<bool>("Indicator", "Low Battery Warning Toggle", true, "When true, shows a warning on the HUD when the battery reaches a certain percentage");
            LowBatteryWarningPercentage = Config.Bind<int>("Indicator", "Low Battery Warning Percentage", 30, new ConfigDescription("The percentage at which the low battery warning shows up", new AcceptableValueRange<int>(0, 100)));

            FlashColorRed = Config.Bind<int>("Flashlight Colors", "Red Color Value", 255, new ConfigDescription("Light red color value", new AcceptableValueRange<int>(0, 255)));
            FlashColorGreen = Config.Bind<int>("Flashlight Colors", "Green Color Value", 255, new ConfigDescription("Light green color value", new AcceptableValueRange<int>(0, 255)));
            FlashColorBlue = Config.Bind<int>("Flashlight Colors", "Blue Color Value", 255, new ConfigDescription("Light blue color value", new AcceptableValueRange<int>(0, 255)));

            UIColorRed = Config.Bind<int>("HUD Colors", "Red Color Value", 255, new ConfigDescription("Indicator red color value", new AcceptableValueRange<int>(0, 255)));
            UIColorGreen = Config.Bind<int>("HUD Colors", "Green Color Value", 255, new ConfigDescription("Indicator green color value", new AcceptableValueRange<int>(0, 255)));
            UIColorBlue = Config.Bind<int>("HUD Colors", "Blue Color Value", 255, new ConfigDescription("Indicator blue color value", new AcceptableValueRange<int>(0, 255)));

            flashlightToggleModSynergyquestionmark = Config.Bind<bool>("Other", "Prioritize flashlights in player inventory", true, "Setting this to true will prevent the light turning on while you have a flashlight in your inventory");
            FlashVolume = Config.Bind<int>("Other", "Flashlight Volume", 50);
            soundOption = Config.Bind<SoundOptions>("Other", "Sound Options", SoundOptions.InGameFlashlight, "different flashlight sounds (Light on, off, out of battery...)");
            soundAggros = Config.Bind("Other", "Sounds heard by enemies", true, "If set to true, the sounds that the flashlight makes will be heard by enemies that are close to you");
            ShadowsEnabled = Config.Bind<bool>("Other", "(EXPERIMENTAL) Enable shadows", true, "set this as an experimental feature because shadows are inconsistent, especially when they come from lights that are inside the player. this is an issue that the vanilla game also has with its helmet lights");
            rechargeOption = Config.Bind<RechargeOptions>("Other", "Recharge method", RechargeOptions.Time, "The way that the flashlight can be recharged. Time, Shake and Dynamo are intended for shorter battery times while Apparatice and OnShipEnter are intended for longer battery times");

            flickerOnBatteryBurn = Config.Bind<bool>("Other", "Enable flashlight flickering", false, "When set to true, the flashlight will flicker for a bit and then turn off when using up the battery completely, and will flicker for a bit when someone gets haunted by a ghost girl (the same thing that happens to normal flashlights)");
            dimEnabled = Config.Bind<bool>("Other", "Enable flashlight dimming", false, "When true, the intensity of the light dims down to a certain battery amount");
            flashlightStopDimBatteryValue = Config.Bind<int>("Other", "Minimum battery amount dim", 15, new ConfigDescription("The battery percentage at which the light stops dimming", new AcceptableValueRange<int>(0, 100)));

            BatteryBurnOut = Config.Bind<bool>("Time battery recharge configs", "Battery Burnout", true, "When true, if the flashlight turns off with no more battery, it goes on cooldown for a longer time");
            BatteryCool = Config.Bind<float>("Time battery recharge configs", "Battery Recharge Cooldown", 1, "The cooldown before the battery starts recharging");
            BurnOutCool = Config.Bind<float>("Time battery recharge configs", "Battery Recharge Cooldown (burnt out)", 3);

            shakeActionCooldown = Config.Bind<float>("Shake battery recharge configs", "Shake cooldown", 0.2f, "The time needed to wait before you can shake the flashlight again");
            shakeStaminaConsume = Config.Bind<int>("Shake battery recharge configs", "Consumed stamina amount", 3, new ConfigDescription("The amount of stamina shaking the flashlight consumes", new AcceptableValueRange<int>(0, 100)));

            dynamoUseMoveMult = Config.Bind<float>("Dynamo battery recharge configs", "Move speed multiplier when recharging", 0.75f, new ConfigDescription("Changes the movement speed of the player when recharging the flashlight", new AcceptableValueRange<float>(0, 1)));

            apparaticeFlashlightIntensityMult = Config.Bind<float>("Apparatice battery recharge configs", "Light intensity multiplier", 0.5f, "The multiplier of the light intensity when using the Apparatice recharge option");

            onlyShowErrorOnce = Config.Bind("Other", "(DEBUG) Only show mod errors once", true, "When true, only shows the error that the mod may cause once per game enter (When set to false, the mod might have multiple errors pop up for a bit, and the mod should work unless it starts spamming said errors)");
            #endregion

            // Loading asset bundles from the folder (instead of loading the embedded asset bundle, as it doesn't waste memory this way)
            var modPath = System.IO.Path.GetDirectoryName(Info.Location);
            var bundlePath = System.IO.Path.Combine(modPath, "localbundle");
            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
                mls.LogError("failed to get mod assets!");

            ///detecting when a new scene is loaded, and adding the gameobject if the scene is the in-game ship scene
            SceneManager.sceneLoaded += OnPlayingSceneLoaded;

            ///patching hud to add the battery indicator, and patching other stuff while at it
            har.PatchAll(typeof(Patches));

            mls.LogInfo("hey look i got a free flashlight from the company! (mod is up and working)");
        }
        #endregion

        #region Scene changing
        private void OnPlayingSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            //detect if the player is the in-game scene
            if (scene.name == "SampleSceneRelay")
            {
                GameObject gameObject = new("LightController");
                gameObject.AddComponent<LightScript>();
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave;
                mls.LogDebug("joined game, made lightcontroller");
            }

            //detect if the player is in main menu
            if (scene.name == "MainMenu")
            {
                GameObject lightController = GameObject.Find("LightController");
                GameObject.Destroy(lightController);
                mls.LogDebug("in main menu, destroyed any existing lightcontroller to prevent duplicates of it");
            }
        }
        #endregion
    }
}
