﻿using System;
using System.Collections.Generic;
using System.Linq;
using BagoumLib;
using BagoumLib.Reflection;
using Danmokou.Behavior;
using Danmokou.Core;
using Danmokou.Danmaku;
using Danmokou.Danmaku.Descriptors;
using Danmokou.DMath;
using Danmokou.Expressions;
using Danmokou.Player;
using Danmokou.Reflection2;
using UnityEngine;
using Ex = System.Linq.Expressions.Expression;
using TypeDefKey = Danmokou.Core.FreezableArray<(System.Type type, string name)>;

#pragma warning disable CS0162

namespace Danmokou.Reflection.CustomData {

public class ConstructedType {
    public BuiltCustomDataDescriptor Descriptor { get; }
    public Type BuiltType { get; }
    public Func<PICustomData> Constructor { get; }
    private Stack<PICustomData> Cache { get; } = new();
    public int TypeIndex { get; }
    public int Allocated { get; private set; } 
    public int Popped { get; private set; } //Popped and recached should be about equal
    public int Recached { get; private set; }
    public int Copied { get; internal set; }
    public int Cleared { get; private set; }
    
    public ConstructedType(BuiltCustomDataDescriptor desc, Type builtType, int typeIndex, Func<PICustomData>? constructor = null) {
        this.Descriptor = desc;
        this.BuiltType = builtType;
        this.Constructor = constructor ?? Ex.Lambda<Func<PICustomData>>(Ex.New(builtType.GetConstructor(Type.EmptyTypes)!)).Compile();
        this.TypeIndex = typeIndex;
    }

    public PICustomData MakeNew((LexicalScope scope, GenCtx gcx)? parent = null) {
        PICustomData data;
        if (Cache.Count > 0) {
            data = Cache.Pop();
            ++Popped;
        } else {
            data = Constructor();
            ++Allocated;
        }
        if (parent.Try(out var p) && p.scope is not DMKScope) {
            data.envFrame = EnvFrame.Create(p.scope, p.gcx.EnvFrame);
        } else
            data.envFrame = EnvFrame.Empty;
        data.typeIndex = TypeIndex;
        data.firer = parent?.gcx.exec;
        data.playerController = data.firer switch {
            PlayerController pi => pi,
            FireOption fo => fo.Player,
            Bullet b => b.Player?.firer,
            _ => null
        };
        if (data.playerController == null)
            data.playerController = parent?.gcx.playerController;
        return data;
    }

    public void Return(PICustomData data) {
        data.boundInts.Clear();
        data.boundFloats.Clear();
        data.boundV2s.Clear();
        data.boundV3s.Clear();
        data.boundRV2s.Clear();
        data.firer = null;
        data.playerController = null;
        data.laserController = null;
        data.bullet = null;
        data.playerBullet = null;
        ++Recached;
        Cache.Push(data);
    }

    public void ClearCache() => Cache.Clear();

}

public class PICustomDataBuilder : CustomDataBuilder {
    //For AOT cases, we should fall back to always using dictionary lookups (dynamic lookup).
    public const bool DISABLE_TYPE_BUILDING =
#if !EXBAKE_SAVE && !EXBAKE_LOAD && !WEBGL
        true;
#else
        true;
#endif
    public static readonly PICustomDataBuilder Builder = new();
    
    private readonly Dictionary<TypeDefKey, ConstructedType> typeMap = new();
    private readonly List<ConstructedType> typeList = new();
    public ConstructedType ConstructedBaseType { get; }
    public IReadOnlyList<ConstructedType> TypeList => typeList;

    public PICustomDataBuilder() : base(
#if !EXBAKE_SAVE && !EXBAKE_LOAD && !WEBGL
        typeof(PICustomData), "DanmokouDynamic", null, typeof(float), typeof(int), typeof(Vector2), typeof(Vector3), typeof(V2RV2)
#else
        typeof(PICustomData)
#endif
        ) {
        var consType = ConstructedBaseType = new ConstructedType(customDataDescriptors[CustomDataBaseType], CustomDataBaseType, 0, () => new());
        typeList.Add(consType);
        typeMap[TypeDefKey.Empty] = consType;
    }

    public ConstructedType GetTypeDef(PICustomData pi) => typeList[pi.typeIndex];

    public ConstructedType GetCustomDataType(in TypeDefKey key) {
        if (DISABLE_TYPE_BUILDING)
            return ConstructedBaseType;
        if (typeMap.TryGetValue(key, out var t))
            return t;
        var builtType = Builder.CreateCustomDataType(new(
            key.Data.Select(x => new CustomDataFieldDescriptor(x.name, x.type)
            ).ToArray()
        ) { BaseType = typeof(PICustomData) }, out var builtDesc);
        t = new(builtDesc, builtType, typeList.Count);
        Logs.Log($"Created custom data type with fields {builtDesc.Descriptor}");
        typeList.Add(t);
        return typeMap[key.Freeze()] = t;
    }

    public ConstructedType GetCustomDataType((Type, string)[] aliases) => 
        GetCustomDataType(new TypeDefKey(aliases));

    public void ClearCustomDataTypeCaches() {
        //element 0 is the base type that's used most commonly, so we don't clear it as it's very likely to be reused
        for (int ii = 1; ii < typeList.Count; ++ii)
            typeList[ii].ClearCache();
    }
}
}