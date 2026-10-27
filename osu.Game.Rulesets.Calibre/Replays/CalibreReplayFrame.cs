// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Replays.Types;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Replays
{
    /// <summary>
    /// Frame de replay, convertible au format legacy (interface attendue par lazer pour les frames de replay).
    /// </summary>
    /// <remarks>
    /// lazer n'écrit de fichier .osr que pour les quatre rulesets officiels (Player.ImportScore) :
    /// un replay Calibre vit en mémoire jusqu'à l'écran de résultats, où les analyses le consomment.
    /// </remarks>
    public class CalibreReplayFrame : ReplayFrame, IConvertibleReplayFrame
    {
        /// <summary>
        /// Position du curseur dans l'espace de l'aire de jeu (512x384).
        /// </summary>
        public Vector2 Position;

        public List<CalibreAction> Actions = new List<CalibreAction>();

        public CalibreReplayFrame()
        {
        }

        public CalibreReplayFrame(double time, Vector2 position, params CalibreAction[] actions)
            : base(time)
        {
            Position = position;
            Actions.AddRange(actions);
        }

        // Codage legacy des boutons, repris d'osu!stable :
        //   M1 = Left1, M2 = Right1, K1 = Left1 | Left2, K2 = Right1 | Right2.
        // Le bit « 2 » distingue donc une touche clavier d'un bouton souris, ce qui conserve K1/K2/M1/M2 dans le replay
        // (seule la combinaison simultanée K1 + M1, ou K2 + M2, ne peut pas être représentée).

        public void FromLegacy(LegacyReplayFrame currentFrame, IBeatmap beatmap, ReplayFrame? lastFrame = null)
        {
            Position = currentFrame.Position;

            if (currentFrame.MouseLeft2)
                Actions.Add(CalibreAction.Key1);
            else if (currentFrame.MouseLeft1)
                Actions.Add(CalibreAction.Mouse1);

            if (currentFrame.MouseRight2)
                Actions.Add(CalibreAction.Key2);
            else if (currentFrame.MouseRight1)
                Actions.Add(CalibreAction.Mouse2);
        }

        public LegacyReplayFrame ToLegacy(IBeatmap beatmap)
        {
            var state = ReplayButtonState.None;

            foreach (var action in Actions)
            {
                switch (action)
                {
                    case CalibreAction.Key1:
                        state |= ReplayButtonState.Left1 | ReplayButtonState.Left2;
                        break;

                    case CalibreAction.Mouse1:
                        state |= ReplayButtonState.Left1;
                        break;

                    case CalibreAction.Key2:
                        state |= ReplayButtonState.Right1 | ReplayButtonState.Right2;
                        break;

                    case CalibreAction.Mouse2:
                        state |= ReplayButtonState.Right1;
                        break;
                }
            }

            return new LegacyReplayFrame(Time, Position.X, Position.Y, state);
        }

        public override bool IsEquivalentTo(ReplayFrame other)
            => other is CalibreReplayFrame frame && Time == frame.Time && Position == frame.Position && Actions.SequenceEqual(frame.Actions);
    }
}
