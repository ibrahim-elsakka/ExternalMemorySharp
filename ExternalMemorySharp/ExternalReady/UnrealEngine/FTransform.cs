using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
    public class FTransform : ExternalClass
    {
        #region Offsets
        protected ExternalOffset<Vector4> _rotation;
        protected ExternalOffset<Vector3> _translation;
        protected ExternalOffset<Vector3> _scale3D;
        #endregion

        #region Props
        public Vector4 Rotation
        {
            get => _rotation.GetValue();
            set => _rotation.Write(value);
        }
        public Vector3 Translation
        {
            get => _translation.GetValue();
            set => _translation.Write(value);
        }
        public Vector3 Scale3D
        {
            get => _scale3D.GetValue();
            set => _scale3D.Write(value);
        }
        #endregion

        /// <summary>
        /// Just use this constract for pass this class as Genric Param <para/>
        /// Must call '<see cref="ExternalClass.UpdateAddress(IntPtr)"/> <para />
        /// Must call '<see cref="ExternalClass.UpdateReader(ExternalMemorySharp)"/> <para />
        /// </summary>
        public FTransform() : base(null, IntPtr.Zero) {}
        public FTransform(ExternalMemorySharp emsInstance, IntPtr address) : base(emsInstance, address) {}

        protected override void InitOffsets()
        {
            _rotation = new ExternalOffset<Vector4>(ExternalOffset.None, 0x00);
            _translation = new ExternalOffset<Vector3>(ExternalOffset.None, 0x10);
            _scale3D = new ExternalOffset<Vector3>(ExternalOffset.None, 0x1C);
        }
    }
}
