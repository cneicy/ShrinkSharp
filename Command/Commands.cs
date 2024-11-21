using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Newtonsoft.Json;
using Shrink.Login;

namespace Shrink.Command;

// 命令数据类型
public struct CommandMap
{
    public string Key;
    public string Value;

    public CommandMap(string key, string value)
    {
        Key = key;
        Value = value;
    }
}

// 加入群后的消息数据类型
public struct JoinMessageMap
{
    public uint Key;
    public string Value;
    public JoinMessageMap(uint key, string value)
    {
        Key = key;
        Value = value;
    }
}

public class Commands
{
    private static Commands? _instance;
    
    private static readonly object Lock = new();
    
    private Commands() { }
    
    public static Commands Instance
    {
        get
        {
            if (_instance != null) return _instance;
            lock (Lock)
            {
                _instance ??= new Commands();
            }
            return _instance;
        }
    }

    // todo 可以优化成字典储存
    private List<CommandMap> _commandList = new();
    private List<string> _corpus = new();
    private List<JoinMessageMap> _messageList = new();
    private List<uint> _adminList = new();
    
    // 文件初始化
    public async Task Init()
    {
        if (File.Exists("Admins.json"))
        {
            _adminList = JsonConvert.DeserializeObject<List<uint>>(await File.ReadAllTextAsync("Admins.json"));
        }
        else
        {
            _adminList.Add(3048536893);
            await File.WriteAllTextAsync("Admins.json",JsonConvert.SerializeObject(_adminList, Formatting.Indented));
        }
        
        if (File.Exists("JoinMessage.json"))
        {
            _messageList = JsonConvert.DeserializeObject<List<JoinMessageMap>>(await File.ReadAllTextAsync("JoinMessage.json"));
        }
        else
        {
            _messageList.Add(new JoinMessageMap(620902312,"欢迎"));
            await File.WriteAllTextAsync("JoinMessage.json",JsonConvert.SerializeObject(_messageList, Formatting.Indented));
        }
        
        if (File.Exists("Commands.json"))
        {
            _commandList = JsonConvert.DeserializeObject<List<CommandMap>>(await File.ReadAllTextAsync("Commands.json"));
        }
        else
        {
            _commandList.Add(new CommandMap("/ping","Pong!"));
            await File.WriteAllTextAsync("Commands.json",JsonConvert.SerializeObject(_commandList, Formatting.Indented));
        }

        if (File.Exists("Corpus.json"))
        {
            _corpus = JsonConvert.DeserializeObject<List<string>>(await File.ReadAllTextAsync("Corpus.json"));
        }
        else
        {
            _corpus.Add(" 你这辈子就只能做原生了。");
            _corpus.Add(" 天灵灵地灵灵，魔改大仙快显灵。");
            await File.WriteAllTextAsync("Corpus.json",JsonConvert.SerializeObject(_corpus, Formatting.Indented));
        }
    }

    // 保存方法
    private void SaveCorpus()
    {
        File.WriteAllTextAsync("Corpus.json",JsonConvert.SerializeObject(_corpus, Formatting.Indented));
        _ = Init();
    }
    private void SaveAdmin()
    {
        File.WriteAllTextAsync("Admins.json",JsonConvert.SerializeObject(_adminList, Formatting.Indented));
        _ = Init();
    }

    // bot管理员
    private void NoEnoughPermission(BotContext ctx,uint groupID)
    {
        var chain = MessageBuilder.Group(groupID).Text("权限不足");
        ctx.SendMessage(chain.Build());
    }
    
    // 先Init后再运行
    public Task Run()
    {
        QrCode.Instance.Client.Invoker.OnGroupMemberIncreaseEvent += (context, @event) =>
        {
            var groupid = @event.GroupUin;
            foreach (var chain in from message in _messageList where message.Key == groupid select MessageBuilder.Group(groupid).Text(message.Value))
            {
                context.SendMessage(chain.Build());
            }
        };
        QrCode.Instance.Client.Invoker.OnGroupMessageReceived += (content, @event) =>
        {
            var groupId = @event.Chain.GroupUin.Value;
            var senderId = @event.Chain.FriendUin;
            var text = @event.Chain.ToPreviewText();
            foreach (var chain in from command in _commandList where text.Equals(command.Key) select MessageBuilder.Group(groupId).Text(command.Value))
            {
                content.SendMessage(chain.Build());
            }


            var today = DateTime.Today;
            var seed = today.Year ^ today.Month ^ today.Day ^ senderId;
            var random = new Random((int)seed);

            switch (text)
            {
                //今日人品
                case "/jrrp":
                {
                    var chain = MessageBuilder.Group(groupId).Mention(senderId).Text( "今天的人品值是: "+random.Next(101));
                    content.SendMessage(chain.Build());
                    break;
                }
                case "/modpacktoday":
                case "/modpacktoday?":
                case "/modpacktoday？":
                {
                    var chain = MessageBuilder.Group(groupId).Mention(senderId).Text(_corpus[random.Next(_corpus.Count - 1)]);
                    content.SendMessage(chain.Build());
                    break;
                }
                case "/showall":
                {
                    if(!_adminList.Contains(senderId))
                    {
                        NoEnoughPermission(content, groupId);
                        break;
                    }
                    var chain = MessageBuilder.Friend(senderId).Text(File.ReadAllText("Corpus.json")+"\n"+File.ReadAllText("Commands.json")+"\n"+File.ReadAllText("JoinMessage.json"));
                    content.SendMessage(chain.Build());
                    break;
                }
                case "/reload":
                {
                    _ = Init();
                    var chain = MessageBuilder.Group(groupId).Text("重载完成!");
                    content.SendMessage(chain.Build());
                    break;
                }
            }

            if (text.Contains("/addcorpus") && text.StartsWith("/addcorpus"))
            {
                if (_adminList.Contains(senderId))
                {
                    var temp = text.Remove(0, 10);
                    if (_corpus.Contains(temp))
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已经存在: "+temp);
                        content.SendMessage(chain.Build());
                    }
                    else
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已添加: "+temp);
                        _corpus.Add(temp);
                        content.SendMessage(chain.Build());
                        SaveCorpus();
                    }
                }
                else
                {
                    NoEnoughPermission(content, groupId);
                }
            }

            if (text.Contains("/addadmin") && text.StartsWith("/addadmin"))
            {
                if (_adminList.Contains(senderId))
                {
                    var temp = uint.Parse(text.Remove(0, 9));
                    if (_adminList.Contains(temp))
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已存在管理员: " + temp);
                        content.SendMessage(chain.Build());
                    }
                    else
                    {
                        _adminList.Add(temp);
                        var chain = MessageBuilder.Group(groupId).Text("已添加管理员: " + temp);
                        SaveAdmin();
                        content.SendMessage(chain.Build());
                    }
                }
                else
                {
                    NoEnoughPermission(content, groupId);
                }
            }
            
            if (text.Contains("/delcorpus") && text.StartsWith("/delcorpus"))
            {
                if (_adminList.Contains(senderId))
                {
                    var temp = text.Remove(0, 10);
                    if (_corpus.Remove(temp))
                    {
                        var chain = MessageBuilder.Group(groupId).Text("已删除: " + temp);
                        SaveCorpus();
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