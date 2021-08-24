using System;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Utils;
using System.IO.Pipes;
using LitJson;
using System.Threading;
using System.Diagnostics;

namespace Math0424.Discord
{
    public class Discord : IDisposable
    {

        public bool Ready { get; private set; }

        public DiscordIPC IPC { get; private set; }
        public DiscordRP RP { get; private set; }

        public Discord()
        {
            IPC = new DiscordIPC(this);
            RP = new DiscordRP(this);
        }

        public void Dispose()
        {
            Log("Disposing client connection", LogLevel.Info);
            IPC.Dispose();
        }

        public static void Log(object o, LogLevel level = LogLevel.Debug)
        {
            MyLog.Default.WriteLineAndConsole($"DiscordLog[{level}] {o ?? "null"}");
        }
    }
    
    public enum LogLevel
    {
        Debug,
        Error,
        Info,
        Severe,
        Success,
    }
    
}
