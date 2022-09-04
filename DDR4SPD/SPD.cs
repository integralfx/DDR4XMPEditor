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
        private unsafe struct RawSPD
        {
            public byte bytesUsed;
            public byte revision;
            public byte memoryType; // 12 = DDR4
            public byte moduleType; // 2 = Unbuffered DIMM, 3 = SO-DIMM, 11=LRDIMM
            public byte banks;
            public byte rowColAddress;
            public byte primaryPackageType;
            public byte optionalFeatures;
            public byte thermalRefreshOptions;
            public byte otherOptionalFeatures;
            public byte secondaryPackageType;
            public byte voltage;
            public byte organisation;
            public byte busWidth;
            public byte thermalSensor;
            public fixed byte unknown2[0x10 - 0x0F + 1];
            public byte timebase;   // [0:1]: FTB, [2:3]: MTB
            public byte minCycleTime;
            public byte maxCycleTime;
            public fixed byte clSupported[4]; // 0: 7-14, 1: 15-22, 2: 23-30, 3: 31-36
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
            public fixed byte unknown3[0x74 - 0x2E + 1];
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
            public fixed byte unknown4[0xFD - 0x80 + 1];
            public byte mspCrcLsb;
            public byte mspCrcMsb;
            public fixed byte unknown5[0x13F - 0x100 + 1];
            public fixed byte manufacturer[2];
            public byte manufacturingLocation;
            public byte manufacturingYear;
            public byte manufacturingWeek;
            public fixed byte serialNumber[0x148 - 0x145 + 1];
            public fixed byte partNumber[0x15C - 0x149 + 1];
            public byte revisionCode;
            public fixed byte manufacturerIdCode[2];
            public byte dramStepping;
            public fixed byte unknown6[0x17F - 0x161 + 1];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct XMPHeader
        {
            public byte magic1;
            public byte magic2;
            public byte profileEnabled; // bit 0: profile 1 enabled, bit 1: profile 2 enabled
            public byte version;
            public fixed byte unknown[5];
        };

        public static readonly byte[] HeaderMagic = { 0x0C, 0x4A };
        public static readonly byte[] HeaderMagic0 = { 0, 0 };
        public static readonly byte Version = 0x20;
        public const int TotalSize = 512;
        public const int SpdSize = 0x180;
        public const int MTBps = 125;  // Medium timebase in picoseconds

        private RawSPD rawSPD;
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
            get => rawSPD.minCycleTime;
            set => rawSPD.minCycleTime = value;
        }

        public byte MaxCycleTime
        {
            get => rawSPD.maxCycleTime;
            set => rawSPD.maxCycleTime = value;
        }

        public unsafe byte[] GetClSupported()
        {
            return new byte[] { rawSPD.clSupported[0], rawSPD.clSupported[1], rawSPD.clSupported[2] };
        }

        public unsafe void SetClSupported(int index, byte value)
        {
            rawSPD.clSupported[index] = value;
        }

        public byte CLTicks
        {
            get => rawSPD.clTicks;
            set => rawSPD.clTicks = value;
        }

        public byte RCDTicks
        {
            get => rawSPD.rcdTicks;
            set => rawSPD.rcdTicks = value;
        }

        public byte RPTicks
        {
            get => rawSPD.rpTicks;
            set => rawSPD.rpTicks = value;
        }

        public int RASTicks
        {
            get => ((rawSPD.rasRCUpperNibble & 0xF) << 8) | rawSPD.rasTicks;
            set
            {
                rawSPD.rasRCUpperNibble = (byte)((rawSPD.rasRCUpperNibble & 0xF0) | ((value >> 8) & 0xF));
                rawSPD.rasTicks = (byte)(value & 0xFF);
            }
        }

        public int RCTicks
        {
            get => ((rawSPD.rasRCUpperNibble & 0xF0) << 4) | rawSPD.rcTicks;
            set
            {
                rawSPD.rasRCUpperNibble = (byte)(((value & 0xF00) >> 4) | (rawSPD.rasRCUpperNibble & 0xF));
                rawSPD.rcTicks = (byte)(value & 0xFF);
            }
        }

        public ushort RFC1Ticks
        {
            get => (ushort)((rawSPD.rfc1MsbTicks << 8) | rawSPD.rfc1LsbTicks);
            set
            {
                rawSPD.rfc1MsbTicks = (byte)(value >> 8);
                rawSPD.rfc1LsbTicks = (byte)(value & 0xFF);
            }
        }

        public ushort RFC2Ticks
        {
            get => (ushort)((rawSPD.rfc2MsbTicks << 8) | rawSPD.rfc2LsbTicks);
            set
            {
                rawSPD.rfc2MsbTicks = (byte)(value >> 8);
                rawSPD.rfc2LsbTicks = (byte)(value & 0xFF);
            }
        }

        public ushort RFC4Ticks
        {
            get => (ushort)((rawSPD.rfc4MsbTicks << 8) | rawSPD.rfc4LsbTicks);
            set
            {
                rawSPD.rfc4MsbTicks = (byte)(value >> 8);
                rawSPD.rfc4LsbTicks = (byte)(value & 0xFF);
            }
        }

        public int FAWTicks
        {
            get => ((rawSPD.fawUpperNibble & 0xF) << 8) | rawSPD.fawTicks;
            set
            {
                rawSPD.fawUpperNibble = (byte)(rawSPD.fawUpperNibble & 0xF0 | value >> 8 & 0xF);
                rawSPD.fawTicks = (byte)(value & 0xFF);
            }
        }

        public byte RRDSTicks
        {
            get => rawSPD.rrdsTicks;
            set => rawSPD.rrdsTicks = value;
        }

        public byte RRDLTicks
        {
            get => rawSPD.rrdlTicks;
            set => rawSPD.rrdlTicks = value;
        }

        public byte CCDLTicks
        {
            get => rawSPD.ccdlTicks;
            set => rawSPD.ccdlTicks = value;
        }

        public ushort WRTicks
        {
            get => (ushort)(((rawSPD.wrUpperNibble & 0xF) << 16) | rawSPD.wrTicks);
            set
            {
                rawSPD.wrUpperNibble = (byte)((value >> 16) & 0xF);
                rawSPD.wrTicks = (byte)(value & 0xFF);
            }
        }

        public ushort WTRSTicks
        {
            get => (ushort)(((rawSPD.wtrUpperNibble & 0xF) << 16) | rawSPD.wtrsTicks);
            set
            {
                rawSPD.wtrUpperNibble = (byte)((value >> 16) & 0xF);
                rawSPD.wtrsTicks = (byte)(value & 0xFF);
            }
        }

        public ushort WTRLTicks
        {
            get => (ushort)(((rawSPD.wtrUpperNibble & 0xF) << 16) | rawSPD.wtrlTicks);
            set
            {
                rawSPD.wtrUpperNibble = (byte)((value >> 16) & 0xF);
                rawSPD.wtrlTicks = (byte)(value & 0xFF);
            }
        }

        public sbyte CCDLFC
        {
            get => rawSPD.ccdlFc;
            set => rawSPD.ccdlFc = value;
        }

        public sbyte RRDLFC
        {
            get => rawSPD.rrdlFc;
            set => rawSPD.rrdlFc = value;
        }

        public sbyte RRDSFC
        {
            get => rawSPD.rrdsFc;
            set => rawSPD.rrdsFc = value;
        }

        public sbyte RCFC
        {
            get => rawSPD.rcFc;
            set => rawSPD.rcFc = value;
        }

        public sbyte RPFC
        {
            get => rawSPD.rpFc;
            set => rawSPD.rpFc = value;
        }

        public sbyte RCDFC
        {
            get => rawSPD.rcdFc;
            set => rawSPD.rcdFc = value;
        }

        public sbyte CLFC
        {
            get => rawSPD.clFc;
            set => rawSPD.clFc = value;
        }

        public sbyte MinCycleTimeFC
        {
            get => rawSPD.minCycleTimeFc;
            set => SetAndNotify(ref rawSPD.minCycleTimeFc, value);
        }

        public sbyte MaxCycleTimeFC
        {
            get => rawSPD.maxCycleTimeFc;
            set => SetAndNotify(ref rawSPD.maxCycleTimeFc, value);
        }

        public Densities? Density
        {
            get
            {
                int index = rawSPD.banks & 0xF;
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
                    rawSPD.banks = (byte)((rawSPD.banks & 0xF0) | (index & 0xF));
                }
            }
        }

        public int Banks
        {
            get => bankAddressBitsMap[(rawSPD.banks >> 4) & 0x3];
            set
            {
                int index = Array.IndexOf(bankAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSPD.banks = (byte)((rawSPD.banks & 0xC0) | (index << 4) | (rawSPD.banks & 0xF));
                }
            }
        }

        public int BankGroups
        {
            get => bankGroupBitsMap[(rawSPD.banks >> 6) & 0x3];
            set
            {
                int index = Array.IndexOf(bankGroupBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSPD.banks = (byte)((index << 6) | (rawSPD.banks & 0x3F));
                }
            }
        }

        public int ColumnAddresses
        {
            get => columnAddressBitsMap[rawSPD.rowColAddress & 0x7];
            set
            {
                int index = Array.IndexOf(columnAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSPD.rowColAddress = (byte)((rawSPD.rowColAddress & 0xF8) | (index & 0x7));
                }
            }
        }

        public int RowAddresses
        {
            get => rowAddressBitsMap[(rawSPD.rowColAddress >> 3) & 0x7];
            set
            {
                int index = Array.IndexOf(rowAddressBitsMap, value);
                if (index != -1 && value != -1)
                {
                    rawSPD.rowColAddress = (byte)((rawSPD.rowColAddress & 0xC0) | (index << 3) | (rawSPD.rowColAddress & 0x7));
                }
            }
        }

        public int DeviceWidth
        {
            get => deviceWidthMap[rawSPD.organisation & 0b111];
            set
            {
                int index = Array.IndexOf(deviceWidthMap, value);
                if (index != -1 && value != -1)
                {
                    rawSPD.organisation = (byte)((rawSPD.organisation & 0b11111000) | index);
                }
            }
        }

        public int PackageRanks
        {
            get => packageRanksMap[(rawSPD.organisation >> 3) & 0b111];
            set
            {
                int index = Array.IndexOf(packageRanksMap, value);
                if (index != -1 && value != -1)
                {
                    rawSPD.organisation = (byte)((rawSPD.organisation & 0b11000000) | (index << 3) | (rawSPD.organisation & 0b111));
                }
            }
        }

        /// <summary>
        /// False is symmetrical. True is asymmetrical.
        /// </summary>
        public bool RankMix
        {
            get => (rawSPD.organisation & 0b01000000) == 0b01000000;
            set
            {
                if (value)
                {
                    rawSPD.organisation |= 0b01000000;
                }
                else
                {
                    rawSPD.organisation &= 0b10111111;
                }
            }
        }

        public uint ManufacturingYear
        {
            get => uint.Parse(rawSPD.manufacturingYear.ToString("X"));   // Year is represented in hex e.g. 0x22 = 2022
            set
            {
                if (value > 99)
                {
                    value = 99;
                }

                rawSPD.manufacturingYear = byte.Parse(value.ToString(), System.Globalization.NumberStyles.HexNumber);
            }
        }
        
        public uint ManufacturingWeek
        {
            get => uint.Parse(rawSPD.manufacturingWeek.ToString("X"));   // Week is represented in hex e.g. 0x10 = Week 10
            set
            {
                // 52 weeks in a year
                if (value > 52)
                {
                    value = 52;
                }

                rawSPD.manufacturingWeek = byte.Parse(value.ToString(), System.Globalization.NumberStyles.HexNumber);
            }
        }
        
        public unsafe string PartNumber
        {
            get
            {
                fixed (byte* p = rawSPD.partNumber)
                {
                    return Marshal.PtrToStringAnsi((IntPtr)p);
                }
            }
            set
            {
                const int maxSize = 20;
                var str = value.Substring(0, Math.Min(maxSize, value.Length));
                fixed (byte* p = rawSPD.partNumber)
                {
                    for (int i = 0; i < str.Length; ++i)
                    {
                        p[i] = (byte)str[i];
                    }
                    for (int i = str.Length; i < maxSize; ++i)
                    {
                        p[i] = 0;
                    }
                }
            }
        }

        public ushort CRC1
        {
            get => (ushort)((rawSPD.crcMsb << 8) | rawSPD.crcLsb);
            set
            {
                rawSPD.crcLsb = (byte)(value & 0xFF);
                rawSPD.crcMsb = (byte)((value >> 8) & 0xFF);
            }
        }
        public ushort CRC2
        {
            get => (ushort)((rawSPD.mspCrcMsb << 8) | (rawSPD.mspCrcLsb));
            set
            {
                rawSPD.mspCrcLsb = (byte)(value & 0xFF);
                rawSPD.mspCrcMsb = (byte)((value >> 8) & 0xFF);
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
            var rawSpdBytes = GetBytes();
            CRC1 = Crc16(rawSpdBytes.Take(0x7D + 1).ToArray());
            CRC2 = Crc16(rawSpdBytes.Skip(0x80).Take(0xFD - 0x80 + 1).ToArray());
        }

        public byte[] GetBytes()
        {
            int startIndex = 0;
            byte[] bytes = new byte[TotalSize];

            // Convert raw SPD to byte array.
            IntPtr ptr = IntPtr.Zero;
            try
            {
                int rawSpdSize = Marshal.SizeOf<RawSPD>();
                ptr = Marshal.AllocHGlobal(rawSpdSize);
                Marshal.StructureToPtr(rawSPD, ptr, true);
                Marshal.Copy(ptr, bytes, startIndex, rawSpdSize);
                startIndex += rawSpdSize;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Failed to save SPD data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            if (XMP1Enabled || XMP2Enabled)
            {
                xmpHeader.magic1 = HeaderMagic[0];
                xmpHeader.magic2 = HeaderMagic[1];
                xmpHeader.version = Version;
            }

            // Convert header to byte array.
            ptr = IntPtr.Zero;
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
                MessageBox.Show(e.Message, "Failed to save XMP header", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var rawSpdBytes = bytes.Take(SpdSize).ToArray();
                    SPD spd = new SPD();

                    // Read into RawSpd struct.
                    var handle = GCHandle.Alloc(rawSpdBytes, GCHandleType.Pinned);
                    spd.rawSPD = Marshal.PtrToStructure<RawSPD>(handle.AddrOfPinnedObject());
                    handle.Free();

                    // Read the XMP header.
                    handle = GCHandle.Alloc(xmpBytes, GCHandleType.Pinned);
                    spd.xmpHeader = Marshal.PtrToStructure<XMPHeader>(handle.AddrOfPinnedObject());
                    handle.Free();

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
