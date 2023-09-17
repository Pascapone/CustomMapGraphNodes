using System;
using InsaneScatterbrain.ScriptGraph;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections.Generic;
using PoolManager = InsaneScatterbrain.Pooling.PoolManager;

namespace InsaneScatterbrain.MapGraph
{
    /// <summary>
    /// Walks the shortest path.
    /// </summary>
    [ScriptNode("AStar", "Drawing"), Serializable]
    public class AStarNode : ProcessorNode
    {
        [InPort("Penalties Texture", typeof(TextureData), true), SerializeReference] 
        private InPort textureIn = null;
        
        [InPort("Draw Color", typeof(Color32)), SerializeReference] 
        private InPort drawColorIn = null;
        
        [InPort("Start", typeof(Vector2Int), true), SerializeReference] 
        private InPort startIn = null;

        [InPort("Target", typeof(Vector2Int), true), SerializeReference] 
        private InPort targetIn = null;
        
        [InPort("Unwalkable Mask", typeof(Mask), true), SerializeReference]
        private InPort maskIn = null;
        
        [InPort("No Diagonals", typeof(bool)), SerializeReference]
        private InPort noDiagonalsIn = null;
        
        [InPort("Penalties", typeof(Modifiers)), SerializeReference]
        private InPort modifiersIn = null;

        [OutPort("Texture", typeof(TextureData)), SerializeReference] 
        private OutPort textureOut = null;

        [OutPort("Mask", typeof(Mask)), SerializeReference]
        private OutPort maskOut = null;

        private TextureData textureData;
#if UNITY_EDITOR
        /// <summary>
        /// Gets the latest generated texture data. Only available in the editor.
        /// </summary>
        public TextureData TextureData => textureData;
#endif

        /// <inheritdoc cref="ProcessorNode.OnProcess"/>
        protected override void OnProcess()
        {
            var instanceProvider = Get<IInstanceProvider>();
            var start = startIn.Get<Vector2Int>();
            var target = targetIn.Get<Vector2Int>();
            var drawColor = drawColorIn.Get<Color32>();
            var noDiagonals = noDiagonalsIn.Get<bool>();
            var modifiers = modifiersIn.Get<Modifiers>();
            
            Dictionary<Color32, int> penaltiesDict = new Dictionary<Color32, int>();
            if (modifiers != null)
            {
                foreach (var modifier in modifiers.activeModifiers)
                {
                    penaltiesDict[modifier.namedColor.color] = modifier.movementPenalty;
                }
            }

            textureData = instanceProvider.Get<TextureData>();
            textureIn.Get<TextureData>().Clone(textureData);

            var width = textureData.Width;
            var height = textureData.Height;

            var outMask = maskOut.IsConnected ? instanceProvider.Get<Mask>() : null;
            outMask?.Set(width * height);
            
            var mask = maskIn.Get<Mask>();

            if (mask == null) return;

            int startIndex = width * start.y + start.x;
            int targetIndex = width * target.y + target.x;

            HeapElement startTile = new HeapElement(startIndex, mask.IsPointMasked(startIndex));
            HeapElement targetTile = new HeapElement(targetIndex, mask.IsPointMasked(targetIndex));
            
            if (!startTile.isWalkable || !targetTile.isWalkable)
            {
                textureOut.Set(() => textureData);
                if (outMask != null) maskOut.Set(() => outMask);
                return;
            }
            
            NativeArray<HeapElement> tileMap = new NativeArray<HeapElement>(width * height, Allocator.Persistent);
            for (int i = 0; i < tileMap.Length; i++)
            {
                bool isWalkable = (bool)mask?.IsPointMasked(i);
                bool penalty = penaltiesDict.TryGetValue(textureData[i], out int value);
                value = penalty ? value : 0;
                
                
                tileMap[i] = new HeapElement(i, isWalkable, value);
            }

            NativeArray<HeapElement> waypoints =
                new NativeArray<HeapElement>(width * height, Allocator.Persistent);

            NativeArray<int> waypointCount =
                new NativeArray<int>(1, Allocator.Persistent);
            
            

            Pathfinding pathfinding = new Pathfinding(width, height, noDiagonals, tileMap, startTile, targetTile, waypoints, waypointCount);

            JobHandle jobHandle = pathfinding.Schedule();
            jobHandle.Complete();

            
            textureData = instanceProvider.Get<TextureData>();
            textureData.Set(width, height);

            if (waypointCount[0] != 0)
            {
                for (int i = 0; i < waypointCount[0]; i++)
                {
                    textureData[waypoints[i].index] = drawColor;
                    outMask?.MaskPoint(waypoints[i].index);
                }
            }

            textureOut.Set(() => textureData);
            
            if (outMask != null)
            {
                maskOut.Set(() => outMask);
            }

            tileMap.Dispose();
            waypoints.Dispose();
            waypointCount.Dispose();
        }
    }
}