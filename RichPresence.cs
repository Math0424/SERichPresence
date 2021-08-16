﻿using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SERichPresence.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Plugins;
using VRage.Utils;

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
            client = new Discord(876259975733850192);
            startTime = Now;
        }

        public void Update()
        {
            if (client.Ready)
            {
                lastUpdate++;
                if (lastUpdate == 240)
                {
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
                                details = $"{(MyAPIGateway.Session.CreativeMode ? "Creative" : "Survival")} {(vanilla ? "vanilla" : "modded")} singleplayer",
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
                            ServerEntry entry = ServerDict.GetServerEntry(JoinString);
                            MyLog.Default.WriteLineAndConsole(JoinString);
                            if (entry != null)
                            {
                                client.SetRichPresence(new DiscordRichPresence()
                                {
                                    details = entry.Name,
                                    state = $"{(vanilla ? "" : $"{modCount} mods, ")}{MyAPIGateway.Multiplayer.Players.Count}/{MyAPIGateway.Session.MaxPlayers} players",
                                    largeImageKey = entry.ImageID,
                                    largeImageText = entry.LogoText,
                                    startTimestamp = startTime,
                                    buttons = new Button[]
                                    {
                                        new Button() { label = "Join Game", url = JoinString.Replace("steam://", "steam://connect/") }
                                    }
                                });
                            }
                            else
                            {
                                client.SetRichPresence(new DiscordRichPresence()
                                {
                                    details = $"{(MyAPIGateway.Session.CreativeMode ? "Creative" : "Survival")} {(vanilla ? "vanilla" : "modded")} multiplayer",
                                    state = $"{(vanilla ? "" : $"{modCount} mods, ")}{MyAPIGateway.Multiplayer.Players.Count}/{MyAPIGateway.Session.MaxPlayers} players",
                                    largeImageKey = "se-logo",
                                    startTimestamp = startTime,
                                });
                            }
                        }
                        
                    }
                    lastUpdate = 0;
                }
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