using Stylet;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace DDR4XMPEditor.DDR4SPD
{
    public class XMP : PropertyChangedBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct RawXMP
        {
            public byte voltage;
            public fixed byte unknown1[2];
            public byte sdramCycleTicks;
            public fixed byte clSupported[3]; // 0: 7-14, 1: 15-22, 2: 23-30
            public byte unknown2;
            public byte clTicks;
            public byte rcdTicks;
            public byte rpTicks;
            public byte rasRCUpperNibble; // [0:3]: tRAS upper nibble, [4:7]: tRC upper nibble
            public byte rasTicks;
            public byte rcTicks;
            public ushort rfc1Ticks;
            public ushort rfc2Ticks;
            public ushort rfc4Ticks;
            public byte fawUpperNibble; // [0:3] tFAW upper nibble
            public byte fawTicks;
            public byte rrdsTicks;
            public byte rrdlTicks;
            public fixed byte unknown3[8];
            public sbyte rrdlFc;
            public sbyte rrdsFc;
            public sbyte rcFc;
            public sbyte rpFc;
            public sbyte rcdFc;
            public sbyte clFc;
            public sbyte sdramCycleTimeFc;
            public fixed byte unknown4[8];
        }

        public const int Size = 0x2F;
        private RawXMP rawXMP;

        /// <summary>
        /// The voltage in hundredths of a volt.
        /// </summary>
        public uint Voltage
        {
            get
            {
                int hundredths = rawXMP.voltage & 0x7F;
                int ones = (rawXMP.voltage & 0x80) >> 7;
                return (uint)(ones * 100 + hundredths);
            }
            set
            {
                if (value > 227)
                {
                    value = 227;
                }

                bool ones = value >= 100;
                // Hundredths place can go up to 0xFF (127).
                uint hundredths = value >= 100 ? value - 100 : value & 0x7F;
                rawXMP.voltage = (byte)((ones ? 0x80u : 0x00u) | hundredths);
            }
        }

        public byte SDRAMCycleTicks
        {
            get => rawXMP.sdramCycleTicks;
            set => rawXMP.sdramCycleTicks = value;
        }

        public unsafe byte[] GetClSupported()
        {
            return new byte[] { rawXMP.clSupported[0], rawXMP.clSupported[1], rawXMP.clSupported[2] };
        }

        public unsafe void SetClSupported(int index, byte value)
        {
            rawXMP.clSupported[index] = value;
        }

        public byte CLTicks
        {
            get => rawXMP.clTicks;
            set => rawXMP.clTicks = value;
        }

        public byte RCDTicks
        {
            get => rawXMP.rcdTicks;
            set => rawXMP.rcdTicks = value;
        }

        public byte RPTicks
        {
            get => rawXMP.rpTicks;
            set => rawXMP.rpTicks = value;
        }

        public int RASTicks
        {
            get => ((rawXMP.rasRCUpperNibble & 0xF) << 8) | rawXMP.rasTicks;
            set
            {
                rawXMP.rasRCUpperNibble = (byte)((rawXMP.rasRCUpperNibble & 0xF0) | (value >> 8 & 0xF));
                rawXMP.rasTicks = (byte)(value & 0xFF);
            }
        }

        public int RCTicks
        {
            get => ((rawXMP.rasRCUpperNibble & 0xF0) << 4) | rawXMP.rcTicks;
            set
            {
                rawXMP.rasRCUpperNibble = (byte)(((value & 0xF00) >> 4) | (rawXMP.rasRCUpperNibble & 0xF));
                rawXMP.rcTicks = (byte)(value & 0xFF);
            }
        }

        public ushort RFC1Ticks
        {
            get => rawXMP.rfc1Ticks;
            set => rawXMP.rfc1Ticks = value;
        }

        public ushort RFC2Ticks
        {
            get => rawXMP.rfc2Ticks;
            set => rawXMP.rfc2Ticks = value;
        }

        public ushort RFC4Ticks
        {
            get => rawXMP.rfc4Ticks;
            set => rawXMP.rfc4Ticks = value;
        }

        public int FAWTicks
        {
            get => ((rawXMP.fawUpperNibble & 0xF) << 8) | rawXMP.fawTicks;
            set
            {
                rawXMP.fawUpperNibble = (byte)(rawXMP.fawUpperNibble & 0xF0 | value >> 8 & 0xF);
                rawXMP.fawTicks = (byte)(value & 0xFF);
            }
        }

        public byte RRDSTicks
        {
            get => rawXMP.rrdsTicks;
            set => rawXMP.rrdsTicks = value;
        }

        public byte RRDLTicks
        {
            get => rawXMP.rrdlTicks;
            set => rawXMP.rrdlTicks = value;
        }

        public sbyte RRDLFC
        {
            get => rawXMP.rrdlFc;
            set => rawXMP.rrdlFc = value;
        }

        public sbyte RRDSFC
        {
            get => rawXMP.rrdsFc;
            set => rawXMP.rrdsFc = value;
        }

        public sbyte RCFC
        {
            get => rawXMP.rcFc;
            set => rawXMP.rcFc = value;
        }

        public sbyte RPFC
        {
            get => rawXMP.rpFc;
            set => rawXMP.rpFc = value;
        }

        public sbyte RCDFC
        {
            get => rawXMP.rcdFc;
            set => rawXMP.rcdFc = value;
        }

        public sbyte CLFC
        {
            get => rawXMP.clFc;
            set => rawXMP.clFc = value;
        }

        public sbyte SDRAMCycleTimeFC
        {
            get => rawXMP.sdramCycleTimeFc;
            set => SetAndNotify(ref rawXMP.sdramCycleTimeFc, value);
        }

        public byte[] GetBytes()
        {
            IntPtr ptr = IntPtr.Zero;
            byte[] bytes = null;
            try
            {
                var size = Marshal.SizeOf<RawXMP>();
                bytes = new byte[size];
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(rawXMP, ptr, true);
                Marshal.Copy(ptr, bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return bytes;
        }

        /// <summary>
        /// Parses a single XMP profile.
        /// </summary>
        /// <param name="bytes">The raw bytes of the XMP.</param>
        /// <returns>An <see cref="XMP"/> object.</returns>
        public static XMP Parse(byte[] bytes)
        {
            if (bytes.Length != Size)
            {
                return null;
            }

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            XMP xmp = new XMP
            {
                rawXMP = Marshal.PtrToStructure<RawXMP>(handle.AddrOfPinnedObject())
            };
            handle.Free();
            return xmp;
        }
    }
}
