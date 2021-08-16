using System.Collections.Generic;

namespace SERichPresence
{
    static class ServerDict
    {
        private static ServerEntry ksh = new ServerEntry("Keen Official Server", "ksh", "Offical SE Server");
        private static ServerEntry smd = new ServerEntry("Sigma Draconis", "sigma-draconis", "Sigma Draconis");
        private static ServerEntry stg = new ServerEntry("Stargate Dimensions", "stargate", "Stargate Dimensions");
        private static ServerEntry sto = new ServerEntry("Stone Industries", "stone-industries", "Stone Industries");

        private static Dictionary<string, ServerEntry> RegisteredServers = new Dictionary<string, ServerEntry>()
        {
            //stone industries
            ["162.83.222.54"] = sto,

            //stargate
            ["95.156.230.6"] = stg,

            //draconis servers
            ["51.81.154.23"] = smd,

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

        public static ServerEntry GetServerEntry(string entry)
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

    public class ServerEntry
    {
        public string Name;
        public string ImageID;
        public string LogoText;
        public ServerEntry(string name, string imageID, string logoText)
        {
            Name = name;
            ImageID = imageID;
            LogoText = logoText;
        }
    }

}
