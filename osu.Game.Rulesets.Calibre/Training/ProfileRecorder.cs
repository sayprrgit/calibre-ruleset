// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Calibre.Training
{
    /// <summary>
    /// Met à jour le profil du joueur à la fin de chaque partie jouée (ni replay, ni autoplay, ni partie abandonnée ou ratée).
    /// Une partie ratée est écartée : lazer n'enregistre plus de HitEvents après l'échec, son bilan serait tronqué.
    /// </summary>
    /// <remarks>
    /// Les HitEvents sont copiés sur le thread de jeu à la fin de la partie ; l'analyse et l'écriture du profil
    /// se font ensuite en tâche de fond, jamais sur le thread de mise à jour.
    /// </remarks>
    public partial class ProfileRecorder : Component
    {
        [Resolved]
        private DrawableCalibreRuleset drawableRuleset { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private ScoreProcessor? scoreProcessor { get; set; }

        [Resolved(canBeNull: true)]
        private GameplayState? gameplayState { get; set; }

        [Resolved]
        private Storage storage { get; set; } = null!;

        private readonly IBindable<bool> hasCompleted = new BindableBool();

        private bool recorded;

        /// <summary>
        /// Tâche de mise à jour lancée à la fin de la partie, pour les tests.
        /// </summary>
        public Task? UpdateTask { get; private set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Hors d'un Player (scènes de test du playfield seul), il n'y a pas de partie à enregistrer.
            if (scoreProcessor == null)
                return;

            hasCompleted.BindTo(scoreProcessor.HasCompleted);
            hasCompleted.BindValueChanged(completed =>
            {
                if (completed.NewValue)
                    record();
            });
        }

        private void record()
        {
            if (recorded || drawableRuleset.HasReplayLoaded.Value || gameplayState?.HasFailed == true)
                return;

            recorded = true;

            var hitEvents = scoreProcessor!.HitEvents.ToArray();
            var beatmap = drawableRuleset.Beatmap;

            UpdateTask = Task.Run(() =>
            {
                try
                {
                    var patterns = PatternClassifier.Summarise(TimedHits.From(beatmap, hitEvents));
                    ProfileStore.Update(storage, profile => profile.Apply(patterns, DateTimeOffset.Now));
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Échec de la mise à jour du profil Calibre.");
                }
            });
        }
    }
}
