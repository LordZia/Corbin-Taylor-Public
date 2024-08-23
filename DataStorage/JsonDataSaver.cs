using Newtonsoft.Json;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System;

public class JsonDataSaver : MonoBehaviour
{
    private static JsonDataSaver instance;

    public static JsonDataSaver Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<JsonDataSaver>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("JsonDataSaverSingleton");
                    instance = singletonObject.AddComponent<JsonDataSaver>();
                }
            }

            return instance;
        }
    }

    private JsonDataSaver() { }

    // Serialize a list of variables to a JSON file
    public void SerializeData(List<object> data, string fileName)
    {
        string saveDataPath = Application.persistentDataPath + "/SaveData";
        if (!Directory.Exists(saveDataPath))
        {
            Directory.CreateDirectory(saveDataPath);
        }

        var json = JsonConvert.SerializeObject(data);
        File.WriteAllText(Application.dataPath + "/Resources/SaveData/" + fileName + ".json", json);
    }

    // Deserialize data from a JSON file to a list of variables
    public List<object> DeserializeData(string fileName)
    {
        var json = File.ReadAllText(Application.dataPath + "/Resources/SaveData/" + fileName + ".json");
        return JsonConvert.DeserializeObject<List<object>>(json);
    }

    public bool CheckFileExists(string saveFileName)
    {
        string filePath = (Application.dataPath + "/Resources/SaveData/" + saveFileName + ".json");
        return File.Exists(filePath);
    }
    private void Start()
    {
        /*
        string saveDataPath = Application.persistentDataPath + "/SaveData";
        if (!Directory.Exists(saveDataPath))
        {
            Directory.CreateDirectory(saveDataPath);
        }
        */
    }
}

[Serializable]
public struct DataGraphNode
{
    public int iD;
    public DataVector3 pos;

    public DataGraphNode(int iD, DataVector3 position)
    {
        this.iD = iD;
        this.pos = position;
    }
}
[Serializable]
public struct DataGraphNodeConnections
{
    public int iD;
    public List<int> neighbors;
    public List<int> children;
    public DataGraphNodeConnections(int iD, List<int> neighbors, List<int> children)
    {
        this.iD = iD;
        this.neighbors = neighbors;
        this.children = children;
    }
}

[Serializable]
public struct DataVector3
{
    public float x;
    public float y;
    public float z;

    public DataVector3(Vector3 vector3)
    {
        this.x = vector3.x;
        this.y = vector3.y;
        this.z = vector3.z;
    }
    public DataVector3(float value1, float value2, float value3 )
    {
        this.x = value1;
        this.y = value1;
        this.z = value1;
    }
}

[Serializable]
public struct NamedDataVector3
{
    public string name;
    public float x;
    public float y;
    public float z;

    public NamedDataVector3(string name, Vector3 vector3)
    {
        this.name = name;
        this.x = vector3.x;
        this.y = vector3.y;
        this.z = vector3.z;
    }
    public NamedDataVector3(string name, float value1, float value2, float value3)
    {
        this.name = name;
        this.x = value1;
        this.y = value1;
        this.z = value1;
    }
}


[Serializable]
public struct DataVector2
{
    public float x;
    public float y;

    public DataVector2(Vector2 vector2)
    {
        this.x = vector2.x;
        this.y = vector2.y;
    }
    public DataVector2(float value1, float value2)
    {
        this.x = value1;
        this.y = value1;
    }
}

[Serializable]
public struct DataInventoryItem
{
    public int itemID;
    public int inventorySlot;
    public int quantity;

    public DataInventoryItem(int slot, int iD, int quantity)
    {
        this.itemID = iD;
        this.inventorySlot = slot;
        this.quantity = quantity;
    }
}