using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace ExternalMemory.Helper
{
    public static class HeapHelper
    {
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
    }
}
