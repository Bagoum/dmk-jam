﻿using System;
using System.Threading.Tasks;
using BagoumLib;
using BagoumLib.DataStructures;
using Danmokou.DMath;
using JetBrains.Annotations;
using UnityEngine;

namespace Danmokou.Core {
public enum EngineState {
    RUN = 1,
    //Freeze frames, etc
    EFFECT_PAUSE = 2,
    //Pause menu
    MENU_PAUSE = 3,
    //Loading screens
    LOADING_PAUSE = 4
}

public static class EngineStateHelpers {
    public static bool IsPaused(this EngineState gs) => gs != EngineState.RUN;

    public static bool InputAllowed(this EngineState gs) => gs != EngineState.LOADING_PAUSE;

    public static float Timescale(this EngineState gs) => gs switch {
        EngineState.MENU_PAUSE => 0f,
        _ => 1f,
    };
}

public static class EngineStateManager {
    private static readonly MaxOp<EngineState> stateOverrides = new MaxOp<EngineState>(EngineState.RUN, (x, y) => x > y);
    public static EngineState State { get; private set; } = EngineState.RUN;
    public static bool PendingUpdate { get; private set; } = false;

    /// <summary>
    /// Called by ETime at end of frame for consistency
    /// </summary>
    public static void UpdateEngineState() {
        var lastState = State;
        State = stateOverrides.Aggregate;
        PendingUpdate = false;
        if (lastState.Timescale() != State.Timescale())
            Time.timeScale = State.Timescale();
        if (lastState != State) {
            Log.Unity($"Engine state has been set to to {State}");
            Events.EngineStateHasChanged.OnNext(State);
        }
    }

    public static IDeletionMarker RequestState(EngineState s) {
        if (s == EngineState.RUN)
            throw new Exception($"You cannot request {s}. Instead, delete all tokens requesting a pause state.");
        PendingUpdate = true;
        return stateOverrides.AddValue(s);
    }
}
}
