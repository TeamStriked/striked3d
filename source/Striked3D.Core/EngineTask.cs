using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core
{
    public struct EngineTask
    {
        public Action<EngineCommandResult> callback;
        public EngineCommand command;
        public EngineTask(EngineCommand _command, Action<EngineCommandResult> _callback = null)
        {
            this.callback = _callback;
            this.command = _command;
        }

    }

    public struct EngineCompleteTask
    {
        public EngineTask completedTask;
        public EngineCommandResult result;
    }
}
