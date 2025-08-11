using System;
using System.Collections.Generic;

namespace Elementor.Lore
{
    [Serializable]
    public class LoreStory
    {
        public string title;
        public List<string> plot;
    }

    [Serializable]
    public class LoreElement
    {
        public string element;
        public int count;
    }

    [Serializable]
    public class LoreCompound
    {
        public string name;
        public string type;
        public int count;
        public List<LoreElement> elements;
    }

    [Serializable]
    public class LoreReaction
    {
        public string equation;
        public string type;
        public List<string> conditions;
        public List<LoreCompound> reactants;
        public List<LoreCompound> products;
    }

    [Serializable]
    public class LoreElectronTransfer
    {
        public string from;
        public string to;
        public int electron_count;
        public string description;
    }

    [Serializable]
    public class LoreRequiredIon
    {
        public string name;
        public string from;
        public List<LoreElement> elements;
    }

    [Serializable]
    public class LoreSuccessEffects
    {
        public string animation;
        public List<string> new_items;
        public bool story_continuation;
    }

    [Serializable]
    public class LoreGameplayTrigger
    {
        public List<LoreRequiredIon> required_ions;
        public string reaction_area;
        public LoreSuccessEffects success_effects;
    }

    [Serializable]
    public class LoreData
    {
        public string scene_id;
        public LoreStory story;
        public LoreReaction reaction;
        public LoreElectronTransfer electron_transfer;
        public LoreGameplayTrigger gameplay_trigger;
    }
}
