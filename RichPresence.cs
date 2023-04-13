using Math0424.Discord;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Linq;
using VRage.Plugins;

namespace SERichPresence
{
    public class RichPresence : IPlugin
    {
        private Discord client;
        private int lastUpdate = 0;
        private int startTime = 230;
        private State _state = State.Menu;
        private string JoinString;

        private State state
        {
            set
            {
                if (value != _state)
                    startTime = Now;
                _state = value;
            }
        }

        private int Now
        {
            get { return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds; }
        }

        public void Init(object gameInstance)
        {
            client = new Discord();
            startTime = Now;
        }

        public void Update()
        {
            lastUpdate++;
            if (client.IPC?.IsConnected ?? false)
            {
                if (lastUpdate >= 240)
                {
                    lastUpdate = 0;
                    if (MySession.Static == null || !MySession.Static.Ready)
                    {
                        switch (MyScreenManager.Screens.Last()?.GetFriendlyName())
                        {
                            case "MyGuiScreenProgress":
                            case "MyGuiScreenLoading":
                            case "MyGuiScreenLoadSandbox":
                            case "MyGuiScreenProgressAsync":
                                state = State.Loading;
                                SimpleMessage("Loading into game");
                                break;

                            case "MyGuiScreenMessageBox":
                                state = State.Loading;
                                SimpleMessage("Downloading mods...");
                                break;

                            default:
                                state = State.Menu;
                                SimpleMessage("In Main Menu");
                                break;
                        }
                    }
                    else
                    {
                        state = State.Ingame;
                        
                        int modCount = MySession.Static.Mods.Count;
                        bool vanilla = modCount == 0;
                        if (MySession.Static.IsPausable()) //singleplayer
                        {
                            client.SetRichPresence(new DiscordRichPresence()
                            {
                                details = $"{(MyAPIGateway.Session.CreativeMode ? "Creative" : "Survival")} {(vanilla ? "vanilla" : "modded")} SP",
                                state = $"{(vanilla ? "" : $"{modCount} mods")}",
                                largeImageKey = "se-logo",
                                startTimestamp = startTime,
                            });
                        }
                        else //multiplayer
                        {
                            if (MyLocalCache.GetLastSession()?.ConnectionString != null)
                            {
                                JoinString = MyLocalCache.GetLastSession().ConnectionString;
                            }
                            var entry = ServerDict.GetServerEntry(JoinString);
                            if (entry.HasValue)
                            {
                                var presence = entry.Value;
                                presence.buttons = new Button[]
                                {
                                    new Button() { label = "Join Game", url = JoinString.Replace("steam://", "steam://connect/") }
                                };
                                presence.state = $"{(vanilla ? "" : $"{modCount} mods, ")}{MyAPIGateway.Multiplayer.Players.Count}/{MyAPIGateway.Session.MaxPlayers} players";
                                presence.startTimestamp = startTime;

                                client.SetRichPresence(presence);
                            }
                            else
                            {
                                client.SetRichPresence(new DiscordRichPresence()
                                {
                                    details = $"{(MyAPIGateway.Session.CreativeMode ? "Creative" : "Survival")} {(vanilla ? "vanilla" : "modded")} MP",
                                    state = $"{(vanilla ? "" : $"{modCount} mods, ")}{MyAPIGateway.Multiplayer.Players.Count}/{MyAPIGateway.Session.MaxPlayers} players",
                                    largeImageKey = "se-logo",
                                    startTimestamp = startTime,
                                });
                            }
                        }
                    }
                }
            }
            else if(lastUpdate >= 1000)
            {
                lastUpdate = 0;
                client.ReConnect();
            }
        }

        private void SimpleMessage(string text)
        {
            client.SetRichPresence(new DiscordRichPresence()
            {
                state = text,
                largeImageKey = "se-logo",
                startTimestamp = startTime,
            });
        }

        private enum State
        {
            Menu,
            Loading,
            Ingame,
        }

        public void Dispose()
        {
            client.Dispose();
        }

    }
}
