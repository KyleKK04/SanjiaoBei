using System;
using System.Collections;
using System.Collections.Generic;
using Game.Data;
using TMPro;
using UnityEngine;

namespace Game.Visual
{
    public class DoorTextEffect : MonoBehaviour
    {
        private TextMeshPro textMesh;
        public DoorController door;
        
        private void Start()
        {
            textMesh = GetComponent<TextMeshPro>();
            //拿到父物体的DoorController
            door = GetComponentInParent<DoorController>();
            if (door.doorType == DoorType.EndDoor)
            {
                textMesh.text = $"{door.requiredPower}";
            }
            else if (door.doorType == DoorType.BeginDoor)
            {
                this.gameObject.SetActive(false);
            }
        }
    }
}