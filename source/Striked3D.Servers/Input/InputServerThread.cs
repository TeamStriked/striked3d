using Striked3D.Core;
using System;
using System.Collections.Generic;
using System.Text;


namespace Striked3D.Servers
{
    [ApiDeclaration("InputServer")]
    internal class InputServerThread : ServerThreadRunner
    {
        protected Window _win;

        public void Initialize(Window win)
        {
            _win = win;
        }
    }
}
