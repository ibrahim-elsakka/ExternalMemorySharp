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
		/// <summary>
		/// Just use this constract for pass this class as Genric Param <para/>
		/// Will Use <see cref="ExternalMemorySharp.MainEms"/> As Reader<para />
		/// You Can call '<see cref="ExternalClass.UpdateReader(ExternalMemorySharp)"/> To Override
		/// </summary>
		public TEnumAsByte() : this(ExternalMemorySharp.MainEms, IntPtr.Zero) { }

		protected override void InitOffsets()
		{
			_enumVal = new ExternalOffset<byte>(ExternalOffset.None, 0x00);
		}
	}
}
