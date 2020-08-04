﻿using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using UnityEngine.TestTools;
using DMath;
using UnityEngine;
using static Compilers;
using static DMath.FXYRepo;
using static NUnit.Framework.Assert;
using ExFXY = System.Func<TEx<float>, TEx<float>>;
using static DMath.ExM;

namespace Tests {

    public static class ReflectFXY {
        private const float err = 0.0001f;
        [Test]
        public static void Basic() {
            Assert.AreEqual(FXY(x => 3f)(5f), 3f);
            Assert.AreEqual(FXY(x => Linear(ExC(6f), ExC(2f), x))(5f), 16f);
            var f = FXY(Cos);
            Debug.Log(f(Mathf.PI));
            Debug.Log(f(0));
            Debug.Log(f(Mathf.PI * 2f));
            f = FXY(t => SubMax0(t, Expression.Constant(4f)));
            AreEqual(f(3), 0);
            AreEqual(f(-3), 0);
            AreEqual(f(5), 1);
        }

        private static float softmax(float sharpness, float[] vals) {
            return (float) (vals.Select(x => x * Math.Exp(x * sharpness)).Sum() /
                   vals.Select(x => Math.Exp(x * sharpness)).Sum());
        }

        private static float shiftsoftmax(float sharpness, float pivot, FXY eq1, FXY eq2, float x) {
            return softmax(sharpness, new[] {eq1(x), eq1(pivot) + eq2(x) - eq2(pivot) });
        }

        [Test]
        public static void Softmax() {
            FXY fsoftmax1 = "softmax 1 { (+ 2 t) (8) }".Into<FXY>();
            Assert.AreEqual(softmax(1f, new[] {5f, 8f}), 7.85772238f, err);
            Assert.AreEqual(softmax(1f, new[] {5f, 8f}), fsoftmax1(3f), err);
            Assert.AreEqual(softmax(1f, new[] {9f, 8f}), fsoftmax1(7f), err);
        } 
        [Test]
        public static void ShiftSoftmax() {
            FXY eq1 = x => 5 * x;
            FXY eq2 = x => 2 * x;
            FXY shifter = FXY(SoftmaxShift(x => -1f, x => 10f, x => Mul<float>(x, 5f), x => Mul<float>(x, 2f)));
            Assert.AreEqual(shiftsoftmax(-1f, 10f, eq1, eq2, 110f), 250f, 1f);
            Assert.AreEqual(shiftsoftmax(-1f, 10f, eq1, eq2, 10.2f), shifter(10.2f), err);
            Assert.AreEqual(shiftsoftmax(-1f, 10f, eq1, eq2, 9.3f), shifter(9.3f), err);
        }

        private static void TestTPoints(FXY f, params (float t, float val)[] pts) {
            foreach (var (t, val) in pts) {
                AreEqual(f(t), val, err, $"f({t}) = {val}");
            }
        }
        [Test]
        public static void E() {
            void Test010(FXY e010, (float t, float val)[] otherPts, float ratio=0.5f) {
                TestTPoints(e010, otherPts);
                TestTPoints(e010, new[] { (0f, 0f), (ratio, 1f), (1f, 0f)});
            }
            FXY sineES = FXY(x => Smooth("sine-010", x));
            FXY smES = FXY(x => Smooth("smod-010", x));
            FXY quadES = FXY(x => EQuad0m10(ExC(0.3f), ExC(1f), x));
            Test010(sineES, new[] { (0.1f, 0.30901699f )});
            Test010(smES, new[] { (0.1f, 0.2f ), (0.7f, 0.6f)});
            Test010(quadES, new (float, float)[] {}, 0.3f);
            FXY smthin = FXY(x => Smooth("in-sine", x));
            FXY sc = FXY(x => SmoothIO("in-sine", "out-sine", ExC(6.0f), ExC(2.0f), ExC(3.0f), x));
            TestTPoints(sc, new[] {
                (-1f, 0f),
                (0.2f, smthin(0.1f)),
                (2.5f, 1f),
                (2.9f, 1f),
                //1-outsine(x) = insine(1-x)
                (3.3f, smthin(0.9f)),
                (6.5f, 0f)
            });
            FXY sc2 = FXY(x => SmoothIOe("in-sine", ExC(6.0f), ExC(2.0f), x));
            TestTPoints(sc2, new[] {
                (-1f, 0f),
                (0.2f, smthin(0.1f)),
                (2.5f, 1f),
                (3.9f, 1f),
                (4.3f, 1-smthin(0.15f)),
                (6.5f, 0f)
            });
            FXY smthio = FXY(x => Smooth("io-sine", x));
            sc = "smoothioe io-sine 4 0.5 x".Into<FXY>();
            TestTPoints(sc, 
                (-1f, 0f), 
                (0.2f, smthio(0.4f)), 
                (1f, 1f), 
                (3.3f, 1f), 
                (3.8f, 1-smthio(0.6f)), 
                (4.5f, 0f));
            
        }

