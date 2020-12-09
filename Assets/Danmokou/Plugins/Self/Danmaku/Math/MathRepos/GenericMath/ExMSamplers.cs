﻿using System;
using DMK.Core;
using DMK.DataHoist;
using DMK.Expressions;
using Ex = System.Linq.Expressions.Expression;
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
using ExPred = System.Func<DMK.Expressions.TExPI, DMK.Expressions.TEx<bool>>;

namespace DMK.DMath.Functions {
/// <summary>
/// See <see cref="ExM"/>. This class contains functions related to subsamplers.
/// </summary>
public static class ExMSamplers {
    /// <summary>
    /// If the input time is less than the reference time, evaluate the invokee. Otherwise, return the last returned evaluation.
    /// <para>You can call this with zero sampling time, and it will sample the invokee once. However, in this case SS0 is preferred.</para>
    /// </summary>
    /// <param name="time">Time at which to stop sampling</param>
    /// <param name="p">Target function</param>
    /// <returns></returns>
    [Alias("ss")]
    public static Func<TExPI, TEx<T>> StopSampling<T>(ExBPY time, Func<TExPI, TEx<T>> p) {
        Ex data = DataHoisting.GetClearableDict<T>();
        return bpi => ExUtils.DictIfCondSetElseGet(data, Ex.OrElse(Ex.LessThan(bpi.t, time(bpi)),
            Ex.Not(ExUtils.DictContains<uint, T>(data, bpi.id))), bpi.id, p(bpi));
    }
    
    /// <summary>
    /// If the condition is true, evaluate the invokee. Otherwise, return the last returned evaluation.
    /// <para>You can call this with zero sampling time, and it will sample the invokee once. However, in this case SS0 is preferred.</para>
    /// </summary>
    public static Func<TExPI, TEx<T>> SampleIf<T>(ExPred cond, Func<TExPI, TEx<T>> p) {
        Ex data = DataHoisting.GetClearableDict<T>();
        return bpi => ExUtils.DictIfCondSetElseGet(data, Ex.OrElse(cond(bpi),
            Ex.Not(ExUtils.DictContains<uint, T>(data, bpi.id))), bpi.id, p(bpi));
    }
    
    
    /// <summary>
    /// Samples an invokee exactly once.
    /// </summary>
    /// <param name="p">Target function</param>
    /// <returns></returns>
    public static Func<TExPI, TEx<T>> SS0<T>(Func<TExPI, TEx<T>> p) {
        Ex data = DataHoisting.GetClearableDict<T>();
        return bpi => ExUtils.DictIfCondSetElseGet(data, Ex.Not(ExUtils.DictContains<uint, T>(data, bpi.id)), bpi.id, p(bpi));
    }

}
}