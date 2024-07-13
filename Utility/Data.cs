using Lagrange.Core.Common;
using Newtonsoft.Json;

namespace Shrink.Utility;

public static class Data
{
    public static void SaveKeystore(BotKeystore keystore) =>
        File.WriteAllText("Keystore.json", JsonConvert.SerializeObject(keystore));

    public static BotDeviceInfo GetDeviceInfo()
    {
        if (File.Exists("DeviceInfo.json"))
        {
            var info = JsonConvert.DeserializeObject<BotDeviceInfo>(File.ReadAllText("DeviceInfo.json"));
            if (info != null) return info;

            info = BotDeviceInfo.GenerateInfo();
            File.WriteAllText("DeviceInfo.json", JsonConvert.SerializeObject(info));
            return info;
        }

        var deviceInfo = BotDeviceInfo.GenerateInfo();
        File.WriteAllText("DeviceInfo.json", JsonConvert.SerializeObject(deviceInfo));
        return deviceInfo;
    }
    public static BotKeystore? LoadKeystore()
    {
        try
        {
            var text = File.ReadAllText("Keystore.json");
            return JsonConvert.DeserializeObject<BotKeystore>(text, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
        }
        catch
        {
            return null;
        }
    }
}