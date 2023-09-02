using System;
using System.Collections.Generic;
using InsaneScatterbrain.Extensions;
using InsaneScatterbrain.ScriptGraph;
using InsaneScatterbrain.Services;
using UnityEngine;
using SysRandom = System.Random;

namespace InsaneScatterbrain.MapGraph
{
    /// <summary>
    /// Randomly fills pixels with the given color.
    /// </summary>
    [ScriptNode("Random Points With Distance", "Drawing"), Serializable]
    public class RandomPointsWithDistanceNode : ProcessorNode
    {
        [InPort("Texture", typeof(TextureData), true), SerializeReference] 
        private InPort textureIn = null;
        
        [InPort("Mask", typeof(Mask)), SerializeReference] 
        private InPort maskIn = null;

        [InPort("Draw Color", typeof(Color32)), SerializeReference] 
        private InPort fillColorIn = null;

        [InPort("Point Count", typeof(int)), SerializeReference]
        private InPort pointCountIn = null;
        
        [InPort("Radius", typeof(int)), SerializeReference]
        private InPort radiusIn = null;

        
        [OutPort("Texture", typeof(TextureData)), SerializeReference] 
        private OutPort textureOut = null;
        
        [OutPort("Mask", typeof(Mask)), SerializeReference] 
        private OutPort maskOut = null;
        
        [OutPort("Placements", typeof(Vector2Int[])), SerializeReference] 
        private OutPort placementsOut = null;
        
        
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
            
            var rng = Get<Rng>();
            
            var fillColor = fillColorIn.Get<Color32>();

            var pointCount = pointCountIn.Get<int>();
            
            var mask = maskIn.Get<Mask>();

            var radius = radiusIn.Get<int>();
            
            textureData = instanceProvider.Get<TextureData>();
            textureIn.Get<TextureData>().Clone(textureData);
            
            var width = textureData.Width;
            var height = textureData.Height;
            
            Mask outputMask = null;

            // var availableTiles = instanceProvider.Get<List<int>>();

            var availableTiles = new FastDataStructure();

            if (mask != null)
            {
                outputMask = instanceProvider.Get<Mask>();
                mask.Clone(outputMask);
                availableTiles.AddRange(mask.UnmaskedPoints);
            }
            else
            {
                List<int> unmaskedPoints = null;
                
                if (maskOut.IsConnected)
                {
                    unmaskedPoints = instanceProvider.Get<List<int>>();
                }
                
                // availableTiles.EnsureCapacity(width * height);
                for (var i = 0; i < width * height; ++i)
                {
                    availableTiles.Add(i);
                    unmaskedPoints?.Add(i);
                }
                
                if (maskOut.IsConnected)
                {
                    outputMask = instanceProvider.Get<Mask>();
                    outputMask.Set(unmaskedPoints);
                }
            }

            var placementCoords = instanceProvider.Get<List<Vector2Int>>();
            placementCoords.EnsureCapacity(pointCount);
                
            for (int i = 0; i < pointCount; i++)
            {
                if (availableTiles.Count == 0) break;
                
                int index = availableTiles.GetRandomElement();
                textureData[index] = fillColor;
                availableTiles.Remove(index);
            
                HashSet<int> circleIndexes = GetIndexesInCircle(index % width, index / width, radius, width, height);
                
                availableTiles.RemoveAll(circleIndexes);
            
                outputMask?.MaskPoint(index);
                placementCoords.Add(new Vector2Int(index % width, index / width));   
            }

            var placementsArray = placementCoords.ToArray(); 
            
            textureOut.Set(() => textureData);
            maskOut.Set(() => outputMask);
            placementsOut.Set(() => placementsArray);
        }
        
        private static bool IsInCircle(int index, HashSet<int> indexes)
        {
            return indexes.Contains(index);
        }

        private HashSet<int> GetIndexesInCircle(int centerX, int centerY, int radius, int width, int height)
        {
            HashSet<int> indexes = new HashSet<int>();
            int radiusSquared = radius * radius;

            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (y < 0 || y >= height) continue;

                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || x >= width) continue;

                    int dx = centerX - x;
                    int dy = centerY - y;

                    if (dx * dx + dy * dy <= radiusSquared)
                    {
                        indexes.Add(y * width + x);
                    }
                }
            }

            return indexes;
        }
    }
}

public class FastDataStructure
{
    private List<int> dataList = new List<int>();
    private Dictionary<int, int> dataDict = new Dictionary<int, int>();
    private SysRandom sysRandom = new SysRandom();

    public void Add(int item)
    {
        if (!dataDict.ContainsKey(item))
        {
            dataList.Add(item);
            dataDict[item] = dataList.Count - 1;
        }
    }
    
    public void AddRange(IEnumerable<int> items)
    {
        foreach (int item in items)
        {
            Add(item);
        }
    }

    public bool Remove(int item)
    {
        if (dataDict.TryGetValue(item, out int index))
        {
            // Swap the element to remove with the last element
            int lastIndex = dataList.Count - 1;
            int lastItem = dataList[lastIndex];
            dataList[index] = lastItem;
            dataList.RemoveAt(lastIndex);

            // Update the dictionary
            dataDict[lastItem] = index;
            dataDict.Remove(item);

            return true;
        }
        return false;
    }
    
    public void RemoveAll(IEnumerable<int> items)
    {
        foreach (int item in items)
        {
            Remove(item);
        }
    }

    public int GetRandomElement()
    {
        if (dataList.Count == 0)
        {
            throw new InvalidOperationException("The data structure is empty.");
        }

        int randomIndex = sysRandom.Next(dataList.Count);
        return dataList[randomIndex];
    }

    public void RemoveRandomElement()
    {
        int randomElement = GetRandomElement();
        Remove(randomElement);
    }
    
    public int Count => dataList.Count;
}
