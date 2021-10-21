﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq.Expressions;
using System.Reactive;
using BagoumLib.DataStructures;
using BagoumLib.Events;
using BagoumLib.Functional;
using Danmokou.Core;
using Danmokou.Expressions;
using JetBrains.Annotations;

namespace Danmokou.Core {
/// <summary>
/// Module for managing events.
/// </summary>
public static class Events {
    public static IDeletionMarker SubscribeOnce(this Event0 ev, Action cb) {
        IDeletionMarker dm = null!;
        return dm = ev.Subscribe(() => {
            dm.MarkForDeletion();
            cb();
        });
    }
    
    /// <summary>
    /// Request this type in reflection to create event objects.
    /// </summary>
    /// <typeparam name="E"></typeparam>
    public readonly struct EventDeclaration<E> {
        public readonly E ev;
        private EventDeclaration(E ev) => this.ev = ev;
        public static implicit operator EventDeclaration<E>(E e) => new EventDeclaration<E>(e);
    }

    /// <summary>
    /// Events that take zero parameters.
    /// </summary>
    [Reflect(typeof(EventDeclaration<Event0>))]
    public class Event0 : IObservable<Unit> {
        private static readonly Dictionary<string, List<Event0>> waitingToResolve =
            new Dictionary<string, List<Event0>>();
        private static readonly Dictionary<string, Event0> storedEvents = new Dictionary<string, Event0>();

        private readonly DMCompactingArray<Action> callbacks = new DMCompactingArray<Action>();
        private DeletionMarker<Action>? refractor = null;
        private readonly bool useRefractoryPeriod = false;
        private bool inRefractoryPeriod = false;
        public Event0() : this(false) { }

        private Event0(bool useRefractoryPeriod) {
            this.useRefractoryPeriod = useRefractoryPeriod;
        }

        private void ListenForReactivation(string reactivator) {
            if (storedEvents.TryGetValue(reactivator, out Event0 refEvent)) {
                refractor = refEvent.Subscribe(() => inRefractoryPeriod = false);
            } else {
                if (!waitingToResolve.TryGetValue(reactivator, out List<Event0> waiters)) {
                    waitingToResolve[reactivator] = waiters = new List<Event0>();
                }
                waiters.Add(this);
            }
        }

        /// <summary>
        /// An event that may trigger repeatedly.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static EventDeclaration<Event0> Continuous(string name) {
            if (storedEvents.ContainsKey(name)) throw new Exception($"Event already declared: {name}");
            return storedEvents[name] = new Event0(false);
        }

        /// <summary>
        /// An event that will only trigger once.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static EventDeclaration<Event0> Once(string name) {
            if (storedEvents.ContainsKey(name)) throw new Exception($"Event already declared: {name}");
            return storedEvents[name] = new Event0(true);
        }

        /// <summary>
        /// An event that will trigger once, but may be reset when another event is triggered.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="reactivator"></param>
        /// <returns></returns>
        public static EventDeclaration<Event0> Refract(string name, string reactivator) {
            if (storedEvents.ContainsKey(name)) throw new Exception($"Event already declared: {name}");
            var ev = storedEvents[name] = new Event0(true);
            ev.ListenForReactivation(reactivator);
            if (waitingToResolve.TryGetValue(name, out var waiters)) {
                for (int ii = 0; ii < waiters.Count; ++ii) waiters[ii].ListenForReactivation(name);
                waitingToResolve.Remove(name);
            }
            return ev;
        }

        public static Event0 Find(string name) {
            if (!storedEvents.TryGetValue(name, out var ev)) throw new Exception($"Event not declared: {name}");
            return ev;
        }

        public static Event0? FindOrNull(string name) {
            if (!storedEvents.TryGetValue(name, out var ev)) return null;
            return ev;
        }

        private void Publish() {
            int temp_last = callbacks.Count;
            for (int ii = 0; ii < temp_last; ++ii) {
                DeletionMarker<Action> listener = callbacks.Data[ii];
                if (!listener.MarkedForDeletion) listener.Value();
            }
            callbacks.Compact();
        }

        public void Proc() {
            if (!inRefractoryPeriod) Publish();
            if (useRefractoryPeriod) inRefractoryPeriod = true;
        }

        private static readonly ExFunction proc = ExUtils.Wrap<Event0>("Proc");
        public Expression exProc() => proc.InstanceOf(Expression.Constant(this));

        public static void Reset() {
            foreach (var ev in storedEvents.Values) {
                ev.inRefractoryPeriod = false;
            }
        }

        public static void Reset(string[] names) {
            for (int ii = 0; ii < names.Length; ++ii) {
                storedEvents[names[ii]].inRefractoryPeriod = false;
            }
        }

        public static void DestroyAll() {
            foreach (var ev in storedEvents.Values) {
                ev.Destroy();
            }
            storedEvents.Clear();
        }

        private void Destroy() {
            callbacks.Empty();
            refractor?.MarkForDeletion();
        }

        public DeletionMarker<Action> Subscribe(Action cb) => callbacks.Add(cb);

        public IDisposable Subscribe(IObserver<Unit> observer) => 
            callbacks.Add(() => observer.OnNext(Unit.Default));
    }

    public static readonly IBSubject<EngineState> EngineStateChanged = new Event<EngineState>();
    public static readonly Event0 PhaseCleared = new Event0();
    public static readonly Event0 SceneCleared = new Event0();
#if UNITY_EDITOR || ALLOW_RELOAD
    public static readonly Event0 LocalReset = new Event0();
#endif
}
}