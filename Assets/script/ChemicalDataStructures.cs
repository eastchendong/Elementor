using System;

namespace Elementor
{
    [System.Serializable]
    public class ChemicalResponse
    {
        public string scene_id;
        public StoryData story;
        public ReactionData reaction;
        public ElectronTransferData electron_transfer;
        public GameplayTriggerData gameplay_trigger;
    }

    [System.Serializable]
    public class StoryData
    {
        public string title;
        public string[] plot;
    }

    [System.Serializable]
    public class ReactionData
    {
        public string equation;
        public string type;
        public string[] conditions;
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

    [System.Serializable]
    public class ElectronTransferData
    {
        public string from;
        public string to;
        public int electron_count;
        public string description;
    }

    [System.Serializable]
    public class GameplayTriggerData
    {
        public RequiredIonData[] required_ions;
        public string reaction_area;
        public SuccessEffectsData success_effects;
    }

    [System.Serializable]
    public class RequiredIonData
    {
        public string name;
        public string from;
        public ElementData[] elements;
    }

    [System.Serializable]
    public class SuccessEffectsData
    {
        public string animation;
        public string[] new_items;
        public bool story_continuation;
    }

    [System.Serializable]
    public class SynthesisResponse
    {
        public bool can_synthesize;
        public string compound_formula;
        public string compound_name;
        public string explanation;
    }
}
