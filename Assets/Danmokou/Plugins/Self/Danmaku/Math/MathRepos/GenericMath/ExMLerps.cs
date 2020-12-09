﻿using System;
using System.Linq.Expressions;
using DMK.Core;
using DMK.Expressions;
using Ex = System.Linq.Expressions.Expression;
using static DMK.Expressions.ExUtils;
using static DMK.Expressions.ExMHelpers;
using tfloat = DMK.Expressions.TEx<float>;
using tbool = DMK.Expressions.TEx<bool>;
using tv2 = DMK.Expressions.TEx<UnityEngine.Vector2>;
using tv3 = DMK.Expressions.TEx<UnityEngine.Vector3>;
using trv2 = DMK.Expressions.TEx<DMK.DMath.V2RV2>;
using efloat = DMK.Expressions.EEx<float>;
using ev2 = DMK.Expressions.EEx<UnityEngine.Vector2>;
using ev3 = DMK.Expressions.EEx<UnityEngine.Vector3>;
using erv2 = DMK.Expressions.EEx<DMK.DMath.V2RV2>;
using ExBPY = System.Func<DMK.Expressions.TExPI, DMK.Expressions.TEx<float>>;
using static DMK.DMath.Functions.ExM;
using static DMK.DMath.Functions.ExMConditionals;
using static DMK.DMath.Functions.ExMConversions;
using static DMK.DMath.Functions.ExMMod;

namespace DMK.DMath.Functions {
/// <summary>
/// See <see cref="DMK.DMath.Functions.ExM"/>. This class contains functions related to lerping and smoothing.
/// </summary>
public static class ExMLerps {
    /// <summary>
    /// Lerp between two functions.
    /// <br/>Note: Unless marked otherwise, all lerp functions clamp the controller.
    /// </summary>
    /// <param name="zeroBound">Lower bound for lerp controller</param>
    /// <param name="oneBound">Upper bound for lerp controller</param>
    /// <param name="controller">Lerp controller</param>
    /// <param name="f1">First function (when controller leq zeroBound, return this)</param>
    /// <param name="f2">Second function (when controller geq oneBound, return this)</param>
    /// <returns></returns>
    public static TEx<T> Lerp<T>(efloat zeroBound, efloat oneBound, efloat controller, TEx<T> f1, TEx<T> f2) => 
        EEx.Resolve(zeroBound, oneBound, controller, (z, o, c) => {
            var rc = VFloat();
            return Ex.Block(new[] {rc},
                rc.Is(Clamp(z, o, c).Sub(z).Div(o.Sub(z))),
                rc.Mul(f2).Add(rc.Complement().Mul(f1))
            );
        });

    /// <summary>
    /// Lerp between a value for easy difficulty and lunatic difficulty.
    /// </summary>
    public static TEx<T> LerpD<T>(TEx<T> f1, TEx<T> f2) => Lerp(FixedDifficulty.Easy.Counter(),
        FixedDifficulty.Lunatic.Counter(), ExMDifficulty.Dc(), f1, f2);

    /// <summary>
    /// Lerp between two functions with 0-1 as the bounds for the controller.
    /// </summary>
    public static TEx<T> Lerp01<T>(efloat controller, TEx<T> f1, TEx<T> f2) => Lerp(E0, E1, controller, f1, f2);
    /// <summary>
    /// Lerp between two functions with smoothing applied to the controller.
    /// </summary>
    public static TEx<T> LerpSmooth<T>([LookupMethod] Func<tfloat, tfloat> smoother, 
        efloat zeroBound, efloat oneBound, efloat controller, TEx<T> f1, TEx<T> f2) 
        => EEx.Resolve(zeroBound, oneBound, controller, (z, o, c) => {
            var rc = VFloat();
            return Ex.Block(new[] {rc},
                rc.Is(Smooth(smoother, Clamp(z, o, c).Sub(z).Div(o.Sub(z)))),
                rc.Mul(f2).Add(rc.Complement().Mul(f1))
            );
        });
    
