using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public abstract class CompleteBonus : ILiveContent {
    internal abstract IEnumerator Logic();

    internal abstract bool IsComplete();
}