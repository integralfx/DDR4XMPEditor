using Stylet;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace DDR4XMPEditor.DDR4SPD
{
    public class SPD : PropertyChangedBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Header
        {
            public byte magic1;
            public byte magic2;
            public byte profileEnabled; // bit 0: profile 1 enabled, bit 1: profile 2 enabled
            public byte version;
            public byte unknown1;
            public byte unknown2;
            public byte unknown3;
            public byte unknown4;
            public byte reserved;
        };

        public static readonly byte[] HeaderMagic = { 0x0C, 0x4A };
        public const int Size = 512;
        public const int PreHeaderSize = 0x180;

        private byte[] preHeader = new byte[PreHeaderSize];
        private Header header;
        private readonly XMP[] xmp = new XMP[2];

        public bool XMP1Enabled
        {
            get => (header.profileEnabled & 0x1) == 0x1;
            set
            {
                if (value)
                {
                    header.profileEnabled |= 0x1;
                }
                else
                {
                    header.profileEnabled &= 0xFE;
                }
            }
        }

        public bool XMP2Enabled
        {
            get => (header.profileEnabled & 0x2) == 0x2;
            set
            {
                if (value)
                {
                    header.profileEnabled |= 0x2;
                }
                else
                {
                    header.profileEnabled &= 0xFD;
                }
            }
        }

        public XMP XMP1
        {
            get => xmp[0];
            set => xmp[0] = value;
        }
        
        public XMP XMP2
        {
            get => xmp[1];
            set => xmp[1] = value;
        }

        public byte[] GetBytes()
        {
            int startIndex = 0;
            byte[] bytes = new byte[Size];
            Array.Copy(preHeader, bytes, preHeader.Length);
            startIndex += preHeader.Length;

            // Convert header to byte array.
            IntPtr ptr = IntPtr.Zero;
            try
            {
                int headerSize = Marshal.SizeOf<Header>();
                ptr = Marshal.AllocHGlobal(headerSize);
                Marshal.StructureToPtr(header, ptr, true);
                Marshal.Copy(ptr, bytes, startIndex, headerSize);
                startIndex += headerSize;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            // Copy XMPs.
            byte[] xmp1Bytes = XMP1.GetBytes();
            byte[] xmp2Bytes = XMP2.GetBytes();
            Array.Copy(xmp1Bytes, 0, bytes, startIndex, xmp1Bytes.Length);
            startIndex += xmp1Bytes.Length;
            Array.Copy(xmp2Bytes, 0, bytes, startIndex, xmp2Bytes.Length);

            return bytes;
        }

        public static SPD Parse(byte[] bytes)
        {
            try
            {
                if (bytes.Length != Size)
                {
                    MessageBox.Show($"SPD must be {Size} bytes", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                var spdBytes = bytes.Skip(PreHeaderSize).ToArray();
                // Check the magic constants.
                if (spdBytes.Take(HeaderMagic.Length).SequenceEqual(HeaderMagic))
                {
                    SPD spd = new SPD
                    {
                        preHeader = bytes.Take(PreHeaderSize).ToArray()
                    };

                    // Read the header.
                    var handle = GCHandle.Alloc(spdBytes, GCHandleType.Pinned);
                    spd.header = Marshal.PtrToStructure<Header>(handle.AddrOfPinnedObject());
                    handle.Free();

                    // Parse the XMP profiles (if any).
                    spd.ParseXMP(spdBytes.Skip(Marshal.SizeOf<Header>()).ToArray());

                    return spd;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private void ParseXMP(byte[] bytes)
        {
            XMP1 = XMP.Parse(bytes.Take(XMP.Size).ToArray());
            XMP2 = XMP.Parse(bytes.Skip(XMP.Size).Take(XMP.Size).ToArray());
        }
    }
}