    /// <summary>
    /// Lerp between two functions. The controller is not clamped.
    /// </summary>
    /// <param name="zeroBound">Lower bound for lerp controller</param>
    /// <param name="oneBound">Upper bound for lerp controller</param>
    /// <param name="controller">Lerp controller</param>
    /// <param name="f1">First function</param>
    /// <param name="f2">Second function</param>
    /// <returns></returns>
    public static TEx<T> LerpU<T>(efloat zeroBound, efloat oneBound, efloat controller, TEx<T> f1, TEx<T> f2) => 
        EEx.Resolve(zeroBound, oneBound, controller, (z, o, c) => {
            var rc = VFloat();
            return Ex.Block(new[] {rc},
                rc.Is(c.Sub(z).Div(o.Sub(z))),
                rc.Mul(f2).Add(rc.Complement().Mul(f1))
            );
        });
    
    /// <summary>
    /// Lerp between three functions.
    /// Between zeroBound and oneBound, lerp from the first to the second.
    /// Between zeroBound2 and oneBound2, lerp from the second to the third.
    /// </summary>
    public static TEx<T> Lerp3<T>(efloat zeroBound, efloat oneBound,
        efloat zeroBound2, efloat oneBound2, efloat controller, TEx<T> f1, TEx<T> f2, TEx<T> f3) => 
        EEx.Resolve(zeroBound, oneBound, zeroBound2, oneBound2, controller, (z1, o1, z2, o2, c) => 
            Ex.Condition(c.LT(z2), Lerp(z1, o1, c, f1, f2), Lerp(z2, o2, c, f2, f3)));

    /// <summary>
    /// Lerp between three functions.
    /// Between zeroBound and oneBound, lerp from the first to the second.
    /// Between oneBound and twoBound, lerp from the second to the third.
    /// </summary>
    public static TEx<T> Lerp3c<T>(tfloat zeroBound, efloat oneBound, tfloat twoBound,
        tfloat controller, TEx<T> f1, TEx<T> f2, TEx<T> f3) => EEx.Resolve(oneBound,
        ob => Lerp3(zeroBound, ob, ob, twoBound, controller, f1, f2, f3));

    /// <summary>
    /// Lerp between two functions.
    /// Between zeroBound and oneBound, lerp from the first to the second.
    /// Between oneBound2 and zeroBound2, lerp from the second back to the first.
    /// </summary>
    /// <param name="zeroBound">Lower bound for lerp controller</param>
    /// <param name="oneBound">Upper bound for lerp controller</param>
    /// <param name="oneBound2">Upper bound for lerp controller</param>
    /// <param name="zeroBound2">Lower bound for lerp controller</param>
    /// <param name="controller">Lerp controller</param>
    /// <param name="f1">First function (when controller leq zeroBound, return this)</param>
    /// <param name="f2">Second function (when controller geq oneBound, return this)</param>
    /// <returns></returns>
    public static TEx<T> LerpBack<T>(tfloat zeroBound, tfloat oneBound, tfloat oneBound2,
        tfloat zeroBound2, tfloat controller, TEx<T> f1, TEx<T> f2) =>
        Lerp3(zeroBound, oneBound, oneBound2, zeroBound2, controller, f1, f2, f1);

    /// <summary>
    /// Lerp between many functions.
    /// </summary>
    public static TEx<T> LerpMany<T>((tfloat bd, TEx<T> val)[] points, efloat controller) => EEx.Resolve(controller,
        x => {
            Ex ifLt = points[0].val;
            for (int ii = 0; ii < points.Length - 1; ++ii) {
                ifLt = Ex.Condition(x.LT(points[ii].bd), ifLt,
                    LerpU(points[ii].bd, points[ii + 1].bd, x, points[ii].val, points[ii + 1].val));
            }
            return Ex.Condition(x.LT(points[points.Length - 1].bd), ifLt, points[points.Length - 1].val);
        });

    /// <summary>
    /// Select one of an array of values. If OOB, selects the last element.
    /// Note: this expands to (if i = 0) arr[0] (if i = 1) arr[1] ....
    /// This may sound stupid, but since each value is a function, there's no way to actually store it in an array.
    /// </summary>
    public static TEx<T> Select<T>(tfloat index, TEx<T>[] points) => EEx.Resolve((EEx<int>) ((Ex) index).As<int>(),
        i => {
            Ex ifNeq = points[points.Length - 1];
            for (int ii = points.Length - 2; ii >= 0; --ii) {
                ifNeq = Ex.Condition(Ex.Equal(i, ExC(ii)), points[ii], ifNeq);
            }
            return ifNeq;
        });

