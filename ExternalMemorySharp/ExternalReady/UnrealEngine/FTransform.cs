using System;
using System.Numerics;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
    public class FTransform : ExternalClass
    {
        public static FTransform Zero = new FTransform(ExternalMemorySharp.MainEms, UIntPtr.Zero);
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
        
        public FTransform(ExternalMemorySharp emsInstance, UIntPtr address) : base(emsInstance, address) {}
        /// <summary>
        /// Just use this constract for pass this class as Genric Param <para/>
        /// Will Use <see cref="ExternalMemorySharp.MainEms"/> As Reader<para />
        /// You Can call '<see cref="ExternalClass.UpdateReader(ExternalMemorySharp)"/> To Override
        /// </summary>
        public FTransform() : this(ExternalMemorySharp.MainEms, UIntPtr.Zero) {}

        protected override void InitOffsets()
        {
	        base.InitOffsets();

            _rotation = new ExternalOffset<Vector4>(ExternalOffset.None, 0x00);
            _translation = new ExternalOffset<Vector3>(ExternalOffset.None, 0x10);
            _scale3D = new ExternalOffset<Vector3>(ExternalOffset.None, 0x1C);
        }

        public Matrix4x4 ToMatrixWithScale()
        {
	        var m = new Matrix4x4();

	        m.M41 = Translation.X;
	        m.M42 = Translation.Y;
	        m.M43 = Translation.Z;

	        float x2 = Rotation.X + Rotation.X;
	        float y2 = Rotation.Y + Rotation.Y;
	        float z2 = Rotation.Z + Rotation.Z;

	        float xx2 = Rotation.X * x2;
	        float yy2 = Rotation.Y * y2;
	        float zz2 = Rotation.Z * z2;
	        m.M11 = (1.0f - (yy2 + zz2)) * Scale3D.X;
	        m.M22 = (1.0f - (xx2 + zz2)) * Scale3D.Y;
	        m.M33 = (1.0f - (xx2 + yy2)) * Scale3D.Z;


	        float yz2 = Rotation.Y * z2;
	        float wx2 = Rotation.W * x2;
	        m.M32 = (yz2 - wx2) * Scale3D.Z;
	        m.M23 = (yz2 + wx2) * Scale3D.Y;


	        float xy2 = Rotation.X * y2;
	        float wz2 = Rotation.W * z2;
	        m.M21 = (xy2 - wz2) * Scale3D.Y;
	        m.M12 = (xy2 + wz2) * Scale3D.X;


	        float xz2 = Rotation.X * z2;
	        float wy2 = Rotation.W * y2;
	        m.M31 = (xz2 + wy2) * Scale3D.Z;
	        m.M13 = (xz2 - wy2) * Scale3D.X;

	        m.M14 = 0.0f;
	        m.M24 = 0.0f;
	        m.M34 = 0.0f;
	        m.M44 = 1.0f;

	        return m;
        }
    }
}
