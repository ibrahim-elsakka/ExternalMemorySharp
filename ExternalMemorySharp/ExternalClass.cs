using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExternalMemory.Helper;

namespace ExternalMemory
{
    public abstract class ExternalClass
    {
        public ExternalMemorySharp Ems { get; private set; }
        internal List<ExternalOffset> Offsets { get; }
        internal int ClassSize { get; set; }
        internal byte[] FullClassBytes { get; set; }

        public IntPtr BaseAddress { get; private set; }

        protected ExternalClass(ExternalMemorySharp emsInstance, IntPtr address)
        {
            Ems = emsInstance;
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
                    curOffset.Ems = emsInstance;

                    // If It's 32bit Game Then Pointer Is 4Byte
                    if (curOffset.OffsetType == OffsetType.IntPtr && !curOffset.IsGame64Bit)
                        curOffset.ReSetValueSize(0x4);
                    else if (curOffset.OffsetType == OffsetType.ExternalClass && curOffset.ExternalClassIsPointer)
                        curOffset.ReSetValueSize(curOffset.IsGame64Bit ? 0x8 : 0x4);

                    return curOffset;
                })
                .ToList();

            // Get Size Of Class
            ClassSize = Utils.GetDependenciesSize(ExternalOffset.None, Offsets);
        }

        /// <summary>
        /// Override this function to init Offsets Of Your Class
        /// </summary>
        protected virtual void InitOffsets()
        {
        }

        /// <summary>
        /// Update <see cref="BaseAddress"/> Of This Class
        /// </summary>
        /// <param name="newAddress"></param>
        public virtual void UpdateAddress(IntPtr newAddress)
        {
            BaseAddress = newAddress;
        }

        /// <summary>
        /// Update Memory Reader/Writer
        /// </summary>
        /// <param name="externalMemorySharp">New External Memory Sharp Class That's Will Used To Read And Write</param>
        public void UpdateReader(ExternalMemorySharp externalMemorySharp)
        {
            Ems = externalMemorySharp;
        }

        /// <summary>
        /// Read Data And Set It On This Class
        /// </summary>
        public virtual bool UpdateData()
        {
	        return BaseAddress != IntPtr.Zero && Ems.ReadClass(this, BaseAddress);
        }
    }
}
