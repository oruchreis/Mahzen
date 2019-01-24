using Serilog;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    /// <summary>
    /// Manages node talking command protocol.
    /// </summary>
    class NodeTalkCommandDispatcher : CommandDispatcher
    {
        static readonly NodeTalkCommandInvoker _nodeTalkCommandInvoker = new NodeTalkCommandInvoker();

        public NodeTalkCommandDispatcher(NetworkStream stream, CancellationToken cancelToken) :
            base(stream, cancelToken)
        {
        }

        public override async Task HandleAsync()
        {
            try
            {
                var commands = await CommandSerializer.DeserializeAsync(Stream).ConfigureAwait(false);
                var defaultInvokers = GetInvokers();
                foreach (var command in commands)
                {
                    if (CancelToken.IsCancellationRequested)
                        return;
                    using (new CommandContext(command, Response))
                    {
                        var invoked = false;

                        //high priority
                        if (_nodeTalkCommandInvoker.CanInvoke(command))
                        {
                            await _nodeTalkCommandInvoker.InvokeAsync(command).ConfigureAwait(false);
                            invoked = true;
                            break;
                        }

                        //nodes can request default commands to each other, too.
                        foreach (var defaultInvoker in defaultInvokers)
                        {
                            if (defaultInvoker.CanInvoke(command))
                            {
                                await defaultInvoker.InvokeAsync(command).ConfigureAwait(false);
                                invoked = true;
                                break;
                            }
                        }

                        if (!invoked)
                        {
                            CommandContext.Current.Response
                                .WriteError(new UnknownCommandError(command.Keyword));
                        }
                    }
                }
            }
            catch (SyntaxErrorException e)
            {
                //todo: Add to stats
                CommandContext.Current.Response
                    .WriteError(e.ToSyntaxError());
            }
            catch (Exception e) //Dispatcher global error handler:
            {
                //todo: Add to stats
                CommandContext.Current? //if command context is not null then we can return a response.
                    .Response.WriteError(new InternalError("NTC0001"));
                Log.Error(e, "Error occured when handling commands");
            }
            finally
            {
                await Response.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}
