using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace DDR4XMPEditor.DDR4SPD
{
    public class SPD : PropertyChangedBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct RawSpd
        {
            public fixed byte unknown1[0x10 + 1];
            public byte timebase;   // [0:1]: FTB, [2:3]: MTB
            public byte minCycleTime;
            public byte maxCycleTime;
            public byte clSupported1; // 7 - 14
            public byte clSupported2; // 15 - 22
            public byte clSupported3; // 23 - 30
            public byte clSupported4; // 31 - 36
            public byte clTicks;
            public byte rcdTicks;
            public byte rpTicks;
            public byte rasRCUpperNibble; // [0:3]: tRAS upper nibble, [4:7]: tRC upper nibble
            public byte rasTicks;
            public byte rcTicks;
            public byte rfc1LsbTicks;
            public byte rfc1MsbTicks;
            public byte rfc2LsbTicks;
            public byte rfc2MsbTicks;
            public byte rfc4LsbTicks;
            public byte rfc4MsbTicks;
            public byte fawUpperNibble; // [0:3] tFAW upper nibble
            public byte fawTicks;
            public byte rrdsTicks;
            public byte rrdlTicks;
            public byte ccdlTicks;
            public byte wrUpperNibble;
            public byte wrTicks;
            public byte wtrUpperNibble;
            public byte wtrsTicks;
            public byte wtrlTicks;
            public fixed byte unknown2[0x74 - 0x2E + 1];
            public sbyte ccdlFc;
            public sbyte rrdlFc;
            public sbyte rrdsFc;
            public sbyte rcFc;
            public sbyte rpFc;
            public sbyte rcdFc;
            public sbyte clFc;
            public sbyte maxCycleTimeFc;
            public sbyte minCycleTimeFc;
            public byte crcLsb;
            public byte crcMsb;
            // Module specific section
            public fixed byte unknown3[0xFD - 0x80 + 1];
            public byte mspCrcLsb;
            public byte mspCrcMsb;
            public fixed byte unknown4[0x13F - 0x100 + 1];
            public fixed byte manufacturer[2];
            public byte manufacturingLocation;
            public byte manufacturingYear;
            public byte manufacturingWeek;
            public fixed byte serialNumber[0x148 - 0x145 + 1];
            public fixed byte partNumber[0x15C - 0x149 + 1];
            public byte revisionCode;
            public fixed byte manufacturerIdCode[2];
            public byte dramStepping;
            public fixed byte unknown5[0x17F - 0x161 + 1];
        }

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
        private struct XMPHeader
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
        public const int MTBps = 125;  // Medium timebase in picoseconds

        private byte[] rawSpdBytes = new byte[SpdSize];
        private RawSpd rawSpd;
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
        private static readonly int[] deviceWidthMap = new int[8] { 4, 8, 16, 32, -1, -1, -1, -1 };
        private static readonly int[] packageRanksMap = new int[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

        public byte MinCycleTime
        {
            get => rawSpd.minCycleTime;
            set => rawSpd.minCycleTime = value;
        }

        public byte MaxCycleTime
        {
            get => rawSpd.maxCycleTime;
            set => rawSpd.maxCycleTime = value;
        }

        public byte CLTicks
        {
            get => rawSpd.clTicks;
            set => rawSpd.clTicks = value;
        }

        public byte RCDTicks
        {
            get => rawSpd.rcdTicks;
            set => rawSpd.rcdTicks = value;
        }

        public byte RPTicks
        {
            get => rawSpd.rpTicks;
            set => rawSpd.rpTicks = value;
        }

        public int RASTicks
        {
            get => (rawSpd.rasRCUpperNibble & 0xF << 8) | rawSpd.rasTicks;
            set
            {
                rawSpd.rasRCUpperNibble = (byte)((rawSpd.rasRCUpperNibble & 0xF0) | (value >> 8 & 0xF));
                rawSpd.rasTicks = (byte)(value & 0xFF);
            }
        }

        public int RCTicks
        {
            get => ((rawSpd.rasRCUpperNibble & 0xF0) << 4) | rawSpd.rcTicks;
            set
            {
                rawSpd.rasRCUpperNibble = (byte)(((value & 0xF00) >> 4) | (rawSpd.rasRCUpperNibble & 0xF));
                rawSpd.rcTicks = (byte)(value & 0xFF);
            }
        }

        public ushort RFC1Ticks
        {
            get => (ushort)((rawSpd.rfc1MsbTicks << 8) | rawSpd.rfc1LsbTicks);
            set
            {
                rawSpd.rfc1MsbTicks = (byte)(value >> 8);
                rawSpd.rfc1LsbTicks = (byte)(value & 0xFF);
            }
        }

        public ushort RFC2Ticks
        {
            get => (ushort)((rawSpd.rfc2MsbTicks << 8) | rawSpd.rfc2LsbTicks);
            set
            {
                rawSpd.rfc2MsbTicks = (byte)(value >> 8);
                rawSpd.rfc2LsbTicks = (byte)(value & 0xFF);
            }
        }

        public ushort RFC4Ticks
        {
            get => (ushort)((rawSpd.rfc4MsbTicks << 8) | rawSpd.rfc4LsbTicks);
            set
            {
                rawSpd.rfc4MsbTicks = (byte)(value >> 8);
                rawSpd.rfc4LsbTicks = (byte)(value & 0xFF);
            }
        }

        public int FAWTicks
        {
            get => ((rawSpd.fawUpperNibble & 0xF) << 8) | rawSpd.fawTicks;
            set
            {
                rawSpd.fawUpperNibble = (byte)(rawSpd.fawUpperNibble & 0xF0 | value >> 8 & 0xF);
                rawSpd.fawTicks = (byte)(value & 0xFF);
            }
        }

        public byte RRDSTicks
        {
            get => rawSpd.rrdsTicks;
            set => rawSpd.rrdsTicks = value;
        }

        public byte RRDLTicks
        {
            get => rawSpd.rrdlTicks;
            set => rawSpd.rrdlTicks = value;
        }

        public byte CCDLTicks
        {
            get => rawSpd.ccdlTicks;
            set => rawSpd.ccdlTicks = value;
        }

        public ushort WRTicks
        {
            get => (ushort)(((rawSpd.wrUpperNibble & 0xF) << 16) | rawSpd.wrTicks);
            set
            {
                rawSpd.wrUpperNibble = (byte)((value >> 16) & 0xF);
                rawSpd.wrTicks = (byte)(value & 0xFF);
            }
        }

        public ushort WTRSTicks
        {
            get => (ushort)(((rawSpd.wtrUpperNibble & 0xF) << 16) | rawSpd.wtrsTicks);
            set
            {
                rawSpd.wtrUpperNibble = (byte)((value >> 16) & 0xF);
                rawSpd.wtrsTicks = (byte)(value & 0xFF);
            }
        }

        public ushort WTRLTicks
        {
            get => (ushort)(((rawSpd.wtrUpperNibble & 0xF) << 16) | rawSpd.wtrlTicks);
            set
            {
                rawSpd.wtrUpperNibble = (byte)((value >> 16) & 0xF);
                rawSpd.wtrlTicks = (byte)(value & 0xFF);
            }
        }

        public sbyte CCDLFC
        {
            get => rawSpd.ccdlFc;
            set => rawSpd.ccdlFc = value;
        }

        public sbyte RRDLFC
        {
            get => rawSpd.rrdlFc;
            set => rawSpd.rrdlFc = value;
        }

        public sbyte RRDSFC
        {
            get => rawSpd.rrdsFc;
            set => rawSpd.rrdsFc = value;
        }

        public sbyte RCFC
        {
            get => rawSpd.rcFc;
            set => rawSpd.rcFc = value;
        }

        public sbyte RPFC
        {
            get => rawSpd.rpFc;
            set => rawSpd.rpFc = value;
        }

        public sbyte RCDFC
        {
            get => rawSpd.rcdFc;
            set => rawSpd.rcdFc = value;
        }

        public sbyte CLFC
        {
            get => rawSpd.clFc;
            set => rawSpd.clFc = value;
        }

        public sbyte MinCycleTimeFC
        {
            get => rawSpd.minCycleTimeFc;
            set => SetAndNotify(ref rawSpd.minCycleTimeFc, value);
        }

        public sbyte MaxCycleTimeFC
        {
            get => rawSpd.maxCycleTimeFc;
            set => SetAndNotify(ref rawSpd.maxCycleTimeFc, value);
        }

        public Densities? Density
        {
            get
            {
                int index = rawSpdBytes[4] & 0xF;
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
                    rawSpdBytes[4] = (byte)((rawSpdBytes[4] & 0xF0) | (index & 0xF));
                }
            }
        }

        public int Banks
        {
            get => bankAddressBitsMap[(rawSpdBytes[4] >> 4) & 0x3];
            set
            {
                int index = Array.IndexOf(bankAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpdBytes[4] = (byte)((rawSpdBytes[4] & 0xC0) | (index << 4) | (rawSpdBytes[4] & 0xF));
                }
            }
        }

        public int BankGroups
        {
            get => bankGroupBitsMap[(rawSpdBytes[4] >> 6) & 0x3];
            set
            {
                int index = Array.IndexOf(bankGroupBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpdBytes[4] = (byte)((index << 6) | (rawSpdBytes[4] & 0x3F));
                }
            }
        }

        public int ColumnAddresses
        {
            get => columnAddressBitsMap[rawSpdBytes[5] & 0x7];
            set
            {
                int index = Array.IndexOf(columnAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpdBytes[5] = (byte)((rawSpdBytes[5] & 0xF8) | (index & 0x7));
                }
            }
        }

        public int RowAddresses
        {
            get => rowAddressBitsMap[(rawSpdBytes[5] >> 3) & 0x7];
            set
            {
                int index = Array.IndexOf(rowAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpdBytes[5] = (byte)((rawSpdBytes[5] & 0xC0) | (index << 3) | (rawSpdBytes[5] & 0x7));
                }
            }
        }

        public int DeviceWidth
        {
            get => deviceWidthMap[rawSpdBytes[0xC] & 0b111];
            set
            {
                int index = Array.IndexOf(deviceWidthMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpdBytes[0xC] = (byte)((rawSpdBytes[0xC] & 0b11111000) | index);
                }
            }
        }

        public int PackageRanks
        {
            get => packageRanksMap[(rawSpdBytes[0xC] >> 3) & 0b111];
            set
            {
                int index = Array.IndexOf(packageRanksMap, value);
                if (index != -1 && value != -1)
                {
                    rawSpdBytes[0xC] = (byte)((rawSpdBytes[0xC] & 0b11000000) | (index << 3) | (rawSpdBytes[0xC] & 0b111));
                }
            }
        }

        /// <summary>
        /// False is symmetrical. True is asymmetrical.
        /// </summary>
        public bool RankMix
        {
            get => (rawSpdBytes[0xC] & 0b01000000) == 0b01000000;
            set
            {
                if (value)
                {
                    rawSpdBytes[0xC] |= 0b01000000;
                }
                else
                {
                    rawSpdBytes[0xC] &= 0b10111111;
                }
            }
        }

        public ushort CRC1
        {
            get => (ushort)((rawSpd.crcMsb << 8) | rawSpd.crcLsb);
            set
            {
                rawSpd.crcLsb = (byte)(value & 0xFF);
                rawSpd.crcMsb = (byte)((value >> 8) & 0xFF);
            }
        }
        public ushort CRC2
        {
            get => (ushort)((rawSpd.mspCrcMsb << 8) | (rawSpd.mspCrcLsb));
            set
            {
                rawSpd.mspCrcLsb = (byte)(value & 0xFF);
                rawSpd.mspCrcMsb = (byte)((value >> 8) & 0xFF);
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
            CRC1 = Crc16(rawSpdBytes.Take(0x7D + 1).ToArray());
            CRC2 = Crc16(rawSpdBytes.Skip(0x80).Take(0xFD - 0x80 + 1).ToArray());
        }

        public byte[] GetBytes()
        {
            int startIndex = 0;
            byte[] bytes = new byte[TotalSize];
            Array.Copy(rawSpdBytes, bytes, rawSpdBytes.Length);
            startIndex += rawSpdBytes.Length;

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
                        rawSpdBytes = bytes.Take(SpdSize).ToArray()
                    };

                    // Read into RawSpd struct.
                    var handle = GCHandle.Alloc(spd.rawSpdBytes, GCHandleType.Pinned);
                    spd.rawSpd = Marshal.PtrToStructure<RawSpd>(handle.AddrOfPinnedObject());
                    handle.Free();

                    // Read the XMP header.
                    handle = GCHandle.Alloc(xmpBytes, GCHandleType.Pinned);
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

        public static bool IsCLSupported(byte[] clSupported, int cl)
        {
            int[] mask = { 1, 2, 4, 8, 16, 32, 64, 128 };
            if (cl >= 7 && cl <= 14)
            {
                return (clSupported[0] & mask[cl - 7]) == mask[cl - 7];
            }
            else if (cl >= 15 && cl <= 22)
            {
                return (clSupported[1] & mask[cl - 15]) == mask[cl - 15];
            }
            else if (cl >= 23 && cl <= 30)
            {
                return (clSupported[2] & mask[cl - 23]) == mask[cl - 23];
            }
            else
            {
                return false;
            }
        }

        public static void SetCLSupported(byte[] clSupported, int cl, bool supported)
        {
            if (cl < 7 || cl > 30)
            {
                return;
            }

            int[] mask = { 1, 2, 4, 8, 16, 32, 64, 128 };
            if (cl >= 7 && cl <= 14)
            {
                if (supported)
                {
                    clSupported[0] |= (byte)mask[cl - 7];
                }
                else
                {
                    clSupported[0] &= (byte)~mask[cl - 7];
                }
            }
            else if (cl >= 15 && cl <= 22)
            {
                if (supported)
                {
                    clSupported[1] |= (byte)mask[cl - 15];
                }
                else
                {
                    clSupported[1] &= (byte)~mask[cl - 15];
                }
            }
            else if (cl >= 23 && cl <= 30)
            {
                if (supported)
                {
                    clSupported[2] |= (byte)mask[cl - 23];
                }
                else
                {
                    clSupported[2] &= (byte)~mask[cl - 23];
                }
            }
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
