using System;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Utils;
using System.IO.Pipes;
using LitJson;
using System.Threading;
using System.Diagnostics;

namespace SERichPresence.Lib
{
    /// <summary>
    /// @Author Math0424
    /// This is the meat of the plugin and the hardest part
    /// </summary>
    public class Discord : IDisposable
    {

        public bool Ready { get; private set; }

        private NamedPipeClientStream Stream;
        private Thread ReadThread;

        public Discord(long clientId)
        {
            if (EstablishPipe())
            {
                EstablishHandshake(clientId);
                Log($"Established handshake with Discord client", LogLevel.Success);
                StartRead();
                Ready = true;
            }
        }

        public void SetRichPresence(DiscordRichPresence presence)
        {
            if (Ready)
            {
                string nonce = Guid.NewGuid().ToString();
                int pid = Process.GetCurrentProcess().Id;
                presence.instance = true;

                string output = $"{{\"nonce\":\"{nonce}\",\"cmd\":\"SET_ACTIVITY\",\"args\":{{\"pid\":{pid},\"activity\":{presence.ToJson()}}}}}";

                Stream.Write(new Packet(OpCode.Frame, output));
            }
        }

        private void StartRead()
        {

            ReadThread = new Thread(() => {

                while (Stream != null && Stream.IsConnected)
                {
                    if (Stream.CanRead)
                    {
                        var code = Stream.ReadInt32();
                        int length = Stream.ReadInt32();
                        
                        var data = new byte[length];
                        Stream.Read(data, 0, length);

                        switch ((OpCode)code)
                        {
                            case OpCode.Close:
                                Log($"Discord requested close of ipc, Disconnecting...", LogLevel.Info);
                                Ready = false;
                                Dispose();
                                break;

                            case OpCode.Ping:
                                Log($"Pong!", LogLevel.Info);
                                Stream.Write(new Packet(OpCode.Pong, Encoding.UTF8.GetString(data)));
                                break;
                                
                            case OpCode.Frame:
                            case OpCode.Handshake:
                            case OpCode.Pong:
                                break;
                            default:
                                Log($"Recieved unknown code ({code}) with length {length}");
                                break;
                        }
                    }
                    else
                    {
                        Log("Unable to read stream");
                    }

                    Thread.Sleep(100);
                }
            
            });

            ReadThread.Start();
        }

        private void EstablishHandshake(long clientId)
        {
            var handshake = new Handshake()
            {
                client_id = clientId.ToString(),
                v = "1",
                steam_id = 244850,
            };
            var json = JsonMapper.ToJson(handshake);
            Stream.Write(new Packet(OpCode.Handshake, json));
        }

        private bool EstablishPipe()
        {
            string discordPipe = null;
            
            var pipes = Directory.GetFiles(@"\\.\pipe\");
            for (var i = 0; i < pipes.Length; i++)
            {
                var pipe = pipes[i].Split('\\').Last().Split('-');
                if (pipe.Length == 3)
                {
                    if (pipe[0] == "discord" && pipe[1] == "ipc")
                    {
                        Log($"Found discord ipc pipe ({pipe[2]})", LogLevel.Info);
                        discordPipe = pipes[i];
                        break;
                    }
                }
            }
            if (discordPipe != null)
            {
                string pipeName = discordPipe.Substring(9);
                Log($"Establishing connection with '{pipeName}'", LogLevel.Info);
                Stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                Stream.Connect(1000);

                while (!Stream.IsConnected)
                    Thread.Sleep(20);

                return true;
            }
            Log("Failed to establish connection with Discord ipc!", LogLevel.Error);
            return false;
        }

        private static void Log(object o, LogLevel level = LogLevel.Debug)
        {
            MyLog.Default.WriteLineAndConsole($"DiscordLog[{level}] {o ?? "null"}");
        }

        public void Dispose()
        {
            Log("Disposing client connection", LogLevel.Info);
            ReadThread?.Abort();
            Thread.Sleep(1); //finish while loop or something

            Stream?.Close();
            Stream?.Dispose();
            ReadThread = null;
            Stream = null;
        }

        private enum LogLevel
        {
            Debug,
            Error,
            Info,
            Severe,
            Success,
        }

        private struct Handshake
        {
            public string v;
            public string client_id;
            public int steam_id;
        }
    }

