using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevelopingNET6
{
    public enum Command
    {
        Register,
        Message,
        Confirmation,
        List
    }

    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; } = null!;
        public bool Received { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public virtual User FromUser { get; set; } = null!;
        public virtual User ToUser { get; set; } = null!;

        public Command Command { get; set; }
        public string FromName { get; set; } = null!;
        public string ToName { get; set; } = null!;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
        public static Message FromJson(string json)
        {
            return JsonSerializer.Deserialize<Message>(json);
        }
    }
}