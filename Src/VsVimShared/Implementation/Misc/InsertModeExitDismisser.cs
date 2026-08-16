using System;
using System.ComponentModel.Composition;
using Vim;
using Vim.Extensions;
using Vim.UI.Wpf;

namespace Vim.VisualStudio.Implementation.Misc
{
    /// <summary>
    /// Dismiss the active display windows (completion, signature help, etc ...) when
    /// the buffer switches out of an insert style mode.  The dismissal which happens
    /// when a physical Escape key travels through the command filter chain doesn't
    /// cover exits via key mappings like ':imap jk &lt;Esc&gt;' where the Escape is
    /// produced internally by the mapping, which left the completion UI visible
    /// after returning to normal mode
    /// </summary>
    [Export(typeof(IVimBufferCreationListener))]
    internal sealed class InsertModeExitDismisser : IVimBufferCreationListener
    {
        private readonly IDisplayWindowBrokerFactoryService _displayWindowBrokerFactory;

        [ImportingConstructor]
        internal InsertModeExitDismisser(IDisplayWindowBrokerFactoryService displayWindowBrokerFactory)
        {
            _displayWindowBrokerFactory = displayWindowBrokerFactory;
        }

        void IVimBufferCreationListener.VimBufferCreated(IVimBuffer vimBuffer)
        {
            var broker = _displayWindowBrokerFactory.GetDisplayWindowBroker(vimBuffer.TextView);

            void onSwitchedMode(object sender, SwitchModeEventArgs args)
            {
                // Only dismiss when leaving an insert style mode for a non insert
                // mode.  Any display window still open belongs to the insert session
                // which just completed
                if (args.PreviousMode != null &&
                    args.PreviousMode.ModeKind.IsAnyInsert() &&
                    !args.CurrentMode.ModeKind.IsAnyInsert() &&
                    broker.IsAnyDisplayActive())
                {
                    broker.DismissDisplayWindows();
                }
            }

            vimBuffer.SwitchedMode += onSwitchedMode;
            vimBuffer.Closed += (sender, args) => vimBuffer.SwitchedMode -= onSwitchedMode;
        }
    }
}
