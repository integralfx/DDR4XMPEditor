using Stylet;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace DDR4XMPEditor.DDR4SPD
{
    public class XMP : PropertyChangedBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct RawXMP
        {
            public byte voltage;
            public byte unknown1;
            public byte unknown2;
            public byte sdramCycleTicks;
            public byte clSupported1, // 7 - 14
                        clSupported2, // 15 - 22
                        clSupported3; // 23 - 30
            public byte unknown3;
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
            public byte unknown4;
            public byte unknown5;
            public byte unknown6;
            public byte unknown7;
            public byte unknown8;
            public byte unknown9;
            public byte unknown10;
            public byte unknown11;
            public sbyte rrdlFC;
            public sbyte rrdsFC;
            public sbyte rcFC;
            public sbyte rpFC;
            public sbyte rcdFC;
            public sbyte clFC;
            public sbyte sdramCycleTimeFC;
            public byte unknown12;
            public byte unknown13;
            public byte unknown14;
            public byte unknown15;
            public byte unknown16;
            public byte unknown17;
            public byte unknown18;
            public byte unknown19;
        }
        
        public const int Size = 0x2F;
        public const int MTBps = 125;  // Medium timebase in picoseconds
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

        public bool IsCLSupported(int cl)
        {
            int[] mask = { 1, 2, 4, 8, 16, 32, 64, 128 };
            if (cl >= 7 && cl <= 14)
            {
                return (rawXMP.clSupported1 & mask[cl - 7]) == mask[cl - 7];
            }
            else if (cl >= 15 && cl <= 22)
            {
                return (rawXMP.clSupported2 & mask[cl - 15]) == mask[cl - 15];
            }
            else if (cl >= 23 && cl <= 30)
            {
                return (rawXMP.clSupported3 & mask[cl - 23]) == mask[cl - 23];
            }
            else
            {
                return false;
            }
        }

        public bool SetCLSupported(int cl, bool supported)
        {
            if (cl < 7 || cl > 30)
            {
                return false;
            }

            int[] mask = { 1, 2, 4, 8, 16, 32, 64, 128 };
            if (cl >= 7 && cl <= 14)
            {
                if (supported)
                {
                    rawXMP.clSupported1 |= (byte)mask[cl - 7];
                }
                else
                {
                    rawXMP.clSupported1 &= (byte)~mask[cl - 7];
                }
            }
            else if (cl >= 15 && cl <= 22)
            {
                if (supported)
                {
                    rawXMP.clSupported2 |= (byte)mask[cl - 15];
                }
                else
                {
                    rawXMP.clSupported2 &= (byte)~mask[cl - 15];
                }
            }
            else if (cl >= 23 && cl <= 30)
            {
                if (supported)
                {
                    rawXMP.clSupported3 |= (byte)mask[cl - 23];
                }
                else
                {
                    rawXMP.clSupported3 &= (byte)~mask[cl - 23];
                }
            }

            return true;
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
            get => (rawXMP.rasRCUpperNibble & 0xF << 8) | rawXMP.rasTicks;
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

        public ushort RFCTicks
        {
            get => rawXMP.rfcTicks;
            set => rawXMP.rfcTicks = value;
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
            get => rawXMP.rrdlFC;
            set => rawXMP.rrdlFC = value;
        }

        public sbyte RRDSFC
        {
            get => rawXMP.rrdsFC;
            set => rawXMP.rrdsFC = value;
        }

        public sbyte RCFC
        {
            get => rawXMP.rcFC;
            set => rawXMP.rcFC = value;
        }

        public sbyte RPFC
        {
            get => rawXMP.rpFC;
            set => rawXMP.rpFC = value;
        }

        public sbyte RCDFC
        {
            get => rawXMP.rcdFC;
            set => rawXMP.rcdFC = value;
        }

        public sbyte CLFC
        {
            get => rawXMP.clFC;
            set => rawXMP.clFC = value;
        }

        public sbyte SDRAMCycleTimeFC
        {
            get => rawXMP.sdramCycleTimeFC;
            set => SetAndNotify(ref rawXMP.sdramCycleTimeFC, value);
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
