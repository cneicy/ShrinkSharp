using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shrink.Service;

public class CrtVersionChecker
{
    private static readonly HttpClient client = new HttpClient();
    private const string BASE_URL = "https://api.curseforge.com";
    private const string CF_API_KEY = "YOUR_API_KEY_HERE"; // 替换为你的API Key

    public static async Task CheckCrtVersion()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/v1/mods/239197");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("x-api-key", CF_API_KEY);

        try
        {
            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var data = JObject.Parse(responseBody)["data"]["latestFiles"] as JArray;

            foreach (var item in data)
            {
                string gameVersion = item["gameVersions"][0].ToString();
                string jsonString = item.ToString();

                switch (gameVersion)
                {
                    case "1.12.2":
                    case "1.16.5":
                    case "1.18.1":
                        var crtVersion = JsonConvert.DeserializeObject<CrtVersionSerialization>(jsonString)
                            .ParseToCrtVersion();
                        Console.WriteLine($"Parsed CRT Version: {crtVersion}");
                        break;
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
        }
    }

    public static void UpdateVersion(string data)
    {
        var version = JsonConvert.DeserializeObject<CrtVersionSerialization>(data);
        Console.WriteLine($"Updated version: {version}");
    }
}

public class CrtVersionSerialization
{
    public string GameVersion { get; set; }
    public string FileName { get; set; }
    public string FileDate { get; set; }
    public string DownloadUrl { get; set; }

    public CrtVersion ParseToCrtVersion()
    {
        return new CrtVersion(
            this.GameVersion.GetGameVersion(),
            this.FileName,
            DateTime.ParseExact(this.FileDate, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            new Uri(this.DownloadUrl)
        );
    }
}

public class CrtVersion
{
    public GameVersion GameVersion { get; set; }
    public string FileName { get; set; }
    public DateTime FileDate { get; set; }
    public Uri DownloadUrl { get; set; }

    public CrtVersion(GameVersion gameVersion, string fileName, DateTime fileDate, Uri downloadUrl)
    {
        GameVersion = gameVersion;
        FileName = fileName;
        FileDate = fileDate;
        DownloadUrl = downloadUrl;
    }

    public override string ToString()
    {
        return
            $"CrtVersion(GameVersion={GameVersion}, FileName='{FileName}', FileDate={FileDate}, DownloadUrl={DownloadUrl})";
    }
}

public enum GameVersion
{
    V1122,
    V1165,
    V1181,
    OTHER
}

public static class GameVersionExtensions
{
    public static GameVersion GetGameVersion(this string version)
    {
        return version switch
        {
            "1.12.2" => GameVersion.V1122,
            "1.16.5" => GameVersion.V1165,
            "1.18.1" => GameVersion.V1181,
            _ => GameVersion.OTHER
        };
    }
}