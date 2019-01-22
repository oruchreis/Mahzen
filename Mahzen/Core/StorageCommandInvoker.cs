﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    class StorageCommandInvoker : ICommandInvoker
    {
        private static HashSet<string> _keywords = new HashSet<string>
        {

        };

        public bool CanInvoke(Command command) => _keywords.Contains(command.Keyword);

        public Task InvokeAsync(Command command)
        {
            throw new NotImplementedException();
        }
    }
}
