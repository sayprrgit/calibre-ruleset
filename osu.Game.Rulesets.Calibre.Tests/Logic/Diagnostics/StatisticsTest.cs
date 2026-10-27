// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Diagnostics
{
    [TestFixture]
    public class StatisticsTest
    {
        [Test]
        public void TestDescriptiveStatistics()
        {
            var statistics = OffsetStatistics.From(new double[] { 2, 4, 4, 4, 5, 5, 7, 9 });

            Assert.That(statistics.Count, Is.EqualTo(8));
            Assert.That(statistics.Mean, Is.EqualTo(5).Within(1e-12));

            // Somme des carrés des écarts = 32 : variance d'échantillon 32 / 7, écart-type de population 2, donc UR 20.
            Assert.That(statistics.Variance, Is.EqualTo(32.0 / 7).Within(1e-12));
            Assert.That(statistics.UnstableRate, Is.EqualTo(20).Within(1e-12));
        }

        [Test]
        public void TestTooFewValues()
        {
            Assert.That(OffsetStatistics.From(Array.Empty<double>()).Mean, Is.Null);

            var single = OffsetStatistics.From(new double[] { 12 });
            Assert.That(single.Mean, Is.EqualTo(12));
            Assert.That(single.Variance, Is.Null);
            Assert.That(single.UnstableRate, Is.Zero);
        }

        [Test]
        public void TestRegressionSlope()
        {
            Assert.That(LinearRegression.Slope(new double[] { 0, 1, 2, 3 }, new double[] { 1, 3, 5, 7 }), Is.EqualTo(2).Within(1e-12));
            Assert.That(LinearRegression.Slope(new double[] { 1, 1 }, new double[] { 0, 5 }), Is.Null);
            Assert.That(LinearRegression.Slope(new double[] { 1 }, new double[] { 0 }), Is.Null);
        }

        [Test]
        public void TestLogGamma()
        {
            Assert.That(StatisticalTests.LogGamma(5), Is.EqualTo(Math.Log(24)).Within(1e-12));
            Assert.That(StatisticalTests.LogGamma(0.5), Is.EqualTo(0.5 * Math.Log(Math.PI)).Within(1e-12));
        }

        [Test]
        public void TestRegularizedIncompleteBeta()
        {
            Assert.That(StatisticalTests.RegularizedIncompleteBeta(0.3, 1, 1), Is.EqualTo(0.3).Within(1e-12));
            Assert.That(StatisticalTests.RegularizedIncompleteBeta(0.5, 2, 2), Is.EqualTo(0.5).Within(1e-12));

            // I_x(1, b) = 1 - (1 - x)^b.
            Assert.That(StatisticalTests.RegularizedIncompleteBeta(0.3, 1, 3), Is.EqualTo(1 - Math.Pow(0.7, 3)).Within(1e-12));
            Assert.That(StatisticalTests.RegularizedIncompleteBeta(0.9, 1, 3), Is.EqualTo(1 - Math.Pow(0.1, 3)).Within(1e-12));
        }

        // Valeurs critiques des tables de la loi de Student.
        [TestCase(2.228138852, 10, 0.05)]
        [TestCase(1.812461123, 10, 0.10)]
        [TestCase(12.70620474, 1, 0.05)]
        [TestCase(2.570581836, 5, 0.05)]
        [TestCase(1.959963985, 1e7, 0.05)]
        [TestCase(0, 10, 1)]
        public void TestStudentPValues(double t, double df, double expected)
        {
            Assert.That(StatisticalTests.StudentTwoSidedPValue(t, df), Is.EqualTo(expected).Within(1e-6));
            Assert.That(StatisticalTests.StudentTwoSidedPValue(-t, df), Is.EqualTo(expected).Within(1e-6));
        }

        // Valeurs critiques des tables de la loi de Fisher (queue haute 5 % ou 2,5 %, donc p bilatérale 10 % ou 5 %).
        [TestCase(2.978237016, 10, 10, 0.10)]
        [TestCase(3.289056, 5, 20, 0.05)]
        [TestCase(1, 12, 12, 1)]
        public void TestFisherPValues(double f, double d1, double d2, double expected)
        {
            Assert.That(StatisticalTests.FTwoSidedPValue(f, d1, d2), Is.EqualTo(expected).Within(1e-5));
        }

        [Test]
        public void TestWelchTTest()
        {
            // Deux séries de variance 2,5 et de moyennes 3 et 5 : t = 2 avec 8 degrés de liberté.
            var first = OffsetStatistics.From(new double[] { 1, 2, 3, 4, 5 });
            var second = OffsetStatistics.From(new double[] { 3, 4, 5, 6, 7 });

            var result = StatisticalTests.WelchTTest(first, second);

            Assert.That(result.Statistic, Is.EqualTo(2).Within(1e-12));
            Assert.That(result.DegreesOfFreedom, Is.EqualTo(8).Within(1e-12));
            Assert.That(result.PValue, Is.EqualTo(0.080516).Within(1e-5));
        }

        [Test]
        public void TestWelchTTestWithoutVariance()
        {
            var zeros = OffsetStatistics.From(new double[] { 0, 0, 0 });
            var tens = OffsetStatistics.From(new double[] { 10, 10, 10 });

            Assert.That(StatisticalTests.WelchTTest(zeros, tens).PValue, Is.Zero);
            Assert.That(StatisticalTests.WelchTTest(zeros, zeros).PValue, Is.EqualTo(1));
        }
    }
}
