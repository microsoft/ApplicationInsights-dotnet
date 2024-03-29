
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : StackFrame_types.cs
//
// Changes to this file may cause incorrect behavior and will be lost when
// the code is regenerated.
// <auto-generated />
//------------------------------------------------------------------------------


// suppress "Missing XML comment for publicly visible type or member"
#pragma warning disable 1591


#region ReSharper warnings
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantUsingDirective
#endregion

namespace AI
{
    using System.Collections.Generic;

    // [global::Bond.Attribute("Description", "Stack frame information.")]
    // [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class StackFrame
    {
        // [global::Bond.Attribute("Description", "Level in the call stack. For the long stacks SDK may not report every function in a call stack.")]
        // [global::Bond.Id(10), global::Bond.Required]
        public int level { get; set; }

        // [global::Bond.Attribute("Description", "Method name.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(20), global::Bond.Required]
        public string method { get; set; }

        // [global::Bond.Attribute("Description", "Name of the assembly (dll, jar, etc.) containing this function.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(30)]
        public string assembly { get; set; }

        // [global::Bond.Attribute("Description", "File name or URL of the method implementation.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(50)]
        public string fileName { get; set; }

        // [global::Bond.Attribute("Description", "Line number of the code implementation.")]
        // [global::Bond.Id(60)]
        public int line { get; set; }

        public StackFrame()
            : this("AI.StackFrame", "StackFrame")
        {}

        protected StackFrame(string fullName, string name)
        {
            method = "";
            assembly = "";
            fileName = "";
        }
    }
} // AI
