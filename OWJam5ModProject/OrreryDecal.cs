﻿using System;
using UnityEngine;

namespace OWJam5ModProject
{
    internal class OrreryDecal : MonoBehaviour
    {
        [SerializeField] Transform orreryCenter = null;

        void Update()
        {
            transform.LookAt(orreryCenter, orreryCenter.transform.up);
        }
    }
}
