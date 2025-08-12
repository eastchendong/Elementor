using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Elementor.Core
{
    [System.Serializable]
    public class SlotRequirement
    {
        public CharacterSlot slot;
        public string requiredName;
        public int requiredCount = 1;
    }

    [System.Serializable]
    public class ReactionOutcome
    {
        public CharacterSlot outputSlot;
        public string newGroupName;
        public List<string> characterNamesInGroup;
        public int productCount = 1;         // Number of products to create
    }

    [System.Serializable]
    public class ReactionStage
    {
        public string reactionName;
        public List<SlotRequirement> requirements; // 反应物 (Reactants)
        public List<ReactionOutcome> outcomes;     // 生成物 (Products)
        public string reactionCondition;           // 反应条件 (Condition)
        public UnityEvent onReactionPhenomenon = new UnityEvent();    // 反应现象 (Phenomenon)
    }

    // JSON parsing data classes
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
