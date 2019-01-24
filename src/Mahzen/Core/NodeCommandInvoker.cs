using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    /* 
     * │ Keyword         │ Return        │ Parameters                        │
     * ├─────────────────┼───────────────┼───────────────────────────────────┤
     * │ PING            │ bool(true)    │                                   │
     */
    class NodeCommandInvoker : ICommandInvoker
    {
        private static readonly Dictionary<string, Action<Memory<MessageProtocolObject>>> _syncActions = new Dictionary<string, Action<Memory<MessageProtocolObject>>>
        {
            ["PING"] = Ping
        };

        private static readonly Dictionary<string, Func<Memory<MessageProtocolObject>, Task>> _asyncActions = new Dictionary<string, Func<Memory<MessageProtocolObject>, Task>>
        {
            
        };

        public bool CanInvoke(Command command) => _syncActions.ContainsKey(command.Keyword) || _asyncActions.ContainsKey(command.Keyword);

        public async Task InvokeAsync(Command command)
        {
            if (_syncActions.TryGetValue(command.Keyword, out var syncAction))
            {
                syncAction(command.Parameters);
            }
            else if (_asyncActions.TryGetValue(command.Keyword, out var asyncAction))
            {
                await asyncAction(command.Parameters);
            }

            CommandContext.Current.Response.WriteError(new UnknownCommandError(command.Keyword));
        }

        public static void Ping(Memory<MessageProtocolObject> parameters)
        {
            CommandContext.Current.Response.Write(true);
        }
    }
}
