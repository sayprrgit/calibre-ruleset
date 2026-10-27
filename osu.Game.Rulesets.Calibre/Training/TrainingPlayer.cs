// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Game.Scoring;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Calibre.Training
{
    /// <summary>
    /// Player des exercices : une partie normale, écran de résultats et mise à jour du profil compris.
    /// </summary>
    public partial class TrainingPlayer : SoloPlayer
    {
        public TrainingPlayer()
            : base(new PlayerConfiguration())
        {
        }

        // La beatmap d'un exercice est générée à la volée et n'existe pas dans la base de lazer : on n'y importe pas le score.
        protected override Task ImportScore(Score score) => Task.CompletedTask;
    }
}
