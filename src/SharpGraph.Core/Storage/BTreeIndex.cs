namespace SharpGraph.Core.Storage;

/// <summary>
/// B-tree index for range queries and ordered scans
/// Supports efficient operations like: find all records where value > X, or get records in sorted order
/// </summary>
/// <typeparam name="TKey">Type of the indexed value (must be comparable)</typeparam>
public class BTreeIndex<TKey> where TKey : IComparable<TKey>
{
    private BTreeNode<TKey>? _root;
    private readonly int _order; // Maximum number of children per node
    private readonly object _lock = new();
    
    public BTreeIndex(int order = 32)
    {
        _order = order;
        _root = null;
    }
    
    /// <summary>
    /// Insert a key-value pair into the index
    /// </summary>
    public void Insert(TKey key, string recordId)
    {
        lock (_lock)
        {
            if (_root == null)
            {
                _root = new BTreeNode<TKey>(_order, isLeaf: true);
                _root.Keys.Add(key);
                _root.RecordIds.Add(new List<string> { recordId });
            }
            else
            {
                // If root is full, split it
                if (_root.Keys.Count >= _order - 1)
                {
                    var newRoot = new BTreeNode<TKey>(_order, isLeaf: false);
                    newRoot.Children.Add(_root);
                    SplitChild(newRoot, 0);
                    _root = newRoot;
                }
                
                InsertNonFull(_root, key, recordId);
            }
        }
    }
    
    /// <summary>
    /// Find all record IDs with the exact key value
    /// </summary>
    public List<string> Find(TKey key)
    {
        lock (_lock)
        {
            if (_root == null)
                return new List<string>();
            
            return FindInNode(_root, key);
        }
    }
    
    /// <summary>
    /// Find all record IDs where key >= minKey and key <= maxKey
    /// </summary>
    public List<string> FindRange(TKey minKey, TKey maxKey)
    {
        lock (_lock)
        {
            var results = new List<string>();
            if (_root == null)
                return results;
            
            FindRangeInNode(_root, minKey, maxKey, results);
            return results;
        }
    }
    
    /// <summary>
    /// Find all record IDs where key >= minKey
    /// </summary>
    public List<string> FindGreaterThan(TKey minKey)
    {
        lock (_lock)
        {
            var results = new List<string>();
            if (_root == null)
                return results;
            
            FindGreaterThanInNode(_root, minKey, results);
            return results;
        }
    }
    
    /// <summary>
    /// Find all record IDs where key <= maxKey
    /// </summary>
    public List<string> FindLessThan(TKey maxKey)
    {
        lock (_lock)
        {
            var results = new List<string>();
            if (_root == null)
                return results;
            
            FindLessThanInNode(_root, maxKey, results);
            return results;
        }
    }
    
    /// <summary>
    /// Get all record IDs in sorted order
    /// </summary>
    public List<string> GetAllSorted()
    {
        lock (_lock)
        {
            var results = new List<string>();
            if (_root == null)
                return results;
            
            TraverseInOrder(_root, results);
            return results;
        }
    }
    
    /// <summary>
    /// Remove all entries for a key
    /// Implements proper B-tree deletion with borrowing from siblings and merging nodes
    /// </summary>
    public void Remove(TKey key)
    {
        lock (_lock)
        {
            if (_root == null)
                return;
            
            RemoveFromNode(_root, key);
            
            // If root is empty after deletion and has children, make its only child the new root
            if (_root.Keys.Count == 0)
            {
                if (!_root.IsLeaf && _root.Children.Count > 0)
                {
                    _root = _root.Children[0];
                }
                else
                {
                    _root = null; // Tree is now empty
                }
            }
        }
    }
    
    private void RemoveFromNode(BTreeNode<TKey> node, TKey key)
    {
        int idx = FindKeyIndex(node, key);
        
        if (idx < node.Keys.Count && node.Keys[idx].CompareTo(key) == 0)
        {
            // Key found in this node
            if (node.IsLeaf)
            {
                RemoveFromLeaf(node, idx);
            }
            else
            {
                RemoveFromNonLeaf(node, idx);
            }
        }
        else
        {
            // Key not in this node
            if (node.IsLeaf)
            {
                // Key doesn't exist in tree
                return;
            }
            
            // Key is in subtree rooted at node.Children[idx]
            bool isInLastChild = (idx == node.Keys.Count);
            
            // Ensure child has minimum degree keys before recursing
            if (node.Children[idx].Keys.Count < MinKeys)
            {
                Fill(node, idx);
            }
            
            // After filling, the key might have moved to previous child
            if (isInLastChild && idx > node.Keys.Count)
            {
                RemoveFromNode(node.Children[idx - 1], key);
            }
            else
            {
                RemoveFromNode(node.Children[idx], key);
            }
        }
    }
    
