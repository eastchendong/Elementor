using System.Collections.Generic;
using UnityEngine;
using Elementor.Core.Speech;
using Elementor.Core;
using System.IO;

namespace Elementor
{
    public static class ReactionDataParser
    {
        public static List<ReactionStage> ParseReactionFromJSON(string jsonPath)
        {
            try
            {
                string jsonText = "";
                
                // Try to load from persistent data path first (for runtime generated files)
                string persistentPath = Path.Combine(UnityEngine.Application.persistentDataPath, jsonPath);
                if (System.IO.File.Exists(persistentPath))
                {
                    jsonText = System.IO.File.ReadAllText(persistentPath);
                    Debug.Log($"📖 ReactionDataParser loaded from persistent path: {persistentPath}");
                }
                else
                {
                    // For StreamingAssets files in Android APK, we need to use UnityWebRequest
                    Debug.LogWarning($"⚠️ ReactionDataParser: File not found in persistent storage: {jsonPath}");
                    Debug.LogWarning("💡 For Android APK builds, ensure JSON files are pre-loaded to persistent storage or use LoreJsonReader for StreamingAssets access");
                    return new List<ReactionStage>();
                }
                
                var loreData = JsonUtility.FromJson<LoreData>(jsonText);
                return ParseReactionFromLore(loreData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse reaction from JSON: {e.Message}");
                return new List<ReactionStage>();
            }
        }

        private static List<ReactionStage> ParseReactionFromLore(LoreData loreData)
        {
            var stages = new List<ReactionStage>();
            var stage = new ReactionStage();
            stage.reactionName = loreData.reaction.equation;
            
            stage.conditionEffectName = GetConditionEffectName(loreData.reaction);
            stage.phenomenonEffectName = GetPhenomenonEffectName(loreData.reaction);
            
            stage.requirements = new List<SlotRequirement>();
            stage.outcomes = new List<ReactionOutcome>();

            // Parse reactants
            foreach (var reactant in loreData.reaction.reactants)
            {
                var requirement = new SlotRequirement();
                requirement.requiredCount = reactant.count;
                requirement.requiredName = reactant.name;
                stage.requirements.Add(requirement);
            }

            // Parse products
            foreach (var product in loreData.reaction.products)
            {
                var outcome = new ReactionOutcome();
                outcome.newGroupName = product.name;
                outcome.productCount = product.count;
                outcome.characterNamesInGroup = new List<string>();

                foreach (var element in product.elements)
                {
                    for (int i = 0; i < element.count; i++)
                    {
                        outcome.characterNamesInGroup.Add(element.element);
                    }
                }

                stage.outcomes.Add(outcome);
            }

            stages.Add(stage);
            return stages;
        }

        private static string GetConditionEffectName(ReactionData reactionData)
        {
            if (reactionData.equation.Contains("燃烧") || reactionData.equation.Contains("burn"))
                return "BurningCondition";
            else if (reactionData.equation.Contains("发光") || reactionData.equation.Contains("glow"))
                return "GlowingCondition";
            
            return reactionData.equation + "_Condition";
        }

        private static string GetPhenomenonEffectName(ReactionData reactionData)
        {
            if (reactionData.equation.Contains("燃烧") || reactionData.equation.Contains("burn"))
                return "BurningPhenomenon";
            else if (reactionData.equation.Contains("发光") || reactionData.equation.Contains("glow"))
                return "GlowingPhenomenon";
            
            return reactionData.equation + "_Phenomenon";
        }

        // Data classes for JSON parsing
        [System.Serializable]
        public class LoreData
        {
            public ReactionData reaction;
        }

        [System.Serializable]
        public class ReactionData
        {
            public string equation;
            public ReactantData[] reactants;
            public ProductData[] products;
        }

        [System.Serializable]
        public class ReactantData
        {
            public string name;
            public string type;
            public int count;
            public ElementData[] elements;
        }

        [System.Serializable]
        public class ProductData
        {
            public string name;
            public string type;
            public int count;
            public ElementData[] elements;
        }

        [System.Serializable]
        public class ElementData
        {
            public string element;
            public int count;
        }
    }
}
