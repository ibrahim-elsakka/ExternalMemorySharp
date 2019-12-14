using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ExternalMemory.Helper;

namespace ExternalMemory
{
    public enum OffsetType
    {
        None,
        Byte,
        Integer,
        Float,
        IntPtr,
        String,
        PString,
        Vector2,
        Vector3,
        Vector4,
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ExternalOffset
    {
        public static ExternalOffset None { get; } = new ExternalOffset(null, 0x0, OffsetType.None);

        public ExternalOffset Dependency { get; }
        public int Offset { get; }
        public OffsetType OffsetType { get; }

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
        internal bool IsGame64Bit { get; set; } = false;
        internal bool DataAssigned { get; private set; }

        public ExternalOffset(ExternalOffset dependency, int offset, OffsetType offsetType)
        {
            Dependency = dependency;
            Offset = offset;
            OffsetType = offsetType;

            SetValueSize();
        }
        private void SetValueSize()
        {
            Value = OffsetType switch
            {
                OffsetType.None => new byte[1],
                OffsetType.Byte => new byte[1],
                OffsetType.Integer => new byte[4],
                OffsetType.Float => new byte[4],
                OffsetType.IntPtr => new byte[IsGame64Bit ? 8 : 4],
                OffsetType.String => new byte[IsGame64Bit ? 8 : 4],
                OffsetType.PString => new byte[IsGame64Bit ? 8 : 4],
                OffsetType.Vector2 => new byte[4 * 2],
                OffsetType.Vector3 => new byte[4 * 3],
                OffsetType.Vector4 => new byte[4 * 4],
                _ => throw new ArgumentOutOfRangeException($"SetValueSize Can't set value size"),
            };
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
            if (typeof(T) == typeof(Vector2))
            {
                var ret = new Vector2
                {
                    X = BitConverter.ToSingle(Value, 0x00),
                    Y = BitConverter.ToSingle(Value, 0x04)
                };
                return (T)(object)ret;
            }
            if (typeof(T) == typeof(Vector3))
            {
                var ret = new Vector3
                {
                    X = BitConverter.ToSingle(Value, 0x00),
                    Y = BitConverter.ToSingle(Value, 0x04),
                    Z = BitConverter.ToSingle(Value, 0x08)
                };
                return (T)(object)ret;
            }
            if (typeof(T) == typeof(Vector4))
            {
                var ret = new Vector4
                {
                    X = BitConverter.ToSingle(Value, 0x0),
                    Y = BitConverter.ToSingle(Value, 0x4),
                    Z = BitConverter.ToSingle(Value, 0x8),
                    W = BitConverter.ToSingle(Value, 0xC)
                };
                return (T)(object)ret;
            }

            return (T)Convert.ChangeType((dynamic)Value.ToStructure(typeof(T)), typeof(T));
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

        public int GetStringSizeFromBytes(byte[] bytes, bool isUnicode)
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
