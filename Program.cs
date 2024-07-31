using Shrink.Command;
using Shrink.Login;

namespace Shrink;

public static class Program
{
    public static async Task Main()
    {
        /*var originalAssemblyPath = "Lagrange.Core.dll";
        var tempAssemblyPath = "ModifiedExternalAssembly.dll";
        var oldUrl = "YOU NEED FIND IT BY YOURSELF";
        var newUrl = "YOUR SIGN SERVER";

        ModifyAssembly(originalAssemblyPath, tempAssemblyPath, oldUrl, newUrl);*/

        await QrCode.Instance.Login();
        await Commands.Instance.Init();
        await Commands.Instance.Run();
    }
    /*private static void ModifyAssembly(string originalAssemblyPath, string tempAssemblyPath, string oldUrl,
        string newUrl)
    {
        var assembly = AssemblyDefinition.ReadAssembly(originalAssemblyPath);
        var module = assembly.MainModule;
        // Windows请自行修改为WindowsSigner
        var type = module.Types.FirstOrDefault(t => t.Name == "LinuxSigner");
        if (type == null)
        {
            Console.WriteLine("无LinuxSigner类型");
            return;
        }

        var method = type.Methods.FirstOrDefault(m => m.Name == "Sign");
        if (method == null)
        {
            Console.WriteLine("无Sign方法");
            return;
        }

        var ilProcessor = method.Body.GetILProcessor();
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.OpCode != OpCodes.Ldstr || instruction.Operand is not string str || str != oldUrl) continue;
            instruction.Operand = newUrl;
            Console.WriteLine($"替换: {oldUrl}为{newUrl}");
        }

        assembly.Write(tempAssemblyPath);
        Console.WriteLine($"已将修改后的程序集保存在{tempAssemblyPath}");
    }*/
}