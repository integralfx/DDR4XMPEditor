using DDR4XMPEditor.DDR4SPD;
using Stylet;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DDR4XMPEditor.Pages
{
    public class XMPEditorViewModel : Screen
    {
        public XMP Profile { get; set; }
        public bool IsEnabled { get; set; }

        public double? SDRAMCycleTime { get; set; }
        public double? Frequency
        {
            get
            {
                if (SDRAMCycleTime.HasValue && SDRAMCycleTime.Value > 0)
                {
                    return 1000 / SDRAMCycleTime.Value;
                }

                return null;
            }
        }

        public bool[] CLSupported { get; set; }

        public int? tCL 
        {
            get => TimeToTicks(Profile?.CLTicks * XMP.MTBps + Profile?.CLFC);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.CLTicks = (byte)ticks.Value;
                }
            }
        }
        public double? tCLTime => (Profile?.CLTicks * XMP.MTBps + Profile?.CLFC) / 1000.0;
        public int? tRCD 
        {
            get => TimeToTicks(Profile?.RCDTicks * XMP.MTBps + Profile?.RCDFC);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RCDTicks = (byte)ticks.Value;
                }
            }
        }
        public double? tRCDTime => (Profile?.RCDTicks * XMP.MTBps + Profile?.RCDFC) / 1000.0;
        public int? tRP 
        {
            get => TimeToTicks(Profile?.RPTicks * XMP.MTBps + Profile?.RPFC);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RPTicks = (byte)ticks.Value;
                }
            }
        }
        public double? tRPTime => (Profile?.RPTicks * XMP.MTBps + Profile?.RPFC) / 1000.0;
        public int? tRAS 
        { 
            get => TimeToTicks(Profile?.RASTicks * XMP.MTBps); 
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RASTicks = (byte)ticks.Value;
                }
            }
        }
        public int? tRC 
        {
            get => TimeToTicks(Profile?.RCTicks * XMP.MTBps + Profile?.RCFC); 
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RCTicks = (byte)ticks.Value;
                }
            }
        }
        public double? tRCTime => (Profile?.RCTicks * XMP.MTBps + Profile?.RCFC) / 1000.0;
        public int? tRFC 
        {
            get => TimeToTicks(Profile?.RFCTicks * XMP.MTBps);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RASTicks = (byte)ticks.Value;
                }
            }
        }
        public int? tRFC2 
        { 
            get => TimeToTicks(Profile?.RFC2Ticks * XMP.MTBps); 
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RFC2Ticks = (byte)ticks.Value;
                }
            }
        }
        public int? tRFC4 
        {
            get => TimeToTicks(Profile?.RFC4Ticks * XMP.MTBps);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RFC4Ticks = (byte)ticks.Value;
                }
            }
        }
        public int? tRRDS 
        { 
            get => TimeToTicks(Profile?.RRDSTicks * XMP.MTBps + Profile?.RRDSFC);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RRDSTicks = (byte)ticks.Value;
                }
            }
        }
        public double? tRRDSTime => (Profile?.RRDSTicks * XMP.MTBps + Profile?.RRDSFC) / 1000.0;
        public int? tRRDL 
        {
            get => TimeToTicks(Profile?.RRDLTicks * XMP.MTBps + Profile?.RRDLFC);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RRDLTicks = (byte)ticks.Value;
                }
            }
        }
        public double? tRRDLTime => (Profile?.RRDLTicks * XMP.MTBps + Profile?.RRDLFC) / 1000.0;
        public int? tFAW 
        {
            get => TimeToTicks(Profile?.FAWTicks * XMP.MTBps);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.FAWTicks = (byte)ticks.Value;
                }
            }
        }

        /// <summary>
        /// Convert <paramref name="timeps"/> to DRAM ticks.
        /// </summary>
        /// <param name="timeps">Time in picoseconds.</param>
        /// <returns></returns>
        private int? TimeToTicks(int? timeps)
        {
            if (!timeps.HasValue || ! SDRAMCycleTime.HasValue || timeps.Value <= 0)
            {
                return null;
            }
            return (int)Math.Round(timeps.Value/1000.0 / SDRAMCycleTime.Value);
        }

        /// <summary>
        /// Convert <paramref name="dramTicks"/> to MTB ticks.
        /// </summary>
        /// <param name="dramTicks">Ticks using DRAM cycle time units.</param>
        /// <returns>Ticks using MTB units.</returns>
        private int? DRAMTicksToMTBTicks(int? dramTicks)
        {
            if (!dramTicks.HasValue)
            {
                return null;
            }

            int sdramCycleTime = Profile.SDRAMCycleTicks * XMP.MTBps + Profile.SDRAMCycleTimeFC;
            return (int)Math.Ceiling(1.0 * dramTicks.Value * sdramCycleTime / XMP.MTBps);
        }
    }

    /// <summary>
    /// Convert ticks to time (ns).
    /// </summary>
    public class TicksToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value) * XMP.MTBps / 1000.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)Math.Round(((double)value * 1000.0 / XMP.MTBps));
        }
    }
}
