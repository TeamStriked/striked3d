using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Engine
{
    public class EditorArguments
    {
        [Option('d', "debug", Required = false, HelpText = "Enable the debug mode and render validation.")]
        public bool Debug { get; set; }

        [Option('p', "project", Required = false, HelpText = "Path to the project folder")]
        public string ProjectPath { get; set; }
    }
}
