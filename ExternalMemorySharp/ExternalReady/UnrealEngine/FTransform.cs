using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
    public class FTransform : ExternalClass
    {
        #region Offsets
        private ExternalOffset _rotation;
        private ExternalOffset _translation;
        private ExternalOffset _scale3D;
        #endregion

        #region Props
        public ExternalMemorySharp Reader { get; }

        public Vector4 Rotation => _rotation.GetValue<Vector4>();
        public Vector3 Translation => _translation.GetValue<Vector3>();
        public Vector3 Scale3D => _scale3D.GetValue<Vector3>();
        #endregion

        public FTransform(ExternalMemorySharp emsInstance, IntPtr address) : base(address)
        {
            Reader = emsInstance;
        }

        protected override void InitOffsets()
        {
            _rotation = new ExternalOffset(ExternalOffset.None, 0x00, OffsetType.Vector4);
            _translation = new ExternalOffset(ExternalOffset.None, 0x10, OffsetType.Vector3);
            _scale3D = new ExternalOffset(ExternalOffset.None, 0x20, OffsetType.Vector3);
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
