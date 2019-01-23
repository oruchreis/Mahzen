using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    /// <summary>
    /// Responsible for invoking commands.
    /// </summary>
    public interface ICommandInvoker
    {
        /// <summary>
        /// Checks if this invoker can invoke this command or not.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        bool CanInvoke(Command command);

        /// <summary>
        /// Invokes the command.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task InvokeAsync(Command command);
    }
}
