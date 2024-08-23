using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static PlayerStats;
using static PlayerStateMachine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Inventory))]
public class PlayerDataSaver : MonoBehaviour, ISaveable
{
    private PlayerStats playerStats;
    private Inventory playerInventory;

    PlayerStateMachine playerState;
    private void Awake()
    {
        playerStats = this.GetComponent<PlayerStats>();
        playerInventory = this.GetComponent<Inventory>();
        ApplicationCloser.Instance.OnApplicationClose += CallDataSave;

        if (JsonDataSaver.Instance.CheckFileExists("PlayerData"))
        {
            //List<object> loadedData = LoadData();
            List<object> savedObjectStats = JsonDataSaver.Instance.DeserializeData("PlayerData");
            UnpackData(savedObjectStats);
        }
        else
        {
            // no save file found respawn player
            PlayerSpawnManager.Instance.SpawnPlayer(playerStats);
            Debug.Log("Calling save data");
            CallDataSave();
        }

        playerState = this.GetComponent<PlayerStateMachine>();
        playerState.OnStateChange += HandleStateChange;
    }

    

    private void UnpackData(List<object> savedObjectStats)
    {
        List<Stat> statValues = new List<Stat>();
        Dictionary<int, InventoryItem> inventorySlots = new Dictionary<int, InventoryItem>();

        //Debug.Log("Detected Player Save Data : Unpacking");
        foreach (object obj in savedObjectStats)
        {
            string jsonData = obj.ToString();
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);

            if (data.ContainsKey("name") && data.ContainsKey("x") && data.ContainsKey("y") && data.ContainsKey("z"))
            {
                // Extract values from the dictionary
                string name = data["name"].ToString();
                float x = Convert.ToSingle(data["x"]);
                float y = Convert.ToSingle(data["y"]);
                float z = Convert.ToSingle(data["z"]);


                if (name == "Pos")
                {
                    Vector3 pos = new Vector3(x, y, z);
                    this.transform.position = pos;
                }
                if (name == "Rot")
                {
                    Vector3 eulerAngles = new Vector3(x, y, z);
                    this.transform.rotation = Quaternion.Euler(eulerAngles);
                }
            }


            if (data.ContainsKey("max") && data.ContainsKey("current"))
            {
                float max = Convert.ToSingle(data["max"]);
                float current = Convert.ToSingle(data["current"]);
                Stat newStat = new Stat((int)max, (int)current);
                statValues.Add(newStat);
                //Debug.Log("Detected Player Saved Stat Data : Unpacking");

            }

            if (data.ContainsKey("itemID"))
            {
                int itemID = (int)Convert.ToSingle(data["itemID"]);
                int slot = (int)Convert.ToSingle(data["inventorySlot"]);
                int quantity = (int)Convert.ToSingle(data["quantity"]);
                Item item = ItemIndex.Instance.GetItemByID(itemID);

                if (item != null)
                {
                    InventoryItem invItem = new InventoryItem(item, quantity);
                    inventorySlots.Add(slot, invItem);
                }
            }
        }

        foreach (var slotItem in inventorySlots)
        {
            playerInventory.AddItemToSlot(slotItem.Key, slotItem.Value);
        }

        playerStats.SetStats(statValues);
    }

    private void OnDestroy()
    {
        playerState.OnStateChange -= HandleStateChange;
        ApplicationCloser.Instance.OnApplicationClose -= CallDataSave;
    }
    private void HandleStateChange(playerMainState mainState, playerSubState subState)
    {
        if (subState == PlayerStateMachine.playerSubState.InventoryControl)
        {
            // for now save player data on inventory open, debug testing save logic;
            CallDataSave();
            Debug.Log("saving player data");
        }
    }

    public void CallDataSave()
    {
        List<object> statObjects = playerStats.stats.Cast<object>().ToList();

        NamedDataVector3 playerPosition = new NamedDataVector3("Pos", this.transform.position);
        NamedDataVector3 playerEulerRotation = new NamedDataVector3("Rot", this.transform.rotation.eulerAngles);

        statObjects.Add(playerPosition);
        statObjects.Add(playerEulerRotation);

        foreach (KeyValuePair<int, InventoryItem> itemSlot in playerInventory.InventorySlots)
        {
            // Convert inventory slots into condensed data inventory items.
            DataInventoryItem item = new DataInventoryItem(itemSlot.Key, itemSlot.Value.item.ItemId, itemSlot.Value.quantity);
            statObjects.Add(item);
        }

        SaveData(statObjects);
    }
    public void SaveData(List<object> data)
    {
        JsonDataSaver.Instance.SerializeData(data, "PlayerData");

        /*
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/PlayerData.dat");
        formatter.Serialize(file, data);
        file.Close();

        string persistentDataPath = Application.persistentDataPath + "/PlayerData.dat";
        Debug.Log("Persistent Data Path: " + persistentDataPath);
        */
    }

    public List<object> LoadData()
    {
        //return JsonDataSaver.Instance.DeserializeData("PlayerData");

        List<object> data = new List<object>();

        string filePath = Application.persistentDataPath + "/PlayerData.dat";

        if (File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream file = File.Open(filePath, FileMode.Open))
            {
                data = (List<object>)formatter.Deserialize(file);
            }
            Debug.LogError("Found data at : " + filePath);
        }
        else
        {
            Debug.LogError("File not found: " + filePath);
        }

        return data;
    }
}

public interface ISaveable
{
    void SaveData(List<object> data);
    List<object> LoadData();
}
