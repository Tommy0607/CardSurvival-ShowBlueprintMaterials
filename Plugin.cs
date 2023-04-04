using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MyPlugin
{
    [BepInPlugin(
        "xyz.tommy0607.csti.show_blueprint_materials",
        "ShowBlueprintMaterials",
        "1.0.0"
    )]
    public class Plugin : BaseUnityPlugin
    {

        private static ManualLogSource logger;
        private static GameObject showElementsIcon;
        private const string TOOLTIP_TITLE = "剩余材料";
        
        private void Start()
        {
            Logger.LogInfo("ShowBlueprintMaterials Start");
            logger = Logger;
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BlueprintConstructionPopup), "SetupInventory")]
        public static void BlueprintConstructionPopup_SetupInventory(BlueprintConstructionPopup __instance, InGameCardBase _Card)
        {
            var cardStages = _Card.CardModel.BlueprintStages;
            var currentStageIndex = _Card.BlueprintData.CurrentStage;
            if (currentStageIndex < 0 || currentStageIndex >= cardStages.Length)
            {
                return;
            }
            
            var elementSizeDict = new Dictionary<string, int>();
            for (int i = currentStageIndex; i < cardStages.Length; i++)
            {
                foreach (var blueprintElement in cardStages[i].RequiredElements)
                {
                    var elementName = blueprintElement.GetName;
                    var elementCount = blueprintElement.GetQuantity;
                    if (!elementSizeDict.ContainsKey(elementName))
                    {
                        elementSizeDict[elementName] = elementCount;
                    }
                    else
                    {
                        elementSizeDict[elementName] += elementCount;
                    }
                }
            }
            var tooltipContent = "";
            foreach (var elementSize in elementSizeDict)
            {
                tooltipContent += elementSize.Value + "x" + elementSize.Key + "\n";
            }
            tooltipContent = tooltipContent.Substring(0, tooltipContent.Length - 1);
            if (showElementsIcon == null)
            {
                GameObject gameObject = new GameObject();
                gameObject.name = "Show Elements Icon";
                var assembly = Assembly.GetExecutingAssembly();
                Texture2D texture = new Texture2D(0, 0);
                var stream = assembly.GetManifestResourceStream("MyPlugin.Info.png");
                texture.LoadImage(ReadBytesFromStream(stream));
                Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
                Image image = gameObject.AddComponent<Image>();
                image.sprite = sprite;
                var tooltip = gameObject.AddComponent<TooltipProvider>();
                tooltip.SetTooltip(TOOLTIP_TITLE, tooltipContent, "");
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.SetParent(__instance.transform);
                rectTransform.localScale = new Vector3(1, 1, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.sizeDelta = new Vector2(64, 64);
                rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(64, -62);
                gameObject.SetActive(true);
                showElementsIcon = gameObject;
            }
            else
            {
                showElementsIcon.GetComponent<TooltipProvider>().SetTooltip(TOOLTIP_TITLE, tooltipContent, "");
            }

        }

        private static byte[] ReadBytesFromStream(Stream input)
        {
            var buffer = new byte[16*1024];
            using (var memoryStream = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, read);
                }
                return memoryStream.ToArray();
            }
        }

    }
}
