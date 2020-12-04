﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveUtils {
    public const string DIR = "DMK_Saves/";
    public const string AYADIR = "DMK_Saves/Aya/";


    public static IEnumerable<string> EnumerateDirectory(string dir) {
        dir = DIR + dir;
        CheckDirectory(dir);
        return Directory.EnumerateFiles(dir).Select(f => f.Substring(DIR.Length));
    }

    public static void CheckDirectory(string final) {
        var dir = Path.GetDirectoryName(final);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }
    public static void Write(string file, object obj) {
        file = DIR + file;
        CheckDirectory(file);
        using (StreamWriter sw = new StreamWriter(file)) {
            sw.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
    }
    [CanBeNull]
    public static T Read<T>(string file) where T : class {
        try {
            using (StreamReader sr = new StreamReader($"{DIR}{file}")) {
                return JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
            }
        } catch (Exception e) {
            Log.Unity($"Couldn't read {typeof(T)} from file {DIR}{file}.", false, Log.Level.WARNING);
            return null;
        }
    }

    public static void WriteTex(string file, Texture2D tex) {
        CheckDirectory(file);
        File.WriteAllBytes(file, tex.EncodeToJPG(95));
    }

    public static void Destroy(string file) {
        if (File.Exists(file)) File.Delete(file);
    }


    public class InputConfig {
        public KeyCode FocusHold = KeyCode.LeftShift;
        public KeyCode ShootHold = KeyCode.Z;
        public KeyCode Bomb = KeyCode.X;
    }
    private const string INPUT = "input.txt";
    private const string INPUTREF = "input_reference.txt";
    public static InputConfig i { get; }

    static SaveUtils() {
        i = Read<InputConfig>(INPUT);
        if (i == null) {
            i = new InputConfig();
            Write(INPUT, i);
            var kcs = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().Select(kc => $"{kc.ToString()} = {(int) kc}").ToArray();
            Write(INPUTREF, kcs);
        }
    }
}