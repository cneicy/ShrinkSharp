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
    
    public static QrCode Instance
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

    public BotContext? Client;
    
    // 登录方法
    public async Task Login()
    {
        #region bot实例化

        var deviceInfo = Data.GetDeviceInfo();
        var keyStore = Data.LoadKeystore() ?? new BotKeystore();

        Client = BotFactory.Create(new BotConfig
        {
            UseIPv6Network = false,
            GetOptimumServer = true,
            AutoReconnect = true,
            Protocol = Protocols.Linux
        }, deviceInfo, keyStore);

        #endregion
        
        // Log模式
        Client.Invoker.OnBotLogEvent += (_, @event) =>
        {
            @event.Level.ChangeColorByTitle();
            Console.WriteLine(@event.ToString());
        };

        // bot信息保存，但存在bug未修复
        Client.Invoker.OnBotOnlineEvent += (_, @event) =>
        {
            Console.WriteLine(@event.ToString());
            Data.SaveKeystore(Client.UpdateKeystore());
        };
        // 二维码生成，启动后在根目录下生成qr.png
        var qrCode = await Client.FetchQrCode();
        if (qrCode != null)
        {
            await File.WriteAllBytesAsync("qr.png", qrCode.Value.QrCode);
            await Client.LoginByQrCode();
        }
    }
}