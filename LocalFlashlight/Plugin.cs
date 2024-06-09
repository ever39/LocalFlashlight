using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalCompanyInputUtils.Api;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LocalFlashlight.Networking;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;

namespace LocalFlashlight
{
    #region Keybinds and inputs
    public class ToggleButton : LcInputActions
    {
        [InputAction("<Keyboard>/f", Name = "Toggle key")]
        public InputAction toggleKey { get; set; }

        [InputAction("<Keyboard>/h", Name = "Position and angle switch key")]
        public InputAction switchLightPosKey { get; set; }

        [InputAction("<Keyboard>/q", Name = "Recharge key")]
        public InputAction rechargeKey { get; set; }
    }
    #endregion

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.4.0";
        public const string GUID = "command.LocalFlashlight";
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
        public static ConfigEntry<bool> enableNetworking { get; private set; }
        public static ConfigEntry<string> flashlightColorHex { get; private set; }
        public static ConfigEntry<string> HUDColorHex { get; private set; }
        public static ConfigEntry<int> DarkVisionMult { get; private set; }
        public static ConfigEntry<bool> rechargeInOrbit { get; private set; }
        #endregion

        #region Other values
        public static AssetBundle bundle;
        public static ManualLogSource mls { get; private set; }
        internal static ToggleButton flashlightToggleInstance = new();
        private readonly Harmony har = new(GUID);

        static string sceneName;
        #endregion

        #region Awake void
        private void Awake()
        {
            mls = Logger;

            #region Setting configs
            //now with lethalconfig support!

            Intensity = Config.Bind<float>("Flashlight", "Light intensity", 350, new ConfigDescription("The intensity of the light (in lumens, i think)", new AcceptableValueRange<float>(10, 5000)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(Intensity, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig,
                Min = 10,
                Max = 5000
            }));

            Range = Config.Bind<float>("Flashlight", "Light range", 17, new ConfigDescription("The range of the light (in units)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(Range, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig,
                Min = 1,
                Max = float.PositiveInfinity
            }));

            Angle = Config.Bind<float>("Flashlight", "Light angle", 55, new ConfigDescription("The size of the light's circle (Spot angle)", new AcceptableValueRange<float>(1, 120)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(Angle, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig,
                Min = 1,
                Max = 120
            }));

