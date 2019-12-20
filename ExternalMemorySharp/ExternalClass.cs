using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExternalMemory.Helper;

namespace ExternalMemory
{
    public abstract class ExternalClass
    {
        internal ExternalMemorySharp Reader { get; }
        internal List<ExternalOffset> Offsets { get; }
        internal int ClassSize { get; set; }
        internal byte[] FullClassBytes { get; set; }

        public IntPtr BaseAddress { get; set; }

        protected ExternalClass(ExternalMemorySharp emsInstance, IntPtr address)
        {
            Reader = emsInstance;
            BaseAddress = address;

            // ReSharper disable once VirtualMemberCallInConstructor
            InitOffsets();

            // Get All Offset Props
            Offsets = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(ExternalOffset) || f.FieldType.IsSubclassOfRawGeneric(typeof(ExternalOffset<>)))
                .Select(f =>
                {
                    ExternalOffset curOffset = (ExternalOffset)f.GetValue(this);

                    // Set Info
                    curOffset.Name = f.Name;
                    curOffset.IsGame64Bit = emsInstance.Is64BitGame;

                    // If It's 32bit Game Then Pointer Is 4Byte
                    if (curOffset.OffsetType == OffsetType.IntPtr && !curOffset.IsGame64Bit)
                        curOffset.ReSetValueSize(4);

                    return curOffset;
                })
                .ToList();

            // Get Size Of Class
            ClassSize = Utils.GetDependenciesSize(ExternalOffset.None, Offsets);
        }

        protected abstract void InitOffsets();
    }
}
