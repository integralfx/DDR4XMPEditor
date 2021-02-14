using DDR4XMPEditor.DDR4SPD;

namespace DDR4XMPEditor.Events
{
    public class SelectedSPDFileEvent
    {
        public string FilePath { get; set; }
        public SPD SPD { get; set; }
    }
}
