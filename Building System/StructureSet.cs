using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StructureSet : MonoBehaviour
{
    [SerializeField] private LayerMask structureLayer;
    private Transform orientation;
    private AbstractGraph<StructureData> structureGraph = new AbstractGraph<StructureData>();

    private Queue<StructureData> structuresToRemove = new Queue<StructureData>();

    // Add a StructureData node to the graph.
    private void Awake()
    {
        structureGraph = new AbstractGraph<StructureData>();
    }
    public void ConfigureStructureSet(Transform rootStructure, LayerMask structureLayer)
    {
        if (rootStructure == null)
        {
            Debug.Log("root is null");
        }
        orientation = rootStructure;
        this.transform.position = rootStructure.position;
        this.transform.rotation = rootStructure.rotation;
        structureGraph = new AbstractGraph<StructureData>();

        this.structureLayer = structureLayer;
    }
    public void AddStructure(StructureData structure)
    {
        if (structure == null)
        {
            Debug.Log("placed structure doesnt exist");
        }

        structureGraph.AddNode(structure);
        structure.transform.parent = this.transform;

        TryGetExtendedNeighbors(structure);
    }
    // Remove a StructureData node from the graph.
    public void RemoveStructure(StructureData structure)
    {
        if (!structureGraph.AdjanceyList.ContainsKey(structure))
        {
            return;
        }

        // Collect neighbors in a separate list
        List<StructureData> neighbors = new List<StructureData>(GetConnectedStructures(structure));

        string neighborsString = "";
        foreach (StructureData neighbor in neighbors)
        {
            neighborsString += ": " + neighbor.gameObject.name + " ";
            if (neighbor == structure) continue;

            Debug.Log($"{structure.gameObject.name} has been destroyed, it has {neighbor.gameObject.name} as a neighbor, removing it");
            neighbor.RemoveNeighbor(structure);

            // Ensure removal from neighbor only happens after logging
            if (structureGraph.AdjanceyList.ContainsKey(neighbor))
            {
                structureGraph.AdjanceyList[neighbor].Remove(structure);
            }
        }

        Debug.Log($"{structure.gameObject.name} is destroyed, it had [ {neighbors.Count} ] neighbors to process here are their names {neighborsString}");
        structureGraph.RemoveNode(structure);

        // Ensure empty structure set objects are removed from the scene
        if (structureGraph.AdjanceyList.Count <= 0)
        {
            Destroy(this.gameObject);
        }
    }
    // Add a connection (edge) between two StructureData instances in the graph.
    public void ConnectStructures(StructureData fromStructure, StructureData toStructure)
    {
        Debug.Log($"placing a new structure {toStructure.gameObject.name} attaching it to {fromStructure.gameObject.name}");
        if (!structureGraph.AdjanceyList.ContainsKey(toStructure))
        {
            // if graph does not contain the new toStructure add it as a node before connecting it to fromStructure
            AddStructure(toStructure);
        }
        structureGraph.AddEdge(fromStructure, toStructure);

        fromStructure.AddChild(toStructure);
        toStructure.AddParent(fromStructure);
    }

    // Get the connected structures (neighbors) of a given structure.
    public List<StructureData> GetConnectedStructures(StructureData structure)
    {
        return structureGraph.GetNeighbors(structure);
    }

    private void TryGetExtendedNeighbors(StructureData structure)
    {
        Bounds structureBounds = Utility.GetBounds(structure.gameObject);

        List<float> extents = new List<float>
            {
                structureBounds.extents.x,
                structureBounds.extents.y,
                structureBounds.extents.z
            };

        float highestExtent = extents.Max();
        // Get colliders within the detection radius
        Collider[] colliders = Physics.OverlapSphere(structure.transform.position, highestExtent * 2.0f, structureLayer);
        Utility.DebugDrawSphere(structure.transform.position, highestExtent * 2.0f, Color.yellow);

        float positionTolerance = 1f; // Example tolerance for position comparison

        // Iterate through the colliders
        foreach (Collider col in colliders)
        {
            StructureData nearbyStructure = Utility.GetComponentFromAnyParent<StructureData>(col.gameObject);
            if (nearbyStructure == structure) continue; // prevents object from considering itself as support.
            if (nearbyStructure != null)
            {
                bool shouldConnect = false;
                foreach (Transform snapPoint1 in structure.SnapPoints)
                {
                    foreach (Transform snapPoint2 in nearbyStructure.SnapPoints)
                    {
                        // Calculate the differences in position
                        Vector3 positionDifference = snapPoint1.position - snapPoint2.position;

                        // Check if the differences are within the tolerance
                        if (positionDifference.sqrMagnitude <= positionTolerance)
                        {
                            // Match found
                            shouldConnect = true;
                        }
                    }
                    if (shouldConnect)
                    {
                        break; // Exit outer loop since match is found
                    }
                }

                if (shouldConnect)
                {
                    ConnectStructures(nearbyStructure, structure);
                }
               
            }
        }
    }

    private void ForceStabilityUpdates()
    {
        foreach (var item in structureGraph.AdjanceyList)
        {
            item.Key.UpdateStability();
        }
    }
    public void CalculateStability(StructureData structure)
    {

    }

    // Clear the structure set (clear the graph).
    public void Clear()
    {
        structureGraph.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var structure in structureGraph.AdjanceyList)
        {
            List<StructureData> neighbors = structure.Value;
            foreach (var neighbor in neighbors)
            {
                Gizmos.DrawLine(structure.Key.gameObject.transform.position, neighbor.gameObject.transform.position);
            }
        }
    }
}

public struct StructureConnectionData
{
    StructureData structure;
    List<StructureData> parents;
    List<StructureData> children;

    public StructureConnectionData(StructureData structure, List<StructureData> parents, List<StructureData> children)
    {
        this.structure = structure;
        this.parents = parents;
        this.children = children;
    }
}
