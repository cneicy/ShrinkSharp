using System.Text.Json;
using System.Text.Json.Serialization;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Shrink.Utility;
using Console = System.Console;

namespace Shrink.Login;

public class BotService
{
    private static readonly Lazy<BotService> _instance = new(() => new BotService());
    public static BotService Instance => _instance.Value;

    public BotContext? Client;
    private bool _isOnline;
    private static string KeystoreFilePath => "Keystore.json";
    private static string DeviceInfoFilePath => "DeviceInfo.json";


    private static BotDeviceInfo GetDeviceInfo() =>
        ReadOrCreateJsonFile(DeviceInfoFilePath, BotDeviceInfo.GenerateInfo);
    
    public async Task Login()
    {
        var deviceInfo = File.Exists(DeviceInfoFilePath)
            ? ReadJsonFromFile<BotDeviceInfo>(DeviceInfoFilePath)
            : GetDeviceInfo();

        var keyStore = File.Exists(KeystoreFilePath)
            ? ReadJsonFromFile<BotKeystore>(KeystoreFilePath)
            : new BotKeystore();

        Client = BotFactory.Create(new BotConfig
        {
            UseIPv6Network = false,
            GetOptimumServer = true,
            AutoReconnect = true,
            Protocol = Protocols.Linux
        }, deviceInfo, keyStore);
        
        Client.Invoker.OnBotLogEvent += (_, @event) =>
        {
            @event.Level.ChangeColorByTitle();
            Console.WriteLine(@event.ToString());
        };
        
        Client.Invoker.OnBotOnlineEvent += (_, @event) =>
        {
            Console.WriteLine(@event.ToString());
            _isOnline = true;
        };
        Client.Invoker.OnBotOfflineEvent += (_, @event) =>
        {
            Console.WriteLine(@event.ToString());
            _isOnline = false;
        };

        if (File.Exists(KeystoreFilePath))
        {
            await Client.LoginByPassword();
            if (!_isOnline)
            {
                Console.WriteLine("账密登录失败，请尝试二维码登录。");
                var qrCode = await Client.FetchQrCode();
                if (qrCode != null)
                {
                    await File.WriteAllBytesAsync("qr.png", qrCode.Value.QrCode);
                    await Client.LoginByQrCode();
                }
            }
        }

        await File.WriteAllTextAsync(KeystoreFilePath, JsonSerializer.Serialize(Client.UpdateKeystore()));
        await File.WriteAllTextAsync(DeviceInfoFilePath, JsonSerializer.Serialize(Client.UpdateDeviceInfo()));
    }

    private static T ReadJsonFromFile<T>(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve })!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取错误： {filePath}: {ex.Message}");
            throw;
        }
    }

    private static T ReadOrCreateJsonFile<T>(string filePath, Func<T> createFunc)
    {
        if (File.Exists(filePath))
        {
            return ReadJsonFromFile<T>(filePath);
        }

        var newData = createFunc();
        WriteJsonToFile(filePath, newData);
        return newData;
    }

    private static void WriteJsonToFile<T>(string filePath, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"写入错误： {filePath}: {ex.Message}");
            throw;
        }
    }
}