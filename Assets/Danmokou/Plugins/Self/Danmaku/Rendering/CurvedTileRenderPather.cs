﻿using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using DMath;
using JetBrains.Annotations;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

//Warning to future self: Pathers function really strangely in that their transform is located at the starting point, and the mesh
//simply moves farther away from that source. The transform does not move. The reason for this is that it's difficult to maintain
//a list of points you travelled through relative to your current point, but it's easy to maintain a list of points that someone else
//has travelled through relative to your static point, which is essentially what we do. This is why pathers do not support parenting.

namespace Danmaku {
[Serializable]
public class PatherRenderCfg : TiledRenderCfg {
    public float lineRadius;
    public float headCutoffRatio = 0.05f;
    public float tailCutoffRatio = 0.05f;
}
/// <summary>
/// A pather remembers the positions it has been in and draws a line through them.
/// </summary>
public class CurvedTileRenderPather : CurvedTileRender {
    //Important implementation note: The centers array is a list of *global* positions (as of v5.1.0).
    //This is for efficiency and simplicity re: the TrailRenderer implementation.
    //Lasers require centers to be a list of local positions so it can be pased as a mesh.
    
    /// = 0.033s
    private const int FramePosCheck = 4;
    private int cL;
    private readonly float lineRadius;
    private float scaledLineRadius;
    private readonly float headCutoffRatio;
    private readonly float tailCutoffRatio;
    private BPY remember;
    [CanBeNull] private BPY hueShift;
    private (TP4 black, TP4 white)? recolor;
    private Velocity velocity;
    /// <summary>
    /// The last return value of Velocity.Update. Used for backstepping.
    /// </summary>
    private Vector2 lastDelta;
    private int read_from;
    private ParametricInfo bpi;
    public ref ParametricInfo BPI => ref bpi;
    
    //set in pathtracker.awake
    private Action onCameraCulled = null;
    private int cullCtr;
    private const int checkCullEvery = 120;

    private Pather exec;

    //Note: trailRenderer requires reversing the sprite.
    protected override bool HandleAsMesh => false;
    public readonly TrailRenderer trailR;

    private SOPlayerHitbox target;
    private PlayerBulletCfg? playerBullet;

    public CurvedTileRenderPather(PatherRenderCfg cfg, GameObject obj) : base(obj) {
        lineRadius = cfg.lineRadius;
        trailR = obj.GetComponent<TrailRenderer>();
        tailCutoffRatio = cfg.tailCutoffRatio;
        headCutoffRatio = cfg.headCutoffRatio;
    }
    public void SetYScale(float scale) {
        PersistentYScale = scale;
        scaledLineRadius = lineRadius * scale;
    }
    public void Initialize(Pather locationer, TiledRenderCfg cfg,  Material material, bool isNew, Velocity vel, 
        uint bpiId, int firingIndex, BPY rememberTime, float maxRememberTime, SOPlayerHitbox collisionTarget, 
        ref RealizedBehOptions options) {
        exec = locationer;
        int newTexW = (int) Math.Ceiling(maxRememberTime * ETime.ENGINEFPS) + 1;
        base.Initialize(locationer, cfg, material, isNew, false, options.playerBullet != null, newTexW);
        if (locationer.HasParent()) throw new NotImplementedException("Pather cannot be parented");
        velocity = vel;
        bpi = new ParametricInfo(vel.rootPos, firingIndex, bpiId);
        _ = velocity.UpdateZero(ref bpi, 0f);
        lastDataIndex = cL = centers.Length;
        remember = rememberTime;
        intersectStatus = SelfIntersectionStatus.RAS;
        read_from = cL;
        //isnonzero = false;
        for (int ii = 0; ii < cL; ++ii) {
            centers[ii] = bpi.loc;
        }
        prevRemember = trailR.time = 0f;
        target = collisionTarget;
        playerBullet = options.playerBullet;
        bounds = new AABB(velocity.rootPos, Vector2.zero);
        
        hueShift = options.hueShift;
        recolor = options.recolor;
        //This needs to be reset to zero here to ensure that the value isn't dirty, since hue-shift is always active
        pb.SetFloat(PropConsts.HueShift, hueShift?.Invoke(bpi) ?? 0f);
        if (hueShift != null || recolor != null) DontUpdateTimeAfter = M.IntFloatMax;
    }

