using System.Diagnostics.CodeAnalysis;

// These will hopefully be a no-brainer:

[assembly:SuppressMessage(
            category:       "StyleCop.CSharp.DocumentationRules",
            checkId:        "SA1005: Single line comment must begin with a space.",
            Justification = "Aligning comments lines with spaces can help readability."
                          + " Critical from the accessibility standpoint for people with dyslexia.")]

[assembly:SuppressMessage(
            category:       "StyleCop.CSharp.DocumentationRules",
            checkId:        "SA1025: Code Must Not Contain Multiple Whitespace In A Row",
            Justification = "Aligning elements across lines with spaces can help readability."
                          + " Critical from the accessibility standpoint for people with dyslexia.")]

[assembly:SuppressMessage(
            category:       "StyleCop.CSharp.MaintainabilityRules",
            checkId:        "SA1119: Statement Must Not Use Unnecessary Parenthesis",
            Justification = "Parentheses are important for making long expressions readable."
                          + " Critical from the accessibility standpoint for people with dyslexia.")]

[assembly:SuppressMessage(
            category:       "StyleCop.CSharp.DocumentationRules",
            checkId:        "SA1623: Property Summary Documentation Must Match Accessors",
            Justification = "Forcing to start an element description with a specific word is counter-productive."
                          + " Focus should be on quality formulations in the docs, not on blindly starting the sentences with the same word.")]

[assembly:SuppressMessage(
            category:       "StyleCop.CSharp.DocumentationRules",
            checkId:        "SA1625: Element Documentation Must Not Be Copied And Pasted",
            Justification = "Specifically works around the remark 'ToDo: Complete documentation before stable release.'."
                          + " Needs to be re-enabled when all those are addressed.")]

[assembly:SuppressMessage(
            category:       "StyleCop.CSharp.DocumentationRules",
            checkId:        "SA1643: Destructor Summary Documentation Must Begin With Standard Text",
            Justification = "Useless rule. It is not even clear what this standard text should be.")]