    /// <summary>
    /// Return 0 if the controller is leq the lower bound, 1 if the controller is geq the lower bound, and
    /// a linear interpolation in between.
    /// </summary>
    public static tfloat SStep(efloat zeroBound, tfloat oneBound, efloat controller) => EEx.Resolve(zeroBound, controller, (z, c) => Clamp01(c.Sub(z).Div(oneBound.Sub(z))));
    
    /// <summary>
    /// Provide a soft ceiling for the value, multiplying any excess by the value RATIO.
    /// </summary>
    public static tfloat Damp(efloat ceiling, tfloat ratio, efloat value) => EEx.Resolve(ceiling, value, (c, x) =>
        If(x.GT(c), c.Add(ratio.Mul(x.Sub(c))), x));
    
    public static Func<TExPI, TEx<T>> LerpT<T>(ExBPY zeroBound, ExBPY oneBound, 
        Func<TExPI, TEx<T>> f1, Func<TExPI, TEx<T>> f2) => b => 
        Lerp(zeroBound(b), oneBound(b), BPYRepo.T()(b), f1(b), f2(b));
    
    
    public static Func<TExPI, TEx<T>> LerpT3<T>(ExBPY zeroBound, ExBPY oneBound, ExBPY twoBound, ExBPY threeBound, 
        Func<TExPI, TEx<T>> f1, Func<TExPI, TEx<T>> f2, Func<TExPI, TEx<T>> f3) => b => 
        Lerp3(zeroBound(b), oneBound(b), twoBound(b), threeBound(b), BPYRepo.T()(b), f1(b), f2(b), f3(b));
    
    
    /// <summary>
    /// Return one of two functions depending on the input,
    /// adjusting the switch variable by the reference switch amount if returning the latter function.
    /// </summary>
    /// <param name="switchVar">The variable upon which pivoting is performed. Should be either "p" (firing index) or "t" (time).</param>
    /// <param name="at">Reference</param>
    /// <param name="f1">Function when <c>t \leq at</c></param>
    /// <param name="f2">Function when <c>t \gt at</c></param>
    /// <returns></returns>
    public static Func<TExPI, TEx<T>> SwitchH<T>(ExBPY switchVar, ExBPY at, Func<TExPI, TEx<T>> f1, Func<TExPI, TEx<T>> f2) => bpi => {
        var pivot = VFloat();
        var cold = new TExPI();
        return Ex.Block(new[] { pivot }, 
            pivot.Is(at(bpi)),
            Ex.Condition(Ex.GreaterThan(switchVar(bpi), pivot), 
                Ex.Block(new ParameterExpression[] { cold },
                    Ex.Assign(cold, bpi),
                    SubAssign(switchVar(cold), pivot),
                    f2(cold)
                ), f1(bpi))
        );
    };
    
    /// <summary>
    /// Apply a ease function on top of a target derivative function that uses time as a controller.
    /// Primarily used for velocity parametrics.
    /// </summary>
    /// <param name="smoother">Smoothing function</param>
    /// <param name="maxTime">Time over which to perform easing</param>
    /// <param name="f">Target parametric (describing velocity)</param>
    /// <returns></returns>
    public static Func<TExPI, TEx<T>> EaseD<T>([LookupMethod] Func<tfloat, tfloat> smoother, float maxTime, 
        Func<TExPI, TEx<T>> f) 
        => ExMHelpers.EaseD(smoother, maxTime, f, bpi => bpi.t, (bpi, t) => bpi.CopyWithT(t));

    /// <summary>
    /// Apply a ease function on top of a target function that uses time as a controller.
    /// </summary>
    /// <param name="smoother">Smoothing function</param>
    /// <param name="maxTime">Time over which to perform easing</param>
    /// <param name="f">Target parametric (describing offset)</param>
    /// <returns></returns>
    public static Func<TExPI, TEx<T>> Ease<T>([LookupMethod] Func<tfloat, tfloat> smoother, float maxTime, 
        Func<TExPI, TEx<T>> f) 
        => ExMHelpers.Ease(smoother, maxTime, f, bpi => bpi.t, (bpi, t) => bpi.CopyWithT(t));