        [Test]
        public static void FancyMod() {
            FXY f = FXY(t => ModWithPause(ExC(50f), ExC(20f), ExC(5f), t));
            Assert.AreEqual(f(5), 5, err);
            Assert.AreEqual(f(22), 20, err);
            Assert.AreEqual(f(26), 21, err);
            Assert.AreEqual(f(60), 5, err);
            Assert.AreEqual(f(77), 20, err);
            Assert.AreEqual(f(81), 21, err);
            FXY fh = "hmod 9 t".Into<FXY>();
            TestTPoints(fh, (0, 0), (9, 0), (1, 1), (5, 1), (8, 4));
            FXY fhn = "hnmod 9 t".Into<FXY>();
            TestTPoints(fhn, (0, 0), (9, 0), (1, 1), (5, -1), (8, -4));
        }

        private static Expression ExC(object x) => Expression.Constant(x);

        [Test]
        public static void FMod() {
            FXY f = FXY(t=>RangeSoftMod(ExC(4f), t));
            Assert.AreEqual(f(3), 3, err);
            Assert.AreEqual(f(4.1f), 3.9, err);
            Assert.AreEqual(f(11), -3f, err);
            Assert.AreEqual(f(-3f), -3f, err);
            Assert.AreEqual(f(-5f), -3f, err);
            Assert.AreEqual(f(12.04f), -3.96f, err);
            f = FXY(t=>RangeMod(ExC(4f), t));
            Assert.AreEqual(f(3), 3, err);
            Assert.AreEqual(f(4.1f), -3.9f, err);
            Assert.AreEqual(f(11), 3f, err);
            Assert.AreEqual(f(-3f), -3f, err);
            Assert.AreEqual(f(-5f), 3f, err);
            Assert.AreEqual(f(12.04f), -3.96f, err);
        }

        [Test]
        public static void Sines() {
            FXY sd = "dsine 2 4 t".Into<FXY>(); // = d/dx 4 sin(2pi x/2) = 4pi cos (pi x)
            Func<float, float> sdc = x => 4 * M.PI * Mathf.Cos(M.PI * x);
            Assert.AreEqual(sd(2.3f), sdc(2.3f), err);
            Assert.AreEqual(sd(9.75f), sdc(9.75f), err);
            Assert.AreEqual(sd(13.75f), sd(17.75f), 0.0001f);
            FXY cd = "dcosine 2 4 t".Into<FXY>();; // = d/dx 4 cos(2pi x/2) = -4pi sin (pi x)
            Func<float, float> cdc = x => -4 * M.PI * Mathf.Sin(M.PI * x);
            Assert.AreEqual(cd(2.3f), cdc(2.3f), err);
            Assert.AreEqual(cd(9.75f), cdc(9.75f), err);
            Assert.AreEqual(cd(13.75f), cd(17.75f), 0.0001f);
            Func<float, float> sc = x => 2f * Mathf.Sin(M.PI / 4 * x);
            FXY s = FXY(Ease("out-sine", 2, X()));
            AreEqual(s(0.2f), sc(0.2f), err);
            AreEqual(s(0.6f), sc(0.6f), err);
        }

