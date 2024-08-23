using System.Collections.Generic;
using UnityEngine;

public class OctreeNode
{
    public int nodeID = 0;

    Octree parentOctree;
    float minSize;
    Bounds nodeBounds;
    Bounds[] childBounds;
    Vector3 nodeSize;

    int nodeIndex = 0;
    int depth;

    OctreeNode parentNode = null;
    OctreeNode[] childNodes = null;
    private List<OctreeNode> neighborNodes;

    /*
    public OctreeNode(Bounds b, float minNodeSize)
    {
        nodeBounds= b;
        minSize= minNodeSize;
        nodeSize = b.size;



        float quarter = nodeBounds.size.y / 4.0f;
        float childLength = nodeBounds.size.y / 2;
        Vector3 childSize = new Vector3(childLength, childLength, childLength);
        childBounds = new Bounds[8];
        /*
        childBounds[0] = new Bounds(nodeBounds.center + new Vector3(-quarter, quarter, -quarter), childSize);
        childBounds[1] = new Bounds(nodeBounds.center + new Vector3(quarter, quarter, -quarter), childSize);
        childBounds[2] = new Bounds(nodeBounds.center + new Vector3(-quarter, quarter, quarter), childSize);
        childBounds[3] = new Bounds(nodeBounds.center + new Vector3(quarter, quarter, quarter), childSize);
        childBounds[4] = new Bounds(nodeBounds.center + new Vector3(-quarter, -quarter, -quarter), childSize);
        childBounds[5] = new Bounds(nodeBounds.center + new Vector3(quarter, -quarter, -quarter), childSize);
        childBounds[6] = new Bounds(nodeBounds.center + new Vector3(-quarter, -quarter, quarter), childSize);
        childBounds[7] = new Bounds(nodeBounds.center + new Vector3(quarter, -quarter, quarter), childSize);
        
        for (int i = 0; i < 8; i++)
        {
            Vector3 offset = new Vector3(
                (i & 1) == 0 ? -quarter : quarter,
                (i & 2) == 0 ? quarter : -quarter,
                (i & 4) == 0 ? -quarter : quarter
            );

            childBounds[i] = new Bounds(nodeBounds.center + offset, childSize);
        }
    }

    public void DivideAndAdd()
    {
        if(nodeBounds.size.y <= minSize) { return; }
        if (children == null)
        {
            children = new OctreeNode[8];
        }

        bool dividing = false;
        for (int i = 0; i <8; i++)
        {
            if (children[i] != null)
            {
                children[i] = new OctreeNode(childBounds[i], minSize);
            }

            Collider[] collidersInChild = Physics.OverlapBox(childBounds[i].center, childBounds[i].extents);
            if (collidersInChild.Length > 0)
            {
                Debug.Log("octree detected Collider");
                dividing = true;
                children[i].DivideAndAdd();
                break;  // You can break out of the loop as soon as you find a collider in a child bound
            }

        }
        if (dividing == false ) { children = null; }
    }
    */
    bool containsCollider;

    public OctreeNode[] ChildNodes { get { return childNodes; } }
    public List<OctreeNode> NeighborNodes { get { return neighborNodes; } }
    public Bounds NodeBounds { get { return nodeBounds; } }
    public int Depth { get { return depth; } }
    public bool ContainsCollider { get { return containsCollider; } }
    public Vector3 NodeSize { get { return nodeSize; } }

    public OctreeNode(Octree _parentOctree, Bounds b, float minNodeSize) // root node constructor
    {
        this.parentOctree = _parentOctree;
        this.nodeIndex = parentOctree.NodeIndex.Count;
        parentOctree.NodeIndex.Add(this);

        this.nodeBounds = b;
        this.minSize = minNodeSize;
        this.nodeSize = b.size;
        this.depth = 0;
        this.containsCollider = CheckForColliders();
        
        neighborNodes = new List<OctreeNode>();


        if (containsCollider)
        {
            CalculateChildBounds();
            DivideAndAdd();
        }
    }
    public OctreeNode(OctreeNode _parentNode, Bounds b, float minNodeSize, int _depth) //recursive node constructor
    {
        this.parentNode = _parentNode;
        this.parentOctree = parentNode.parentOctree;
        this.nodeIndex = parentOctree.NodeIndex.Count;
        parentOctree.NodeIndex.Add(this);

        this.nodeBounds = b;
        this.minSize = minNodeSize;
        this.nodeSize = b.size;
        this.depth = _depth;

        neighborNodes = new List<OctreeNode>();

        this.containsCollider = CheckForColliders();

        if (containsCollider)
        {
            CalculateChildBounds();
            DivideAndAdd();
        }
        else
        {
            parentNode.NeighborNodes.Add(this);

            foreach (var siblingNode in parentNode.childNodes)
            {
                if (siblingNode != null && !siblingNode.ContainsCollider)
                {
                    this.neighborNodes.Add(siblingNode);
                }
            }
        }

    }

    bool CheckForColliders()
    {
        Collider[] collidersInNode = Physics.OverlapBox(nodeBounds.center, nodeBounds.extents);
        return collidersInNode.Length > 0;
    }

    void CalculateChildBounds()
    {
        float quarter = nodeSize.y / 4.0f;
        float childLength = nodeSize.y / 2;
        Vector3 childSize = new Vector3(childLength, childLength, childLength);
        childBounds = new Bounds[8];

        for (int i = 0; i < 8; i++)
        {
            //bitwise opperator assigns the offset of each childNode. 
            Vector3 offset = new Vector3(
                (i & 1) == 0 ? -quarter : quarter,
                (i & 2) == 0 ? quarter : -quarter,
                (i & 4) == 0 ? -quarter : quarter
            );

            childBounds[i] = new Bounds(nodeBounds.center + offset, childSize);
        }
    }

    public void DivideAndAdd()
    {
        if (nodeBounds.size.y <= minSize || depth >= 8) 
        {
            return;
        }
        if (childNodes == null)
        {
            childNodes = new OctreeNode[8];
        }

        for (int i = 0; i < 8; i++)
        {
            childNodes[i] = new OctreeNode(this, childBounds[i], minSize, this.depth + 1 );
            //childNodes[i].parentNode = this;
        }
        
    }
    public void Draw(Color color, float sizeFactor)
    {
        if (!containsCollider)
        {
            //Gizmos.color = Color.black;
            //Gizmos.DrawWireCube(nodeBounds.center, nodeBounds.size);

            Gizmos.color = color;
            Gizmos.DrawWireCube(nodeBounds.center, nodeBounds.size * sizeFactor);
        }

        if (childNodes == null)
        {
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            if (childNodes[i] == null)
            {
                return;
            }

            childNodes[i].Draw(color, sizeFactor);
        }

    }

}