    public Vector2 GlobalPosition => bpi.loc;

    public void SetCameraCullable(Action onCull) {
        onCameraCulled = onCull;
    }

    private const float CULL_RAD = 4;
    private const float FIRST_CULLCHECK_TIME = 4;
    //This is run during Update
    private bool CullCheck() {
        if (bpi.t > FIRST_CULLCHECK_TIME && LocationService.OffPlayableScreenBy(CULL_RAD, centers[read_from])) {
            onCameraCulled();
            return true;
        }
        return false;
    }

    public override void UpdateMovement(float dT) {
        velocity.UpdateDeltaAssignAcc(ref bpi, out lastDelta, dT);
        base.UpdateMovement(dT);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateGraphics() {
        if (hueShift != null) {
            pb.SetFloat(PropConsts.HueShift, M.degRad * hueShift(bpi));
        }
        if (recolor.Try(out var rc)) {
            pb.SetVector(PropConsts.RecolorB, rc.black(bpi));
            pb.SetVector(PropConsts.RecolorW, rc.white(bpi));
        }
    }
    public override void UpdateRender() {
        exec.SetDirection(lastDelta);
        if (ETime.LastUpdateForScreen) {
            UpdateGraphics();
            
            tr.localPosition = bpi.loc;
            //trailR.AddPosition(bpi.loc);
            if (prevRemember != nextRemember) {
                trailR.time = prevRemember = nextRemember;
            }
        }
        base.UpdateRender();
    }

    private float prevRemember = 0f;
    private float nextRemember = 0f;

    private AABB bounds;

    //private bool isnonzero;
    /// <summary>
    /// The oldest index containing direction information.
    /// </summary>
    private int lastDataIndex;

    protected override unsafe void UpdateVerts(bool renderRequired) {
        var last = cL - 1;
        
        lastDataIndex = (lastDataIndex > 0) ? lastDataIndex - 1 : 0;
        if (lastDataIndex == last) return; //Need at least two frames to draw

        int remembered = (int) Math.Ceiling((nextRemember = remember(bpi)) * ETime.ENGINEFPS);
        if (remembered < 2) remembered = 2;
        if (remembered > cL - lastDataIndex) remembered = cL - lastDataIndex;
        
        read_from = cL - remembered;
        
        float minX = 999f;
        float minY = 999f;
        float maxX = -999f;
        float maxY = -999f;
        float wx, wy;
        for (int ii = 0; ii < texRptWidth; ++ii) {
            /*vertsPtr[ii].loc.x = vertsPtr[ii + 1].loc.x;
            vertsPtr[ii].loc.y = vertsPtr[ii + 1].loc.y;
            vertsPtr[ii + cL].loc.x = vertsPtr[ii + cL + 1].loc.x;
            vertsPtr[ii + cL].loc.y = vertsPtr[ii + cL + 1].loc.y;*/
            centers[ii].x = wx = centers[ii + 1].x;
            centers[ii].y = wy = centers[ii + 1].y;
            if (ii >= read_from) {
                if (wx < minX) minX = wx;
                if (wx > maxX) maxX = wx;
                if (wy < minY) minY = wy;
                if (wy > maxY) maxY = wy;
            }
        }
        bounds = new AABB(minX, maxX, minY, maxY);

        centers[last] = bpi.loc;
    }

    private const float BACKSTEP = 2f;

    public CollisionResult CheckCollision() {
        cullCtr = (cullCtr + 1) % checkCullEvery; 
        if (cullCtr == 0 && exec.myStyle.CameraCullable && CullCheck())
            return CollisionResult.noColl;
        
        int cut1 = (int) Math.Ceiling((cL - read_from + 1) * tailCutoffRatio);
        int cut2 = (int) Math.Ceiling((cL - read_from + 1) * headCutoffRatio);
        if (playerBullet.Try(out var plb)) {
            var fe = Enemy.FrozenEnemies;
            for (int ii = 0; ii < fe.Count; ++ii) {
                if (fe[ii].Active && DMath.Collision.CircleOnSegments(fe[ii].pos, fe[ii].radius, Vector2.zero, 
                        centers, read_from + cut1, 1, cL - cut2, scaledLineRadius, 1, 0, out int segment) &&
                    fe[ii].enemy.TryHitIndestructible(bpi.id, plb.cdFrames)) {
                    fe[ii].enemy.QueueDamage(plb.bossDmg, plb.stageDmg, target.location);
                    fe[ii].enemy.ProcOnHit(plb.effect, centers[segment]);
                }
            }
            return CollisionResult.noColl;
        }
        if (target.Active && DMath.Collision.CircleOnAABB(
            bounds, target.location, target.largeRadius)) {
            return DMath.Collision.GrazeCircleOnSegments(target.Hitbox, Vector2.zero, centers, read_from + cut1, 1, cL - cut2, scaledLineRadius, 1, 0);
        } else return CollisionResult.noColl;
    }
    public void FlipVelX() {
        velocity.FlipX();
        intersectStatus = SelfIntersectionStatus.CHECK_THIS_AND_NEXT;
    }
    public void FlipVelY() {
        velocity.FlipY();
        intersectStatus = SelfIntersectionStatus.CHECK_THIS_AND_NEXT;
    }

    public void SpawnSimple(string style) {
        for (int ii = texRptWidth; ii > read_from; ii -= FramePosCheck * 2) {
            BulletManager.RequestNullSimple(style, centers[ii], (centers[ii] - centers[ii-1]).normalized);
        }
    }
    
    
    public override void SetSprite(Sprite s, float yscale) {
        base.SetSprite(s, yscale);
        trailR.widthMultiplier = spriteBounds.y;
    }

    public override void Deactivate() {
        base.Deactivate();
        trailR.emitting = false;
        trailR.Clear();
    }

    public override void Activate() {
        UpdateGraphics();
        base.Activate();
        tr.localPosition = bpi.loc;
        trailR.SetPropertyBlock(pb);
        trailR.Clear();
        trailR.emitting = true;
    }


#if UNITY_EDITOR
    public unsafe void Draw() {
        Handles.color = Color.cyan;
        int cut1 = (int) Math.Ceiling((cL - read_from + 1) * tailCutoffRatio);
        int cut2 = Mathf.CeilToInt((cL - read_from + 1) * headCutoffRatio);
        GenericColliderInfo.DrawGizmosForSegments(centers, (read_from + cut1), 1, cL - cut2, Vector2.zero, scaledLineRadius, 0);
        /*
        Handles.color = Color.magenta;
        for (int ii = 0; ii < cL; ++ii) {
            Handles.DrawWireDisc(vertsPtr[ii].loc + (Vector3) velocity.rootPos, Vector3.forward, 0.005f + 0.005f * ii / cL);
        }
        Handles.color = Color.blue;
        for (int ii = 0; ii < cL; ++ii) {
            Handles.DrawWireDisc(vertsPtr[ii + cL].loc + (Vector3) velocity.rootPos, Vector3.forward, 0.005f + 0.005f *ii / cL);
        }*/
    }

    [ContextMenu("Debug info")]
    public void DebugPath() {
        //read_from = start + 1
        Log.Unity($"Start {read_from} Skip 1 End {centers.Length}", level: Log.Level.INFO);
    }
#endif
}
}