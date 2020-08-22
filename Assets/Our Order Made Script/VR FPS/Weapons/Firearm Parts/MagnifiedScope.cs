using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class MagnifiedScope : Attachment
    {

        private Camera scopeCamera;
        [SerializeField] protected GameObject scopeTexture;

        protected override void Start() { base.Start(); scopeCamera = GetComponentInChildren<Camera>(); }

        public override void Enable()
        {
            scopeCamera.enabled = true;
            scopeTexture.SetActive(true);
        }

        public override void Disable()
        {
            scopeCamera.enabled = false;
            scopeTexture.SetActive(false);
        }
    }
}