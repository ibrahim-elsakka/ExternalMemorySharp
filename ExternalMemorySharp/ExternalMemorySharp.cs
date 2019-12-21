using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExternalMemory.Helper;

namespace ExternalMemory
{
    public class ExternalMemorySharp
    {
        #region Delegates
        public delegate bool ReadCallBack(IntPtr address, long size, out byte[] bytes);
        public delegate bool WriteCallBack(IntPtr address, byte[] bytes);
        #endregion

        #region Props
        public ReadCallBack ReadBytesCallBack { get; }
        public WriteCallBack WriteBytesCallBack { get; }
        public bool Is64BitGame { get; }
        public int PointerSize { get; }
        #endregion

        public ExternalMemorySharp(ReadCallBack readBytesDelegate, WriteCallBack writeBytesDelegate, bool is64BitGame)
        {
            Is64BitGame = is64BitGame;
            PointerSize = Is64BitGame ? 0x8 : 0x4;
            ReadBytesCallBack = readBytesDelegate;
            WriteBytesCallBack = writeBytesDelegate;
        }
        public bool ReadBytes(IntPtr address, int size, out byte[] bytes)
        {
            bool retState = ReadBytesCallBack(address, size, out bytes);
            // if (!retState)
                // throw new Exception($"Can't read memory at `0x{address.ToInt64():X8}`");

            return retState;
        }
        private string ReadString(IntPtr lpBaseAddress, bool isUnicode = false)
        {
            int charSize = isUnicode ? 2 : 1;
            string ret = string.Empty;

            while (true)
            {
                if (!ReadBytes(lpBaseAddress, charSize, out byte[] buf))
                    break;

                // Null-Terminator
                if (buf.All(b => b == 0x00))
                    break;

                ret += isUnicode ? Encoding.Unicode.GetString(buf) : Encoding.ASCII.GetString(buf);
                lpBaseAddress += charSize;
            }

            return ret;
        }
        private static void RemoveValueData(IEnumerable<ExternalOffset> unrealOffsets)
        {
            foreach (var unrealOffset in unrealOffsets)
                unrealOffset.RemoveValueAndData();
        }
        private static List<ExternalOffset> GetOffsets<T>(T instance) where T : ExternalClass
        {
            // Collect Data From Offsets
            List<ExternalOffset> unrealOffsets = instance.Offsets
                .Where(f => f != null)
                .ToList();

            // Sort By Dependencies
            unrealOffsets = unrealOffsets.Sort(off => unrealOffsets.Where(offset => offset == off.Dependency));

            return unrealOffsets;
        }

        public bool ReadClass<T>(T instance, byte[] fullClassBytes) where T : ExternalClass
        {
            // Collect Offsets
            List<ExternalOffset> unrealOffsets = GetOffsets(instance);

            // Set Bytes
            instance.FullClassBytes = fullClassBytes;

            // Read Offsets
            foreach (ExternalOffset offset in unrealOffsets)
            {
                #region SetValue
                // if it's Base Offset
                if (offset.Dependency == ExternalOffset.None)
                {
                    offset.SetValue(instance.FullClassBytes);
                }
                else if (offset.Dependency.DataAssigned)
                {
                    offset.SetValue(offset.Dependency.Data);
                }
                // Dependency Is Null-Pointer OR Bad Pointer Then Just Skip
                else if (offset.Dependency.OffsetType == OffsetType.IntPtr && !offset.Dependency.DataAssigned)
                {
                    continue;
                }
                else
                {
                    throw new Exception("Dependency Data Not Set !!");
                }
                #endregion

                #region Pre-Pointer Types (PString, .., etc)
                // Init
                if (offset.OffsetType == OffsetType.PString)
                {
                    // Get Pointer
                    IntPtr pStr = offset.GetValue<IntPtr>();
                    bool isUni = true;

                    if (pStr != IntPtr.Zero)
                    {
                        string str = ReadString(pStr, isUni);
                        offset.Value = Utils.StringToBytes(str, isUni);
                    }
                }
                #endregion

                #region Init For Dependencies
                // If It's Pointer, Read Pointed Data To Use On Other Offset Dependent On It
                if (offset.OffsetType == OffsetType.IntPtr)
                {
                    // Get Size Of Pointed Data
                    int pointedSize = Utils.GetDependenciesSize(offset, unrealOffsets);

                    // If Size Is Zero Then It's Usually Dynamic (Unknown Size) Pointer (Like `Data` Member In `TArray`)
                    // Or Just An Pointer Without Dependencies
                    if (pointedSize == 0)
                        continue;

                    // Can't Read Bytes
                    if (!ReadBytes(offset.GetValue<IntPtr>(), pointedSize, out byte[] dataBytes))
                        continue;

                    offset.SetData(dataBytes);
                }

                // Nested External Class (For Now I Support Pointer Of Type Only)
                else if (offset.OffsetType == OffsetType.ExternalClass)
                {
                    // Get Address Of Nested Class
                    IntPtr valPtr = offset.GetValue<IntPtr>();

                    // Read Nested Class
                    offset.ExternalClassObject = (ExternalClass)Activator.CreateInstance(offset.ExternalClassType);
                    if (!ReadClass(offset.ExternalClassObject, valPtr))
                        throw new Exception($"Can't Read `{offset.ExternalClassType.Name}` As `ExternalClass`.", new Exception($"Value Count = {offset.Size}"));
                }
                #endregion
            }

            return true;
        }
        public bool ReadClass<T>(T instance, IntPtr address) where T : ExternalClass
        {
            // Collect Offsets
            List<ExternalOffset> unrealOffsets = GetOffsets(instance);

            if (address.ToInt64() <= 0)
            {
                // Clear All Class Offset
                RemoveValueData(unrealOffsets);
                return false;
            }

            // Read Full Class
            if (!ReadBytes(address, instance.ClassSize, out byte[] fullClassBytes))
            {
                // Clear All Class Offset
                RemoveValueData(unrealOffsets);
                return false;
            }

            return ReadClass(instance, fullClassBytes);
        }

        public void ReadClass<T>(T instance, int address) where T : ExternalClass, new() => ReadClass(instance, (IntPtr)address);
        public void ReadClass<T>(T instance, long address) where T : ExternalClass, new() => ReadClass(instance, (IntPtr)address);
    }
}
