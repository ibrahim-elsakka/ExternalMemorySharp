using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace ExternalMemory.Helper
{
    public static class HeapHelper
    {
        public class StructAllocer<TStruct> : IDisposable
        {
            public IntPtr UnManagedPtr { get; private set; }
            public TStruct ManagedStruct { get; private set; }

            public StructAllocer()
            {
                UnManagedPtr = Marshal.AllocHGlobal(Marshal.SizeOf<TStruct>());
            }
            ~StructAllocer()
            {
                if (UnManagedPtr == IntPtr.Zero)
                    return;

                Marshal.FreeHGlobal(UnManagedPtr);
                UnManagedPtr = IntPtr.Zero;
            }

            /// <summary>
            /// Update unmanaged data from `<see cref="UnManagedPtr"/>` to managed struct
            /// </summary>
            public bool Update()
            {
                if (UnManagedPtr == IntPtr.Zero)
                    return false;

                ManagedStruct = Marshal.PtrToStructure<TStruct>(UnManagedPtr);
                return true;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(UnManagedPtr);
                UnManagedPtr = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }

            public static implicit operator IntPtr(StructAllocer<TStruct> w)
            {
                return w.UnManagedPtr;
            }
        }
        public class StringAllocer : IDisposable
        {
            public enum StringType
            {
                Ansi,
                Unicode
            }

            public IntPtr Ptr { get; private set; }
            public int Length { get; set; }
            public StringType StrType { get; }
            public string ManagedString { get; private set; }

            public StringAllocer(int len, StringType stringType)
            {
                StrType = stringType;
                Length = StrType == StringType.Ansi ? len : len * 2;
                Ptr = Marshal.AllocHGlobal(Length);
            }

            ~StringAllocer()
            {
                if (Ptr == IntPtr.Zero)
                    return;

                Marshal.FreeHGlobal(Ptr);
                Ptr = IntPtr.Zero;
            }

            /// <summary>
            /// Change size of allocated string.
            /// </summary>
            /// <param name="len">New size of string</param>
            public void ReSize(int len)
            {
                Length = StrType == StringType.Ansi ? len : len * 2;
                Ptr = Marshal.ReAllocHGlobal(Ptr, (IntPtr)Length);
                Update();
            }

            /// <summary>
            /// Update unmanaged data from <see cref="Ptr"/> to managed struct
            /// </summary>
            public bool Update()
            {
                if (Ptr == IntPtr.Zero)
                    return false;

                switch (StrType)
                {
                    case StringType.Ansi:
                        ManagedString = Marshal.PtrToStringAnsi(Ptr);
                        break;
                    case StringType.Unicode:
                        ManagedString = Marshal.PtrToStringUni(Ptr);
                        break;
                }

                return true;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(Ptr);
                Ptr = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }

            public static implicit operator IntPtr(StringAllocer w)
            {
                return w.Ptr;
            }

            public static implicit operator string(StringAllocer w)
            {
                return w.ManagedString;
            }
        }

        public static object ToStructure(this byte[] bytes, Type structType)
        {
            object stuff;
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                stuff = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), structType);
            }
            finally
            {
                handle.Free();
            }
            return stuff;
        }
        public static T ToStructure<T>(this byte[] bytes)
        {
            return (T)ToStructure(bytes, typeof(T));
        }

        public static byte[] ToByteArray<T>(this T obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
        public static T FromByteArray<T>(this byte[] data)
        {
            if (data == null)
                return default;
            BinaryFormatter bf = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream(data);
            object obj = bf.Deserialize(ms);
            return (T)obj;
        }
    }
}
