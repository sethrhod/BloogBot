﻿using Binarysharp.Assemblers.Fasm;
using GameData.Core.Models;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ForegroundBotRunner.Mem
{
    public static unsafe class MemoryManager
    {
        [Flags]
        private enum ProcessAccessFlags
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            SYNCHRONIZE = 0x00100000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            PROCESS_ALL_ACCESS = 0x001F0FFF,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SUSPEND_RESUME = 0x0800,
            PROCESS_TERMINATE = 0x0001,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020
        }

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtect(nint address, int size, uint newProtect, out uint oldProtect);

        [DllImport("kernel32.dll")]
        private static extern nint OpenProcess(ProcessAccessFlags desiredAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(
            nint hProcess,
            nint lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            ref int lpNumberOfBytesWritten);

        [Flags]
        public enum Protection
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtect(nint lpAddress, nuint dwSize, uint flNewProtect, out uint lpflOldProtect);

        private static readonly nint wowProcessHandle = Process.GetCurrentProcess().Handle;
        public static readonly nint imageBase = Process.GetCurrentProcess().MainModule.BaseAddress;
        private static readonly FasmNet fasm = new();

        [HandleProcessCorruptedStateExceptions]
        static internal byte ReadByte(nint address, bool isRelative = false)
        {
            if (address == nint.Zero)
                return 0;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                return *(byte*)address;
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type Byte");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public short ReadShort(nint address, bool isRelative = false)
        {
            if (address == nint.Zero)
                return 0;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                return *(short*)address;
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type Short");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public int ReadInt(nint address, bool isRelative = false)
        {
            if (address == nint.Zero)
                return 0;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                return *(int*)address;
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type Int");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public uint ReadUint(nint address, bool isRelative = false)
        {
            if (address == nint.Zero)
                return 0;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                return *(uint*)address;
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type Uint");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public ulong ReadUlong(nint address, bool isRelative = false)
        {
            if (address == nint.Zero)
                return 0;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                return *(ulong*)address;
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type Ulong");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public nint ReadIntPtr(nint address, bool isRelative = false)
        {
            if (address == nint.Zero)
                return nint.Zero;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                return *(nint*)address;
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type IntPtr");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public float ReadFloat(nint address, bool isRelative = false)
        {
            if (address == nint.Zero)
                return 0;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                return *(float*)address;
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type Float");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public string ReadString(nint address, int size = 512, bool isRelative = false)
        {
            if (address == nint.Zero)
                return null;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                var buffer = ReadBytes(address, size);
                if (buffer.Length == 0)
                    return default;

                var ret = Encoding.ASCII.GetString(buffer);

                if (ret.IndexOf('\0') != -1)
                    ret = ret.Remove(ret.IndexOf('\0'));

                return ret;
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type string");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return "";
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public byte[] ReadBytes(nint address, int count, bool isRelative = false)
        {
            if (address == nint.Zero)
                return null;

            if (isRelative)
                address = imageBase + (int)address;

            try
            {
                var ret = new byte[count];
                var ptr = (byte*)address;

                for (var i = 0; i < count; i++)
                    ret[i] = ptr[i];

                return ret;
            }
            catch (NullReferenceException)
            {
                return default;
            }
            catch (AccessViolationException)
            {
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        static public ItemCacheEntry ReadItemCacheEntry(nint address)
        {
            if (address == nint.Zero)
                return null;

            try
            {
                return new ItemCacheEntry(address);
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("Access Violation on " + address.ToString("X") + " with type ItemCacheEntry");
                return default;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MEMORY]{e.Message}{e.InnerException.StackTrace}");
                return default;
            }
        }

        static internal void WriteByte(nint address, byte value) => Marshal.StructureToPtr(value, address, false);

        static internal void WriteInt(nint address, int value) => Marshal.StructureToPtr(value, address, false);

        // certain memory locations (Warden for example) are protected from modification.
        // we use OpenAccess with ProcessAccessFlags to remove the protection.
        // you can check whether memory is successfully being modified by setting a breakpoint
        // here and checking Debug -> Windows -> Disassembly.
        // if you have further issues, you may need to use VirtualProtect from the Win32 API.
        static internal void WriteBytes(nint address, byte[] bytes)
        {
            if (address == nint.Zero)
                return;

            var access = ProcessAccessFlags.PROCESS_CREATE_THREAD |
                         ProcessAccessFlags.PROCESS_QUERY_INFORMATION |
                         ProcessAccessFlags.PROCESS_SET_INFORMATION |
                         ProcessAccessFlags.PROCESS_TERMINATE |
                         ProcessAccessFlags.PROCESS_VM_OPERATION |
                         ProcessAccessFlags.PROCESS_VM_READ |
                         ProcessAccessFlags.PROCESS_VM_WRITE |
                         ProcessAccessFlags.SYNCHRONIZE;

            var process = OpenProcess(access, false, Environment.ProcessId);

            int ret = 0;
            WriteProcessMemory(process, address, bytes, bytes.Length, ref ret);

            var protection = Protection.PAGE_EXECUTE_READWRITE;
            // now set the memory to be executable
            VirtualProtect(address, bytes.Length, (uint)protection, out uint _);
        }

        static internal nint InjectAssembly(string hackName, string[] instructions)
        {
            // first get the assembly as bytes for the allocated area before overwriting the memory
            fasm.Clear();
            fasm.AddLine("use32");
            foreach (var x in instructions)
                fasm.AddLine(x);

            var byteCode = new byte[0];
            try
            {
                byteCode = fasm.Assemble();
            }
            catch (FasmAssemblerException ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            var start = Marshal.AllocHGlobal(byteCode.Length);
            fasm.Clear();
            fasm.AddLine("use32");
            foreach (var x in instructions)
                fasm.AddLine(x);
            byteCode = fasm.Assemble(start);

            var hack = new Hack(hackName, start, byteCode);
            HackManager.AddHack(hack);

            return start;
        }

        static internal void InjectAssembly(string hackName, uint ptr, string instructions)
        {
            fasm.Clear();
            fasm.AddLine("use32");
            fasm.AddLine(instructions);
            var start = new nint(ptr);
            var byteCode = fasm.Assemble(start);

            var hack = new Hack(hackName, start, byteCode);
            HackManager.AddHack(hack);
        }
    }
}