        [Test]
        public static void Basics() {
            FXY f = "++ * 2 x".Into<FXY>();
            Assert.AreEqual(f(6), 13);
            f = "--- * 2 x 5".Into<FXY>();
            Assert.AreEqual(f(7), 8);
        }

        [Test]
        public static void TSmooth() {
            FXY ios = FXY(Ease("io-sine", 100, X()));
            Assert.AreEqual(ios(10), 4.89434837f/2f, err);
            Assert.AreEqual(ios(50), 50, err);
        }

        [Test]
        public static void THeight() {
            FXY h = FXY(x => Height(10f, 10, -10, x));
            Assert.AreEqual(h(0), 10f, err);
            Assert.AreEqual(h(1), 15f, err);
            Assert.AreEqual(h(2), 10f, err);
            Assert.AreEqual(h(3), -5f, err);
        }

        [Test]
        public static void TOpacity() {
            FXY op1 = "opacity 0.9 x".Into<FXY>();
            AreEqual(op1(5), 4.6f, err);
            FXY op2 = "opacity 0.1 x".Into<FXY>();
            AreEqual(op2(0.4f), 0.94f, err);
            FXY op3 = "opacity x x".Into<FXY>();
            AreEqual(op3(0.5f), 0.75f, err);
        }

        [Test]
        public static void Utils() {
            FXY f1 = "superpose 0.6 x 0.4 * 2 x".Into<FXY>();
            FXY f2 = "superposec 0.6 x * 2 x".Into<FXY>();
            AreEqual(0.28f, f1(0.2f));
            AreEqual(0.28f, f2(0.2f));
            AreEqual(0.56f, f1(0.4f));
            AreEqual(0.56f, f2(0.4f));
            f1 = "damp 5 0.1 x".Into<FXY>();
            AreEqual(4f, f1(4));
            AreEqual(3f, f1(3));
            AreEqual(5.1f, f1(6));
            AreEqual(6f, f1(15));
        }

        private const double ddelta = 0.0005f;

        private static double DerivativeAt(FXY func, float x, double delta = ddelta) =>
            ((double)func(x + (float)delta / 2f) - func(x - (float)delta / 2f)) / delta;
        [Test]
        public static void TestDerivatives() {
            var funcs = new[] {
                ("sine", "sine 3 1 t".Into<ExFXY>(), "dsine 3 1 t".Into<ExFXY>()),
                ("cosine", "cosine 3 1 t".Into<ExFXY>(), "dcosine 3 1 t".Into<ExFXY>()),
                ("einsine", EInSine, EDInSine),
                ("eoutsine", EOutSine, EDOutSine),
                ("eiosine", EIOSine, EDIOSine),
                ("elinear", ELinear, EDLinear),
                ("einquad", EInQuad, EDInQuad),
                ("esine010", ESine010, EDSine010),
                //("esoftmod010", ESoftmod010(X()), EDSoftmod010(X()))
            };
            foreach (var (name, exf, exfd) in funcs) {
                var (f, fd) = (FXY(exf), FXY(exfd));
                for (float x = -1.01f; x < 2f; x += 0.1f) {
                    AreEqual(fd(x), DerivativeAt(f, x), 0.01f, $"{name} at {x}");
                }
            }
        }

        [Test]
        public static void TestSWings() {
            var f = FXY(x => SWing2(ExC(1.33f / 4.53f), ExC(4.53f), ExC(-1.66f), ExC(2.42f), ExC(2.9f), x));
            TestTPoints(f, new[] {
                (0, 2.42f),
                (1.33f, -1.66f),
                (1.33f + (float)(0.5 * 5.28771100391), 2.9f),
                (4.53f, 2.42f),
                (1.33f+4.53f, -1.66f)
            });
        }

    }
}