using System;
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

    public class ExternalOffset<T> : ExternalOffset
    {
        public ExternalOffset(int offset) : this(None, offset) {}
        public ExternalOffset(int offset, bool externalClassIsPointer) : this(None, offset, externalClassIsPointer) {}

        public ExternalOffset(ExternalOffset dependency, int offset) : base(dependency, offset, OffsetType.Custom)
        {
            Init();
        }
        public ExternalOffset(ExternalOffset dependency, int offset, bool externalClassIsPointer) : base(dependency, offset, OffsetType.ExternalClass)
        {
            if (!typeof(T).IsSubclassOf(typeof(ExternalClass)))
                throw new Exception("This Constructor For `ExternalClass` Types Only.");

            ExternalClassType = typeof(T);
            ExternalClassIsPointer = externalClassIsPointer;

            Init();
        }

        private void Init()
        {
            int size = 0;
            if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr))
            {
                OffsetType = OffsetType.IntPtr;
            }
            else if (typeof(T).IsSubclassOf(typeof(ExternalClass)))
            {
                // OffsetType Set On Other Constructor If It's `ExternalClass`
                if (OffsetType != OffsetType.ExternalClass)
                    throw new Exception("Use Other Constructor For `ExternalClass` Types.");

                // If externalClassIsPointer == true, ExternalClass Will Fix The Size Before Calc Class Size
                // So It's Okay To Leave It Like That
                size = ((ExternalClass)Activator.CreateInstance(typeof(T))).ClassSize;
            }
            else
            {
                size = Marshal.SizeOf<T>();
            }

            ReSetValueSize(size);
        }

        public T GetValue() => GetValue<T>();
        internal void SetValue(T value) => SetValue<T>(value);
        public bool Write(T value) => Write<T>(value);
    }

    public class ExternalOffset
    {
        public static ExternalOffset None { get; } = new ExternalOffset(null, 0x0, OffsetType.None);

        internal IntPtr OffsetAddress { get; set; }
        public ExternalOffset Dependency { get; }
        public int Offset { get; }
        public OffsetType OffsetType { get; protected set; }

        #region GenricExternalClass
        /// <summary>
        /// DON'T USE, IT FOR `<see cref="ExternalOffset{T}"/>` And `<see cref="OffsetType"/>.ExternalClass` Only
        /// </summary>
        internal Type ExternalClassType { get; set; }

        /// <summary>
        /// DON'T USE, IT FOR `<see cref="ExternalOffset{T}"/>` And `<see cref="OffsetType"/>.ExternalClass` Only
        /// </summary>
        internal ExternalClass ExternalClassObject { get; set; }

        /// <summary>
        /// DON'T USE, IT FOR `<see cref="ExternalOffset{T}"/>` And `<see cref="OffsetType"/>.ExternalClass` Only
        /// </summary>
        internal bool ExternalClassIsPointer { get; set; }
        #endregion

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

        public ExternalOffset(int offset, OffsetType offsetType) : this(None, offset, offsetType) {}
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
            Type tType = typeof(T);

            if (tType == typeof(string))
            {
                return (T)(object)Utils.BytesToString(Value, true).Trim('\0');
            }
            if (tType == typeof(IntPtr))
            {
                return (T)(object)(IntPtr)(IsGame64Bit ? GetValue<long>() : GetValue<int>());
            }
            if (tType == typeof(UIntPtr))
            {
                return (T)(object)(UIntPtr)(IsGame64Bit ? GetValue<ulong>() : GetValue<uint>());
            }
            if (tType.IsSubclassOf(typeof(ExternalClass)))
            {
                return (T)(object)ExternalClassObject;
            }

            // return (T)Convert.ChangeType((dynamic)Value.ToStructure(typeof(T)), typeof(T));
            return Value.Length == 0 ? Activator.CreateInstance<T>() : new MarshalType<T>().ByteArrayToObject(Value);
        }
        public bool Write<T>(T value)
        {
            if (OffsetAddress == IntPtr.Zero)
            {
                return false;
            }

            SetValue(value);
            return Ems.WriteBytes(OffsetAddress, Value);
        }

        internal void SetValue<T>(T value)
		{
            if (value == null)
                throw new ArgumentNullException("'value' Can't be null.");

            Type tType = typeof(T);

            if (tType == typeof(string))
			{
				Value = Utils.StringToBytes(((string)(object)value).Trim('\0'), true);
			}
			if (tType == typeof(IntPtr))
			{
				Value = IsGame64Bit ? ((long)(object)value).ToByteArray() : ((int)(object)value).ToByteArray();
			}
			if (tType == typeof(UIntPtr))
			{
				Value = IsGame64Bit ? ((ulong)(object)value).ToByteArray() : ((uint)(object)value).ToByteArray();
			}
			if (tType.IsSubclassOf(typeof(ExternalClass)))
			{
				ExternalClassObject = (ExternalClass)(object)value;
			}

			Value = new MarshalType<T>().ObjectToByteArray(value);
		}
        private void SetValueSize()
        {
            Value = OffsetType switch
            {
                OffsetType.None => new byte[1],
                OffsetType.Custom => new byte[1],
                OffsetType.ExternalClass => new byte[1],

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
        internal void SetValueBytes(byte[] fullDependencyBytes)
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
