﻿using System;
using System.Collections.Generic;
using System.Linq;
using BagoumLib;
using Danmokou.DMath;
using UnityEngine;

namespace Danmokou.UI.XML {

/// <summary>
/// A group of arbitrarily positioned nodes and groups with an special "unselector" node that is used to remove selection from any other node.
/// <br/>Used with <see cref="XMLDynamicMenu"/>.
/// </summary>
public class UIFreeformGroup : CompositeUIGroup, IFixedXMLObjectContainer {
    private readonly UINode? unselector;
    public UIFreeformGroup(UIRenderSpace container, UINode? unselector = null) : base(container, Array.Empty<UIGroup>(), new[] { unselector }) {
        this.unselector = unselector;
        ExitNodeOverride = unselector;
    }

    private static readonly float[] _angleLimits2 = { 37, 65, 89 };

    public override UIResult Navigate(UINode node, UICommand req) {
        var resp = base.Navigate(node, req);
        if (node == unselector && resp == NoOp)
            return SilentNoOp;
        return resp;
    }

    protected override UIResult? NavigateAmongComposite(UINode current, UICommand dir) {
        var targets = NodesAndDependentNodes.Where(n => n.AllowInteraction && n != unselector).ToList();
        if (targets.Count > 0) {
            if (current == unselector) {
                //Return the node farthest in the pressed direction
                return dir switch {
                    UICommand.Down => targets.MaxBy(n => n.WorldLocation.y),
                    UICommand.Up => targets.MaxBy(n => -n.WorldLocation.y),
                    UICommand.Left => targets.MaxBy(n => -n.WorldLocation.x),
                    UICommand.Right => targets.MaxBy(n => n.WorldLocation.x),
                    _ => throw new Exception()
                };
            }
            if (FindClosest(current.WorldLocation, dir, targets, _angleLimits2, n => n != current) 
             is {} result) 
                return FinalizeTransition(current, result);
        }
        if (TryDelegateNavigationToEnclosure(current, dir, out var res))
            return res;
        //no wraparound permitted for now
        return null;
    }

    private static Vector2 DirAsVec(UICommand dir) => dir switch {
        UICommand.Down => Vector2.down,
        UICommand.Up => Vector2.up,
        UICommand.Left => Vector2.left,
        UICommand.Right => Vector2.right,
        _ => throw new Exception()
    };

    public static UINode? FindClosest(Rect from, UICommand dir, IEnumerable<UINode> targets, 
        float[] angleLimits, Func<UINode, bool>? allowed = null) {
        if (dir is not (UICommand.Down or UICommand.Up or UICommand.Left or UICommand.Right))
            return null;
        var dirAsVec = DirAsVec(dir);
        var dirAsAng = M.Atan2D(dirAsVec.y, dirAsVec.x);
        var ordering = targets.Select(n => {
            var delta = M.ShortestDistancePushOutOverlap(from, n.HTML.worldBound);
            //y axis is inverted in XML
            var angleDelta = Mathf.Abs(M.DeltaD(M.Atan2D(-delta.y, delta.x), dirAsAng));
            return (n, angleDelta, (delta / 20).magnitude + angleDelta);
        }).OrderBy(x => x.Item3).ToList();
        foreach (var limit in angleLimits) {
            foreach (var (candidate, angle, _) in ordering) {
                if (!candidate.AllowInteraction || allowed?.Invoke(candidate) is false)
                    continue;
                if (angle >= limit)
                    continue;
                return candidate;
            }
        }
        return null;
    }
}

}