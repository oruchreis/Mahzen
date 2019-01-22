using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    class NetworkCommandDispatcher : CommandDispatcher
    {
        public NetworkCommandDispatcher(NetworkStream stream, System.Threading.CancellationToken cancelToken) :
            base(stream, cancelToken)
        {
        }

        public async override Task HandleAsync()
        {
            try
            {
                var commands = await CommandSerializer.DeserializeAsync(Stream).ConfigureAwait(false);
                var invokers = GetInvokers();
                foreach (var command in commands)
                {
                    using (new CommandContext(command, Response))
                    {
                        var invoked = false;
                        foreach (var invoker in invokers)
                        {
                            if (invoker.CanInvoke(command))
                            {
                                await invoker.InvokeAsync(command).ConfigureAwait(false);
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
                    .Response.WriteError(new InternalError("NCD0001"));
                Log.Error(e, "Error occured when handling commands");
            }
            finally
            {
                await Response.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}
