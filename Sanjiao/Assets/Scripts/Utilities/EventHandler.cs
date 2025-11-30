using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventHandler
{
    public static event Action GameStarted;
    public static void CallGameStarted()
    {
        GameStarted?.Invoke();
    }
}
