﻿using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Danmaku {
public class LevelController : BehaviorEntity {
    public StageConfig stage;
    private static LevelController main;
    public static string DefaultSuicideStyle => (main == null) ? null : main.stage.defaultSuicideStyle;

    //[Header("On Level Completion")] public RString KVRKey;
    //public RInt KVRValue;
    //public bool overrideKVR = true;

    public enum LevelRunMethod {
        SINGLE,
        CONTINUE
    }
    public readonly struct LevelRunRequest {
        public readonly int toPhase;
        [CanBeNull] public readonly Action cb;
        public readonly LevelRunMethod method;
        public readonly StageConfig stage;

        public LevelRunRequest(int phase, [CanBeNull] Action callback, LevelRunMethod runMethod, StageConfig stageConf) {
            toPhase = phase;
            cb = callback;
            method = runMethod;
            stage = stageConf;
        }
    }

    public static void Request(LevelRunRequest req) {
        if (req.method == LevelRunMethod.SINGLE) main.phaseController.Override(req.toPhase, req.cb);
        else if (req.method == LevelRunMethod.CONTINUE) main.phaseController.SetGoTo(req.toPhase, req.cb);
        main.stage = req.stage;
        main.behaviorScript = req.stage.stateMachine;
        main.RunAttachedSM();
    }

    protected override void Awake() {
        base.Awake();
        main = this;
        behaviorScript = null;
#if UNITY_EDITOR
        if (SceneIntermediary.IsFirstScene) {
            Log.Unity("Running default level controller script under editor first-scene conditions");
            //Only run the default stage under editor testing conditions
            behaviorScript = (stage == null) ? null : stage.stateMachine;
        }
#endif
    }

#if UNITY_EDITOR
    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        else if (Input.GetKeyDown(KeyCode.Keypad5)) {
            GameManagement.Difficulty = DifficultySet.Easier;
            Restart();
        } else if (Input.GetKeyDown(KeyCode.T)) {
            GameManagement.Difficulty = DifficultySet.Easy;
            Restart();
        } else if (Input.GetKeyDown(KeyCode.Y)) {
            GameManagement.Difficulty = DifficultySet.Normal;
            Restart();
        } else if (Input.GetKeyDown(KeyCode.U)) {
            GameManagement.Difficulty = DifficultySet.Hard;
            Restart();
        } else if (Input.GetKeyDown(KeyCode.I)) {
            GameManagement.Difficulty = DifficultySet.Lunatic;
            Restart();
        } else if (Input.GetKeyDown(KeyCode.O)) {
            GameManagement.Difficulty = DifficultySet.Ultra;
            Restart();
        } else if (Input.GetKeyDown(KeyCode.P)) {
            GameManagement.Difficulty = DifficultySet.Abex;
            Restart();
        } else if (Input.GetKeyDown(KeyCode.LeftBracket)) {
            GameManagement.Difficulty = DifficultySet.Assembly;
            Restart();
        }
    }

    private void Restart() {
        Debug.Log("Reloading level. To avoid Event DelMarker bugs, running HardCancel first.");
        Debug.Log($"{GameManagement.DifficultyString.ToUpper()} is the current difficulty");
        UIManager.UpdateTags();
        HardCancel(false); //Prevents event DM caching bugs...
        global::GameManagement.LocalReset();
        RunAttachedSM();
    }

    public static LevelController Main => main;

#endif
}
}