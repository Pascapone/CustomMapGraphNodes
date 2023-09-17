using System;
using InsaneScatterbrain.ScriptGraph;
using UnityEngine;
using System.Collections.Generic; 
using UnityEngine.UIElements;

namespace InsaneScatterbrain.MapGraph
{
    [ScriptNode("Modifier Node", "Custom"), Serializable]
    public class ModifierNode : ProcessorNode
    {
        [OutPort("Modifiers", typeof(Modifiers)), SerializeReference]
        private OutPort modifiersOut = null;
        
        public Modifiers modifiers;
        
#if UNITY_EDITOR
        protected OutPort ModifiersOut => modifiersOut;
#endif

        protected override void OnProcess()
        {
            modifiersOut.Set(() => modifiers);
        }
    }
    
    [Serializable]
    public class Modifiers
    {
        public List<Modifier> activeModifiers = new List<Modifier>();
        public string selectedName;
    }

    [Serializable]
    public class Modifier
    {
        public ModifierColor namedColor;
        public int movementPenalty;
        public VisualElement visualElement;

        public Modifier(ModifierColor namedColor, int movementPenalty, VisualElement visualElement)
        {
            this.namedColor = namedColor;
            this.movementPenalty = movementPenalty;
            this.visualElement = visualElement;
        }
    }
    
    [Serializable]
    public class ModifierColor
    {
        public string name;
        public Color32 color;

        public ModifierColor(string name, Color32 color)
        {
            this.name = name;
            this.color = color;
        }
    }
}
