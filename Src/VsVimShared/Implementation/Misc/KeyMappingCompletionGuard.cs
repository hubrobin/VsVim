using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Vim;

namespace Vim.VisualStudio.Implementation.Misc
{
    /// <summary>
    /// When a key mapping like ':imap jk &lt;Esc&gt;' is in progress the typed keys
    /// are buffered by VsVim and never make it into the text buffer.  The
    /// asynchronous completion machinery sits above the legacy command filter chain
    /// though and can trigger a completion session for the typed character anyway,
    /// popping up intellisense in the middle of the mapping.  Dismiss any completion
    /// session which triggers while VsVim is buffering key mapping input.
    ///
    /// If the mapping times out the buffered keys are replayed and inserted normally
    /// at which point completion is free to trigger again, matching the behavior of
    /// typing the keys without a mapping
    /// </summary>
    [Export(typeof(IVimBufferCreationListener))]
    internal sealed class KeyMappingCompletionGuard : IVimBufferCreationListener
    {
        private readonly IVim _vim;

        [ImportingConstructor]
        internal KeyMappingCompletionGuard(IVim vim, IAsyncCompletionBroker asyncCompletionBroker)
        {
            _vim = vim;
            asyncCompletionBroker.CompletionTriggered += OnCompletionTriggered;
        }

        private void OnCompletionTriggered(object sender, CompletionTriggeredEventArgs e)
        {
            try
            {
                if (_vim.TryGetVimBuffer(e.TextView, out IVimBuffer vimBuffer) &&
                    !vimBuffer.BufferedKeyInputs.IsEmpty)
                {
                    e.CompletionSession.Dismiss();
                }
            }
            catch (Exception)
            {
                // Dismissing the completion session is best effort.  Never let an
                // exception propagate into the completion machinery
            }
        }

        void IVimBufferCreationListener.VimBufferCreated(IVimBuffer vimBuffer)
        {
            // The work of this type is driven by the completion broker event
            // subscribed in the constructor.  The IVimBufferCreationListener export
            // exists to ensure this component is created
        }
    }
}
