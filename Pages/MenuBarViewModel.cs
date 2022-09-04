using DDR4XMPEditor.Events;
using Microsoft.Win32;
using Stylet;
using System.IO;

namespace DDR4XMPEditor.Pages
{
    public class MenuBarViewModel : Screen
    {
        private IEventAggregator eventAggregator;
        private string lastOpenDirectory = Directory.GetCurrentDirectory();

        public MenuBarViewModel(IEventAggregator aggregator)
        {
            eventAggregator = aggregator;
        }

        public void Open()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                InitialDirectory = lastOpenDirectory,
                Filter = "SPD files (*.spd)|*.spd|Binary files (*.bin)|*.bin|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog().Value)
            {
                lastOpenDirectory = ofd.FileName;
                eventAggregator.Publish(new SelectedSPDFileEvent 
                {
                    FilePath = ofd.FileName,
                    SPD = DDR4SPD.SPD.Parse(File.ReadAllBytes(ofd.FileName))
                });
            }
        }

        public void Save()
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                InitialDirectory = lastOpenDirectory,
                Filter = "SPD files (*.spd)|*.spd|Binary files (*.bin)|*.bin|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (sfd.ShowDialog().Value)
            {
                eventAggregator.Publish(new SaveSPDFileEvent
                {
                    FilePath = sfd.FileName
                });
            }
        }
    }
}
