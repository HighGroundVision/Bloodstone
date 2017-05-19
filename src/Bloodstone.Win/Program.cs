using Dota2GSI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dota2GSI.Nodes;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace HGV.AD.AutoDrafter
{
    static class Program
    {
        private const DOTA_GameState GS_WAITING = DOTA_GameState.DOTA_GAMERULES_STATE_WAIT_FOR_PLAYERS_TO_LOAD;
        private const DOTA_GameState GS_DRAFTING = DOTA_GameState.DOTA_GAMERULES_STATE_HERO_SELECTION;
 
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            GSIIntinalizer.Intinalize();

            var port = int.Parse(Properties.Settings.Default.DotaGSIPort);

            var gsl = new GameStateListener(port);
            gsl.NewGameState += new NewGameStateHandler(OnNewGameState);

            var result = gsl.Start();
            if (result == true)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
            }
            else
            {
                Environment.Exit(42);
            }
        }
        

        static void OnNewGameState(GameState gs)
        {
            try
            {
                if (gs.Previously.Map.GameState == GS_WAITING && gs.Map.GameState == GS_DRAFTING)
                {
                    // gs.Hero.ID

                    Thread.Sleep(100);

                    var draftAnalyzer = new DraftAnalyzer();

                    var template = ScreenCapture.CaptureApplication();

                    Thread.Sleep(3000); // Wait for ability draft screen to load. Yes it takes 3 seconds to finish the load animation.

                    var draft = ScreenCapture.CaptureApplication();

                    var ultimates = draftAnalyzer.ExtractUltimates(template, draft);

                    if (ultimates.Count == 12)
                    {
                        var key = string.Join(",", ultimates.Select(_ => _.Key));
                        var url = string.Format("http://www.abilitydrafter.com/Draft?key={0}", key);
                        Process.Start(url);
                    }
                    else
                    {
                        var logsDir = Path.Combine(Application.StartupPath, "logs", gs.Map.MatchID.ToString());
                        if (Directory.Exists(logsDir) == false)
                            Directory.CreateDirectory(logsDir);

                        template.Save(Path.Combine(logsDir, "template.png"), ImageFormat.Png);
                        draft.Save(Path.Combine(logsDir, "draft.png"), ImageFormat.Png);

                        foreach (var item in ultimates)
                            item.Value.Save(Path.Combine(logsDir, string.Format("{0}.png", item.Key)), ImageFormat.Png);
                    }
                }
            }
            catch (Exception /*ex*/)
            {
                // Next Time Gadget. NEXT TIME!
            }
        }
    }
}
