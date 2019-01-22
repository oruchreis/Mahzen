using System;
using System.Collections.Generic;
using System.Text;

namespace Mahzen.Core
{
    class BufferOverflowException : Exception
    {
        public BufferOverflowException(string msg)
            : base(msg)
        {

        }
    }
}