    public struct Button
    {
        public string url;
        public string label;
    }

    public struct DiscordRichPresence
    {
        public string state; /* max 128 bytes */
        public string details; /* max 128 bytes */
        public int? startTimestamp;
        public int? endTimestamp;
        public string largeImageKey; /* max 32 bytes */
        public string largeImageText; /* max 128 bytes */
        public string smallImageKey; /* max 32 bytes */
        public string smallImageText; /* max 128 bytes */

        public string partyId; /* max 128 bytes */
        public int partySize;
        public int partyMax;
        //public string matchSecret; /* max 128 bytes */
        public string joinSecret; /* max 128 bytes */
        //public string spectateSecret; /* max 128 bytes */

        public bool instance;
        public Button[] buttons;

        public string ToJson()
        {
            StringBuilder builder = new StringBuilder();
            JsonWriter writer = new JsonWriter(builder);

            writer.WriteObjectStart();

            writer.WritePropertyName("state");
            writer.Write(state);

            if (details != null)
            {
                writer.WritePropertyName("details");
                writer.Write(details);
            }

            if (startTimestamp.HasValue)
            {
                writer.WritePropertyName("timestamps");
                writer.WriteObjectStart();
                writer.WritePropertyName("start");
                writer.Write(startTimestamp.Value);
                if (endTimestamp.HasValue)
                {
                    writer.WritePropertyName("end");
                    writer.Write(endTimestamp.Value);
                }
                writer.WriteObjectEnd();
            }
            
            writer.WritePropertyName("assets");
            writer.WriteObjectStart();
            writer.WritePropertyName("large_image");
            writer.Write(largeImageKey ?? "null");
            if (largeImageText != null)
            {
                writer.WritePropertyName("large_text");
                writer.Write(largeImageText);
            }
            writer.WritePropertyName("small_image");
            writer.Write(smallImageKey ?? "null");
            if (smallImageText != null)
            {
                writer.WritePropertyName("small_text");
                writer.Write(smallImageText);
            }
            writer.WriteObjectEnd();

            if (joinSecret != null)
            {
                writer.WritePropertyName("secrets");
                writer.WriteObjectStart();
                writer.WritePropertyName("join");
                writer.Write(joinSecret);
                writer.WriteObjectEnd();
            }

            if (partyId != null)
            {
                writer.WritePropertyName("party");
                writer.WriteObjectStart();
                writer.WritePropertyName("id");
                writer.Write(partyId);

                if (partySize != 0)
                {
                    writer.WritePropertyName("size");
                    writer.WriteArrayStart();
                    writer.Write(partySize);
                    writer.Write(partyMax);
                    writer.WriteArrayEnd();
                }

                writer.WriteObjectEnd();
            }

            writer.WritePropertyName("instance");
            writer.Write(instance);

            if (buttons != null)
            {
                writer.WritePropertyName("buttons");
                writer.WriteArrayStart();
                foreach (var b in buttons)
                {
                    writer.WriteObjectStart();
                    writer.WritePropertyName("url");
                    writer.Write(b.url);
                    writer.WritePropertyName("label");
                    writer.Write(b.label);
                    writer.WriteObjectEnd();
                }
                writer.WriteArrayEnd();
            }
            
            writer.WriteObjectEnd();
            return builder.ToString();
        }

    }

    static class Extenstions
    {
        public static void Write(this NamedPipeClientStream stream, Packet data)
        {
            var b = data.ToBytes();
            stream.Write(b, 0, b.Length);
            stream.Flush();
        }
    }

    public enum OpCode
    {
        Handshake,
        Frame,
        Close,
        Ping,
        Pong
    }

    public class Packet
    {
        private OpCode _code;
        private object _data;
        public Packet(OpCode code, object data)
        {
            _code = code;
            _data = data;
        }
        public byte[] ToBytes()
        {
            byte[] d = Encoding.UTF8.GetBytes(_data.ToString());
            byte[] f = new byte[d.Length + (sizeof(int) * 2)];
            Array.Copy(BitConverter.GetBytes((int)_code), 0, f, 0, 4);
            Array.Copy(BitConverter.GetBytes(d.Length), 0, f, 4, 4);
            Array.Copy(d, 0, f, 8, d.Length);
            return f;
        }
    }
    
}
