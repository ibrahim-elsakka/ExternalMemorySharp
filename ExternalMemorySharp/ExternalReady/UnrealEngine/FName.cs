using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class FName : ExternalClass
	{
		#region Offsets
		protected ExternalOffset<int> _index;
		protected ExternalOffset<int> _number;
		#endregion

		#region Props
		public int Index => _index.Read();
		public int Number => _number.Read();
		#endregion

		public FName(ExternalMemorySharp emsInstance, UIntPtr address) : base(emsInstance, address) {}

		/// <summary>
		/// Just use this constract for pass this class as Genric Param <para/>
		/// Will Use <see cref="ExternalMemorySharp.MainEms"/> As Reader<para />
		/// You Can call '<see cref="ExternalClass.UpdateReader(ExternalMemorySharp)"/> To Override
		/// </summary>
		public FName() : this(ExternalMemorySharp.MainEms, UIntPtr.Zero) {}

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_index = new ExternalOffset<int>(ExternalOffset.None, 0x00);
			_number = new ExternalOffset<int>(ExternalOffset.None, 0x04);
		}
	}
}
