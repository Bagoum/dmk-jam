﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using BagoumLib;
using BagoumLib.Events;
using UnityEngine;

namespace Danmokou.Core {
public static class Logs {
    private const int MIN_LEVEL = (int) LogLevel.DEBUG1;
    private const int BUILD_MIN = (int) LogLevel.DEBUG2;

    public static readonly ISubject<LogMessage> DMKLogs = new Event<LogMessage>();

    private static StreamWriter? file;
    private const string LOGDIR = "DMK_Logs/";
    private static readonly List<IDisposable> listeners = new List<IDisposable>();

    static Logs() {
        if (!Application.isPlaying) return;
        var d = DateTime.Now;
        var log = $"{LOGDIR}log_{d.Year}-{d.Month}-{d.Day}-{d.Hour}-{d.Minute}-{DateTime.Now.Second}.log";
        FileUtils.CheckDirectory(log);
        file = new StreamWriter(log);
        listeners.Add(Logging.Logs.Subscribe(PrintToUnityLog));
        listeners.Add(DMKLogs.Subscribe(PrintToUnityLog));
    }

    public static void CloseLog() {
        file?.Close();
        file = null;
        foreach (var t in listeners)
            t.Dispose();
        listeners.Clear();
    }

    public static void Log(string msg, bool stackTrace = true, LogLevel level = LogLevel.INFO) =>
        DMKLogs.OnNext(new LogMessage(msg, level, null, stackTrace));

    public static void LogException(Exception e) => 
        DMKLogs.OnNext(new LogMessage("", LogLevel.ERROR, e, true));

    public static void UnityError(string msg) {
        Log(msg, true, LogLevel.ERROR);
    }

    private static string PrintException(Exception e, string prefixMsg="") => 
        (string.IsNullOrWhiteSpace(prefixMsg) ? "" : $"{prefixMsg}\n") +
        Exceptions.PrintNestedException(e);

    private static void PrintToUnityLog(LogMessage lm) {
        if ((int) lm.Level < MIN_LEVEL) return;
#if UNITY_EDITOR
#else
        if ((int) lm.Level < BUILD_MIN) return;
#endif
        var msg = (lm.Exception == null) ? lm.Message : PrintException(lm.Exception, lm.Message);
        msg = $"Frame {ETime.FrameNumber}: {msg}";
        
        LogOption lo = (lm.ShowStackTrace == false) ? LogOption.NoStacktrace : LogOption.None;
        LogType unityLT = LogType.Log;
        file?.WriteLine(msg);
        if (lm.Level == LogLevel.WARNING) 
            unityLT = LogType.Warning;
        if (lm.Level == LogLevel.ERROR) {
            //unityLT = LogType.Error;
            Debug.LogError(msg);
        } else {
            Debug.LogFormat(unityLT, lo, null, msg.Replace("{", "{{").Replace("}", "}}"));
        }
    }

}
}
