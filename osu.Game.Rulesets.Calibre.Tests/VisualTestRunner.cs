// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework;
using osu.Framework.Platform;
using osu.Game.Tests;

namespace osu.Game.Rulesets.Calibre.Tests
{
    public static class VisualTestRunner
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // Stockage dédié (%APPDATA%\calibre-visual-tests) au lieu de « osu », qui est celui de lazer :
            // le navigateur de tests embarque le osu.Game du NuGet, potentiellement plus récent que le lazer installé,
            // et ouvrirait (voire migrerait) la vraie base Realm du joueur.
            using (DesktopGameHost host = Host.GetSuitableDesktopHost(@"calibre-visual-tests"))
            {
                // « --screenshots <dossier> » régénère les captures du README au lieu d'ouvrir le navigateur.
                int screenshots = Array.IndexOf(args, "--screenshots");
                host.Run(screenshots >= 0 && screenshots + 1 < args.Length ? new ScreenshotRunner(args[screenshots + 1]) : new OsuTestBrowser());
                return 0;
            }
        }
    }
}
