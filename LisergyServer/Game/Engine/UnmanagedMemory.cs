﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

#if UNITY
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Game.Engine
{
    /// <summary>
    /// Main place to handle unmanaged memory. We use unmanaged memory to be reused while allowing garbage collector not need to worry about this memory.
    /// </summary>
    public static class UnmanagedMemory
    {
        internal static readonly Dictionary<IntPtr, int> _allocs = new Dictionary<IntPtr, int>();

        internal static Dictionary<IntPtr, int> _available = new Dictionary<IntPtr, int>();

#if WINDOWS
        [DllImport("kernel32.dll")]
        static extern void RtlZeroMemory(IntPtr dst, UIntPtr length);
#endif

        public static void SetZeros(IntPtr ptr, int size)
        {
#if WINDOWS
            UIntPtr usize = new UIntPtr((uint)size);
            RtlZeroMemory(ptr, usize);
#else
            Marshal.Copy(new byte[size], 0, ptr, size);
#endif
        }

        public unsafe static T* Alloc<T>() where T : unmanaged
        {
            return (T*)Alloc(sizeof(T));
        }

        public static IntPtr Alloc(int size)
        {
            IntPtr available = GetAvailable(size);
            if (available.ToInt64() > 0)
            {
                _ = _available.Remove(available);
                _allocs[available] = size;
                SetZeros(available, size);
                return available;
            }
            IntPtr p = Marshal.AllocHGlobal(size);
#if !UNITY
            //GC.AddMemoryPressure(size);
#endif
            _allocs[p] = size;
            SetZeros(p, size);
            return p;
        }

        private static IntPtr GetAvailable(int size)
        {
            KeyValuePair<IntPtr, int> kp = _available.FirstOrDefault(kp => kp.Value == size);
            return kp.Key;
        }

        /// <summary>
        /// Flag memory to be reused. Mainly for tests or reuse map server to spawn different map;
        /// </summary>
        public static void FlagMemoryToBeReused()
        {
            _available = new Dictionary<IntPtr, int>(_allocs);
            _allocs.Clear();
        }

        public static void FreeForReuse(IntPtr ptr)
        {
            if (_allocs.ContainsKey(ptr))
            {
                _available[ptr] = _allocs[ptr];
                _ = _allocs.Remove(ptr);
            }
        }


        public static void DeallocateMemory(IntPtr p)
        {
            var size = _allocs[p];
            Marshal.FreeHGlobal(p);
#if !UNITY
            //GC.RemoveMemoryPressure(size);
#endif
            _allocs.Clear();
        }

        public static void FreeAll()
        {
            foreach (var (p, s) in _allocs)
            {
                Marshal.FreeHGlobal(p);
                GC.RemoveMemoryPressure(s);
            }
            _allocs.Clear();
            _available.Clear();
        }

        public static string[] GetMetrics()
        {
            return new string[]
            {
                $"Unmanaged Pointers: {_allocs.Count}",
                $"Total Unmanaged Size: {_allocs.Values.Sum()/1024} KB"
            };
        }
    }
}
