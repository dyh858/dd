Imports System.Net
Imports System.IO
Imports System.Text

Module Module1


    Sub Main()
        Dim myWebReq As WebRequest = WebRequest.Create("http://www.contoso.com")
        Dim myWebRes As WebResponse = myWebReq.GetResponse

        Dim ReceiveStream As Stream = myWebRes.GetResponseStream
        Dim encode As Encoding = System.Text.Encoding.GetEncoding("utf-8")

        Dim readStream As New StreamReader(ReceiveStream, encode)

        Console.WriteLine(ControlChars.Cr + "Response stream received")
        Console.ReadLine()
        Dim read(256) As [Char]

        Dim count As Integer = readStream.Read(read, 0, 256)
        Console.WriteLine("HTML" + ControlChars.Lf + ControlChars.Cr)
        While count > 0

        End While
    End Sub


End Module
