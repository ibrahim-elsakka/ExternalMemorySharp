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

        Byte,
        Integer,
        Float,
        IntPtr,
        String,
        PString
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ExternalOffset<T> : ExternalOffset
    {
        public ExternalOffset(ExternalOffset dependency, int offset) : base(dependency, offset, OffsetType.Custom)
        {
            if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr))
            {
                OffsetType = OffsetType.IntPtr;
            }

            var size = Marshal.SizeOf<T>();
            ReSetValueSize(size);
        }

        public T GetValue()
        {
            return GetValue<T>();
        }
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ExternalOffset
    {
        public static ExternalOffset None { get; } = new ExternalOffset(null, 0x0, OffsetType.None);

        public ExternalOffset Dependency { get; }
        public int Offset { get; }
        public OffsetType OffsetType { get; protected set; }

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
        public bool IsGame64Bit { get; internal set; }

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
            OffsetType type = (OffsetType)Enum.Parse(typeof(OffsetType), chunks[1]);

            return new ExternalOffset(dependency, offset, type);
        }
    }
}
