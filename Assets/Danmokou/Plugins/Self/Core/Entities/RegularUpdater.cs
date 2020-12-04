﻿using System;
using System.Collections.Generic;
using Core;
using JetBrains.Annotations;
using UnityEngine;

public abstract class RegularUpdater : MonoBehaviour, IRegularUpdater {
    private readonly List<IDeletionMarker> tokens = new List<IDeletionMarker>();

    /// <summary>
    /// Safe to call twice.
    /// </summary>
    protected void EnableUpdates() {
        if (tokens.Count == 0) {
            tokens.Add(ETime.RegisterRegularUpdater(this));
            BindListeners();
        }
    }
    protected virtual void BindListeners() { }

    protected void Listen<T>(Events.IEvent<T> ev, Action<T> sub) {
        tokens.Add(ev.Subscribe(sub));
    }
    protected void Listen(Events.Event0 ev, Action sub) {
        tokens.Add(ev.Subscribe(sub));
    }

    protected void RegisterDI<T>(T me) where T : class => tokens.Add(DependencyInjection.Register(me));

    protected virtual void OnEnable() => EnableUpdates();

    public abstract void RegularUpdate();
    public virtual void RegularUpdateParallel() { }
    public virtual int UpdatePriority => UpdatePriorities.DEFAULT;
    
    public virtual bool UpdateDuringPause => false;


    public virtual void PreSceneClose() { }

    /// <summary>
    /// Safe to call twice.
    /// </summary>
    protected void DisableUpdates() {
        for (int ii = 0; ii < tokens.Count; ++ii) {
            tokens[ii].MarkForDeletion();
        }
        tokens.Clear();
    }

    protected virtual void OnDisable() => DisableUpdates();

    protected void DisableDestroy() {
        DisableUpdates();
        Destroy(gameObject);
    }
}
