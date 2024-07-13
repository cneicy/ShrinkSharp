using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Shrink.Utility;
using Console = System.Console;

namespace Shrink.Login;

public class QrCode
{
    private static QrCode? _instance;
    
    private static readonly object Lock = new();
    
    private QrCode() { }
    
    public static QrCode? Instance
    {
        get
        {
            // 双重检查锁定
            if (_instance != null) return _instance;
            lock (Lock)
            {
                _instance ??= new QrCode();
            }
            return _instance;
        }
    }

    public BotContext Client;
    public async Task Login()
    {
        var deviceInfo = Data.GetDeviceInfo();
        var keyStore = Data.LoadKeystore() ?? new BotKeystore();

        Client = BotFactory.Create(new BotConfig
        {
            UseIPv6Network = false,
            GetOptimumServer = true,
            AutoReconnect = true,
            Protocol = Protocols.Linux
        }, deviceInfo, keyStore);

        Client.Invoker.OnBotLogEvent += (context, @event) =>
        {
            Utility.Console.ChangeColorByTitle(@event.Level);
            Console.WriteLine(@event.ToString());
        };

        Client.Invoker.OnBotOnlineEvent += (context, @event) =>
        {
            Console.WriteLine(@event.ToString());
            Data.SaveKeystore(Client.UpdateKeystore());
        };
        var qrCode = await Client.FetchQrCode();
        if (qrCode != null)
        {
            await File.WriteAllBytesAsync("qr.png", qrCode.Value.QrCode);
            await Client.LoginByQrCode();
        }
    }
}