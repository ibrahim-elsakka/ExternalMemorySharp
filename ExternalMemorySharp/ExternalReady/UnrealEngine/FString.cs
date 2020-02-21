using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class FString : ExternalClass
	{
		#region Offsets
		protected ExternalOffset<UIntPtr> _stringPointer;
		protected ExternalOffset<string> _stringData;
		#endregion

		#region Props
		public string Str => _stringData.Read();
		#endregion

		public FString(ExternalMemorySharp emsInstance, UIntPtr address) : base(emsInstance, address) {}

		/// <summary>
		/// Just use this constract for pass this class as genric Param <para/>
		/// Will Use <see cref="ExternalMemorySharp.MainEms"/> As Reader<para />
		/// You Can call '<see cref="ExternalClass.UpdateReader(ExternalMemorySharp)"/> To Override
		/// </summary>
		public FString() : this(ExternalMemorySharp.MainEms, UIntPtr.Zero) {}

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_stringPointer = new ExternalOffset<UIntPtr>(0x00);
			_stringData = new ExternalOffset<string>(_stringPointer, 0x00);
		}
	}
}
