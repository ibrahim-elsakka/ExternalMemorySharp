using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class FString : ExternalClass
	{
		#region Offsets
		protected ExternalOffset<IntPtr> _stringPointer;
		protected ExternalOffset<string> _stringData;
		#endregion

		#region Props
		public string Str => _stringData.GetValue();
		#endregion

		public FString(ExternalMemorySharp emsInstance, IntPtr address) : base(emsInstance, address) {}

		/// <summary>
		/// Just use this constract for pass this class as genric Param <para/>
		/// Will Use <see cref="ExternalMemorySharp.MainEms"/> As Reader<para />
		/// You Can call '<see cref="ExternalClass.UpdateReader(ExternalMemorySharp)"/> To Override
		/// </summary>
		public FString() : this(ExternalMemorySharp.MainEms, IntPtr.Zero) {}

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_stringPointer = new ExternalOffset<IntPtr>(0x00);
			_stringData = new ExternalOffset<string>(_stringPointer, 0x00);
		}
	}
}
