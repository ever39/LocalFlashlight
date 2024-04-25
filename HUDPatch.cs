using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace localFlashlight
{
    internal class HUDPatch
    {
        #region Values
        private static GameObject UIContainer, frameObj, meterObj, textObj, warningObj;
        private static TextMeshProUGUI textmesh;
        private static Image frameImage, meterImage, warningImage;
        private static Sprite frame, meter;
        private static Sprite warning = Plugin.bundle.LoadAsset<Sprite>("warning");
        private static bool fadeIn, fadeOut = false;
        private static bool warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
        private static float warningPercent = Plugin.LowBatteryWarningPercentage.Value;
        private static BatteryDisplayOptions selectedStyle;
        private static TextDisplayOptions selectedText;
        private static float elemScale;
        #endregion

        [HarmonyPatch(typeof(HUDManager), "Start")]
        [HarmonyPostfix]
        public static void GetBatteryUI(ref HUDManager __instance)
        {
            selectedStyle = Plugin.BatteryDisplay.Value;
            selectedText = Plugin.TextDisplay.Value;
            warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
            warningPercent = Plugin.LowBatteryWarningPercentage.Value;
            elemScale = Plugin.UIScale.Value;

            if (selectedStyle == BatteryDisplayOptions.Percentage)
            {
                UIContainer = new GameObject("LocalFlashlightHUDElements");
                UIContainer.AddComponent<RectTransform>();
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                textmesh = UIContainer.AddComponent<TextMeshProUGUI>();
                RectTransform transform = textmesh.rectTransform;
                transform.SetParent(__instance.HUDContainer.transform, false);
                textmesh.font = __instance.controlTipLines[0].font;
                textmesh.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                textmesh.fontSize = 25;
                textmesh.overflowMode = TextOverflowModes.Overflow;
                textmesh.enabled = true;
                textmesh.text = "";
            }
            if (selectedStyle == BatteryDisplayOptions.Bar)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("frame.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                UIContainer = new GameObject("LocalFlashlightHUDElements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                frameObj = new GameObject("Frame");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                frameObj.transform.localPosition = Vector2.zero;
                frameObj.transform.localScale = new Vector2(1.5f, 1.25f);

                meterObj = new GameObject("Meter");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Horizontal;
                meterObj.transform.localPosition = Vector2.zero;
                meterObj.transform.localScale = new Vector2(1.5f, 1.25f);

                UIContainer.SetActive(true);
                frameObj.SetActive(true);
                meterObj.SetActive(true);
            }
            if (selectedStyle == BatteryDisplayOptions.All)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("meter.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                UIContainer = new GameObject("LocalFlashlightHUDElements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                textObj = new GameObject("Text");
                textObj.transform.SetParent(UIContainer.transform, false);
                textObj.AddComponent<RectTransform>();
                textmesh = textObj.AddComponent<TextMeshProUGUI>();
                RectTransform textTransform = textmesh.rectTransform;
                textTransform.SetParent(textObj.transform, false);
                textTransform.localPosition = new Vector2(15, 0);
                textmesh.font = __instance.controlTipLines[0].font;
                textmesh.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                textmesh.fontSize = 20;
                textmesh.overflowMode = TextOverflowModes.Overflow;
                textmesh.enabled = true;
                textmesh.text = "";

                frameObj = new GameObject("Frame");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = new Color(0, 0, 0, 0.5f);
                frameObj.transform.localPosition = Vector2.zero;
                frameObj.transform.localScale = new Vector2(2.625f, 0.875f);

                meterObj = new GameObject("Meter");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Horizontal;
                meterObj.transform.localPosition = Vector2.zero;
                meterObj.transform.localScale = new Vector2(2.625f, 0.875f);
            }
            if (selectedStyle == BatteryDisplayOptions.CircularBar)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("meter2.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter2.png");

                UIContainer = new GameObject("LocalFlashlightHUDElements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                frameObj = new GameObject("Frame");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = new Color(0, 0, 0, 0.3f);
                frameObj.transform.localPosition = Vector2.zero;
                frameObj.transform.localScale = new Vector2(0.7f, 0.7f);

                meterObj = new GameObject("Meter");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Radial360;
                meterImage.fillClockwise = false;
                meterImage.fillOrigin = 2;

                meterObj.transform.localPosition = Vector2.zero;
                meterObj.transform.localScale = new Vector2(0.7f, 0.7f);

                UIContainer.SetActive(true);
                frameObj.SetActive(true);
                meterObj.SetActive(true);
            }
            if (selectedStyle == BatteryDisplayOptions.VerticalBar)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("meter3.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter3.png");

                UIContainer = new GameObject("LocalFlashlightHUDElements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                frameObj = new GameObject("Frame");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = new Color(0, 0, 0, 0.5f);
                frameObj.transform.localPosition = Vector2.zero;
                frameObj.transform.localScale = new Vector2(1.25f, 3f);

                meterObj = new GameObject("Meter");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Vertical;
                meterObj.transform.localPosition = Vector2.zero;
                meterObj.transform.localScale = new Vector2(1.25f, 3f);

                UIContainer.SetActive(true);
                frameObj.SetActive(true);
                meterObj.SetActive(true);
            }
            if (selectedStyle == BatteryDisplayOptions.Disabled)
            {
                if (warningEnabled)
                {
                    UIContainer = new GameObject("LocalFlashlightHUDElement");
                    UIContainer.AddComponent<RectTransform>();
                    UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                    UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                    UIContainer.AddComponent<CanvasGroup>();
                    UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                    warningObj = new GameObject("Warning");
                    warningObj.transform.SetParent(UIContainer.transform, false);
                    warningObj.AddComponent<RectTransform>();
                    warningObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    warningImage = warningObj.AddComponent<Image>();
                    warningImage.sprite = warning;
                    warningImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;

                    UIContainer.SetActive(false);
                }
            }

            Plugin.mls.LogInfo("Made HUD elements!");
        }

        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        public static void UpdateBatteryText()
        {
            float t0 = LightScript.batteryTime;
            float timeMinutes = Mathf.FloorToInt(t0 / 60);
            float timeSeconds = Mathf.FloorToInt(t0 % 60);

            if (selectedStyle == BatteryDisplayOptions.Percentage)
            {
                if (selectedText == TextDisplayOptions.Percent) textmesh.text = LightScript.BatteryPercent.ToString() + "%";
                if (selectedText == TextDisplayOptions.AccuratePercent) textmesh.text = LightScript.truePercentBattery.ToString("0.0") + "%";
                if (selectedText == TextDisplayOptions.Time) textmesh.text = $"{timeMinutes:0}:{timeSeconds:00}";
                if (selectedText == TextDisplayOptions.All) textmesh.text = LightScript.truePercentBattery.ToString("0.0") + "%" + $" | {timeMinutes:0}:{timeSeconds:00}";

                if (Plugin.HideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | LightScript.UIHideTime > 0) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.Bar)
            {
                meterImage.fillAmount = LightScript.BatteryClamped;

                if (Plugin.HideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | LightScript.UIHideTime > 0) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.All)
            {
                if (selectedText == TextDisplayOptions.Percent) textmesh.text = LightScript.BatteryPercent.ToString() + "%";
                if (selectedText == TextDisplayOptions.AccuratePercent) textmesh.text = LightScript.truePercentBattery.ToString("0.0") + "%";
                if (selectedText == TextDisplayOptions.Time) textmesh.text = $"{timeMinutes:0}:{timeSeconds:00}";
                if (selectedText == TextDisplayOptions.All) textmesh.text = LightScript.truePercentBattery.ToString("0.0") + "%" + $" | {timeMinutes:0}:{timeSeconds:00}";

                meterImage.fillAmount = LightScript.BatteryClamped;

                if (Plugin.HideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | LightScript.UIHideTime > 0) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.CircularBar)
            {
                meterImage.fillAmount = LightScript.BatteryClamped;

                if (LightScript.BatteryClamped >= 1)
                {
                    meterImage.fillAmount = 1;
                }

                if (Plugin.HideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | LightScript.UIHideTime > 0) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.VerticalBar)
            {
                meterImage.fillAmount = LightScript.BatteryClamped;

                if (Plugin.HideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | LightScript.UIHideTime > 0) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.Disabled)
            {
                UIContainer.GetComponent<CanvasGroup>().alpha = 1;

                if (warningEnabled)
                {
                    if (LightScript.BatteryPercent < warningPercent)
                    {
                        UIContainer.SetActive(true);
                    }
                    if (LightScript.BatteryPercent > warningPercent)
                    {
                        UIContainer.SetActive(false);
                    }
                }
            }

            //color updates!
            if (frameObj != null && selectedStyle == BatteryDisplayOptions.Bar)
            {
                frameImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
            }
            if (meterObj != null)
            {
                meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
            }
            if (warningObj != null)
            {
                warningImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
                warningPercent = Plugin.LowBatteryWarningPercentage.Value;
                warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
            }
            if (textObj != null)
            {
                textmesh.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
            }

            if (selectedStyle == BatteryDisplayOptions.All | selectedStyle == BatteryDisplayOptions.Percentage)
            {
                if (selectedText == TextDisplayOptions.AccuratePercent)
                {
                    if (LightScript.batteryTime <= 0)
                    {
                        textmesh.text = "0.0%";
                    }
                }
                if (selectedText == TextDisplayOptions.Time)
                {
                    if (LightScript.batteryTime <= 0)
                    {
                        textmesh.text = "0:00";
                    }
                }
                if (selectedText == TextDisplayOptions.All)
                {
                    if (LightScript.batteryTime <= 0)
                    {
                        textmesh.text = "0.0% | 0:00";
                    }
                }
            }

            UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
            elemScale = Plugin.UIScale.Value;
            UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

            //fade things
            if (fadeIn)
            {
                if (UIContainer.GetComponent<CanvasGroup>().alpha < 1)
                {
                    UIContainer.GetComponent<CanvasGroup>().alpha += Time.deltaTime * 6;
                    if (UIContainer.GetComponent<CanvasGroup>().alpha >= 1)
                    {
                        UIContainer.GetComponent<CanvasGroup>().alpha = 1;
                        fadeIn = false;
                    }
                }
            }

            if (fadeOut)
            {
                if (UIContainer.GetComponent<CanvasGroup>().alpha > Plugin.UIHiddenAlpha.Value)
                {
                    UIContainer.GetComponent<CanvasGroup>().alpha -= Time.deltaTime * 4;
                    if (UIContainer.GetComponent<CanvasGroup>().alpha <= Plugin.UIHiddenAlpha.Value)
                    {
                        UIContainer.GetComponent<CanvasGroup>().alpha = Plugin.UIHiddenAlpha.Value;
                        fadeOut = false;
                    }
                }
            }
        }


        //the actual fade voids
        private static void ShowUI()
        {
            fadeIn = true;
        }

        private static void HideUI()
        {
            fadeOut = true;
        }
    }
}
