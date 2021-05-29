using Stylet;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace DDR4XMPEditor.DDR4SPD
{
    public class SPD : PropertyChangedBase
    {
        public enum Densities
        {
            _256Mb,
            _512Mb,
            _1Gb,
            _2Gb,
            _4Gb,
            _8Gb,
            _16Gb,
            _32Gb,
            _12Gb,
            _24Gb,
            Count
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct XMPHeader
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
        public static readonly byte[] HeaderMagic0 = { 0, 0 };
        public static readonly byte Version = 0x20;
        public const int TotalSize = 512;
        public const int SpdSize = 0x180;

        private byte[] rawSpd = new byte[SpdSize];
        private XMPHeader xmpHeader;
        private readonly XMP[] xmp = new XMP[2];

        private static readonly Densities[] densityMap = new Densities[(int)Densities.Count]
        {
            Densities._256Mb,
            Densities._512Mb,
            Densities._1Gb,
            Densities._2Gb,
            Densities._4Gb,
            Densities._8Gb,
            Densities._16Gb,
            Densities._32Gb,
            Densities._12Gb,
            Densities._24Gb,
        };
        private static readonly int[] bankAddressBitsMap = new int[4] { 4, 8, -1, -1 };
        private static readonly int[] bankGroupBitsMap = new int[4] { 0, 2, 4, -1 };
        private static readonly int[] columnAddressBitsMap = new int[8] { 9, 10, 11, 12, -1, -1, -1, -1 };
        private static readonly int[] rowAddressBitsMap = new int[8] { 12, 13, 14, 15, 16, 17, 18, -1 };

        public Densities? Density
        {
            get
            {
                int index = rawSpd[4] & 0xF;
                if (index >= (int)Densities.Count)
                {
                    return null;
                }
                return densityMap[index];
            }
            set
            {
                if (value.HasValue)
                {
                    int index = Array.FindIndex(densityMap, d => d == value.Value);
                    rawSpd[4] = (byte)((rawSpd[4] & 0xF0) | (index & 0xF));
                }
            }
        }

        public int BankAddressBits
        {
            get => bankAddressBitsMap[(rawSpd[4] >> 4) & 0x3];
            set
            {
                int index = Array.IndexOf(bankAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpd[4] = (byte)((rawSpd[4] & 0xC0) | (index << 4) | (rawSpd[4] & 0xF));
                }
            }
        }

        public int BankGroupBits
        {
            get => bankGroupBitsMap[(rawSpd[4] >> 6) & 0x3];
            set
            {
                int index = Array.IndexOf(bankGroupBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpd[4] = (byte)((index << 6) | (rawSpd[4] & 0x3F));
                }
            }
        }

        public int ColumnAddressBits
        {
            get => columnAddressBitsMap[rawSpd[5] & 0x7];
            set
            {
                int index = Array.IndexOf(columnAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpd[5] = (byte)((rawSpd[5] & 0xF8) | (index & 0x7));
                }
            }
        }

        public int RowAddressBits
        {
            get => rowAddressBitsMap[(rawSpd[5] >> 3) & 0x7];
            set
            {
                int index = Array.IndexOf(rowAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpd[5] = (byte)((rawSpd[5] & 0xC0) | (index << 3) | (rawSpd[5] & 0x7));
                }
            }
        }

        public ushort CRC1
        {
            get => (ushort)((rawSpd[0x7F] << 8) | (rawSpd[0x7E]));
            set
            {
                rawSpd[0x7E] = (byte)(value & 0xFF);
                rawSpd[0x7F] = (byte)((value >> 8) & 0xFF);
            }
        }
        public ushort CRC2
        {
            get => (ushort)((rawSpd[0xFF] << 8) | (rawSpd[0xFE]));
            set
            {
                rawSpd[0xFE] = (byte)(value & 0xFF);
                rawSpd[0xFF] = (byte)((value >> 8) & 0xFF);
            }
        }

        public bool XMP1Enabled
        {
            get => (xmpHeader.profileEnabled & 0x1) == 0x1;
            set
            {
                if (value)
                {
                    xmpHeader.profileEnabled |= 0x1;
                }
                else
                {
                    xmpHeader.profileEnabled &= 0xFE;
                }
            }
        }

        public bool XMP2Enabled
        {
            get => (xmpHeader.profileEnabled & 0x2) == 0x2;
            set
            {
                if (value)
                {
                    xmpHeader.profileEnabled |= 0x2;
                }
                else
                {
                    xmpHeader.profileEnabled &= 0xFD;
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

        public void UpdateCrc()
        {
            CRC1 = Crc16(rawSpd.Take(0x7D + 1).ToArray());
            CRC2 = Crc16(rawSpd.Skip(0x80).Take(0xFD - 0x80 + 1).ToArray());
        }

        public byte[] GetBytes()
        {
            int startIndex = 0;
            byte[] bytes = new byte[TotalSize];
            Array.Copy(rawSpd, bytes, rawSpd.Length);
            startIndex += rawSpd.Length;

            // Convert header to byte array.
            IntPtr ptr = IntPtr.Zero;
            try
            {
                int headerSize = Marshal.SizeOf<XMPHeader>();
                ptr = Marshal.AllocHGlobal(headerSize);
                Marshal.StructureToPtr(xmpHeader, ptr, true);
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
                if (bytes.Length != TotalSize)
                {
                    MessageBox.Show($"SPD must be {TotalSize} bytes", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                var xmpBytes = bytes.Skip(SpdSize).ToArray();
                var headerMagic = xmpBytes.Take(HeaderMagic.Length);
                // Check the magic constants.
                if (headerMagic.SequenceEqual(HeaderMagic) || headerMagic.SequenceEqual(HeaderMagic0))
                {
                    SPD spd = new SPD
                    {
                        rawSpd = bytes.Take(SpdSize).ToArray()
                    };

                    // Read the header.
                    var handle = GCHandle.Alloc(xmpBytes, GCHandleType.Pinned);
                    spd.xmpHeader = Marshal.PtrToStructure<XMPHeader>(handle.AddrOfPinnedObject());
                    handle.Free();

                    // Write the header magic and version if the SPD didn't come with XMP.
                    if (spd.xmpHeader.magic1 == 0)
                    {
                        spd.xmpHeader.magic1 = HeaderMagic[0];
                    }
                    if (spd.xmpHeader.magic2 == 0)
                    {
                        spd.xmpHeader.magic2 = HeaderMagic[1];
                    }
                    if (spd.xmpHeader.version == 0)
                    {
                        spd.xmpHeader.version = Version;
                    }

                    // Parse the XMP profiles (if any).
                    spd.ParseXMP(xmpBytes.Skip(Marshal.SizeOf<XMPHeader>()).ToArray());

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

        private ushort Crc16(byte[] bytes)
        {
            int crc = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                crc ^= bytes[i] << 8;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) == 0x8000)
                    {
                        crc = (crc << 1) ^ 0x1021;
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }

            return (ushort)(crc & 0xFFFF);
        }
    }
}