    public static tv2 RotateLerp(efloat zeroBound, efloat oneBound, efloat controller, ev2 source, ev2 target) =>
        EEx.Resolve(zeroBound, oneBound, controller, source, target, (z, o, c, f1, f2) => RotateRad(
            Clamp(z, o, c).Sub(z).Div(o.Sub(z)).Mul(RadDiff(f2, f1)),
            f1
        ));
    public static tv2 RotateLerpCCW(efloat zeroBound, efloat oneBound, efloat controller, ev2 source, ev2 target) =>
        EEx.Resolve(zeroBound, oneBound, controller, source, target, (z, o, c, f1, f2) => RotateRad(
            Clamp(z, o, c).Sub(z).Div(o.Sub(z)).Mul(RadDiffCCW(f2, f1)),
            f1
        ));
    public static tv2 RotateLerpCW(efloat zeroBound, efloat oneBound, efloat controller, ev2 source, ev2 target) =>
        EEx.Resolve(zeroBound, oneBound, controller, source, target, (z, o, c, f1, f2) => RotateRad(
            Clamp(z, o, c).Sub(z).Div(o.Sub(z)).Mul(RadDiffCW(f2, f1)),
            f1
        ));
    
    #region Easing

    /// <summary>
    /// *DEPRECATED*. INSTEAD OF RUNNING Smooth(EIOSine, t) YOU MAY RUN EIOSine(t) DIRECTLY.
    /// Apply a contortion to a 0-1 range.
    /// This returns an approximately linear function.
    /// </summary>
    /// <param name="smoother">Smoothing function</param>
    /// <param name="controller">0-1 value</param>
    /// <returns></returns>
    public static tfloat Smooth([LookupMethod] Func<tfloat, tfloat> smoother, tfloat controller) => smoother(controller);

    /// <summary>
    /// Apply a contortion to a clamped 0-1 range.
    /// This returns an approximately linear function.
    /// </summary>
    /// <param name="smoother">Smoothing function</param>
    /// <param name="controller">0-1 value (clamped if outside)</param>
    /// <returns></returns>
    public static tfloat SmoothC([LookupMethod] Func<tfloat, tfloat> smoother, tfloat controller) 
        => smoother(Clamp01(controller));

    /// <summary>
    /// Apply a contortion to a 0-x range, returning:
    /// <br/> 0-1 in the range [0,s1]
    /// <br/> 1 in the range [s1,x-s2]
    /// <br/> 1-0 in the range [x-s2,x]
    /// </summary>
    /// <param name="smoother1">First smoothing function</param>
    /// <param name="smoother2">Second smoothing function</param>
    /// <param name="total">Total time</param>
    /// <param name="smth1">Smooth-in time</param>
    /// <param name="smth2">Smooth-out time</param>
    /// <param name="controller">0-x value</param>
    /// <returns></returns>
    public static tfloat SmoothIO(
        [LookupMethod] Func<tfloat, tfloat> smoother1, 
        [LookupMethod] Func<tfloat, tfloat> smoother2, 
        efloat total, efloat smth1, efloat smth2, efloat controller) 
        => EEx.Resolve(total, smth1, smth2, controller,
            (T, s1, s2, t) => Ex.Condition(t.LT(T.Sub(smth2)), 
                    SmoothC(smoother1, t.Div(s1)),
                    E1.Sub(SmoothC(smoother2, t.Sub(T.Sub(s2)).Div(s2)))
                ));

    /// <summary>
    /// Apply SmoothIO where name=name1=name2 and smth=smth1=smth2.
    /// </summary>
    public static tfloat SmoothIOe([LookupMethod] Func<tfloat, tfloat> smoother,
        tfloat total, efloat smth, tfloat controller) 
        => EEx.Resolve(smth, s => SmoothIO(smoother, smoother, total, s, s, controller));

