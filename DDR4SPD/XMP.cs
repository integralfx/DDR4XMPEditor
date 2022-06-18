using Stylet;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace DDR4XMPEditor.DDR4SPD
{
    public class XMP : PropertyChangedBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct RawXmp
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
            public ushort rfcTicks;
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
        private RawXmp rawXmp;

        /// <summary>
        /// The voltage in hundredths of a volt.
        /// </summary>
        public uint Voltage
        {
            get
            {
                int hundredths = rawXmp.voltage & 0x7F;
                int ones = (rawXmp.voltage & 0x80) >> 7;
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
                rawXmp.voltage = (byte)((ones ? 0x80u : 0x00u) | hundredths);
            }
        }

        public byte SDRAMCycleTicks
        {
            get => rawXmp.sdramCycleTicks;
            set => rawXmp.sdramCycleTicks = value;
        }

        public unsafe byte[] GetClSupported()
        {
            return new byte[] { rawXmp.clSupported[0], rawXmp.clSupported[1], rawXmp.clSupported[2] };
        }

        public unsafe void SetClSupported(int index, byte value)
        {
            rawXmp.clSupported[index] = value;
        }

        public byte CLTicks
        {
            get => rawXmp.clTicks;
            set => rawXmp.clTicks = value;
        }

        public byte RCDTicks
        {
            get => rawXmp.rcdTicks;
            set => rawXmp.rcdTicks = value;
        }

        public byte RPTicks
        {
            get => rawXmp.rpTicks;
            set => rawXmp.rpTicks = value;
        }

        public int RASTicks
        {
            get => (rawXmp.rasRCUpperNibble & 0xF << 8) | rawXmp.rasTicks;
            set
            {
                rawXmp.rasRCUpperNibble = (byte)((rawXmp.rasRCUpperNibble & 0xF0) | (value >> 8 & 0xF));
                rawXmp.rasTicks = (byte)(value & 0xFF);
            }
        }

        public int RCTicks
        {
            get => ((rawXmp.rasRCUpperNibble & 0xF0) << 4) | rawXmp.rcTicks;
            set
            {
                rawXmp.rasRCUpperNibble = (byte)(((value & 0xF00) >> 4) | (rawXmp.rasRCUpperNibble & 0xF));
                rawXmp.rcTicks = (byte)(value & 0xFF);
            }
        }

        public ushort RFCTicks
        {
            get => rawXmp.rfcTicks;
            set => rawXmp.rfcTicks = value;
        }

        public ushort RFC2Ticks
        {
            get => rawXmp.rfc2Ticks;
            set => rawXmp.rfc2Ticks = value;
        }

        public ushort RFC4Ticks
        {
            get => rawXmp.rfc4Ticks;
            set => rawXmp.rfc4Ticks = value;
        }

        public int FAWTicks
        {
            get => ((rawXmp.fawUpperNibble & 0xF) << 8) | rawXmp.fawTicks;
            set
            {
                rawXmp.fawUpperNibble = (byte)(rawXmp.fawUpperNibble & 0xF0 | value >> 8 & 0xF);
                rawXmp.fawTicks = (byte)(value & 0xFF);
            }
        }

        public byte RRDSTicks
        {
            get => rawXmp.rrdsTicks;
            set => rawXmp.rrdsTicks = value;
        }

        public byte RRDLTicks
        {
            get => rawXmp.rrdlTicks;
            set => rawXmp.rrdlTicks = value;
        }

        public sbyte RRDLFC
        {
            get => rawXmp.rrdlFc;
            set => rawXmp.rrdlFc = value;
        }

        public sbyte RRDSFC
        {
            get => rawXmp.rrdsFc;
            set => rawXmp.rrdsFc = value;
        }

        public sbyte RCFC
        {
            get => rawXmp.rcFc;
            set => rawXmp.rcFc = value;
        }

        public sbyte RPFC
        {
            get => rawXmp.rpFc;
            set => rawXmp.rpFc = value;
        }

        public sbyte RCDFC
        {
            get => rawXmp.rcdFc;
            set => rawXmp.rcdFc = value;
        }

        public sbyte CLFC
        {
            get => rawXmp.clFc;
            set => rawXmp.clFc = value;
        }

        public sbyte SDRAMCycleTimeFC
        {
            get => rawXmp.sdramCycleTimeFc;
            set => SetAndNotify(ref rawXmp.sdramCycleTimeFc, value);
        }

        public byte[] GetBytes()
        {
            IntPtr ptr = IntPtr.Zero;
            byte[] bytes = null;
            try
            {
                var size = Marshal.SizeOf<RawXmp>();
                bytes = new byte[size];
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(rawXmp, ptr, true);
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
                rawXmp = Marshal.PtrToStructure<RawXmp>(handle.AddrOfPinnedObject())
            };
            handle.Free();
            return xmp;
        }
    }
}
