using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExternalMemory.Helper;

namespace ExternalMemory
{
    public abstract class ExternalClass
    {
        public IntPtr BaseAddress { get; set; }
        internal List<ExternalOffset> Offsets { get; }
        internal int ClassSize { get; set; }
        internal byte[] FullClassBytes { get; set; }

        protected ExternalClass()
        {
            throw new Exception("Don't Call Empty Constructor.");
        }
        protected ExternalClass(IntPtr address)
        {
            BaseAddress = address;

            // ReSharper disable once VirtualMemberCallInConstructor
            InitOffsets();

            // Get All Offset Props
            Offsets = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(ExternalOffset))
                .Select(f =>
                {
                    ExternalOffset curOffset = (ExternalOffset)f.GetValue(this);
                    if (curOffset != null)
                        curOffset.Name = f.Name;
                    return curOffset;
                })
                .ToList();

            // Get Size Of Class
            ClassSize = Utils.GetDependenciesSize(ExternalOffset.None, Offsets);
        }

        protected abstract void InitOffsets();
    }
}