    /// <summary>
    /// Get the value of an easer at a given point between 0 and 1.
    /// The return value is periodized, so if the input is 5.4, then the output is 5 + ease(0.4).
    /// </summary>
    /// <param name="smoother">Smoothing function</param>
    /// <param name="controller"></param>
    /// <returns></returns>
    public static tfloat SmoothLoop([LookupMethod] Func<tfloat, tfloat> smoother, efloat controller) 
        => EEx.Resolve(controller, x => {
            var per = VFloat();
            return Ex.Block(new[] {per},
                per.Is(Floor(x)),
                per.Add(smoother(x.Sub(per)))
            );
        });

    /// <summary>
    /// Apply a contortion to a 0-R range, returning R * Smooth(name, controller/R).
    /// This returns an approximately linear function.
    /// </summary>
    /// <param name="smoother">Smoothing function</param>
    /// <param name="range">Range</param>
    /// <param name="controller">0-R value</param>
    /// <returns></returns>
    public static tfloat SmoothR([LookupMethod] Func<tfloat, tfloat> smoother, efloat range, tfloat controller) =>
        EEx.Resolve(range, r => r.Mul(Smooth(smoother, controller.Div(r))));
    
    /// <summary>
    /// Returns R * SmoothLoop(name, controller/R).
    /// </summary>
    public static tfloat SmoothLoopR([LookupMethod] Func<tfloat, tfloat> smoother, efloat range, tfloat controller) =>
        EEx.Resolve(range, r => r.Mul(SmoothLoop(smoother, controller.Div(r))));
    
    /// <summary>
    /// In-Sine easing function.
    /// </summary>
    [Alias("in-sine")]
    public static tfloat EInSine(tfloat x) => E1.Sub(Cos(hpi.Mul(x)));
    /// <summary>
    /// Out-Sine easing function.
    /// </summary>
    [Alias("out-sine")]
    public static tfloat EOutSine(tfloat x) => Sin(hpi.Mul(x));
    /// <summary>
    /// In-Out-Sine easing function.
    /// </summary>
    [Alias("io-sine")]
    public static tfloat EIOSine(tfloat x) => E05.Sub(E05.Mul(Cos(pi.Mul(x))));
    /// <summary>
    /// Linear easing function (ie. y = x).
    /// </summary>
    public static tfloat ELinear(tfloat x) => x;
    /// <summary>
    /// In-Quad easing function.
    /// </summary>
    public static tfloat EInQuad(tfloat x) => Sqr(x);
    /// <summary>
    /// Sine easing function with 010 pattern.
    /// </summary>
    public static tfloat ESine010(tfloat x) => Sin(pi.Mul(x));
    /// <summary>
    /// Softmod easing function with 010 pattern.
    /// </summary>
    [Alias("smod-010")]
    public static tfloat ESoftmod010(tfloat x) => Mul(E2, SoftMod(E05, x));

    public static tfloat EBounce2(tfloat x) => EEx.Resolve<float>((Ex)x, c => {
        var c1 = VFloat();
        var c2 = VFloat();
        return Ex.Block(new[] {c1, c2},
            c1.Is(Min(E05, c.Mul(ExC(0.95f)))),
            c2.Is(Max(E0, c.Sub(E05))),
            c1.Add(c2).Add(ExC(0.4f).Mul(
                    Sin(tau.Mul(c1)).Add(Sin(tau.Mul(c2)))
                ))
        );
    }); //https://www.desmos.com/calculator/ix37mllnyp
    
    /// <summary>
    /// Quadratic function that joins an ease-out and an ease-in, ie. two joined parabolas.
    /// </summary>
    /// <param name="midp"></param>
    /// <param name="period"></param>
    /// <param name="controller"></param>
    /// <returns></returns>
    public static tfloat EQuad0m10(efloat midp, tfloat period, tfloat controller) => EEx.Resolve(midp, m => {
        var t = VFloat();
        return Ex.Block(new[] {t},
            t.Is(controller.Sub(m)),
            Sqr(t).Div(Ex.Condition(t.LT0(), Sqr(m), Sqr(period.Sub(m)))).Complement()
        );
    });
    
    #endregion
    


}
}