            BatteryLife = Config.Bind<float>("Battery", "Battery life", 12f, new ConfigDescription("The battery life of the flashlight (in seconds)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(BatteryLife, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
                Min = 1f,
                Max = float.PositiveInfinity
            }));

            RechargeMult = Config.Bind<float>("Battery", "Recharge Multiplier", 0.8f, new ConfigDescription("The flashlight's battery recharge multiplier (For instance, setting it to 1 will make it recharge at about the same rate that it is depleted when using the Time recharge method, however this config also applies to other recharge methods)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(RechargeMult, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            BatteryDisplay = Config.Bind<BatteryDisplayOptions>("Indicator", "Battery Details", BatteryDisplayOptions.Bar, "How the indicator displays the flashlight's remaining battery time");
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<BatteryDisplayOptions>(BatteryDisplay, new EnumDropDownOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
            }));

            TextDisplay = Config.Bind<TextDisplayOptions>("Indicator", "Battery text display", TextDisplayOptions.Percent, "(Only applies to \"Text\" and \"All\" indicator display options) Wherether the mod should display the battery information text in percents, accurate percents or time left");
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<TextDisplayOptions>(TextDisplay, new EnumDropDownOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            HideUI = Config.Bind<bool>("Indicator", "Indicator fading", true, "When true, the indicator will hide after a while of not using the flashlight");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(HideUI, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            }));

            HideUIDelay = Config.Bind<float>("Indicator", "Battery indicator fade delay", 1.5f, new ConfigDescription("The delay before fading out the indicator (in seconds)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(HideUIDelay, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            UIHiddenAlpha = Config.Bind<float>("Indicator", "Indicator faded out opacity", 0.2f, new ConfigDescription("The opacity of the indicator when the indicator is faded out", new AcceptableValueRange<float>(0f, 1f)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(UIHiddenAlpha, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 1
            }));

            UIScale = Config.Bind<float>("Indicator", "Indicator scale", 1, new ConfigDescription("The scale of the indicator, updates in-game", new AcceptableValueRange<float>(0.1f, 1f)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(UIScale, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0.1f,
                Max = float.PositiveInfinity
            }));

            UIPositionX = Config.Bind<float>("Indicator", "IndicatorPositionX", 350, new ConfigDescription("The position of the UI on the X axis, updates in-game", new AcceptableValueRange<float>(-450f, 450f)));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(UIPositionX, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = -450,
                Max = 450
            }));

            UIPositionY = Config.Bind<float>("Indicator", "IndicatorPositionY", -150, new ConfigDescription("The position of the UI on the Y axis, updates in-game", new AcceptableValueRange<float>(-280f, 280f)));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(UIPositionY, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = -280,
                Max = 280
            }));

            UIDisabledLowBatteryWarning = Config.Bind<bool>("Indicator", "Low Battery Warning Toggle", true, "(Only applies to when the indicator is disabled) When true, shows a warning on the HUD when the battery reaches a certain percentage");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(UIDisabledLowBatteryWarning, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
            }));

            LowBatteryWarningPercentage = Config.Bind("Indicator", "Low Battery Warning Percentage", 30, new ConfigDescription("(Only applies when the indicator is disabled) The percentage at which the low battery warning shows up", new AcceptableValueRange<int>(0, 100)));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(LowBatteryWarningPercentage, new IntInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));

            flashlightColorHex = Config.Bind<string>("Colors", "Flashlight Color (Hex)", "#FFFFFF", new ConfigDescription("very important, add the # before the hex! if it breaks or doesn't work, try resetting the config and typing the code in manually"));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(flashlightColorHex, new TextInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig
            }));

            HUDColorHex = Config.Bind<string>("Colors", "Indicator Color (Hex)", "#FFFFFF", new ConfigDescription("very important, add the # before the hex! if it breaks or doesn't work, try resetting the config and typing the code in manually"));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(HUDColorHex, new TextInputFieldOptions
            {
                RequiresRestart = false
            }));

            flashlightToggleModSynergyquestionmark = Config.Bind<bool>("Other", "Prioritize flashlights in player inventory", true, "Setting this to true will prevent the light turning on while you have a flashlight in your inventory, however the local flashlight can still be turned off in case you picked up an active flashlight");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(flashlightToggleModSynergyquestionmark, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            }));

            FlashVolume = Config.Bind<int>("Other", "Flashlight volume", 50);
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(FlashVolume, new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));

            soundOption = Config.Bind<SoundOptions>("Other", "Sound options", SoundOptions.InGameFlashlight, "Different flashlight sounds depending on what you chose");
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<SoundOptions>(soundOption, new EnumDropDownOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            ShadowsEnabled = Config.Bind<bool>("Other", "Enable shadows", true, "now less inconsistent, as scrap no longer takes up three quarters of the light anymore");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(ShadowsEnabled, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            rechargeOption = Config.Bind<RechargeOptions>("Other", "Recharge method", RechargeOptions.Time, "The way that the flashlight can be recharged. Time, Shake and Dynamo are intended for shorter battery times while FacilityPowered and ShipRecharge are intended for longer battery times");
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<RechargeOptions>(rechargeOption, new EnumDropDownOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            rechargeInOrbit = Config.Bind("Other", "Fully recharge in orbit", true, "When set to true, fully recharges the flashlight when reaching orbit (when the round ends)");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(rechargeInOrbit, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            }));

            DarkVisionMult = Config.Bind("Other", "Ambient light intensity multiplier", 50, "Sets the multiplier of the ambient light. Useful if you feel like forcing yourself to use the flashlight more");
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(DarkVisionMult, new IntSliderOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
                Min = 0,
                Max = 100
            }));

            flickerOnBatteryBurn = Config.Bind<bool>("Other", "Enable light flickering", true, "When true, the flashlight attempts to flicker like the in-game flashlights, and also flickers when its battery runs out");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(flickerOnBatteryBurn, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            dimEnabled = Config.Bind<bool>("Other", "Enable light dimming", false, "When true, the intensity of the light dims down to a certain point");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(dimEnabled, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            flashlightStopDimBatteryValue = Config.Bind<int>("Other", "Minimum battery amount dim", 15, new ConfigDescription("The battery percentage at which the light stops dimming"));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(flashlightStopDimBatteryValue, new IntInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));

            BatteryBurnOut = Config.Bind<bool>("Time battery recharge configs", "Battery burnout", true, "When true, if the flashlight turns off with no battery left, the cooldown before starting to recharge lasts longer");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(BatteryBurnOut, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            BatteryCool = Config.Bind<float>("Time battery recharge configs", "Battery recharge cooldown", 1, "The cooldown before the battery starts recharging normally");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(BatteryCool, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            BurnOutCool = Config.Bind<float>("Time battery recharge configs", "Battery recharge cooldown (no battery)", 3, "The cooldown before the battery starts recharging when it is fully depleted");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(BurnOutCool, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = float.PositiveInfinity
            }));


            shakeActionCooldown = Config.Bind<float>("Shake battery recharge configs", "Shake cooldown", 0.2f, "The time needed to wait before you can recharge the flashlight again via shaking it");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(shakeActionCooldown, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            shakeStaminaConsume = Config.Bind<int>("Shake battery recharge configs", "Consumed stamina amount", 3, new ConfigDescription("The amount of stamina shaking the flashlight consumes"));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(shakeStaminaConsume, new IntInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));

            dynamoUseMoveMult = Config.Bind<float>("Dynamo battery recharge configs", "Movespeed multiplier when recharging", 0.75f, "The multiplier of the player's normal movement speed while recharging the flashlight");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(dynamoUseMoveMult, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 1
            }));

            apparaticeFlashlightIntensityMult = Config.Bind<float>("Facility powered battery recharge configs", "Light intensity multiplier", 0.5f, "The multiplier of the light intensity when using the Facility Powered recharge option");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(apparaticeFlashlightIntensityMult, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig,
                Min = 0,
                Max = 1
            }));

            enableNetworking = Config.Bind("Networking", "(EXPERIMENTAL) Enable networking", false, "Enables networking. While this is enabled, you cannot join other servers without this mod and its networking also enabled. However, you can see other players' lights (alongside the intensity, range, angle and color they set) and hear their flashlight sounds");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(enableNetworking, new BoolCheckBoxOptions
            {
                RequiresRestart = true,
                CanModifyCallback = denyNetworkConfigModify
            }));
            #endregion

            // Loading asset bundles from the mod folder (instead of loading the embedded asset bundle, as it doesn't waste memory this way)
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(Info.Location);
                var bundlePath = System.IO.Path.Combine(modPath, "localbundle");
                bundle = AssetBundle.LoadFromFile(bundlePath);
            }
            catch
            {
                mls.LogError("WHAT!!!!!! error while getting mod assets????? failed to get mod assets??? are they even in the same folder as the mod? check that if you haven't done so yet");
                return;
            }

            //scene check is now used for denying or allowing config changes instead of loading player object
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            ///now the base of the entire mod, since it adds the script to the player object
            har.PatchAll(typeof(Patches));

            //networking!! it exists now!!! thanks lethal company modding wiki
            if (enableNetworking.Value)
            {
                har.PatchAll(typeof(NetworkingPatches));
                PatchNetcode();
                mls.LogWarning("NETWORKING ENABLED!!!! this is EXPERIMENTAL territory, and you're pretty much stuck joining servers where the host has LocalFlashlight and also has networking enabled! you may also encounter many many bugs!!!!! if you didn't mean to have this enabled, go back to the mod manager of your choice, disable the enable networking config that resides in the Networking category, and then restart the game.");
            }
            else
            {
                mls.LogInfo("oh, networking is disabled :)");
            }

            mls.LogInfo("mod loaded successfully!");
        }
        #endregion
        #region Methods used for locking configs
        ///The scenes are back. Oh god the scenes are back (USED FOR LOCKING SOME CONFIGS IF IN-GAME)
        private static void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            sceneName = currentScene.name;
        }

        private static CanModifyResult modifyConfig()
        {
            return (sceneName == "MainMenu", "This setting cannot be changed while in a lobby.");
        }

        private static CanModifyResult denyNetworkConfigModify()
        {
            return (false, "Enabling or disabling networking cannot be done while the game is open.\nEdit it from your mod manager of choice instead.");
        }

        private static CanModifyResult modifySpecialConfig()
        {
            if (Plugin.enableNetworking.Value)
            {
                return (sceneName == "MainMenu", "Setting cannot be changed while in a lobby when networking is enabled.");
            }
            else return (true);
        }
        #endregion

        //Patching netcode with Evaisa's Unity Netcode Patcher (also the reason why i didn't allow changing networking from LethalConfig, as the game stays patched even when switching back which leads to the player not being able to join)
        private static void PatchNetcode()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
