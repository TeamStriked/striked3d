using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core
{
    public struct EngineCommandResult
    {
        public bool success;
        public object result;
    }

    public struct EngineCommand
    {
        public string method;
        public object[] arguments;
        public EngineCommand(string _method, params object[] _arguments)
        {
            method = _method; 
            arguments = _arguments; 
        }
    }
}
