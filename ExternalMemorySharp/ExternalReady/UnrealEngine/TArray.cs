using System;
using System.Collections.Generic;
using System.Linq;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
    // ToDo: Add Function CallBack To Get New Address Every Time Read Called
    public class TArray<T> : ExternalClass where T : ExternalClass, new()
    {
        public List<T> Items { get; } = new List<T>();
        private readonly bool _gameIs64Bit;

        #region Offsets
        private ExternalOffset _data;
        private ExternalOffset _count;
        private ExternalOffset _max;
        #endregion

        #region Props
        public ExternalMemorySharp Reader { get; }
        public int MaxCountTArrayCanCarry { get; } = 0x20000;

        public IntPtr Data => _data.GetValue<IntPtr>();
        public int Count => _count.GetValue<int>();
        public int Max => _max.GetValue<int>();
        #endregion

        public TArray(ExternalMemorySharp emsInstance, IntPtr address, bool gameIs64Bit) : base(address)
        {
            Reader = emsInstance;
            _gameIs64Bit = gameIs64Bit;
        }
        public TArray(ExternalMemorySharp emsInstance, IntPtr address, bool gameIs64Bit, int maxCountTArrayCanCarry) : base(address)
        {
            Reader = emsInstance;
            _gameIs64Bit = gameIs64Bit;
            MaxCountTArrayCanCarry = maxCountTArrayCanCarry;
        }

        protected override void InitOffsets()
        {
            int curOff = 0x0;
            _data = new ExternalOffset(ExternalOffset.None, curOff, OffsetType.IntPtr); curOff += _gameIs64Bit ? 0x8 : 0x4;
            _count = new ExternalOffset(ExternalOffset.None, curOff, OffsetType.Integer); curOff += 0x4;
            _max = new ExternalOffset(ExternalOffset.None, curOff, OffsetType.Integer);
        }

        public void UpdateAddress(IntPtr newAddress)
        {
            BaseAddress = newAddress;
        }
        public void Update()
        {
            // Read Array (Base and Size)
            if (!Read())
                return;

            // Pointer Address + Some Junk Two Int
            int distance = (_gameIs64Bit ? 8 : 4) + 0x8;

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
                Items[i].BaseAddress = itemAddress;
                Reader.ReadClass(Items[i], itemAddress);
            }
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
                    Enumerable.Range(Items.Count, Count).ToList().ForEach(num => Items.Add((T)Activator.CreateInstance(typeof(T), (IntPtr)0x0)));
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        #region Indexer
        public T this[int index] => Items[index];
        #endregion
    }
}
