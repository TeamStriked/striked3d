using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ApiDeclarationAttribute : System.Attribute
    {
        private string serviceName;
        public ApiDeclarationAttribute(string _serviceName)
        {
            this.serviceName = _serviceName;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class ApiDeclarationMethodAttribute : System.Attribute
    {

    }
}
