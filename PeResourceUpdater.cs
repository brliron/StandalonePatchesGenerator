using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StandaloneGeneratorV3
{
    class PeResourceUpdater : IDisposable
    {
        class Kernel32
        {
            [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr BeginUpdateResourceW(string lpFileName, int bDeleteExistingResources);
            [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern int UpdateResourceW(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, uint cb);
            [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern int EndUpdateResourceW(IntPtr hUpdate, int fDiscard);

            public static readonly IntPtr RT_ICON = new IntPtr(3); // MAKEINTRESOURCE(3)
            public static readonly IntPtr RT_STRING = new IntPtr(6); // MAKEINTRESOURCE(6)
            public static readonly IntPtr RT_GROUP_ICON = new IntPtr(14); // MAKEINTRESOURCE(RT_ICON + 11)

            public static ushort MAKELANGID(ushort p, ushort s) => (ushort)((s << 10) | p);
            public static readonly ushort LANG_NEUTRAL = 0;
            public static readonly ushort SUBLANG_NEUTRAL = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
        struct GRPICONDIR
        {
            public ushort idReserved;
            public ushort idType;
            public ushort idCount;
            // idEntries is actually an array, but we use only the 1st element.
            // And we don't want it as a C# array, because Marshal would convert it to a pointer.
            public GRPICONDIRENTRY idEntries;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
        struct GRPICONDIRENTRY
        {
            public byte bWidth;
            public byte bHeight;
            public byte bColorCount;
            public byte bReserved;
            public ushort wPlanes;
            public ushort wBitCount;
            public uint dwBytesInRes;
            public ushort nId;
        };

        private IntPtr hUpdate;

        public PeResourceUpdater(string exe)
        {
            hUpdate = Kernel32.BeginUpdateResourceW(exe, 0);
            if (hUpdate == IntPtr.Zero)
                return;
        }

        public void ReplaceStringTable(List<string> tableIn)
        {
            if (tableIn.Count > 16)
                throw new ArgumentOutOfRangeException("This function supports at most 16 strings.");

            List<byte> tableList = new List<byte>();
            uint n = 0;
            foreach (string str in tableIn)
            {
                tableList.Add((byte)str.Length);
                tableList.Add((byte)(str.Length / 256));
                tableList.AddRange(Encoding.Unicode.GetBytes(str));
                n++;
            }
            while (n < 16)
            {
                tableList.Add(0);
                tableList.Add(0);
                n++;
            }

            byte[] tableArray = tableList.ToArray();
            Kernel32.UpdateResourceW(hUpdate, Kernel32.RT_STRING, new IntPtr(1) /* MAKEINTRESOURCE(1) */,
                Kernel32.MAKELANGID(Kernel32.LANG_NEUTRAL, Kernel32.SUBLANG_NEUTRAL),
                tableArray, (uint)tableArray.Length);
        }
        byte[] structToBytes<T>(T str)
        {
            int size = Marshal.SizeOf<T>();
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public void ReplaceIcon(string iconPath)
        {
            Bitmap inputBitmap = (Bitmap)Bitmap.FromFile(iconPath);
            uint streamSize;

            using (MemoryStream iconStream = new MemoryStream())
            {
                inputBitmap.Save(iconStream, ImageFormat.Png);
                streamSize = (uint)iconStream.Length;
                Kernel32.UpdateResourceW(hUpdate, Kernel32.RT_ICON, new IntPtr(1) /* MAKEINTRESOURCE(1) */,
                    Kernel32.MAKELANGID(Kernel32.LANG_NEUTRAL, Kernel32.SUBLANG_NEUTRAL),
                    iconStream.ToArray(), streamSize);
            }

            GRPICONDIR dir;
            dir.idReserved = 0;
            dir.idType = 1; // icon
            dir.idCount = 1;
            dir.idEntries.bWidth = (byte)inputBitmap.Width;
            dir.idEntries.bHeight = (byte)inputBitmap.Height;
            dir.idEntries.bColorCount = 0; // Size of palette
            dir.idEntries.bReserved = 0;
            dir.idEntries.wPlanes = 0;
            dir.idEntries.wBitCount = 32;
            dir.idEntries.dwBytesInRes = streamSize;
            dir.idEntries.nId = 1;

            byte[] headerData = structToBytes(dir);
            Kernel32.UpdateResourceW(hUpdate, Kernel32.RT_GROUP_ICON, new IntPtr(1) /* MAKEINTRESOURCE(1) */,
                Kernel32.MAKELANGID(Kernel32.LANG_NEUTRAL, Kernel32.SUBLANG_NEUTRAL),
                headerData, (uint)headerData.Length);
        }
        public void Dispose()
        {
            if (hUpdate == IntPtr.Zero)
                return;

            Kernel32.EndUpdateResourceW(hUpdate, 0 /* Don't discard changes */);
            hUpdate = IntPtr.Zero;
        }

        ~PeResourceUpdater()
        {
            Dispose();
        }
    }
}
