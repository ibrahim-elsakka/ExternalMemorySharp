using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class TEnumAsByte<T> : ExternalClass where T : Enum
	{
		#region Offsets
		protected ExternalOffset<byte> _enumVal;
		#endregion

		#region Props
		public T Value => (T)(object)_enumVal.GetValue();
		#endregion

		/// <summary>
		/// Just use this constract for pass this class as Genric Param <para/>
		/// Must call '<see cref="ExternalClass.UpdateAddress(IntPtr)"/> <para />
		/// Must call '<see cref="ExternalClass.UpdateReader(ExternalMemorySharp)"/> <para />
		/// </summary>
		public TEnumAsByte() : base(null, IntPtr.Zero) { }
		public TEnumAsByte(ExternalMemorySharp emsInstance, IntPtr address) : base(emsInstance, address) { }

		protected override void InitOffsets()
		{
			_enumVal = new ExternalOffset<byte>(ExternalOffset.None, 0x00);
		}
	}
}
