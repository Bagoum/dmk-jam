using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BagoumLib.Cancellation;
using Danmokou.Core;
using Danmokou.Services;
using Danmokou.UI;
using Danmokou.VN;
using Suzunoya.ControlFlow;
using Suzunoya.Data;
using SuzunoyaUnity;
using UnityEngine;

/// <summary>
/// Loads mimic information from GameManagement.References.SuzunoyaReferences.
/// Also adjusts position to account for UICamera offset.
/// </summary>
public class DMKVNWrapper : VNWrapper {
    public UIManager ui = null!;
    protected override void Awake() {
        var refs = GameManagement.References.suzunoyaReferences;
        if (refs != null) {
            renderGroupMimic = refs.renderGroupMimic;
            entityMimics = refs.entityMimics;
        }
        base.Awake();
    }

    private void Start() {
        tr.localPosition = ui.uiCamera.transform.localPosition;
    }

    public override ExecutingVN TrackVN(IVNState vn) {
        var evn = base.TrackVN(vn);
        evn.tokens.Add(Events.EngineStateHasChanged.Subscribe(s => vn.InputAllowed.Value = s == EngineState.RUN));
        return evn;
    }


    public async Task<I> ExecuteVN<V, I>(Func<I, ICancellee, V> constructor, Func<V, Task> task, I initialSave, ICancellee extCT)
        where I : IInstanceData where V : IVNState {
        while (true) {
            var cT = new Cancellable();
            var jcT = new JointCancellee(cT, extCT);
            var vn = constructor(initialSave, jcT);
            vn.TimePerSkipConfirm = 0.2f;
            var exec = TrackVN(vn);
            Log.Unity($"Starting VN script {vn.Name}");
            VNLocation? backjumpTo = null;
            if (!Replayer.RequiresConsistency)
                exec.doBacklog = loc => {
                    backjumpTo = loc;
                    cT.Cancel();
                };
            var logct = DependencyInjection.Find<IVNBacklog>().TryRegister(exec);
            try {
                await task(vn);
                return (I)vn.UpdateSavedata();
            } catch (Exception e) {
                if (cT.Cancelled && !extCT.Cancelled && !(backjumpTo is null)) {
                    Log.Unity($"Backjumping VN {vn.Name} to line {backjumpTo.Describe}");
                    initialSave = (I) vn.UpdateSavedata();
                    initialSave.Location = backjumpTo;
                    continue;
                }
                if (e is OperationCanceledException) {
                    Log.Unity($"Cancelled VN script {vn.Name}");
                } else {
                    Log.UnityException(e);
                }
                throw;
            } finally {
                Log.Unity($"Completed VN script {vn.Name}. Final state: local {cT.ToCompletion()}, total {jcT.ToCompletion()}");
                logct?.Cancel();
                vn.DeleteAll();
            }
        }

    }
}