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
        public List<T> Items { get; } = new List<T>();
        private readonly bool _gameIs64Bit;

        #region Offsets
        protected ExternalOffset<IntPtr> _data;
        protected ExternalOffset<int> _count;
        protected ExternalOffset<int> _max;
        #endregion

        #region Props
        public int MaxCountTArrayCanCarry { get; } = 0x20000;
        public int DelayEvery { get; set; } = 1;
        public int Delay { get; set; } = 0;

        public IntPtr Data => _data.GetValue<IntPtr>();
        public int Count => _count.GetValue<int>();
        public int Max => _max.GetValue<int>();
        #endregion

        public TArray(ExternalMemorySharp emsInstance, IntPtr address) : base(emsInstance, address)
        {
            _gameIs64Bit = emsInstance.Is64BitGame;
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
            // Pointer Address
            int distance = _gameIs64Bit ? 8 : 4;

            // Get TArray Data
            Reader.ReadBytes(Data, Items.Count * distance, out byte[] tArrayData);
            for (int i = 0; i < Items.Count; i++)
            {
                int bIndex = i * distance;

                // Get Item Address
                IntPtr itemAddress;

                if (_gameIs64Bit)
                    itemAddress = (IntPtr)BitConverter.ToUInt64(tArrayData, bIndex);
                else
                    itemAddress = (IntPtr)BitConverter.ToUInt32(tArrayData, bIndex);

                // Update current item
                Items[i].UpdateAddress(itemAddress);
                Items[i].UpdateData();

                if (Delay == 0)
	                continue;

                counter++;
                if (counter < DelayEvery)
	                continue;

                Thread.Sleep(Delay);
                counter = 0;
            }

            return true;
        }
        private bool Read()
        {
            if (!Reader.ReadClass(this, BaseAddress))
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
                        var instance = (T)Activator.CreateInstance(typeof(T), Reader, (IntPtr)0x0);
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
