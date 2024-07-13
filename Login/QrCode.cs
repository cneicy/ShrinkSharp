using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Shrink.Utility;
using Console = System.Console;

namespace Shrink.Login;

public class QrCode
{
    private static QrCode _instance;

    // 锁对象，用于同步
    private static readonly object _lock = new object();

    // 私有构造函数，防止外部使用 new 关键字创建实例
    private QrCode() { }

    // 公有的静态方法，用于获取类的实例
    public static QrCode Instance
    {
        get
        {
            // 双重检查锁定
            if (_instance != null) return _instance;
            lock (_lock)
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

        /*var friendChain = MessageBuilder.Group(624487948)
            .Text("Shrink Started!");
        await client.SendMessage(friendChain.Build());*/
    }
}