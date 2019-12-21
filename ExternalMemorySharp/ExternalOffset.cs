using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using ExternalMemory.Helper;

namespace ExternalMemory
{
    public enum OffsetType
    {
        None,
        Custom,
        /// <summary>
        /// DON'T USE, It's For <see cref="ExternalOffset{T}"/> Only
        /// </summary>
        ExternalClass,

        Byte,
        Integer,
        Float,
        IntPtr,
        String,
        PString
    }

    public class ExternalOffset<T> : ExternalOffset where T : new()
    {
        public ExternalOffset(ExternalOffset dependency, int offset) : base(dependency, offset, OffsetType.Custom)
        {
            int size = 0;
            if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr))
            {
                OffsetType = OffsetType.IntPtr;
            }
            else if (typeof(T).IsSubclassOf(typeof(ExternalClass)))
            {
                OffsetType = OffsetType.ExternalClass;
                ExternalClassType = typeof(T);
                size = ((ExternalClass)Activator.CreateInstance(typeof(T))).ClassSize;
            }
            else
            {
                size = Marshal.SizeOf<T>();
            }

            ReSetValueSize(size);
        }

        public T GetValue() => GetValue<T>();
    }

    public class ExternalOffset
    {
        public static ExternalOffset None { get; } = new ExternalOffset(null, 0x0, OffsetType.None);

        public ExternalOffset Dependency { get; }
        public int Offset { get; }
        public OffsetType OffsetType { get; protected set; }

        /// <summary>
        /// DON'T USE, IT FOR `<see cref="ExternalOffset{T}"/>` And `<see cref="OffsetType"/>.ExternalClass` Only
        /// </summary>
        internal Type ExternalClassType { get; set; }

        /// <summary>
        /// DON'T USE, IT FOR `<see cref="ExternalOffset{T}"/>` And `<see cref="OffsetType"/>.ExternalClass` Only
        /// </summary>
        internal ExternalClass ExternalClassObject { get; set; }

        /// <summary>
        /// MemoryReader Used To Read This Offset
        /// </summary>
        internal ExternalMemorySharp Ems { get; set; }

        /// <summary>
        /// Offset Name
        /// </summary>
        internal string Name { get; set; }

        /// <summary>
        /// Offset Value As Bytes
        /// </summary>
        internal byte[] Value { get; set; }

        /// <summary>
        /// If Offset Is Pointer Then We Need A Place To Store
        /// Data It's Point To
        /// </summary>
        internal byte[] Data { get; set; }

        internal bool DataAssigned { get; private set; }
        internal int Size => Value.Length;
        internal bool IsGame64Bit => Ems?.Is64BitGame ?? false;

        public ExternalOffset(ExternalOffset dependency, int offset, OffsetType offsetType)
        {
            Dependency = dependency;
            Offset = offset;
            OffsetType = offsetType;

            SetValueSize();
        }

        /// <summary>
        /// Create Offset With Custom Size
        /// </summary>
        public ExternalOffset(ExternalOffset dependency, int offset, int size) : this(dependency, offset, OffsetType.Custom)
        {
            ReSetValueSize(size);
        }

        public T GetValue<T>()
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)Utils.BytesToString(Value, true).Trim('\0');
            }
            if (typeof(T) == typeof(IntPtr))
            {
                return (T)(object)(IntPtr)(IsGame64Bit ? GetValue<long>() : GetValue<int>());
            }
            if (typeof(T) == typeof(UIntPtr))
            {
                return (T)(object)(UIntPtr)(IsGame64Bit ? GetValue<ulong>() : GetValue<uint>());
            }
            if (typeof(T).IsSubclassOf(typeof(ExternalClass)))
            {
                return (T)(object)ExternalClassObject;
            }

            return (T)Convert.ChangeType((dynamic)Value.ToStructure(typeof(T)), typeof(T));
        }

        private void SetValueSize()
        {
            Value = OffsetType switch
            {
                OffsetType.None => new byte[1],
                OffsetType.Custom => new byte[1],

                OffsetType.Byte => new byte[1],
                OffsetType.Integer => new byte[4],
                OffsetType.Float => new byte[4],

                OffsetType.String => new byte[2],
                OffsetType.IntPtr => new byte[IsGame64Bit ? 8 : 4],
                OffsetType.PString => new byte[IsGame64Bit ? 8 : 4],

                _ => throw new ArgumentOutOfRangeException($"SetValueSize Can't set value size"),
            };
        }
        internal void ReSetValueSize(int newSize)
        {
            Value = new byte[newSize];
        }
        internal void SetValue(byte[] fullDependencyBytes)
        {
            // Init Dynamic Size Types (String, .., etc)
            if (OffsetType == OffsetType.String)
            {
                ReSetValueSize(GetStringSizeFromBytes(fullDependencyBytes, true));
            }

            // (Dependency == None) Mean it's Base Class Data
            Array.Copy(Dependency == None ? fullDependencyBytes : Dependency.Data, Offset, Value, 0, Value.Length);

            // It's Like Event
            OnSetValue();
        }
        internal void SetData(byte[] bytes)
        {
            DataAssigned = true;
            Data = bytes;
        }
        internal void RemoveValueAndData()
        {
            DataAssigned = false;
            if (Value != null)
                Array.Clear(Value, 0, Value.Length);
            if (Data != null)
                Array.Clear(Data, 0, Data.Length);
        }
        protected virtual void OnSetValue()
        {
        }

        internal int GetStringSizeFromBytes(byte[] bytes, bool isUnicode)
        {
            int retSize = 0;
            int charSize = isUnicode ? 2 : 1;

            while (true)
            {
                var buf = new byte[charSize];
                Array.Copy(bytes, Offset, buf, 0, charSize);

                retSize += charSize;

                // Null-Terminator
                if (buf.All(b => b == 0x00))
                    break;
            }

            return retSize;
        }
        public static ExternalOffset Parse(ExternalOffset dependency, string hexString)
        {
            string[] chunks = hexString.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
            int offset = int.Parse(chunks[0].Replace("0x", ""), NumberStyles.HexNumber);
            bool enumGood = Enum.TryParse(chunks[1], true, out OffsetType type);

            if (!enumGood)
                throw new Exception("Enum Bad Value.");

            return new ExternalOffset(dependency, offset, type);
        }
    }
}
