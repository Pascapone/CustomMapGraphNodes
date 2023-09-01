using InsaneScatterbrain.ScriptGraph.Editor;
using UnityEngine;

namespace InsaneScatterbrain.MapGraph.Editor
{
    [ScriptNodeView(typeof(RandomPointsWithDistanceNode))]
    public class RandomPointsWithDistanceNodeView : ScriptNodeView
    {
        public RandomPointsWithDistanceNodeView(RandomPointsWithDistanceNode node, ScriptGraphView graphView) : base(node, graphView)
        {
            this.AddPreview<RandomPointsWithDistanceNode>(GetPreviewTexture);
        }

        private Texture2D GetPreviewTexture(RandomPointsWithDistanceNode node) => node.TextureData.ToTexture2D();
    }
}