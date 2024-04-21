using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace localFlashlight
{
    internal class HUDPatch
    {
        private static GameObject UIContainer, frameObj, meterObj, textObj, warningObj;
        private static TextMeshProUGUI textmesh;
        private static Image frameImage;
        private static Image meterImage;
        private static Image warningImage;
        private static Sprite frame;
        private static Sprite meter;
        private static Sprite warning = Plugin.bundle.LoadAsset<Sprite>("warning");
        private static float fadeAlpha = Plugin.UIHiddenAlpha.Value;
        private static bool fadeIn, fadeOut = false;
        private static bool warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
        private static float warningPercent = Plugin.LowBatteryWarningPercentage.Value;
        private static Color setColor = new Color((float)Plugin.colorRed.Value / 255, (float)Plugin.colorGreen.Value / 255, (float)Plugin.colorBlue.Value / 255);
        private static BatteryDisplayOptions selectedStyle;
        private static float elemScale;

        [HarmonyPatch(typeof(HUDManager), "Start")]
        [HarmonyPostfix]
        public static void GetBatteryUI(ref HUDManager __instance)
        {
            selectedStyle = Plugin.batteryDisplay.Value;
            fadeAlpha = Plugin.UIHiddenAlpha.Value;
            warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
            warningPercent = Plugin.LowBatteryWarningPercentage.Value;
            elemScale = Plugin.UIScale.Value;

            if (selectedStyle == BatteryDisplayOptions.Percentage)
            {
                UIContainer = new GameObject("Local Flashlight HUD Elements");
                UIContainer.AddComponent<RectTransform>();
                UIContainer.transform.localPosition = new Vector2(450, -215);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                textmesh = UIContainer.AddComponent<TextMeshProUGUI>();
                RectTransform transform = textmesh.rectTransform;
                transform.SetParent(__instance.HUDContainer.transform, false);
                textmesh.font = __instance.controlTipLines[0].font;
                textmesh.color = setColor;
                textmesh.fontSize = 25;
                textmesh.overflowMode = TextOverflowModes.Overflow;
                textmesh.enabled = true;
                textmesh.text = "";
            }
            if (selectedStyle == BatteryDisplayOptions.Bar)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("frame.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                UIContainer = new GameObject("Local Flashlight HUD Elements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(390, -270);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                frameObj = new GameObject("Frame");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = setColor;
                frameObj.transform.localPosition = new Vector2(-40, 55);
                frameObj.transform.localScale = new Vector2(1.5f, 1.25f);

                meterObj = new GameObject("Meter");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = setColor;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Horizontal;
                meterObj.transform.localPosition = new Vector2(-40, 55);
                meterObj.transform.localScale = new Vector2(1.5f, 1.25f);

                UIContainer.SetActive(true);
                frameObj.SetActive(true);
                meterObj.SetActive(true);
            }
            if (selectedStyle == BatteryDisplayOptions.All)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("meter.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                UIContainer = new GameObject("Local Flashlight HUD Elements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localPosition = new Vector2(375, -255);
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                textObj = new GameObject("Text");
                textObj.transform.SetParent(UIContainer.transform, false);
                textObj.AddComponent<RectTransform>();
                textObj.transform.localPosition = Vector2.zero;
                textmesh = textObj.AddComponent<TextMeshProUGUI>();
                RectTransform textTransform = textmesh.rectTransform;
                textTransform.SetParent(textObj.transform, false);
                textTransform.localPosition = new Vector2(-37, 45);
                textmesh.font = __instance.controlTipLines[0].font;
                textmesh.color = setColor;
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
                frameObj.transform.localPosition = new Vector2(-50, 45);
                frameObj.transform.localScale = new Vector2(2.625f, 0.875f);

                meterObj = new GameObject("Meter");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = setColor;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Horizontal;
                meterObj.transform.localPosition = new Vector2(-50, 45);
                meterObj.transform.localScale = new Vector2(2.625f, 0.875f);
            }
            if (selectedStyle == BatteryDisplayOptions.CircularBar)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("meter2.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter2.png");

                UIContainer = new GameObject("Local Flashlight HUD Elements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(410, -250);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                frameObj = new GameObject("Frame");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = new Color(0, 0, 0, 0.3f);
                frameObj.transform.localPosition = new Vector2(-40, 55);
                frameObj.transform.localScale = new Vector2(0.7f, 0.7f);

                meterObj = new GameObject("Meter");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = setColor;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Radial360;
                meterImage.fillClockwise = false;
                meterImage.fillOrigin = 2;

                meterObj.transform.localPosition = new Vector2(-40, 55);
                meterObj.transform.localScale = new Vector2(0.7f, 0.7f);

                UIContainer.SetActive(true);
                frameObj.SetActive(true);
                meterObj.SetActive(true);
            }
            if (selectedStyle == BatteryDisplayOptions.VerticalBar)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("meter3.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter3.png");

                UIContainer = new GameObject("Local Flashlight HUD Elements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(450, -100);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                frameObj = new GameObject("Frame");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = new Color(0, 0, 0, 0.5f);
                frameObj.transform.localPosition = new Vector2(-40, 55);
                frameObj.transform.localScale = new Vector2(1.25f, 3f);

                meterObj = new GameObject("Meter");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = setColor;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Vertical;
                meterObj.transform.localPosition = new Vector2(-40, 55);
                meterObj.transform.localScale = new Vector2(1.25f, 3f);

                UIContainer.SetActive(true);
                frameObj.SetActive(true);
                meterObj.SetActive(true);
            }
            if (selectedStyle == BatteryDisplayOptions.Disabled)
            {
                if (warningEnabled)
                {
                    UIContainer = new GameObject("Local Flashlight UI Elements");
                    UIContainer.AddComponent<RectTransform>();
                    UIContainer.transform.localPosition = new Vector2(350, -190);
                    UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                    UIContainer.AddComponent<CanvasGroup>();
                    UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);

                    warningObj = new GameObject("Warning");
                    warningObj.transform.SetParent(UIContainer.transform, false);
                    warningObj.AddComponent<RectTransform>();
                    warningObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    warningImage = warningObj.AddComponent<Image>();
                    warningImage.sprite = warning;
                    warningImage.color = setColor;

                    UIContainer.SetActive(false);
                }
            }

            Plugin.mls.LogInfo("Made HUD elements!");
        }

        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        public static void UpdateBatteryText()
        {
            if (selectedStyle == BatteryDisplayOptions.Percentage)
            {
                textmesh.text = LightScript.BatteryPercent.ToString() + "%";

                if (Plugin.hideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | (LightScript.UIHideTime > 0 || LightScript.BatteryPercent < 100)) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.Bar)
            {
                meterImage.fillAmount = LightScript.BatteryClamped;

                if (Plugin.hideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | (LightScript.UIHideTime > 0 || LightScript.BatteryPercent < 100)) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.All)
            {
                textmesh.text = LightScript.BatteryPercent.ToString() + "%";
                meterImage.fillAmount = LightScript.BatteryClamped;

                if (Plugin.hideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | (LightScript.UIHideTime > 0 || LightScript.BatteryPercent < 100)) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.CircularBar)
            {
                meterImage.fillAmount = LightScript.BatteryClamped;

                if(LightScript.BatteryClamped >= 1)
                {
                    meterImage.fillAmount = 1;
                }

                if (Plugin.hideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | (LightScript.UIHideTime > 0 || LightScript.BatteryPercent < 100)) ShowUI();
                }
            }
            if (selectedStyle == BatteryDisplayOptions.VerticalBar)
            {
                meterImage.fillAmount = LightScript.BatteryClamped;

                if (Plugin.hideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | (LightScript.UIHideTime > 0 || LightScript.BatteryPercent < 100)) ShowUI();
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
                if (UIContainer.GetComponent<CanvasGroup>().alpha > fadeAlpha)
                {
                    UIContainer.GetComponent<CanvasGroup>().alpha -= Time.deltaTime * 4;
                    if (UIContainer.GetComponent<CanvasGroup>().alpha <= fadeAlpha)
                    {
                        UIContainer.GetComponent<CanvasGroup>().alpha = fadeAlpha;
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
