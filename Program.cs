using Shrink.Command;
using Shrink.Login;

namespace Shrink;

public static class Program
{
    public static async Task Main()
    {
        await QrCode.Instance.Login();
        await Commands.Instance.Init();
        await Commands.Instance.Run();
    }
}