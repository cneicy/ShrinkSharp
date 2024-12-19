using Mono.Cecil;
using Mono.Cecil.Cil;
using Shrink.Command;
using Shrink.Login;

namespace Shrink;

public static class Program
{
    public static async Task Main()
    {
        /*var originalAssemblyPath = "Lagrange.Core.dll";
        var tempAssemblyPath = "ModifiedExternalAssembly.dll";
        var oldUrl = "自己找";
        var newUrl = "自己找";

        ModifyAssembly(originalAssemblyPath, tempAssemblyPath, oldUrl, newUrl);*/

        await BotService.Instance.Login();
        await Commands.Instance.Init();
        await Commands.Instance.Run();
    }
    /*private static void ModifyAssembly(string originalAssemblyPath, string tempAssemblyPath, string oldUrl, string newUrl)
    {
        var assembly = AssemblyDefinition.ReadAssembly(originalAssemblyPath);
        var module = assembly.MainModule;

        // 查找LinuxSigner类型
        var linuxSignerType = module.Types.FirstOrDefault(t => t.Name == "LinuxSigner");
        if (linuxSignerType == null)
        {
            Console.WriteLine("无LinuxSigner类型");
            return;
        }

        // 查找构造函数
        var constructor = linuxSignerType.Methods.FirstOrDefault(m => m.IsConstructor);
        if (constructor == null)
        {
            Console.WriteLine("无构造函数");
            return;
        }

        var ilProcessor = constructor.Body.GetILProcessor();
        foreach (var instruction in constructor.Body.Instructions)
        {
            // 检查是否是加载字符串操作
            if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string str && str == oldUrl)
            {
                instruction.Operand = newUrl; // 替换为新URL
                Console.WriteLine($"替换: {oldUrl}为{newUrl}");
            }
        }

        assembly.Write(tempAssemblyPath);
        Console.WriteLine($"已将修改后的程序集保存在{tempAssemblyPath}");
    }*/
}