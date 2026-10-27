// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Text;
using osu.Game.Beatmaps;
using LegacyDecoder = osu.Game.Beatmaps.Formats.Decoder;
using osu.Game.IO;

namespace osu.Game.Rulesets.Calibre.Tests.Logic
{
    /// <summary>
    /// Beatmaps osu!standard minimales, décodées par le vrai décodeur legacy : la conversion est ainsi testée
    /// sur les mêmes objets que ceux d'une vraie map.
    /// </summary>
    public static class TestBeatmaps
    {
        /// <summary>
        /// Un cercle à 1000 ms, un slider à 2000 ms, un spinner de 3000 à 4000 ms, puis un cercle à 5000 ms (nouveau combo).
        /// </summary>
        public const string MIXED_OBJECTS = @"osu file format v14

[General]
Mode: 0

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:8
ApproachRate:9
SliderMultiplier:1.4
SliderTickRate:1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
100,100,1000,1,0,0:0:0:0:
200,150,2000,2,0,L|300:150,1,100
256,192,3000,12,0,4000,0:0:0:0:
400,300,5000,5,0,0:0:0:0:
";

        /// <summary>
        /// Un slider horizontal de 280 px parcouru trois fois (deux répétitions) : 140 px par temps à 500 ms le temps,
        /// soit 1000 ms par parcours, un tick au milieu de chaque parcours et une fin à 4000 ms.
        /// </summary>
        public const string SLIDER_WITH_TICKS_AND_REPEATS = @"osu file format v14

[General]
Mode: 0

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:8
ApproachRate:9
SliderMultiplier:1.4
SliderTickRate:1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
100,100,1000,2,0,L|380:100,3,280
";

        public static Beatmap Decode(string osuFileContent)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(osuFileContent));
            using var reader = new LineBufferedReader(stream);

            return LegacyDecoder.GetDecoder<Beatmap>(reader).Decode(reader);
        }

        /// <summary>
        /// Décode puis convertit pour Calibre, en appliquant le processeur de beatmap et les valeurs par défaut (comme lazer).
        /// </summary>
        public static IBeatmap DecodePlayable(string osuFileContent)
            => new FlatWorkingBeatmap(Decode(osuFileContent)).GetPlayableBeatmap(new CalibreRuleset().RulesetInfo);
    }
}
