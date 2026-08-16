using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Vim.VisualStudio.Implementation.Misc
{
    /// <summary>
    /// Modern editor commands like "Go To Definition" (F12) are frequently handled
    /// entirely inside the new editor commanding chain and never reach the legacy
    /// IOleCommandTarget filters where VsVim would notice them.  This handler
    /// participates in the modern chain purely to let the host know a navigation
    /// command was initiated, so the selection of the navigation target which the
    /// command produces gets cleared instead of switching the buffer into visual
    /// mode.  The command itself is always passed on to the real handlers
    /// </summary>
    [Export(typeof(ICommandHandler))]
    [Name("VsVim Navigation Command Guard")]
    [ContentType(VimConstants.ContentType)]
    [Order(Before = DefaultOrderings.Highest)]
    internal sealed class NavigationCommandGuard : ICommandHandler<GoToDefinitionCommandArgs>
    {
        private readonly VsVimHost _vsVimHost;

        [ImportingConstructor]
        internal NavigationCommandGuard(VsVimHost vsVimHost)
        {
            _vsVimHost = vsVimHost;
        }

        public string DisplayName
        {
            get { return "VsVim Navigation Command Guard"; }
        }

        public CommandState GetCommandState(GoToDefinitionCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(GoToDefinitionCommandArgs args, CommandExecutionContext executionContext)
        {
            _vsVimHost.NotifyNavigationCommand();

            // Never handle the command here.  Returning false lets the real Go To
            // Definition handler run
            return false;
        }
    }
}
