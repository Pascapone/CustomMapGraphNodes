using InsaneScatterbrain.ScriptGraph.Editor;
using UnityEngine;

namespace InsaneScatterbrain.MapGraph.Editor
{
    [ScriptNodeView(typeof(AStarNode))]
    public class AStarNodeView : ScriptNodeView
    {
        public AStarNodeView(AStarNode node, ScriptGraphView graphView) : base(node, graphView)
        {
            this.AddPreview<AStarNode>(GetPreviewTexture);
        }

        private Texture2D GetPreviewTexture(AStarNode node) => node.TextureData.ToTexture2D();
    }
}