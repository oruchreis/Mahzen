using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    public interface ICommandInvoker
    {
        bool CanInvoke(Command command);
        Task InvokeAsync(Command command);
    }
}
