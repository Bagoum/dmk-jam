﻿using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {
public class DataStructures {
    private readonly struct I {
        public readonly int x;

        public I(int _x) {
            x = _x;
            
        }
    }

    [Test]
    public void N2Triangle() {
        var arr = new[] {
            new[] {0},
            new[] {10, 11, 12},
            new[] {20, 21, 22, 23, 24}
        };
        var n2 = new N2Triangle<int>(arr);
        Assert.AreEqual(n2[0][0], 0);
        Assert.Throws<IndexOutOfRangeException>(() => {
            var x = n2[0][1];
        });
        Assert.AreEqual(n2[1][0], 11);
        Assert.AreEqual(n2[1][-1], 10);
        Assert.AreEqual(n2[1][1], 12);
        Assert.Throws<IndexOutOfRangeException>(() => {
            var x = n2[1][-2];
        });
        Assert.AreEqual(n2[2][0], 22);
        Assert.AreEqual(n2[2][-2], 20);
        Assert.AreEqual(n2[2][1], 23);
        Assert.Throws<IndexOutOfRangeException>(() => {
            var x = n2[2][3];
        });
        arr = new[] {
            new[] {0},
            new[] {10, 11, 12, 13},
            new[] {20, 21, 22, 23, 24}
        };
        Assert.Throws<ArgumentException>(() => new N2Triangle<int>(arr));
    }

    [Test]
    public void CircleList() {
        var cl = new CircularList<int>(4);
        cl.Add(10);
        cl.Add(11);
        Assert.AreEqual(cl.SafeIndexFromBack(0), 11);
        Assert.AreEqual(cl.SafeIndexFromBack(1), 11);
        Assert.AreEqual(cl.SafeIndexFromBack(2), 10);
        Assert.AreEqual(cl.SafeIndexFromBack(15), 10);
        cl.Add(12);
        cl.Add(13);
        cl.Add(14);
        cl.Add(15);
        Assert.AreEqual(cl.SafeIndexFromBack(2), 14);
        Assert.AreEqual(cl.SafeIndexFromBack(3), 13);
        Assert.AreEqual(cl.SafeIndexFromBack(4), 12);
        Assert.AreEqual(cl.SafeIndexFromBack(5), 12);
    }

    [Test]
    public void NLL() {
        var nll = new NodeLinkedList<int>();
        var n0 = nll.Add(0);
        var n1 = nll.Add(1);
        var n2 = nll.Add(2);
        nll.Remove(n1, false);
        Assert.AreSame(n0.next, n2);
        Assert.AreSame(n2.prev, n0);
        Assert.AreSame(nll.At(1), n2);
        Assert.AreEqual(nll.At(2), null);
        Assert.AreEqual(nll.IndexOf(n1), -1);
        nll.InsertAfter(n0, n1);
        Assert.AreEqual(nll.IndexOf(n1), 1);
        nll.Reset();
        Assert.AreEqual(nll.count, 0);
    }
    
    [Test]
    public void SafeResizeable() {
        var arr = new SafeResizableArray<int>(1);
        arr.SafeAssign(6, 400);
        Assert.AreEqual(arr.SafeGet(6), 400);
        Assert.AreEqual(arr.SafeGet(24), 0);
        arr.Empty(false);
        Assert.AreEqual(arr.SafeGet(6), 400);
        arr.Empty(true);
        Assert.AreEqual(arr.SafeGet(6), 0);
    }

    [Test]
    public void CompactingArray() {
        var ca = new CompactingArray<I>(4);
        for (int ii = 0; ii < 8; ++ii) {
            var x = new I(ii);
            ca.Add(ref x);
        }
        for (int ii = 0; ii < 8; ++ii) {
            Assert.AreEqual(ca[ii].x, ii);
        }
        ca.Compact();
        ca.Delete(0);
        ca.Delete(1);
        ca.Delete(2);
        ca.Delete(4);
        ca.Compact();
        Assert.AreEqual(ca.Count, 4);
        Assert.AreEqual(ca[0].x, 3);
        Assert.AreEqual(ca[1].x, 5);
        Assert.AreEqual(ca[2].x, 6);
        Assert.AreEqual(ca[3].x, 7);
        var _t = new I(20);
        ca[2] = _t;
        ca.Add(ref _t);
        _t = new I(30);
        ca.Add(ref _t);
        _t = new I(40);
        ca.Add(ref _t);
        _t = new I(50);
        ca.Add(ref _t);
        ca.Delete(0);
        ca.Delete(4);
        ca.Delete(7);
        ca.Compact();
        Assert.AreEqual(ca[1].x, 20);
        Assert.AreEqual(ca[3].x, 30);
        Assert.AreEqual(ca[4].x, 40);
        Assert.AreEqual(ca.Count, 5);
    }

    [Test]
    public void DMCArray() {
        var ca = new DMCompactingArray<I>(4);
        DeletionMarker<I>[] dmi = new DeletionMarker<I>[12];
        for (int ii = 0; ii < 8; ++ii) {
            var x = new I(ii);
            dmi[ii] = ca.Add(x);
        }
        for (int ii = 0; ii < 8; ++ii) {
            Assert.AreEqual(ca[ii].x, ii);
        }
        ca.Compact();
        dmi[0].MarkForDeletion();
        dmi[1].MarkForDeletion();
        dmi[2].MarkForDeletion();
        dmi[4].MarkForDeletion();
        ca.Compact();
        Assert.AreEqual(ca.Count, 4);
        Assert.AreEqual(ca[0].x, 3);
        Assert.AreEqual(ca[1].x, 5);
        Assert.AreEqual(ca[2].x, 6);
        Assert.AreEqual(ca[3].x, 7);
        dmi[8] = ca.Add(new I(20));
        dmi[9] = ca.Add(new I(30));
        dmi[10] = ca.Add(new I(40));
        dmi[11] = ca.Add(new I(50));
        dmi[11].MarkForDeletion();
        dmi[8].MarkForDeletion();
        dmi[3].MarkForDeletion(); //ca[0]
        ca.Compact();
        Assert.AreEqual(ca[1].x, 6);
        Assert.AreEqual(ca[3].x, 30);
        Assert.AreEqual(ca[4].x, 40);
        Assert.AreEqual(ca.Count, 5);
        ca.Empty();
        Assert.AreEqual(ca.Count, 0);
    }


    [Test]
    public void ArrayUtils() {
        var arr = new int[5];
        arr[0] = 0;
        arr[1] = 1;
        arr[2] = 2;
        int ct = 3;
        arr.Insert(ref ct, 999, 2);
        Assert.AreEqual(arr[2], 999);
        Assert.AreEqual(arr[3], 2);
        Assert.AreEqual(ct, 4);
        arr.Insert(ref ct, 555, 0);
        Assert.AreEqual(arr[0], 555);
        Assert.AreEqual(arr[1], 0);
        Assert.AreEqual(arr[2], 1);
        Assert.AreEqual(arr[3], 999);
        Assert.AreEqual(arr[4], 2);
        Assert.AreEqual(ct, 5);
        Assert.Throws<IndexOutOfRangeException>(() => arr.Insert(ref ct, 555, 0));
        Assert.AreEqual(arr[0], 555);
        Assert.AreEqual(arr[1], 0);
        Assert.AreEqual(arr[2], 1);
        Assert.AreEqual(arr[3], 999);
        Assert.AreEqual(arr[4], 2);
        Assert.AreEqual(ct, 5);
    }
    
    [Test]
    public void DMCArrayPriority() {
        var ca = new DMCompactingArray<I>(4);
        DeletionMarker<I>[] dmi = new DeletionMarker<I>[12];
        for (int ii = 0; ii < 8; ++ii) {
            var x = new I(ii);
            dmi[ii] = ca.AddPriority(x, 10 - ii);
        }
        for (int ii = 0; ii < 8; ++ii) {
            //Inserted backwards!
            Assert.AreEqual(ca[7 - ii].x, ii);
        }
        ca.Compact();
        dmi[7].MarkForDeletion();
        dmi[6].MarkForDeletion();
        dmi[5].MarkForDeletion();
        dmi[3].MarkForDeletion();
        ca.Compact();
        Assert.AreEqual(ca.Count, 4);
        Assert.AreEqual(ca[0].x, 4);
        Assert.AreEqual(ca[1].x, 2);
        Assert.AreEqual(ca[2].x, 1);
        Assert.AreEqual(ca[3].x, 0);
        ca.AddPriority(new I(999), -1);
        Assert.AreEqual(ca[0].x, 999);
        Assert.AreEqual(ca[4].x, 0);
        ca.AddPriority(new I(555), 1000);
        Assert.AreEqual(ca[4].x, 0);
        Assert.AreEqual(ca[5].x, 555);
        ca.Empty();
        Assert.AreEqual(ca.Count, 0);
    }
    
}
}