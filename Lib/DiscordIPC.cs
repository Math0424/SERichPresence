﻿using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Math0424.Discord
{
    public class DiscordIPC : IDisposable
    {
        public bool IsConnected { get; private set; }

        private NamedPipeClientStream Stream;
        private Thread ReadThread;
        private CancellationTokenSource Cancel;

        public void Write(OpCode code, object data)
        {
            var b = new IPCPacket(code, data).ToBytes();
            Stream.Write(b, 0, b.Length);
            Stream.Flush();
        }

        internal DiscordIPC(Discord discord)
        {
            if (EstablishPipe())
            {
                EstablishHandshake();
                Discord.Log($"Established handshake with Discord client", LogLevel.Success);
                StartRead();
                IsConnected = true;
            }
        }

        private void StartRead()
        {
            Cancel = new CancellationTokenSource();

            ReadThread = new Thread(token => {

                CancellationTokenSource cancel = (CancellationTokenSource)token;
                while (!cancel.IsCancellationRequested && Stream != null && Stream.IsConnected)
                {
                    if (Stream.CanRead)
                    {
                        var code = Stream?.ReadInt32();
                        var length = Stream?.ReadInt32();

                        if (length.HasValue)
                        {
                            var data = new byte[length.Value];
                            Stream?.Read(data, 0, length.Value);

                            switch ((OpCode)(code.Value))
                            {
                                case OpCode.Close:
                                    Discord.Log($"Discord requested close of ipc, Disconnecting...", LogLevel.Info);
                                    IsConnected = false;
                                    Dispose();
                                    break;

                                case OpCode.Ping:
                                    Discord.Log($"Pong!", LogLevel.Info);
                                    Write(OpCode.Ping, Encoding.UTF8.GetString(data));
                                    break;

                                case OpCode.Frame:
                                case OpCode.Handshake:
                                case OpCode.Pong:
                                    break;
                                default:
                                    Discord.Log($"Recieved unknown code ({code}) with length {length}");
                                    Discord.Log($"Message: '{Encoding.UTF8.GetString(data)}'");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Discord.Log("Unable to read stream");
                    }

                    Thread.Sleep(50);
                }

            });

            ReadThread.Start(Cancel);
        }

        /// <summary>
        /// Send initial request to establish connection
        /// </summary>
        private void EstablishHandshake()
        {
            var handshake = new Handshake()
            {
                client_id = Statics.client_id.ToString(),
                v = "1",
                steam_id = 244850,
            };
            Write(OpCode.Handshake, JsonMapper.ToJson(handshake));
        }

        /// <summary>
        /// Connect to the pipe
        /// </summary>
        /// <returns>If no pipe is found return false</returns>
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
                        Discord.Log($"Found discord ipc pipe ({pipe[2]})", LogLevel.Info);
                        discordPipe = pipes[i];
                        break;
                    }
                }
            }
            if (discordPipe != null)
            {
                string pipeName = discordPipe.Substring(9);
                Discord.Log($"Establishing connection with '{pipeName}'", LogLevel.Info);
                Stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                Stream.Connect(1000);

                while (!Stream.IsConnected)
                    Thread.Sleep(20);

                return true;
            }
            Discord.Log("Failed to establish connection with Discord ipc!", LogLevel.Error);
            return false;
        }

        /// <summary>
        /// Discord handshake structure
        /// </summary>
        private struct Handshake
        {
            public string v;
            public string client_id;
            public int steam_id;
        }

        /// <summary>
        /// Close streams and read thread
        /// </summary>
        public void Dispose()
        {
            Stream?.Close();
            Cancel?.Cancel();
            ReadThread?.Abort();
        }
        public enum OpCode
        {
            Handshake,
            Frame,
            Close,
            Ping,
            Pong
        }

        private class IPCPacket
        {
            private OpCode _code;
            private object _data;
            public IPCPacket(OpCode code, object data)
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
}