    private int MinKeys => (_order - 1) / 2;
    
    private int FindKeyIndex(BTreeNode<TKey> node, TKey key)
    {
        int idx = 0;
        while (idx < node.Keys.Count && node.Keys[idx].CompareTo(key) < 0)
        {
            idx++;
        }
        return idx;
    }
    
    private void RemoveFromLeaf(BTreeNode<TKey> node, int idx)
    {
        // Simply remove the key from the leaf
        node.Keys.RemoveAt(idx);
        node.RecordIds.RemoveAt(idx);
    }
    
    private void RemoveFromNonLeaf(BTreeNode<TKey> node, int idx)
    {
        TKey key = node.Keys[idx];
        
        if (node.Children[idx].Keys.Count >= MinKeys + 1)
        {
            // Left child has enough keys - get predecessor
            var pred = GetPredecessor(node, idx);
            node.Keys[idx] = pred.Key;
            node.RecordIds[idx] = pred.RecordIds;
            RemoveFromNode(node.Children[idx], pred.Key);
        }
        else if (node.Children[idx + 1].Keys.Count >= MinKeys + 1)
        {
            // Right child has enough keys - get successor
            var succ = GetSuccessor(node, idx);
            node.Keys[idx] = succ.Key;
            node.RecordIds[idx] = succ.RecordIds;
            RemoveFromNode(node.Children[idx + 1], succ.Key);
        }
        else
        {
            // Both children have minimum keys - merge with right sibling
            Merge(node, idx);
            RemoveFromNode(node.Children[idx], key);
        }
    }
    
    private (TKey Key, List<string> RecordIds) GetPredecessor(BTreeNode<TKey> node, int idx)
    {
        // Keep moving to the rightmost node in the left subtree until we reach a leaf
        var current = node.Children[idx];
        while (!current.IsLeaf)
        {
            current = current.Children[current.Keys.Count];
        }
        
        // Return the last key in the leaf
        int lastIdx = current.Keys.Count - 1;
        return (current.Keys[lastIdx], current.RecordIds[lastIdx]);
    }
    
    private (TKey Key, List<string> RecordIds) GetSuccessor(BTreeNode<TKey> node, int idx)
    {
        // Keep moving to the leftmost node in the right subtree until we reach a leaf
        var current = node.Children[idx + 1];
        while (!current.IsLeaf)
        {
            current = current.Children[0];
        }
        
        // Return the first key in the leaf
        return (current.Keys[0], current.RecordIds[0]);
    }
    
    private void Fill(BTreeNode<TKey> node, int idx)
    {
        // If previous sibling has more than minimum keys, borrow from it
        if (idx != 0 && node.Children[idx - 1].Keys.Count >= MinKeys + 1)
        {
            BorrowFromPrev(node, idx);
        }
        // If next sibling has more than minimum keys, borrow from it
        else if (idx != node.Keys.Count && node.Children[idx + 1].Keys.Count >= MinKeys + 1)
        {
            BorrowFromNext(node, idx);
        }
        // Merge with sibling
        else
        {
            if (idx != node.Keys.Count)
            {
                Merge(node, idx);
            }
            else
            {
                Merge(node, idx - 1);
            }
        }
    }
    
    private void BorrowFromPrev(BTreeNode<TKey> node, int childIdx)
    {
        var child = node.Children[childIdx];
        var sibling = node.Children[childIdx - 1];
        
        // Move a key from parent to child
        child.Keys.Insert(0, node.Keys[childIdx - 1]);
        child.RecordIds.Insert(0, node.RecordIds[childIdx - 1]);
        
        // Move a key from sibling to parent
        node.Keys[childIdx - 1] = sibling.Keys[sibling.Keys.Count - 1];
        node.RecordIds[childIdx - 1] = sibling.RecordIds[sibling.RecordIds.Count - 1];
        
        // Move child pointer if not a leaf
        if (!child.IsLeaf)
        {
            child.Children.Insert(0, sibling.Children[sibling.Children.Count - 1]);
            sibling.Children.RemoveAt(sibling.Children.Count - 1);
        }
        
        // Remove the last key from sibling
        sibling.Keys.RemoveAt(sibling.Keys.Count - 1);
        sibling.RecordIds.RemoveAt(sibling.RecordIds.Count - 1);
    }
    
