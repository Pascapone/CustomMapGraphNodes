using System;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct HeapElement: IEquatable<HeapElement>
{
    public bool Equals(HeapElement other)
    {
        return index == other.index;
    }

    public override bool Equals(object obj)
    {
        return obj is HeapElement other && Equals(other);
    }

    public override int GetHashCode()
    {
        return index;
    }

    public int heapIndex;
    public int fCost  => hCost + gCost; 
    public readonly bool isWalkable;
    public readonly int index;
    public readonly int movementPenalty;
    public int gCost;
    public int hCost;
    public int parentIndex;

    public HeapElement(int index, bool isWalkable, int movementPenalty = 0)
    {
        this.index = index;
        this.isWalkable = isWalkable;
        this.movementPenalty = movementPenalty;
        gCost = int.MaxValue;
        hCost = int.MaxValue;
        parentIndex = 0;
        heapIndex = 0;
    }
    
    public static bool operator ==(HeapElement a, HeapElement b)
    {
        return a.index == b.index;
    }

    public static bool operator !=(HeapElement a, HeapElement b)
    {
        return !(a == b);
    }
}

[BurstCompile]
struct Heap
{
    public int Size { get; private set; }
    private NativeArray<HeapElement> heap;
    private NativeHashMap<HeapElement, int> heapTracker;
    private readonly int capacity;

    public Heap(int capacity)
    {
        this.capacity = capacity;
        Size = 0;
        heap = new NativeArray<HeapElement>(capacity, Allocator.Temp);
        heapTracker = new NativeHashMap<HeapElement, int>(capacity, Allocator.Temp);
    }

    private static int GetParentIndex(int index) => (index - 1) / 2;
    private static int GetLeftChildIndex(int index) => 2 * index + 1;
    private static int GetRightChildIndex(int index) => 2 * index + 2;

    private static bool HasParent(int index) => GetParentIndex(index) >= 0;
    private bool HasLeftChild(int index) => GetLeftChildIndex(index) < Size;
    private bool HasRightChild(int index) => GetRightChildIndex(index) < Size;

    private HeapElement Parent(int index) => heap[GetParentIndex(index)];
    private HeapElement LeftChild(int index) => heap[GetLeftChildIndex(index)];
    private HeapElement RightChild(int index) => heap[GetRightChildIndex(index)];

    private void Swap(int indexOne, int indexTwo)
    {
        HeapElement temp = heap[indexOne];
        HeapElement temp2 = heap[indexTwo];
        temp.heapIndex = indexTwo;
        temp2.heapIndex = indexOne;
        heapTracker[temp] = temp.heapIndex;
        heapTracker[temp2] = temp2.heapIndex;
        heap[temp.heapIndex] = temp;
        heap[temp2.heapIndex] = temp2;
    }

    public HeapElement Peek()
    {
        if (Size == 0) throw new InvalidOperationException("Heap is empty");
        return heap[0];
    }

    public HeapElement Poll()
    {
        if (Size == 0) throw new InvalidOperationException("Heap is empty");

        HeapElement item = heap[0];
        heapTracker.Remove(item);
        HeapElement temp = heap[Size - 1];
        temp.heapIndex = 0;
        heap[0] = temp;
        Size--;
        HeapifyDown();
        return item;
    }

    public void Add(HeapElement item)
    {
        if (Size == capacity) throw new InvalidOperationException("Heap is full");
        heapTracker[item] = Size;
        item.heapIndex = Size;
        heap[Size] = item;
        Size++;
        HeapifyUp();
    }

    public void ClearHeapTracker()
    {
        heap.Dispose();
        heapTracker.Dispose();
    }

    private void HeapifyUp()
    {
        int index = Size - 1;
        while (HasParent(index) && Parent(index).fCost > heap[index].fCost)
        {
            Swap(GetParentIndex(index), index);
            index = GetParentIndex(index);
        }
    }

    private void HeapifyDown()
    {
        int index = 0;
        while (HasLeftChild(index))
        {
            int smallerChildIndex = GetLeftChildIndex(index);
            if (HasRightChild(index) && RightChild(index).fCost < LeftChild(index).fCost)
            {
                smallerChildIndex = GetRightChildIndex(index);
            }

            if (heap[index].fCost < heap[smallerChildIndex].fCost)
            {
                break;
            }

            Swap(index, smallerChildIndex);
            index = smallerChildIndex;
        }
    }
    
    public void UpdateItem(HeapElement item)
    {
        int heapIndex = heapTracker[item];
        heap[heapIndex] = item;
        HeapifyUpFromIndex(heapIndex);
    }

    private void HeapifyUpFromIndex(int index)
    {
        while (HasParent(index) && Parent(index).fCost > heap[index].fCost)
        {
            Swap(GetParentIndex(index), index);
            index = GetParentIndex(index);
        }
    }

    public bool Contains(HeapElement heapElement)
    {
        return heapTracker.TryGetValue(heapElement, out int index);
    }
}

