<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ExternalCalls.aspx.cs" Inherits="Aspx451.ExternalCalls" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
         <asp:Label ID="lblRequestedAction" runat="server" Text="Action:"></asp:Label>
        
        <asp:Label ID="lblResult" runat="server" Text="Result:"></asp:Label>
        <asp:Label ID="lblProgress" runat="server" Text=""></asp:Label>
    </div>
    </form>
</body>
</html>
