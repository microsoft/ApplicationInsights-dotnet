
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : ExceptionDetails_types.cs
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

    // [global::Bond.Attribute("Description", "Exception details of the exception in a chain.")]
    // [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class ExceptionDetails
    {
        // [global::Bond.Attribute("Description", "In case exception is nested (outer exception contains inner one), the id and outerId properties are used to represent the nesting.")]
        // [global::Bond.Id(10)]
        public int id { get; set; }

        // [global::Bond.Attribute("Description", "The value of outerId is a reference to an element in ExceptionDetails that represents the outer exception")]
        // [global::Bond.Id(20)]
        public int outerId { get; set; }

        // [global::Bond.Attribute("Description", "Exception type name.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(30), global::Bond.Required]
        public string typeName { get; set; }

        // [global::Bond.Attribute("Description", "Exception message.")]
        // [global::Bond.Attribute("MaxStringLength", "32768")]
        // [global::Bond.Id(40), global::Bond.Required]
        public string message { get; set; }

        // [global::Bond.Attribute("Description", "Indicates if full exception stack is provided in the exception. The stack may be trimmed, such as in the case of a StackOverflow exception.")]
        // [global::Bond.Id(50)]
        public bool hasFullStack { get; set; }

        // [global::Bond.Attribute("Description", "Text describing the stack. Either stack or parsedStack should have a value.")]
        // [global::Bond.Attribute("MaxStringLength", "32768")]
        // [global::Bond.Id(60)]
        public string stack { get; set; }

        // [global::Bond.Attribute("Description", "List of stack frames. Either stack or parsedStack should have a value.")]
        // [global::Bond.Id(70), global::Bond.Type(typeof(List<StackFrame>))]
        public IList<StackFrame> parsedStack { get; set; }

        public ExceptionDetails()
            : this("AI.ExceptionDetails", "ExceptionDetails")
        {}

        protected ExceptionDetails(string fullName, string name)
        {
            typeName = "";
            message = "";
            hasFullStack = true;
            stack = "";
            parsedStack = new List<StackFrame>();
        }
    }
} // AI