    private void BorrowFromNext(BTreeNode<TKey> node, int childIdx)
    {
        var child = node.Children[childIdx];
        var sibling = node.Children[childIdx + 1];
        
        // Move a key from parent to child
        child.Keys.Add(node.Keys[childIdx]);
        child.RecordIds.Add(node.RecordIds[childIdx]);
        
        // Move a key from sibling to parent
        node.Keys[childIdx] = sibling.Keys[0];
        node.RecordIds[childIdx] = sibling.RecordIds[0];
        
        // Move child pointer if not a leaf
        if (!child.IsLeaf)
        {
            child.Children.Add(sibling.Children[0]);
            sibling.Children.RemoveAt(0);
        }
        
        // Remove the first key from sibling
        sibling.Keys.RemoveAt(0);
        sibling.RecordIds.RemoveAt(0);
    }
    
    private void Merge(BTreeNode<TKey> node, int idx)
    {
        var child = node.Children[idx];
        var sibling = node.Children[idx + 1];
        
        // Pull the key from this node and merge with right sibling
        child.Keys.Add(node.Keys[idx]);
        child.RecordIds.Add(node.RecordIds[idx]);
        
        // Copy keys from sibling to child
        foreach (var key in sibling.Keys)
        {
            child.Keys.Add(key);
        }
        foreach (var recordIds in sibling.RecordIds)
        {
            child.RecordIds.Add(recordIds);
        }
        
        // Copy child pointers from sibling to child
        if (!child.IsLeaf)
        {
            foreach (var siblingChild in sibling.Children)
            {
                child.Children.Add(siblingChild);
            }
        }
        
        // Remove the key from this node
        node.Keys.RemoveAt(idx);
        node.RecordIds.RemoveAt(idx);
        
        // Remove the sibling
        node.Children.RemoveAt(idx + 1);
    }
    
