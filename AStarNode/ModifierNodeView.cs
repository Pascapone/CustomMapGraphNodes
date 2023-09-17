using InsaneScatterbrain.ScriptGraph.Editor;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace InsaneScatterbrain.MapGraph.Editor
{
    [ScriptNodeView(typeof(ModifierNode))]
    public class ModifierNodeView : ScriptNodeView
    {
        private readonly ModifierNode node;
        private readonly MapGraphGraph graph;
        private readonly List<ModifierColor> namedColors;
        private List<string> colorNames;
        private VisualElement mainBox;
        private readonly PopupField<string> popupField;
        private readonly Button addButton;

        public ModifierNodeView(ModifierNode node, ScriptGraphView graphView) : base(node, graphView)
        {
            graph = (MapGraphGraph)graphView.Graph;
            namedColors = new List<ModifierColor>();
            this.node = node;
            this.node.modifiers ??= new Modifiers();

            CreateMainBoxVisuals();

            if (graph.NamedColorSet == null)
            {
                CreateNamedColorSetMissingVisuals();
                return;
            }

            InitializeColorNames();

            addButton = new Button(AddModifier);
            addButton.text = "Add Modifier";
            
            popupField = new PopupField<string>();
            popupField.RegisterValueChangedCallback(OnPopupChange);
            
            UpdatePopupField();

            mainBox.Add(popupField);
            mainBox.Add(addButton);

            foreach (var modifier in node.modifiers.activeModifiers)
            {
                VisualElement visualElement = CreateModifierVisuals(modifier.namedColor.name, modifier.movementPenalty);
                modifier.visualElement = visualElement;
            }
        }

        private void InitializeColorNames()
        {
            foreach (var namedColorId in graph.NamedColorSet.OrderedIds)
            {
                var colorName = graph.NamedColorSet.GetName(namedColorId);
                var color = graph.NamedColorSet.GetColorById(namedColorId);
                namedColors.Add(new ModifierColor(colorName, color));
            }
            
            colorNames = namedColors.Select(namedColor => namedColor.name).ToList();
        }

        private void AddModifier()
        {
            VisualElement visuals = CreateModifierVisuals(node.modifiers.selectedName, 0);

            ModifierColor namedColor = namedColors.Find(x => x.name == node.modifiers.selectedName);
            node.modifiers.activeModifiers.Add(new Modifier(namedColor, 0, visuals));

            UpdatePopupField();
            EditorUtility.SetDirty(Graph);
        }
        
        private void DeleteModifier(ClickEvent evt)
        {
            if (evt.target is not VisualElement visualElement) return;
            
            for (int i = 0; i < node.modifiers.activeModifiers.Count; i++)
            {
                if (node.modifiers.activeModifiers[i].visualElement != visualElement.parent) continue;
                
                node.modifiers.activeModifiers.RemoveAt(i);
                visualElement.parent.parent.Remove(visualElement.parent);

                UpdatePopupField();
                EditorUtility.SetDirty(Graph);
                break;
            }
        }
        
        private void UpdatePopupField()
        {
            var activeNames = node.modifiers.activeModifiers.Select(modifier => modifier.namedColor.name);
            var allowedNames = colorNames.FindAll(n => !activeNames.Contains(n));

            popupField.choices = allowedNames;

            if (allowedNames.Count > 0)
            {
                popupField.value = allowedNames[0];
                node.modifiers.selectedName = allowedNames[0];
                popupField.SetEnabled(true);
                addButton.SetEnabled(true);
            }
            else
            {
                popupField.value = "[None]";
                node.modifiers.selectedName = "[None]";
                popupField.SetEnabled(false);
                addButton.SetEnabled(false);
            }
            EditorUtility.SetDirty(Graph);
        }
        
        private void OnPopupChange(ChangeEvent<string> evt)
        {
            node.modifiers.selectedName = evt.newValue;
            EditorUtility.SetDirty(Graph);
        }
        
        private void OnPenaltyChange(ChangeEvent<int> e)
        {
            VisualElement target = e.target as VisualElement;
            Modifier modifier = node.modifiers.activeModifiers.Find(x => x.visualElement == target?.parent);
            
            if (modifier == null) return;
            
            modifier.movementPenalty = e.newValue;
            EditorUtility.SetDirty(Graph);
        }
        
        private void CreateNamedColorSetMissingVisuals()
        {
            var errorLabel = new Label("No Named Color Set");
            errorLabel.style.color = Color.red;
            mainBox.Add(errorLabel);
        }

        private void CreateMainBoxVisuals()
        {
            mainBox = new VisualElement();
            mainBox.style.minWidth = 200;
            mainBox.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
            mainBox.style.marginBottom = 5f;
            Add(mainBox);
        }
        
        private VisualElement CreateModifierVisuals(string colorName, int penalty)
        {
            VisualElement horizontalContainer = new VisualElement();
            horizontalContainer.style.flexDirection = FlexDirection.Row;
            horizontalContainer.style.flexWrap = Wrap.NoWrap;

            var label = new Label(colorName);
            label.style.flexGrow = 1;
            horizontalContainer.Add(label);
            
            var intField = new IntegerField();
            intField.RegisterCallback<ChangeEvent<int>>(OnPenaltyChange);
            intField.style.width = 50;
            intField.style.minWidth = 50;
            intField.SetValueWithoutNotify(penalty);
            horizontalContainer.Add(intField);
        
            var removeButton = new Button();
            removeButton.RegisterCallback<ClickEvent>(DeleteModifier);
            removeButton.text = "X";
            horizontalContainer.Add(removeButton);
            
            horizontalContainer.style.alignItems = Align.Center;
            mainBox.Add(horizontalContainer);

            return horizontalContainer;
        }
    } 
}