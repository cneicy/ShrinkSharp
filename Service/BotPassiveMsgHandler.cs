using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Newtonsoft.Json;

namespace Shrink.Service;

public class BotPassiveMsgHandler
{
    private static BotPassiveMsgHandler? _instance;
    private static readonly object Lock = new();
    private BotPassiveMsgHandler() { }

    public static BotPassiveMsgHandler Instance
    {
        get
        {
            if (_instance != null) return _instance;
            lock (Lock)
            {
                _instance ??= new BotPassiveMsgHandler();
            }
            return _instance;
        }
    }

    // 使用字典存储命令和群消息
    private Dictionary<string, string> _commandDict = new();
    private List<string> _corpus = new();
    private Dictionary<uint, string> _messageDict = new();
    private HashSet<uint> _adminSet = new();

    // 文件初始化
    public async Task Init()
    {
        // 读取Admins.json
        if (File.Exists("Admins.json"))
        {
            _adminSet = new HashSet<uint>(JsonConvert.DeserializeObject<List<uint>>(await File.ReadAllTextAsync("Admins.json"))!);
        }
        else
        {
            _adminSet.Add(3048536893); // 默认管理员
            await File.WriteAllTextAsync("Admins.json", JsonConvert.SerializeObject(_adminSet.ToList(), Formatting.Indented));
        }

        // 读取JoinMessage.json
        if (File.Exists("JoinMessage.json"))
        {
            _messageDict = JsonConvert.DeserializeObject<List<KeyValuePair<uint, string>>>(await File.ReadAllTextAsync("JoinMessage.json"))!
                .ToDictionary(msg => msg.Key, msg => msg.Value);
        }
        else
        {
            _messageDict.Add(620902312, "欢迎");
            await File.WriteAllTextAsync("JoinMessage.json", JsonConvert.SerializeObject(_messageDict.Select(kv => new KeyValuePair<uint, string>(kv.Key, kv.Value)).ToList(), Formatting.Indented));
        }

        // 读取Commands.json
        if (File.Exists("Commands.json"))
        {
            _commandDict = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(await File.ReadAllTextAsync("Commands.json"))!
                .ToDictionary(cmd => cmd.Key, cmd => cmd.Value);
        }
        else
        {
            _commandDict.Add("/ping", "Pong!");
            await File.WriteAllTextAsync("Commands.json", JsonConvert.SerializeObject(_commandDict.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value)).ToList(), Formatting.Indented));
        }

        // 读取Corpus.json
        if (File.Exists("Corpus.json"))
        {
            _corpus = JsonConvert.DeserializeObject<List<string>>(await File.ReadAllTextAsync("Corpus.json"))!;
        }
        else
        {
            _corpus.Add("你这辈子就只能做原生了。");
            _corpus.Add("天灵灵地灵灵，魔改大仙快显灵。");
            await File.WriteAllTextAsync("Corpus.json", JsonConvert.SerializeObject(_corpus, Formatting.Indented));
        }
    }

    // 保存方法
    private async Task SaveCorpus()
    {
        await File.WriteAllTextAsync("Corpus.json", JsonConvert.SerializeObject(_corpus, Formatting.Indented));
        await Init();
    }

    private async Task SaveAdmin()
    {
        await File.WriteAllTextAsync("Admins.json", JsonConvert.SerializeObject(_adminSet.ToList(), Formatting.Indented));
        await Init();
    }

    // bot管理员权限检查
    private void NoEnoughPermission(BotContext ctx, uint groupId)
    {
        var chain = MessageBuilder.Group(groupId).Text("权限不足");
        ctx.SendMessage(chain.Build());
    }

    // 先Init后再运行
    public Task Run()
    {
        BotService.Instance.Client!.Invoker.OnGroupMemberIncreaseEvent += (context, @event) =>
        {
            var groupid = @event.GroupUin;
            if (!_messageDict.TryGetValue(groupid, out var message)) return;
            var chain = MessageBuilder.Group(groupid).Text(message);
            context.SendMessage(chain.Build());
        };

        BotService.Instance.Client.Invoker.OnGroupMessageReceived += (content, @event) =>
        {
            var groupId = @event.Chain.GroupUin!.Value;
            var senderId = @event.Chain.FriendUin;
            var text = @event.Chain.ToPreviewText();

            // 执行命令
            if (_commandDict.TryGetValue(text, out var value))
            {
                var chain = MessageBuilder.Group(groupId).Text(value);
                content.SendMessage(chain.Build());
            }

            var today = DateTime.Today;
            var seed = today.Year ^ today.Month ^ today.Day ^ senderId;
            var random = new Random((int)seed);

            switch (text)
            {
                // 今日人品
                case "/jrrp":
                    var chain = MessageBuilder.Group(groupId).Mention(senderId).Text("今天的人品值是: " + random.Next(101));
                    content.SendMessage(chain.Build());
                    break;

                case "/modpacktoday":
                case "/modpacktoday?":
                case "/modpacktoday？":
                    var chain1 = MessageBuilder.Group(groupId).Mention(senderId).Text(_corpus[random.Next(_corpus.Count)]);
                    content.SendMessage(chain1.Build());
                    break;

                case "/showall":
                    if (!_adminSet.Contains(senderId))
                    {
                        NoEnoughPermission(content, groupId);
                        break;
                    }
                    var chain2 = MessageBuilder.Friend(senderId).Text(File.ReadAllText("Corpus.json") + "\n" + File.ReadAllText("Commands.json") + "\n" + File.ReadAllText("JoinMessage.json"));
                    content.SendMessage(chain2.Build());
                    break;

                case "/reload":
                    _ = Init();
                    var chain3 = MessageBuilder.Group(groupId).Text("重载完成!");
                    content.SendMessage(chain3.Build());
                    break;
            }

            if (text.Contains("/addcorpus") && text.StartsWith("/addcorpus"))
            {
                if (_adminSet.Contains(senderId))
                {
                    var temp = text.Remove(0, 10);
                    if (_corpus.Contains(temp))
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已经存在: " + temp);
                        content.SendMessage(chain.Build());
                    }
                    else
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已添加: " + temp);
                        _corpus.Add(temp);
                        content.SendMessage(chain.Build());
                        _ = SaveCorpus();
                    }
                }
                else
                {
                    NoEnoughPermission(content, groupId);
                }
            }

            if (text.Contains("/addadmin") && text.StartsWith("/addadmin"))
            {
                if (_adminSet.Contains(senderId))
                {
                    var temp = uint.Parse(text.Remove(0, 9));
                    if (!_adminSet.Add(temp))
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已存在管理员: " + temp);
                        content.SendMessage(chain.Build());
                    }
                    else
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已添加管理员: " + temp);
                        _ = SaveAdmin();
                        content.SendMessage(chain.Build());
                    }
                }
                else
                {
                    NoEnoughPermission(content, groupId);
                }
            }

            if (!text.Contains("/delcorpus") || !text.StartsWith("/delcorpus")) return;
            {
                if (_adminSet.Contains(senderId))
                {
                    var temp = text.Remove(0, 10);
                    if (_corpus.Remove(temp))
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已删除: " + temp);
                        _ = SaveCorpus();
                        content.SendMessage(chain.Build());
                    }
                    else
                    {
                        var chain = MessageBuilder.Group(groupId).Text("未找到此条语料");
                        content.SendMessage(chain.Build());
                    }
                }
                else
                {
                    NoEnoughPermission(content, groupId);
                }
            }
        };

        return Task.CompletedTask;
    }
}
