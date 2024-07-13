using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Newtonsoft.Json;
using Shrink.Login;

namespace Shrink.Command;

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

    private List<CommandMap> _commandList = new();
    private List<string> _corpus = new();
    private List<JoinMessageMap> _messageList = new();
    public async Task Init()
    {
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

    private void SaveCorpus()
    {
        File.WriteAllTextAsync("Corpus.json",JsonConvert.SerializeObject(_corpus, Formatting.Indented));
        Init();
    }
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
            
            //今日人品
            if (text.Equals("/jrrp"))
            {
                
                var chain = MessageBuilder.Group(groupId).Mention(senderId).Text( "今天的人品值是: "+random.Next(101));
                content.SendMessage(chain.Build());
            }

            if (text.Equals("/modpacktoday") || text.Equals("/modpacktoday?") || text.Equals("/modpacktoday？"))
            {
                var chain = MessageBuilder.Group(groupId).Text(_corpus[random.Next(_corpus.Count - 1)]);
                content.SendMessage(chain.Build());
            }

            if (text.Equals("/reload"))
            {
                Init();
                var chain = MessageBuilder.Group(groupId).Text("重载完成!");
                content.SendMessage(chain.Build());
            }

            if (text.Contains("/addcorpus") && text.StartsWith("/addcorpus"))
            {
                var temp = text.Remove(0, 10);
                var chain = MessageBuilder.Group(groupId).Text("已添加: "+temp);
                _corpus.Add(temp);
                SaveCorpus();
                content.SendMessage(chain.Build());
            }
            
        };
        return Task.CompletedTask;
    }
}