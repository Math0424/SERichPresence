using Math0424.Discord;
using System.Collections.Generic;

namespace SERichPresence
{
    static class ServerDict
    {
        private static DiscordRichPresence ksh = new DiscordRichPresence() { details = "Keen Official Server", largeImageKey = "ksh" };

        private static DiscordRichPresence smd = new DiscordRichPresence() { details = "Sigma Draconis", largeImageKey = "sigma-draconis" };
        private static DiscordRichPresence smdi = new DiscordRichPresence() { details = "Sigma Draconis Impossible", largeImageKey = "draconis-impossible", smallImageKey = "rage" };

        private static DiscordRichPresence stg = new DiscordRichPresence() { details = "Stargate Dimensions", largeImageKey = "stargate" };
        private static DiscordRichPresence sto = new DiscordRichPresence() { details = "Stone Industries", largeImageKey = "stone-industries" };

        private static DiscordRichPresence stc = new DiscordRichPresence() { details = "Starcore", largeImageKey = "starcore" };

        private static Dictionary<string, DiscordRichPresence> RegisteredServers = new Dictionary<string, DiscordRichPresence>()
        {
            //starcore
            ["136.50.243.88"] = stc,
            ["184.64.201.192"] = stc,

            //stone industries
            ["162.83.222.54"] = sto,

            //stargate
            ["95.156.230.112"] = stg,
            ["95.156.230.6"] = stg,
            ["109.230.239.41"] = stg,
            ["95.156.230.180"] = stg,

            //draconis servers
            ["51.81.154.232"] = smd,
            ["51.81.154.231"] = smd,
            ["51.81.154.236"] = smd,

            //draconis impossible
            ["51.81.154.234"] = smdi,
            ["52.82.154.235"] = smdi,

            //ksh servers
            ["139.99.144.161"] = ksh,
            ["54.39.49.22"] = ksh,
            ["145.239.150.79"] = ksh,
            ["193.70.6.73"] = ksh,
            ["181.215.243.242"] = ksh,
            ["8.39.235.44"] = ksh,
            ["185.70.107.50"] = ksh,
            ["217.182.196.211"] = ksh,
            ["77.78.100.52"] = ksh,
            ["192.169.93.178"] = ksh,
            ["89.34.97.130"] = ksh,
            ["51.89.155.50"] = ksh,
            ["149.202.87.68"] = ksh,
        };

        public static DiscordRichPresence? GetServerEntry(string entry)
        {
            if (entry == null)
                return null;

            string ip = entry.Substring(0, entry.LastIndexOf(":")).Replace("steam://", "");
            if (RegisteredServers.ContainsKey(ip))
            {
                return RegisteredServers[ip];
            }
            return null;
        }

    }

}
