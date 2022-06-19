using DDR4XMPEditor.DDR4SPD;
using Stylet;
using System;
using System.ComponentModel;
using System.Linq;

namespace DDR4XMPEditor.Pages
{
    public class SPDEditorViewModel : Screen
    {
        public SPD Profile { get; set; }

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

        public SPDEditorViewModel()
        {
            CLSupported = new BindingList<bool>(Enumerable.Range(0, 31).Select(n => false).ToList());
            CLSupported.ListChanged += (s, e) =>
            {
                var clSupported = Profile.GetClSupported();
                SPD.SetCLSupported(clSupported, e.NewIndex, CLSupported[e.NewIndex]);
                for (int i = 0; i < clSupported.Length; ++i)
                {
                    Profile.SetClSupported(i, clSupported[i]);
                }
                Refresh();
            };
        }

        public BindingList<bool> CLSupported { get; private set; }

        public int? tCL 
        {
            get => TimeToTicks(Profile?.CLTicks * SPD.MTBps + Profile?.CLFC);
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
        public double? tCLTime => (Profile?.CLTicks * SPD.MTBps + Profile?.CLFC) / 1000.0;
        public int? tRCD 
        {
            get => TimeToTicks(Profile?.RCDTicks * SPD.MTBps + Profile?.RCDFC);
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
        public double? tRCDTime => (Profile?.RCDTicks * SPD.MTBps + Profile?.RCDFC) / 1000.0;
        public int? tRP 
        {
            get => TimeToTicks(Profile?.RPTicks * SPD.MTBps + Profile?.RPFC);
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
        public double? tRPTime => (Profile?.RPTicks * SPD.MTBps + Profile?.RPFC) / 1000.0;
        public int? tRAS 
        { 
            get => TimeToTicks(Profile?.RASTicks * SPD.MTBps); 
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RASTicks = ticks.Value;
                }
            }
        }
        public int? tRC 
        {
            get => TimeToTicks(Profile?.RCTicks * SPD.MTBps + Profile?.RCFC); 
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RCTicks = ticks.Value;
                }
            }
        }
        public double? tRCTime => (Profile?.RCTicks * SPD.MTBps + Profile?.RCFC) / 1000.0;
        public int? tRFC1
        {
            get => TimeToTicks(Profile?.RFC1Ticks * SPD.MTBps);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RFC1Ticks = (ushort)ticks.Value;
                }
            }
        }
        public int? tRFC2 
        { 
            get => TimeToTicks(Profile?.RFC2Ticks * SPD.MTBps); 
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RFC2Ticks = (ushort)ticks.Value;
                }
            }
        }
        public int? tRFC4 
        {
            get => TimeToTicks(Profile?.RFC4Ticks * SPD.MTBps);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.RFC4Ticks = (ushort)ticks.Value;
                }
            }
        }
        public int? tRRDS 
        { 
            get => TimeToTicks(Profile?.RRDSTicks * SPD.MTBps + Profile?.RRDSFC);
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
        public double? tRRDSTime => (Profile?.RRDSTicks * SPD.MTBps + Profile?.RRDSFC) / 1000.0;
        public int? tRRDL 
        {
            get => TimeToTicks(Profile?.RRDLTicks * SPD.MTBps + Profile?.RRDLFC);
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
        public double? tRRDLTime => (Profile?.RRDLTicks * SPD.MTBps + Profile?.RRDLFC) / 1000.0;
        public int? tFAW 
        {
            get => TimeToTicks(Profile?.FAWTicks * SPD.MTBps);
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
        public int? tWR
        {
            get => TimeToTicks(Profile?.WRTicks * SPD.MTBps);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.WRTicks = (ushort)ticks.Value;
                }
            }
        }
        public double? tWRTime => Profile?.WRTicks * SPD.MTBps / 1000.0;
        public int? tWTRS
        {
            get => TimeToTicks(Profile?.WTRSTicks * SPD.MTBps);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.WTRSTicks = (ushort)ticks.Value;
                }
            }
        }
        public double? tWTRSTime => Profile?.WTRSTicks * SPD.MTBps / 1000.0;
        public int? tWTRL
        {
            get => TimeToTicks(Profile?.WTRLTicks * SPD.MTBps);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.WTRLTicks = (ushort)ticks.Value;
                }
            }
        }
        public double? tWTRLTime => Profile?.WTRLTicks * SPD.MTBps / 1000.0;
        public int? tCCDL
        {
            get => TimeToTicks(Profile?.CCDLTicks * SPD.MTBps + Profile?.CCDLFC);
            set
            {
                if (Profile == null)
                {
                    return;
                }

                int? ticks = DRAMTicksToMTBTicks(value);
                if (ticks.HasValue)
                {
                    Profile.CCDLTicks = (byte)ticks.Value;
                }
            }
        }
        public double? tCCDLTime => (Profile?.CCDLTicks * SPD.MTBps + Profile?.CCDLFC) / 1000.0;

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
            return (int)Math.Ceiling(timeps.Value/1000.0 / SDRAMCycleTime.Value);
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

            int sdramCycleTime = Profile.MinCycleTime * SPD.MTBps + Profile.MinCycleTimeFC;
            return (int)Math.Floor(1.0 * dramTicks.Value * sdramCycleTime / SPD.MTBps);
        }
    }
}
