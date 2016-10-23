using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HGV.AD.AutoDrafter
{
    public static class GSIIntinalizer
    {
        public static void Intinalize()
        {
            var steamKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam");
            var steamInstallPath = steamKey.GetValue("SteamPath") as string;
            var llibraryFilePath = string.Format(@"{0}\steamapps\libraryfolders.vdf", steamInstallPath);

            var serializer = new Gameloop.Vdf.VdfSerializer(new Gameloop.Vdf.VdfSerializerSettings() { IsWindows = true });
            var root = serializer.Deserialize(File.OpenText(llibraryFilePath));
            var node = root.Value as Gameloop.Vdf.VObject;

            var output = 0;
            var folders = node.Children()
                .Where(_ => int.TryParse(_.Key, out output) == true)
                .Select(_ => _.Value.ToString())
                .Select(_ => Path.Combine(_, @"steamapps\common\dota 2 beta\game\dota\cfg\"))
                .Where(_ => Directory.Exists(_) == true)
                .ToList();

            if (folders.Count == 0)
                throw new ApplicationException("Unable to find Steam library folder");

            foreach (var _ in folders)
            {
                var folder = Path.Combine(_, @"gamestate_integration");
                if (Directory.Exists(folder) == false)
                    Directory.CreateDirectory(folder);

                var file = Path.Combine(folder, "gamestate_integration_hgv_draft.cfg");
                var template = Properties.Resources.gamestate_integration_template.Replace("[PORT]", Properties.Settings.Default.DotaGSIPort);
                File.WriteAllText(file, template);
            }
        }
    }
}
