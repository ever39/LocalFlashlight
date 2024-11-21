using GameNetcodeStuff;
using HarmonyLib;
using LocalFlashlight.Compatibilities;
using LocalFlashlight.Networking;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LocalFlashlight
{
    internal class Patches : MonoBehaviour
    {
        #region Values
        public static LightScript? semiInstance { get; private set; } //???
        static GameObject UIContainer, frameObj, meterObj, textObj, warningObj;
        static TextMeshProUGUI textmesh;
        static Image frameImage, meterImage, warningImage;
        static Sprite frame, meter;
        static readonly Sprite warning = Plugin.bundle.LoadAsset<Sprite>("warning");
        public static Color UIColorHex;
        static bool warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
        static float warningPercent = Plugin.LowBatteryWarningPercentage.Value;
        static BatteryDisplayOptions selectedStyle;
        static TextDisplayOptions selectedText;
        static RechargeOptions selectedRecharge;
        static float elemScale;
        public static bool isFlashlightHeld = false;
        public static bool isFlashlightPocketed = false;
        public static bool isFacilityPowered = true;
        public static float randomLightInterferenceMultiplier;
        static float targetAlpha;
        static readonly float soundCd = 3;
        static float lastSoundTime = 0;
        #endregion

        #region Adding and removing the LightController via patches (a lot easier than with scenes, as i don't have to get the player controller in Update anymore)
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        [HarmonyPostfix]
        internal static void MakeLightController()
        {
            semiInstance = null;
            GameObject gameObject = new("LightController");
            gameObject.transform.SetParent(StartOfRound.Instance.localPlayerController.transform, false);
            semiInstance = gameObject.AddComponent<LightScript>();
            isFlashlightHeld = false;
            isFlashlightPocketed = false;
            //setting the ambient light intensity
            StartOfRound.Instance.localPlayerController.nightVision.intensity = StartOfRound.Instance.localPlayerController.nightVision.intensity * ((float)Plugin.DarkVisionMult.Value / 100);
            ////Plugin.mls.LogDebug("player joined lobby, made lightcontroller and parented it to the local player controller");
        }

        //kept in to despawn lingering lights (if that ever happens)
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        internal static void DestroyLightController()
        {
            semiInstance = null;
            GameObject lightController = GameObject.Find("LightController");
            if (lightController != null)
                Destroy(lightController);
            TerminalApi.TerminalApi.DeleteKeyword("LocalFlashlight");

            //despawning all existing light objects (and hopefully fixing a networking bug where lights would be lingering after leaving a lobby)
            try
            {
                Plugin.mls.LogInfo("Attempting to despawn all light objects!!");
                var currentLights = FindObjectsOfType<GameObject>().Where(x => x.name.Contains("lightObject"));
                if (currentLights.Count() == 0) Plugin.mls.LogInfo("no light objects to despawn");
                foreach (var light in currentLights) GameObject.Destroy(light);
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while despawning light objects\n{e}");
            }
            ////Plugin.mls.LogDebug("player disconnected, removing light controller");
        }
        #endregion

        #region HUDManager patches
        [HarmonyPatch(typeof(HUDManager), "Awake")]
        [HarmonyPostfix]
        internal static void GetBatteryUI(ref HUDManager __instance)
        {
            #region setting battery values and ui stuff
            selectedStyle = Plugin.BatteryDisplay.Value;
            selectedText = Plugin.TextDisplay.Value;
            warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
            warningPercent = Plugin.LowBatteryWarningPercentage.Value;
            elemScale = Plugin.UIScale.Value;
            selectedRecharge = Plugin.rechargeOption.Value;
            isFacilityPowered = true;
            ColorUtility.TryParseHtmlString(Plugin.HUDColorHex.Value, out UIColorHex);
            #endregion

            MakeIndicator(__instance);
        }

        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        internal static void UpdateBatteryInfo(ref HUDManager __instance)
        {
            if (UIContainer == null)
            {
                MakeIndicator(__instance);
                return;
            }

            try
            {
                float t0 = LightScript.batteryTime;
                float timeMinutes = Mathf.FloorToInt(t0 / 60);
                float timeSeconds = Mathf.FloorToInt(t0 % 60);

                elemScale = Plugin.UIScale.Value;
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(UIContainer.GetComponent<CanvasGroup>().alpha, targetAlpha, Time.deltaTime * 6);

                //small changes
                if (selectedStyle != BatteryDisplayOptions.Disabled)
                {
                    targetAlpha = Plugin.HideUI.Value ? (LightScript.UIHideTime < 0 ? Plugin.UIHiddenAlpha.Value : 1) : 1;
                    if (selectedStyle != BatteryDisplayOptions.Text) 
                        meterImage.fillAmount = LightScript.BatteryClamped;
                }
                else
                {
                    targetAlpha = 1;
                    UIContainer.SetActive(warningEnabled && LightScript.truePercentBattery <= warningPercent);
                }

                #region updating color
                ColorUtility.TryParseHtmlString(Plugin.HUDColorHex.Value, out UIColorHex);
                if (frameObj != null && selectedStyle is BatteryDisplayOptions.Bar)
                {
                    frameImage.color = UIColorHex;
                }
                if (meterObj != null)
                {
                    meterImage.color = UIColorHex;
                }
                if (warningObj != null)
                {
                    warningImage.color = UIColorHex;
                    warningPercent = Plugin.LowBatteryWarningPercentage.Value;
                    warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
                }
                if (textObj != null)
                {
                    textmesh.color = UIColorHex;
                }
                #endregion

                ///updating text
                if (selectedStyle == BatteryDisplayOptions.All | selectedStyle == BatteryDisplayOptions.Text)
                    UpdateHUDText(timeMinutes, timeSeconds);
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while updating hud!! is the light script even there?? is the hud even there????\n{e}");
                return;
            }
        }
        #endregion

        #region Other patches
        #region Apparatice recharge method code
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPostfix]
        internal static void DisableHUDAgain()
        {
            if (selectedRecharge == RechargeOptions.FacilityPowered) 
                UIContainer.SetActive(false);
            isFacilityPowered = true;

            if (StartOfRound.Instance.localPlayerController.isPlayerDead)
                StartOfRound.Instance.localPlayerController.pocketedFlashlight = null;

            if (Plugin.rechargeInOrbit.Value)
                LightScript.FullyChargeBattery();

            ////Plugin.mls.LogDebug("ShipHasLeft triggered, disabling hud, setting isFacilityPowered to true/fully charge battery if needed");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.openingDoorsSequence))]
        [HarmonyPostfix]
        internal static void FixValues()
        {
            isFlashlightHeld = false;
            isFlashlightPocketed = false;
            randomLightInterferenceMultiplier = UnityEngine.Random.Range(0.1f, 0.4f);

            //added this small breakerbox check to see if power is turned on or off at the start of the round when not hosting, yes i know it's pretty cheesy and you can notice if you're starting out with lights off from the start......
            ///turns out this broke when going to company building, i'll just properly turn it off until i find a proper solution
            /*if (StartOfRound.Instance.currentLevelID != 3)
            {
                BreakerBox breakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
                isFacilityPowered = breakerBox.isPowerOn;
                if (selectedRecharge == RechargeOptions.FacilityPowered)
                    UIContainer.SetActive(!isFacilityPowered);
            }
            else
            {
                isFacilityPowered = true;
                if (selectedRecharge == RechargeOptions.FacilityPowered)
                    UIContainer.SetActive(false);
            }*/
            ////Plugin.mls.LogDebug("openingDoorsSequence triggered, changing randomLightInterferenceMultiplier");
        }
        #endregion

        #region Prioritizing in-game flashlights
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        internal static void PocketedFlashlightChecks()
        {
            try
            {
                var player = StartOfRound.Instance.localPlayerController;

                if ((!player.IsOwner || !player.isPlayerControlled || player.IsServer && !player.isHostPlayerObject) && !player.isTestingPlayer)
                    return;

                if (!player.isPlayerDead)
                {
                    if (Plugin.hasReservedSlots)
                    {
                        ReservedItemSlotCompatibility.CheckForFlashlightInSlots();
                        isFlashlightPocketed = false;
                        isFlashlightHeld = false;
                    }

                    if (!ReservedItemSlotCompatibility.flashlightInReservedSlot)
                    {
                        if (player.currentlyHeldObjectServer is FlashlightItem && player.currentlyHeldObjectServer != player.pocketedFlashlight)
                            player.pocketedFlashlight = player.currentlyHeldObjectServer;

                        if (player.pocketedFlashlight == null) return;

                        if (player.currentlyHeldObjectServer is FlashlightItem && player.isHoldingObject && !player.pocketedFlashlight.insertedBattery.empty)
                        {
                            FlashlightItem? activeFlashlight = player.currentlyHeldObjectServer as FlashlightItem;
                            if (activeFlashlight != null && activeFlashlight.flashlightTypeID != 2)
                                isFlashlightHeld = true;
                        }
                        else isFlashlightHeld = false;

                        if (player.pocketedFlashlight is FlashlightItem && player.pocketedFlashlight.isHeld && !player.pocketedFlashlight.insertedBattery.empty)
                        {
                            FlashlightItem? pocketedLight = player.pocketedFlashlight as FlashlightItem;
                            if (pocketedLight != null && pocketedLight.flashlightTypeID != 2)
                                isFlashlightPocketed = true;
                        }
                        else isFlashlightPocketed = false;
                    }
                }
                else
                {
                    player.pocketedFlashlight = null;
                    isFlashlightHeld = false;
                    isFlashlightPocketed = false;
                }

            }
            catch
            {
                return;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPostfix]
        internal static void ClearLightA(ref PlayerControllerB __instance)
        {
            ////Plugin.mls.LogDebug("local player died, made pocketedflashlight null");
            if (!StartOfRound.Instance.localPlayerController.isPlayerDead) 
                return;

            __instance.pocketedFlashlight = null;
            isFlashlightHeld = false;
            isFlashlightPocketed = false;

            if (Plugin.enableNetworking.Value)
                LFNetworkHandler.Instance?.ToggleLightServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, false);
        }
        #endregion

        //this might still be a bit buggy if you're not host i'm pretty sure
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOnClientRpc))]
        [HarmonyPostfix]
        static void TurnOnFlashlightPower()
        {
            ////Plugin.mls.LogDebug("PowerSwitchOnClientRpc");
            isFacilityPowered = true;
            if (selectedRecharge == RechargeOptions.FacilityPowered)
                UIContainer.SetActive(!isFacilityPowered);
            lastSoundTime = Time.time;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOffClientRpc))]
        [HarmonyPostfix]
        static void TurnOffFlashlightPower()
        {
            isFacilityPowered = false;
            if (selectedRecharge == RechargeOptions.FacilityPowered)
            {
                UIContainer.SetActive(!isFacilityPowered);
                if (Time.time - lastSoundTime > soundCd) LightScript.PlayNoise(LightScript.activeClips[6], 0.2f, false);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MenuManager), "Start")]
        static void DisplayNetworkingPopup(ref MenuManager __instance)
        {
            if (Plugin.enableNetworking.Value)
                __instance.DisplayMenuNotification("<size=150%>HEY!!!!!!</size>\n<size=60%>you have LocalFlashlight's networking enabled, if you didn't intend to enable it, edit it from your mod manager of choice and then restart the game</size>", "got it");
        }
        #endregion

        #region making HUD
        static void MakeIndicator(HUDManager hudManager)
        {
            try
            {
                UIContainer = new GameObject("LF_MOD_HUD", typeof(RectTransform), typeof(CanvasGroup));
                UIContainer.transform.SetParent(hudManager.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);
                //Plugin.mls.LogDebug("made UI container");

                switch (selectedStyle)
                {
                    ///i'll be honest, i could've made prefabs in unity and loaded them directly in here from the assetbundle but alright
                    case BatteryDisplayOptions.Bar:
                        frame = Plugin.bundle.LoadAsset<Sprite>("frame.png");
                        meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                        frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                        frameObj.transform.SetParent(UIContainer.transform, false);
                        frameObj.GetComponent<RectTransform>();
                        frameImage = frameObj.GetComponent<Image>();
                        frameImage.sprite = frame;
                        frameImage.color = UIColorHex;
                        frameObj.transform.localPosition = Vector2.zero;
                        frameObj.transform.localScale = new Vector2(1.5f, 1.25f);
                        //Plugin.mls.LogDebug("made frame object (Bar)");

                        meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                        meterObj.transform.SetParent(UIContainer.transform, false);
                        meterObj.GetComponent<RectTransform>();
                        meterImage = meterObj.GetComponent<Image>();
                        meterImage.sprite = meter;
                        meterImage.color = UIColorHex;
                        meterImage.type = Image.Type.Filled;
                        meterImage.fillMethod = Image.FillMethod.Horizontal;
                        meterObj.transform.localPosition = Vector2.zero;
                        meterObj.transform.localScale = new Vector2(1.5f, 1.25f);
                        //Plugin.mls.LogDebug("made meter object (Bar)");

                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);
                        frameObj.SetActive(true);
                        meterObj.SetActive(true);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.Text:
                        textmesh = UIContainer.AddComponent<TextMeshProUGUI>();
                        RectTransform transform = textmesh.rectTransform;
                        transform.SetParent(UIContainer.transform, false);
                        textmesh.font = hudManager.controlTipLines[0].font;
                        textmesh.color = UIColorHex;
                        textmesh.fontSize = 23;
                        textmesh.overflowMode = TextOverflowModes.Overflow;
                        textmesh.enabled = true;
                        textmesh.text = "";
                        //Plugin.mls.LogDebug("made text object (Percentage)");
                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.All:
                        frame = Plugin.bundle.LoadAsset<Sprite>("meter.png");
                        meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                        textObj = new GameObject("TextHUD", typeof(RectTransform), typeof(TextMeshProUGUI));
                        textObj.transform.SetParent(UIContainer.transform, false);
                        textObj.GetComponent<RectTransform>();
                        textmesh = textObj.GetComponent<TextMeshProUGUI>();
                        RectTransform textTransform = textmesh.rectTransform;
                        textTransform.SetParent(textObj.transform, false);
                        textTransform.localPosition = new Vector2(15, 0);
                        textmesh.font = hudManager.controlTipLines[0].font;
                        textmesh.color = UIColorHex;
                        textmesh.fontSize = 20;
                        textmesh.overflowMode = TextOverflowModes.Overflow;
                        textmesh.enabled = true;
                        textmesh.text = "";
                        //Plugin.mls.LogDebug("made text object (All)");

                        frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                        frameObj.transform.SetParent(UIContainer.transform, false);
                        frameObj.GetComponent<RectTransform>();
                        frameImage = frameObj.GetComponent<Image>();
                        frameImage.sprite = frame;
                        frameImage.color = new Color(0, 0, 0, 0.5f);
                        frameObj.transform.localPosition = Vector2.zero;
                        frameObj.transform.localScale = new Vector2(2.625f, 0.875f);
                        //Plugin.mls.LogDebug("made frame object (All)");

                        meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                        meterObj.transform.SetParent(UIContainer.transform, false);
                        meterObj.GetComponent<RectTransform>();
                        meterImage = meterObj.GetComponent<Image>();
                        meterImage.sprite = meter;
                        meterImage.color = UIColorHex;
                        meterImage.type = Image.Type.Filled;
                        meterImage.fillMethod = Image.FillMethod.Horizontal;
                        meterObj.transform.localPosition = Vector2.zero;
                        meterObj.transform.localScale = new Vector2(2.625f, 0.875f);
                        //Plugin.mls.LogDebug("made meter object (All)");
                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.CircularBar:
                        frame = Plugin.bundle.LoadAsset<Sprite>("meter2.png");
                        meter = Plugin.bundle.LoadAsset<Sprite>("meter2.png");

                        frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                        frameObj.transform.SetParent(UIContainer.transform, false);
                        frameObj.GetComponent<RectTransform>();
                        frameImage = frameObj.GetComponent<Image>();
                        frameImage.sprite = frame;
                        frameImage.color = new Color(0, 0, 0, 0.3f);
                        frameObj.transform.localPosition = Vector2.zero;
                        frameObj.transform.localScale = new Vector2(0.7f, 0.7f);
                        //Plugin.mls.LogDebug("made frame object (Circular)");

                        meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                        meterObj.transform.SetParent(UIContainer.transform, false);
                        meterObj.GetComponent<RectTransform>();
                        meterImage = meterObj.GetComponent<Image>();
                        meterImage.sprite = meter;
                        meterImage.color = UIColorHex;
                        meterImage.type = Image.Type.Filled;
                        meterImage.fillMethod = Image.FillMethod.Radial360;
                        meterImage.fillClockwise = false;
                        meterImage.fillOrigin = 2;
                        //Plugin.mls.LogDebug("made meter object (Circular)");

                        meterObj.transform.localPosition = Vector2.zero;
                        meterObj.transform.localScale = new Vector2(0.7f, 0.7f);

                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);
                        frameObj.SetActive(true);
                        meterObj.SetActive(true);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.VerticalBar:
                        frame = Plugin.bundle.LoadAsset<Sprite>("meter3.png");
                        meter = Plugin.bundle.LoadAsset<Sprite>("meter3.png");

                        frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                        frameObj.transform.SetParent(UIContainer.transform, false);
                        frameObj.GetComponent<RectTransform>();
                        frameImage = frameObj.GetComponent<Image>();
                        frameImage.sprite = frame;
                        frameImage.color = new Color(0, 0, 0, 0.5f);
                        frameObj.transform.localPosition = Vector2.zero;
                        frameObj.transform.localScale = new Vector2(1.25f, 3f);
                        //Plugin.mls.LogDebug("made frame object (Vertical)");

                        meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                        meterObj.transform.SetParent(UIContainer.transform, false);
                        meterObj.GetComponent<RectTransform>();
                        meterImage = meterObj.GetComponent<Image>();
                        meterImage.sprite = meter;
                        meterImage.color = UIColorHex;
                        meterImage.type = Image.Type.Filled;
                        meterImage.fillMethod = Image.FillMethod.Vertical;
                        meterObj.transform.localPosition = Vector2.zero;
                        meterObj.transform.localScale = new Vector2(1.25f, 3f);
                        //Plugin.mls.LogDebug("made meter object (Vertical)");

                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);
                        frameObj.SetActive(true);
                        meterObj.SetActive(true);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.Disabled:
                        if (warningEnabled)
                        {
                            warningObj = new GameObject("WarningHUD", typeof(RectTransform), typeof(Image));
                            warningObj.transform.SetParent(UIContainer.transform, false);
                            warningObj.GetComponent<RectTransform>();
                            warningObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                            warningImage = warningObj.GetComponent<Image>();
                            warningImage.sprite = warning;
                            warningImage.color = UIColorHex;
                            ////Plugin.mls.LogDebug("made warning object (Disabled)");
                        }
                        UIContainer.SetActive(false);

                        ////Plugin.mls.LogDebug("made hud elements");
                        return;
                }
                #endregion
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while making the hud:\n{e}");
            }
        }

        static void UpdateHUDText(float timeMinutes, float timeSeconds)
        {
            if (textmesh == null)
                return;

            switch (selectedText)
            {
                case TextDisplayOptions.Percent:
                    if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                        textmesh.text = LightScript.BatteryPercent.ToString() + "%";
                    if (LightScript.batteryTime <= 0)
                        textmesh.text = "0%";
                    if (LightScript.batteryTime >= LightScript.maxBatteryTime)
                        textmesh.text = "100%";
                    return;
                case TextDisplayOptions.AccuratePercent:
                    if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                        textmesh.text = LightScript.truePercentBattery.ToString("0.0") + "%";
                    if (LightScript.batteryTime <= 0)
                        textmesh.text = "0.0%";
                    if (LightScript.batteryTime >= LightScript.maxBatteryTime)
                        textmesh.text = "100.0%";
                    return;
                case TextDisplayOptions.Time:
                    if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                        textmesh.text = $"{timeMinutes:0}:{timeSeconds:00}";
                    if (LightScript.batteryTime <= 0)
                        textmesh.text = "0:00";
                    if (LightScript.batteryTime >= LightScript.maxBatteryTime)
                        textmesh.text = $"{Mathf.FloorToInt(LightScript.maxBatteryTime / 60):0}:{Mathf.RoundToInt(LightScript.maxBatteryTime % 60):00}";
                    return;
                case TextDisplayOptions.All:
                    if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                        textmesh.text = LightScript.truePercentBattery.ToString("0.0") + "%" + $" | {timeMinutes:0}:{timeSeconds:00}";
                    if (LightScript.batteryTime <= 0)
                        textmesh.text = "0.0% | 0:00";
                    if (LightScript.batteryTime >= LightScript.maxBatteryTime)
                        textmesh.text = "100.0%" + $" | {Mathf.FloorToInt(LightScript.maxBatteryTime / 60):0}:{Mathf.RoundToInt(LightScript.maxBatteryTime % 60):00}";
                    return;
            }
        }
    }
}
