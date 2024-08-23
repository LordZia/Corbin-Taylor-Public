using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StructureData : MonoBehaviour , IDamageable
{
    public StructureSet structureSet;
    public Structure structure;
    private int maxHealth = 1000;

    [SerializeField] private int currentHealth = 1000;
    [SerializeField] private List<Transform> snapPoints = new List<Transform>();
    [SerializeField] private List<BoxCollider> illegalPlacementColliders = new List<BoxCollider>();

    [SerializeField] private StructureType structureType;
    [SerializeField] private StructureGridShape structureGridShape;

    [SerializeField] private List<StructureData> parentStructures = new List<StructureData>();
    [SerializeField] private List<StructureData> childStructures = new List<StructureData>();

    [SerializeField] private int currentStability = 0;
    public string structureName;
    public int Stability => currentStability;
    public List<Transform> SnapPoints => snapPoints;
    public List<BoxCollider> IllegalPlacementCollidersints => illegalPlacementColliders;
    public Structure Structure => structure;
    public StructureType StructureType => structureType;
    public StructureGridShape StructureGridShape => structureGridShape;
    public List<StructureData> SupportingStructures => parentStructures;
    public List<StructureData> SupportedStructures => childStructures;

    bool hasHadInitialStabilityLoad = false;
    bool isBreaking = false;

    [SerializeField] private bool DEBUG_destroyobject = false;
    private void Awake()
    {
        parentStructures = new List<StructureData>();
        childStructures = new List<StructureData>();
    }

    private void FixedUpdate()
    {
        if (DEBUG_destroyobject)
        {
            DEBUG_destroyobject = false;
            CallStructureDestroy();
        }
    }

    public StructureData(StructureSet structureSet, Structure structure, string name, int maxHealth, int currentStability)
    {
        this.structure = structure;
        this.structureName = name;
        this.maxHealth = maxHealth;
        this.currentHealth = maxHealth;
        this.structureSet = structureSet;
        this.currentStability = currentStability;
    }
    public void CallStructureDestroy()
    {
        structureSet.RemoveStructure(this);
        Destroy(this.gameObject); 
    }

    public void ApplyDamage(float damage)
    {
        this.currentHealth -= (int)damage;
        if (this.currentHealth < 0 )
        {
            this.currentHealth = 0;
            CallStructureDestroy();
        }
    }

    public void SetStability(int newStability)
    {
        currentStability = newStability;
    }

    public int GetStability()
    {
        return currentStability;
    }

    public void AddNeighbor(StructureData neighbor)
    {
        if (neighbor == this) return;
        Debug.Log($"adding {neighbor.gameObject.name} as a neighbor to {this.gameObject.name} : {neighbor.gameObject.name} has a stability of {neighbor.Stability} and {this.gameObject.name} has a stability of {this.currentStability}");
        if (neighbor.Stability > this.currentStability)
        {
            if (!parentStructures.Contains(neighbor))
            {
                Debug.Log($"adding {neighbor.gameObject.name} as a supporting structure for {this.gameObject.name}");
                parentStructures.Add(neighbor);

                UpdateStability();
            }
        }
        else
        {
            if (!childStructures.Contains(neighbor))
            {
                Debug.Log($"adding {neighbor.gameObject.name} as a supported structure for {this.gameObject.name}");
                childStructures.Add(neighbor);

                UpdateStability();
            }

        }

    }

    public void AddChild(StructureData neighbor)
    {
        if (neighbor == this) return;
        if (!childStructures.Contains(neighbor))
        {
            Debug.Log($"adding {neighbor.gameObject.name} as a supported structure for {this.gameObject.name}");
            childStructures.Add(neighbor);

            UpdateStability();
        }

    }

    public void AddParent(StructureData neighbor)
    {
        if (neighbor == this) return;
        if (!parentStructures.Contains(neighbor))
        {
            Debug.Log($"adding {neighbor.gameObject.name} as a supporting structure for {this.gameObject.name}");
            parentStructures.Add(neighbor);

            UpdateStability();
        }
    }

    public void RemoveNeighbor(StructureData removedStructure)
    {
        Debug.Log($"removing {removedStructure.gameObject.name} : for {this.gameObject.name}");
        if (removedStructure == this) return;
        if (parentStructures.Contains(removedStructure))
        {
            parentStructures.Remove(removedStructure);
        }

        if (childStructures.Contains(removedStructure))
        {
            childStructures.Remove(removedStructure);
        }

        UpdateStability();
    }

    public void UpdateStability()
    {
        if (isBreaking) 
            return;

        ValidateReferences();

        string parentStructureList = "";
        List<int> supportStrengthList = new List<int>();
        foreach (StructureData structure in parentStructures)
        {
            supportStrengthList.Add(structure.Stability);
            parentStructureList += structure.gameObject.name + ", ";
        }

        if (!hasHadInitialStabilityLoad)
        {
            supportStrengthList.Add(this.currentStability);
            hasHadInitialStabilityLoad = true;
        }

        supportStrengthList.Add(this.structure.ProvidedStability);
        currentStability = supportStrengthList.Max();

        Debug.Log($"{this.gameObject.name} has recieved a call to update it's stability: parent structure list = {parentStructureList} , currentStability is : {currentStability}");

        if (currentStability <= 0)
        {
            isBreaking = true;
            currentStability = 0;
            Debug.Log($"{this.gameObject.name} has a stability score of 0 destroying object, here is its parent structure list : " + parentStructureList);
            CallStructureDestroy();
        }

        // Update child structures' stability outside the main calculation loop
        foreach (var child in childStructures)
        {
            child.UpdateStability();
        }
    }

    private void ValidateReferences()
    {
        parentStructures.RemoveAll(item => item == null);
        childStructures.RemoveAll(item => item == null);
    }

    private void OnDrawGizmosSelected()
    {
        Debug.Log($"{this.gameObject.name} contans {parentStructures.Count} parent structures | and {childStructures.Count} child structures");
        string debugLog = $"I am {this.gameObject.name} : my parent structures are ";
        string debugLog2 = "my child structures are ";

        foreach (var item in parentStructures)
        {
            debugLog += item.gameObject.name + " : ";
            Debug.DrawLine(this.gameObject.transform.position + (Vector3.one * 0.1f), item.transform.position + (Vector3.one * 0.1f), Color.green);
            Utility.DrawCube(this.gameObject.transform.position, Vector3.one, this.transform.rotation, Color.green, 0);
        }
        foreach (var item in childStructures)
        {
            debugLog2 += item.gameObject.name + " : ";
            Debug.DrawLine(this.gameObject.transform.position + (Vector3.one * 0.2f), item.transform.position + (Vector3.one * 0.2f), Color.blue);
            Utility.DrawCube(this.gameObject.transform.position, Vector3.one * 0.5f, this.transform.rotation, Color.blue, 0);
        }
        //Debug.Log(debugLog + " | " + debugLog2);
    }
}

public enum StructureType
{
    NoSnap,
    Foundation,
    Pillar,
    Wall,
    Ceiling
}
public enum StructureGridShape
{
    Square,
    Triangle
}
