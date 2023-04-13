using LitJson;
using System;
using System.Diagnostics;
using System.Text;
using VRage.Utils;

namespace Math0424.Discord
{
    public class Discord : IDisposable
    {
        public bool Ready { get; private set; }

        public DiscordIPC IPC { get; private set; }

        public Discord()
        {
            ReConnect();
        }

        public void ReConnect()
        {
            Ready = false;
            Dispose();
            IPC = new DiscordIPC(this);
            Ready = true;
        }

        public void Dispose()
        {
            Log("Disposing client connection");
            IPC?.Dispose();
        }

        public void SetRichPresence(DiscordRichPresence presence)
        {
            if (IPC.IsConnected)
            {
                string nonce = Guid.NewGuid().ToString();
                int pid = Process.GetCurrentProcess().Id;
                presence.instance = true;

                //Its short so ill just assemble it quickly
                string output = $"{{\"nonce\":\"{nonce}\",\"cmd\":\"SET_ACTIVITY\",\"args\":{{\"pid\":{pid},\"activity\":{presence.ToJson()}}}}}";
                IPC.Write(DiscordIPC.OpCode.Frame, output);
            }
        }

        public static void Log(object o)
        {
            MyLog.Default.WriteLineAndConsole($"DiscordRP {o ?? "null"}");
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

}