    /// <summary>
    /// Clear the entire index
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _root = null;
        }
    }
    
    private void InsertNonFull(BTreeNode<TKey> node, TKey key, string recordId)
    {
        int i = node.Keys.Count - 1;
        
        if (node.IsLeaf)
        {
            // Find position to insert
            while (i >= 0 && key.CompareTo(node.Keys[i]) < 0)
            {
                i--;
            }
            
            // Check if key already exists at position i
            if (i >= 0 && key.CompareTo(node.Keys[i]) == 0)
            {
                // Key exists at position i, add to record list
                node.RecordIds[i].Add(recordId);
            }
            else
            {
                // New key - insert at position i+1
                i++;
                node.Keys.Insert(i, key);
                node.RecordIds.Insert(i, new List<string> { recordId });
            }
        }
        else
        {
            // Find child to insert into
            while (i >= 0 && key.CompareTo(node.Keys[i]) < 0)
            {
                i--;
            }
            i++;
            
            // Split child if full
            if (node.Children[i].Keys.Count >= _order - 1)
            {
                SplitChild(node, i);
                
                if (key.CompareTo(node.Keys[i]) > 0)
                {
                    i++;
                }
            }
            
            InsertNonFull(node.Children[i], key, recordId);
        }
    }
    
    private void SplitChild(BTreeNode<TKey> parent, int childIndex)
    {
        var fullChild = parent.Children[childIndex];
        var newChild = new BTreeNode<TKey>(_order, fullChild.IsLeaf);
        
        int mid = _order / 2;
        
        // Move half of the keys to new child
        for (int i = mid + 1; i < fullChild.Keys.Count; i++)
        {
            newChild.Keys.Add(fullChild.Keys[i]);
            newChild.RecordIds.Add(fullChild.RecordIds[i]);
        }
        
        // Move children if not leaf
        if (!fullChild.IsLeaf)
        {
            for (int i = mid + 1; i < fullChild.Children.Count; i++)
            {
                newChild.Children.Add(fullChild.Children[i]);
            }
            fullChild.Children.RemoveRange(mid + 1, fullChild.Children.Count - mid - 1);
        }
        
        // Move middle key up to parent
        parent.Keys.Insert(childIndex, fullChild.Keys[mid]);
        parent.RecordIds.Insert(childIndex, fullChild.RecordIds[mid]);
        parent.Children.Insert(childIndex + 1, newChild);
        
        // Remove moved keys from full child
        fullChild.Keys.RemoveRange(mid, fullChild.Keys.Count - mid);
        fullChild.RecordIds.RemoveRange(mid, fullChild.RecordIds.Count - mid);
    }
    
    private List<string> FindInNode(BTreeNode<TKey> node, TKey key)
    {
        int i = 0;
        while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) > 0)
        {
            i++;
        }
        
        if (i < node.Keys.Count && key.CompareTo(node.Keys[i]) == 0)
        {
            return new List<string>(node.RecordIds[i]);
        }
        
        if (node.IsLeaf)
        {
            return new List<string>();
        }
        
        return FindInNode(node.Children[i], key);
    }
    
    private void FindRangeInNode(BTreeNode<TKey> node, TKey minKey, TKey maxKey, List<string> results)
    {
        int i = 0;
        
        while (i < node.Keys.Count)
        {
            // Recurse left child if not leaf and if child might contain values in range
            if (!node.IsLeaf)
            {
                FindRangeInNode(node.Children[i], minKey, maxKey, results);
            }
            
            // Add current key if in range
            if (node.Keys[i].CompareTo(minKey) >= 0 && node.Keys[i].CompareTo(maxKey) <= 0)
            {
                results.AddRange(node.RecordIds[i]);
            }
            
            // Stop if we've passed maxKey
            if (node.Keys[i].CompareTo(maxKey) > 0)
            {
                return; // Don't continue to right children
            }
            
            i++;
        }
        
        // Recurse rightmost child if not leaf
        if (!node.IsLeaf && i < node.Children.Count)
        {
            FindRangeInNode(node.Children[i], minKey, maxKey, results);
        }
    }
    
    private void FindGreaterThanInNode(BTreeNode<TKey> node, TKey minKey, List<string> results)
    {
        for (int i = 0; i < node.Keys.Count; i++)
        {
            if (!node.IsLeaf)
            {
                FindGreaterThanInNode(node.Children[i], minKey, results);
            }
            
            if (node.Keys[i].CompareTo(minKey) >= 0)
            {
                results.AddRange(node.RecordIds[i]);
            }
        }
        
        if (!node.IsLeaf)
        {
            FindGreaterThanInNode(node.Children[node.Keys.Count], minKey, results);
        }
    }
    
    private void FindLessThanInNode(BTreeNode<TKey> node, TKey maxKey, List<string> results)
    {
        for (int i = 0; i < node.Keys.Count; i++)
        {
            if (!node.IsLeaf)
            {
                FindLessThanInNode(node.Children[i], maxKey, results);
            }
            
            if (node.Keys[i].CompareTo(maxKey) <= 0)
            {
                results.AddRange(node.RecordIds[i]);
            }
            else
            {
                break; // Stop if we've passed maxKey
            }
        }
        
        if (!node.IsLeaf && node.Keys.Count > 0 && node.Keys[node.Keys.Count - 1].CompareTo(maxKey) <= 0)
        {
            FindLessThanInNode(node.Children[node.Keys.Count], maxKey, results);
        }
    }
    
    private void TraverseInOrder(BTreeNode<TKey> node, List<string> results)
    {
        for (int i = 0; i < node.Keys.Count; i++)
        {
            if (!node.IsLeaf)
            {
                TraverseInOrder(node.Children[i], results);
            }
            
            results.AddRange(node.RecordIds[i]);
        }
        
        if (!node.IsLeaf)
        {
            TraverseInOrder(node.Children[node.Keys.Count], results);
        }
    }
    
    /// <summary>
    /// Get statistics about the index
    /// </summary>
    public (int Height, int KeyCount, int NodeCount) GetStats()
    {
        lock (_lock)
        {
            if (_root == null)
                return (0, 0, 0);
            
            int height = GetHeight(_root);
            int keyCount = GetKeyCount(_root);
            int nodeCount = GetNodeCount(_root);
            
            return (height, keyCount, nodeCount);
        }
    }
    
    private int GetHeight(BTreeNode<TKey> node)
    {
        if (node.IsLeaf)
            return 1;
        
        return 1 + GetHeight(node.Children[0]);
    }
    
    private int GetKeyCount(BTreeNode<TKey> node)
    {
        int count = node.Keys.Count;
        
        if (!node.IsLeaf)
        {
            foreach (var child in node.Children)
            {
                count += GetKeyCount(child);
            }
        }
        
        return count;
    }
    
    private int GetNodeCount(BTreeNode<TKey> node)
    {
        int count = 1;
        
        if (!node.IsLeaf)
        {
            foreach (var child in node.Children)
            {
                count += GetNodeCount(child);
            }
        }
        
        return count;
    }
    
    #region Persistence
    
    /// <summary>
    /// Save the entire B-tree to disk via IndexFile
    /// </summary>
    public void SaveToFile(IndexFile indexFile)
    {
        lock (_lock)
        {
            // Load existing metadata to preserve ColumnName, IndexType, etc.
            var metadata = indexFile.LoadMetadata() ?? new IndexMetadata();
            
            if (_root == null)
            {
                // Save empty index metadata
                metadata.Order = _order;
                metadata.RootPageId = -1;
                metadata.NodeCount = 0;
                metadata.UpdatedAt = DateTime.UtcNow;
                indexFile.SaveMetadata(metadata);
                indexFile.Flush();
                return;
            }
            
            // Assign page IDs to all nodes and save them
            long nextPageId = 1; // Page 0 is metadata
            var rootPageId = SaveNodeRecursive(_root, indexFile, ref nextPageId);
            
            // Update metadata with root page ID
            metadata.Order = _order;
            metadata.RootPageId = rootPageId;
            metadata.NodeCount = nextPageId - 1;
            metadata.UpdatedAt = DateTime.UtcNow;
            indexFile.SaveMetadata(metadata);
            indexFile.Flush();
        }
    }
    
    /// <summary>
    /// Load the entire B-tree from disk via IndexFile
    /// </summary>
    public static BTreeIndex<TKey>? LoadFromFile(IndexFile indexFile)
    {
        var metadata = indexFile.LoadMetadata();
        if (metadata == null)
            return null;
        
        var index = new BTreeIndex<TKey>(metadata.Order);
        
        if (metadata.RootPageId < 0)
        {
            // Empty index
            return index;
        }
        
        // Load root node recursively
        index._root = LoadNodeRecursive(indexFile, metadata.RootPageId, metadata.Order);
        
        return index;
    }
    
    private long SaveNodeRecursive(BTreeNode<TKey> node, IndexFile indexFile, ref long nextPageId)
    {
        var currentPageId = nextPageId++;
        
        // Create node data
        var nodeData = new BTreeNodeData<TKey>
        {
            PageId = currentPageId,
            IsLeaf = node.IsLeaf,
            Keys = new List<TKey>(node.Keys),
            RecordIds = node.RecordIds.Select(list => new List<string>(list)).ToList(),
            ChildPageIds = new List<long>()
        };
        
        // Save children first and collect their page IDs
        if (!node.IsLeaf)
        {
            foreach (var child in node.Children)
            {
                var childPageId = SaveNodeRecursive(child, indexFile, ref nextPageId);
                nodeData.ChildPageIds.Add(childPageId);
            }
        }
        
        // Save this node
        indexFile.SaveNode(currentPageId, nodeData);
        
        return currentPageId;
    }
    
    private static BTreeNode<TKey>? LoadNodeRecursive(IndexFile indexFile, long pageId, int order)
    {
        var nodeData = indexFile.LoadNode<TKey>(pageId);
        if (nodeData == null)
            return null;
        
        var node = new BTreeNode<TKey>(order, nodeData.IsLeaf);
        
        // Restore keys and record IDs
        node.Keys.AddRange(nodeData.Keys);
        node.RecordIds.AddRange(nodeData.RecordIds);
        
        // Load children recursively
        if (!nodeData.IsLeaf)
        {
            foreach (var childPageId in nodeData.ChildPageIds)
            {
                var child = LoadNodeRecursive(indexFile, childPageId, order);
                if (child != null)
                {
                    node.Children.Add(child);
                }
            }
        }
        
        return node;
    }
    
    #endregion
}

/// <summary>
/// B-tree node
/// </summary>
internal class BTreeNode<TKey> where TKey : IComparable<TKey>
{
    public List<TKey> Keys { get; }
    public List<List<string>> RecordIds { get; } // List of record IDs for each key (supports duplicates)
    public List<BTreeNode<TKey>> Children { get; }
    public bool IsLeaf { get; }
    private readonly int _order;
    
    public BTreeNode(int order, bool isLeaf)
    {
        _order = order;
        IsLeaf = isLeaf;
        Keys = new List<TKey>();
        RecordIds = new List<List<string>>();
        Children = new List<BTreeNode<TKey>>();
    }
}
