using DDR4XMPEditor.DDR4SPD;
using DDR4XMPEditor.Events;
using Stylet;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace DDR4XMPEditor.Pages
{
    public class EditorViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<SelectedSPDFileEvent>, 
        IHandle<SaveSPDFileEvent>
    {
        public SPD SPD { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public bool IsSPDValid => SPD != null;

        private readonly XMPEditorViewModel xmpVm1, xmpVm2;
        private readonly MiscViewModel miscVm = new MiscViewModel { DisplayName = "Misc" };

        public EditorViewModel(IEventAggregator aggregator)
        {
            aggregator.Subscribe(this);
            Items.Add(xmpVm1 = new XMPEditorViewModel { DisplayName = "XMP 1" });
            Items.Add(xmpVm2 = new XMPEditorViewModel { DisplayName = "XMP 2" });
            Items.Add(miscVm);
            ActiveItem = Items[0];
        }

        public void Handle(SelectedSPDFileEvent e)
        {
            var spd = SPD.Parse(File.ReadAllBytes(e.FilePath));
            if (spd == null)
            {
                MessageBox.Show("Invalid SPD file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                SPD = spd;

                xmpVm1.IsEnabled = SPD.XMP1Enabled;
                SPD.Bind(x => x.XMP1Enabled, (s, args) => xmpVm1.IsEnabled = args.NewValue);
                xmpVm1.Profile = SPD.XMP1;
                for (int i = 0; i < xmpVm1.CLSupported.Count; ++i)
                {
                    xmpVm1.CLSupported[i] = xmpVm1.Profile.IsCLSupported(i);
                }

                xmpVm2.IsEnabled = SPD.XMP2Enabled;
                SPD.Bind(x => x.XMP2Enabled, (s, args) => xmpVm2.IsEnabled = args.NewValue);
                xmpVm2.Profile = SPD.XMP2;
                for (int i = 0; i < xmpVm2.CLSupported.Count; ++i)
                {
                    xmpVm2.CLSupported[i] = xmpVm2.Profile.IsCLSupported(i);
                }
                
                BindNotifyPropertyChanged(xmpVm1);
                BindNotifyPropertyChanged(xmpVm2);

                FilePath = e.FilePath;
                FileName = Path.GetFileName(FilePath);

                miscVm.IsEnabled = true;
                miscVm.SPD = spd;
            }
        }

        public void Handle(SaveSPDFileEvent e)
        {
            var bytes = SPD.GetBytes();
            File.WriteAllBytes(e.FilePath, bytes);
            MessageBox.Show(
                $"Successfully SPD saved to {e.FilePath}",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void BindNotifyPropertyChanged(XMPEditorViewModel vm)
        {
            void UpdateFrequency()
            {
                int? timeps = vm.Profile?.SDRAMCycleTicks * XMP.MTBps + vm.Profile?.SDRAMCycleTimeFC;
                vm.SDRAMCycleTime = timeps / 1000.0;
            }
            UpdateFrequency();
            vm.Profile.Bind(x => x.SDRAMCycleTicks, (s, e) => UpdateFrequency());
            vm.Profile.Bind(x => x.SDRAMCycleTimeFC, (s, e) => UpdateFrequency());

            // Bind timings.
            vm.Profile.Bind(x => x.CLTicks, (s, e) => vm.Refresh());
            vm.Profile.Bind(x => x.CLFC, (s, e) => vm.Refresh());

            vm.Profile.Bind(x => x.RCDTicks, (s, e) => vm.Refresh());
            vm.Profile.Bind(x => x.RCDFC, (s, e) => vm.Refresh());

            vm.Profile.Bind(x => x.RPTicks, (s, e) => vm.Refresh());
            vm.Profile.Bind(x => x.RPFC, (s, e) => vm.Refresh());

            vm.Profile.Bind(x => x.RASTicks, (s, e) => vm.Refresh());

            vm.Profile.Bind(x => x.RCTicks, (s, e) => vm.Refresh());
            vm.Profile.Bind(x => x.RCFC, (s, e) => vm.Refresh());

            vm.Profile.Bind(x => x.RFCTicks, (s, e) => vm.Refresh());
            vm.Profile.Bind(x => x.RFC2Ticks, (s, e) => vm.Refresh());
            vm.Profile.Bind(x => x.RFC4Ticks, (s, e) => vm.Refresh());

            vm.Profile.Bind(x => x.RRDSTicks, (s, e) => vm.Refresh());
            vm.Profile.Bind(x => x.RRDSFC, (s, e) => vm.Refresh());

            vm.Profile.Bind(x => x.RRDLTicks, (s, e) => vm.Refresh());
            vm.Profile.Bind(x => x.RRDLFC, (s, e) => vm.Refresh());

            vm.Profile.Bind(x => x.FAWTicks, (s, e) => vm.Refresh());
        }
    }
}
