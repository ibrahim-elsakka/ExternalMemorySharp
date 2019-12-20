using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
    public class FTransform : ExternalClass
    {
        #region Offsets
        private ExternalOffset<Vector4> _rotation;
        private ExternalOffset<Vector3> _translation;
        private ExternalOffset<Vector3> _scale3D;
        #endregion

        #region Props
        public Vector4 Rotation => _rotation.GetValue();
        public Vector3 Translation => _translation.GetValue();
        public Vector3 Scale3D => _scale3D.GetValue();
        #endregion

        public FTransform(ExternalMemorySharp emsInstance, IntPtr address) : base(emsInstance, address)
        {
        }

        protected override void InitOffsets()
        {
            _rotation = new ExternalOffset<Vector4>(ExternalOffset.None, 0x00);
            _translation = new ExternalOffset<Vector3>(ExternalOffset.None, 0x10);
            _scale3D = new ExternalOffset<Vector3>(ExternalOffset.None, 0x1C);
        }

        public void UpdateAddress(IntPtr newAddress)
        {
            BaseAddress = newAddress;
        }
        public void Update()
        {
            Reader.ReadClass(this, BaseAddress);
        }
    }
}
