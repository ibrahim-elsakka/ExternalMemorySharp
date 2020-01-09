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

		public TEnumAsByte(ExternalMemorySharp emsInstance, IntPtr address) : base(emsInstance, address) { }

		protected override void InitOffsets()
		{
			_enumVal = new ExternalOffset<byte>(ExternalOffset.None, 0x00);
		}
	}
}
