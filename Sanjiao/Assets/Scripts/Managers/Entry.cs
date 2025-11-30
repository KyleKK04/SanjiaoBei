using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Core
{
    public class Entry : MonoBehaviour
    {
        public AssetReference assetReference;
        private void Awake()
        {
            Addressables.LoadSceneAsync(assetReference);
        }
    }
}