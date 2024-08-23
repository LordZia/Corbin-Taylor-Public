using System;
using System.IO;
using Newtonsoft.Json;

public class DataStorage
{
    public string FileName { get; private set; }
    public string FilePath { get; private set; }

    public DataStorage(string fileName)
    {
        FileName = fileName;
        FilePath = Path.Combine(Environment.CurrentDirectory, fileName);
    }

    public void SaveDataToJson(object data)
    {
        string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);

        try
        {
            File.WriteAllText(FilePath, jsonData);
            Console.WriteLine($"Data saved to {FileName}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving data to {FileName}: {ex.Message}");
        }
    }

    public T LoadDataFromJson<T>()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string jsonData = File.ReadAllText(FilePath);
                T data = JsonConvert.DeserializeObject<T>(jsonData);
                Console.WriteLine($"Data loaded from {FileName}.");
                return data;
            }
            else
            {
                Console.WriteLine($"File {FileName} does not exist.");
                return default(T);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data from {FileName}: {ex.Message}");
            return default(T);
        }
    }
}

// storage use example
public class DataAnalyzer
{
    public static void Main(string[] args)
    {
        DataStorage dataStorage = new DataStorage("example_data.json");

        // Sample data to save
        var dataToSave = new { Name = "John Doe", Age = 30, Occupation = "Developer" };

        // Save data to JSON file
        dataStorage.SaveDataToJson(dataToSave);

        // Load data from JSON file
        var loadedData = dataStorage.LoadDataFromJson<dynamic>();

        // Display loaded data
        //Console.WriteLine($"Name: {loadedData.Name}, Age: {loadedData.Age}, Occupation: {loadedData.Occupation}");
    }
}

