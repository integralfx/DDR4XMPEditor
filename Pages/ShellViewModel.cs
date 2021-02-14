using System;
using Stylet;

namespace DDR4XMPEditor.Pages
{
    public class ShellViewModel : Conductor<Screen>.Collection.OneActive
    {
        public MenuBarViewModel MenuBar { get; private set; }
        public EditorViewModel Editor { get; private set; }

        public ShellViewModel(IEventAggregator aggregator)
        {
            MenuBar = new MenuBarViewModel(aggregator);
            Editor = new EditorViewModel(aggregator);
        }
    }
}
