using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// TArray Class To Fit UnrealEngine, It's only Support Pointer To <see cref="ExternalClass"/> Only
    /// </summary>
    public class TArray<T> : ExternalClass where T : ExternalClass, new()
    {
        public class DelayData
        {
            public int DelayEvery { get; set; } = 1;
            public int Delay { get; set; } = 0;
        }
        public class ReadData
        {
            public bool IsPointer { get; set; } = true;
            public int BadSizeAfterEveryItem { get; set; } = 0x0;
        }

        public List<T> Items { get; } = new List<T>();
        private readonly bool _gameIs64Bit;
        private readonly int _itemSize;

        #region Offsets
        protected ExternalOffset<IntPtr> _data;
        protected ExternalOffset<int> _count;
        protected ExternalOffset<int> _max;
        #endregion

        #region Props
        public int MaxCountTArrayCanCarry { get; } = 0x20000;
        public DelayData DelaypInfo { get; } = new DelayData();
        public ReadData ReadInfo { get; } = new ReadData();

        public IntPtr Data => _data.GetValue<IntPtr>();
        public int Count => _count.GetValue<int>();
        public int Max => _max.GetValue<int>();
        #endregion

        /// <summary>
        /// Just use this constract for pass this class as Genric Param <para/>
        /// Must call '<see cref="ExternalClass.UpdateAddress(IntPtr)"/> <para />
        /// Must call '<see cref="ExternalClass.UpdateReader(ExternalMemorySharp)"/> <para />
        /// </summary>
        public TArray() : base(null, IntPtr.Zero) { }
        public TArray(ExternalMemorySharp emsInstance, IntPtr address) : base(emsInstance, address)
        {
            _gameIs64Bit = emsInstance.Is64BitGame;
            _itemSize = ((T)Activator.CreateInstance(typeof(T), Ems, (IntPtr)0x0)).ClassSize;

        }
        public TArray(ExternalMemorySharp emsInstance, IntPtr address, int maxCountTArrayCanCarry) : this(emsInstance, address)
        {
            MaxCountTArrayCanCarry = maxCountTArrayCanCarry;
        }

        protected override void InitOffsets()
        {
            int curOff = 0x0;
            _data = new ExternalOffset<IntPtr>(ExternalOffset.None, curOff); curOff += _gameIs64Bit ? 0x8 : 0x4;
            _count = new ExternalOffset<int>(ExternalOffset.None, curOff); curOff += 0x4;
            _max = new ExternalOffset<int>(ExternalOffset.None, curOff);
        }

        public override bool UpdateData()
        {
            // Read Array (Base and Size)
            if (!Read())
                return false;

            int counter = 0;
            int itemSize = ReadInfo.IsPointer ? (_gameIs64Bit ? 8 : 4) : _itemSize;

            // Get TArray Data
            Ems.ReadBytes(Data, Items.Count * itemSize, out byte[] tArrayData);
            var bytes = new List<byte>(tArrayData);

            for (int i = 0; i < Items.Count; i++)
            {
                int offset = i * itemSize;

                // Get Item Address (Pointer Value (aka Pointed Address))
                IntPtr itemAddress;
                if (_gameIs64Bit)
                    itemAddress = (IntPtr)BitConverter.ToUInt64(tArrayData, offset);
                else
                    itemAddress = (IntPtr)BitConverter.ToUInt32(tArrayData, offset);

                // Update current item
                Items[i].UpdateAddress(itemAddress);

                if (ReadInfo.IsPointer)
                    Items[i].UpdateData();
                else
                    Items[i].UpdateData(bytes.GetRange(offset, itemSize).ToArray());

                if (DelaypInfo.Delay == 0)
	                continue;

                counter++;
                if (counter < DelaypInfo.DelayEvery)
	                continue;

                Thread.Sleep(DelaypInfo.Delay);
                counter = 0;
            }

            return true;
        }
        private bool Read()
        {
            if (!Ems.ReadClass(this, BaseAddress))
                return false;

            if (Count > MaxCountTArrayCanCarry)
                return false;

            // TODO: Change This Logic
            try
            {
                if (Items.Count > Count)
                {
                    Items.RemoveRange(Count, Items.Count - Count);
                }
                else if (Items.Count < Count)
                {
                    Enumerable.Range(Items.Count, Count).ToList().ForEach(num =>
                    {
                        T instance = (T)Activator.CreateInstance(typeof(T), Ems, (IntPtr)0x0);
                        Items.Add(instance);
                    });
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
        public bool IsValid()
        {
            if (Count == 0 && !Read())
                return false;

            return (Max > Count) && BaseAddress != IntPtr.Zero;
        }

        #region Indexer
        public T this[int index] => Items[index];
        #endregion
    }
}
