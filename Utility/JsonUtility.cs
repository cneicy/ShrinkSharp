using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shrink.Utility;

public static class JsonUtility
{
    public static void WriteJsonToFile<T>(string filePath, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"写入文件出错： {filePath}: {ex.Message}");
            throw;
        }
    }
    public static T ReadJsonFromFile<T>(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve })!;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"读取文件出错： {filePath}: {ex.Message}");
            throw;
        }
    }
    public static T ReadOrCreateJsonFile<T>(string filePath, Func<T> createFunc)
    {
        if (File.Exists(filePath))
        {
            return ReadJsonFromFile<T>(filePath);
        }

        var newData = createFunc();
        WriteJsonToFile(filePath, newData);
        return newData;
    }
}