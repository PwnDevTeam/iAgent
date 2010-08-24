'//============================================================================
'// Name        : MobileDevice.vb
'// Author       : fallensn0w (Originally based on code developed by: geohot, ixtli, nightwatch, warren)
'// Build          : 1.1 (Reb0rn)
'// Copyright   : http://www.fallensn0w-devkit.blogspot.com
'// Description : A VB.NET implementation of MobileDevice.h
'//============================================================================

Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports LibUsbDotNet
Imports LibUsbDotNet.Main
Imports System.IO
Imports LibUsbDotNet.Info

Class MobileDevice

    Public Enum DeviceMode As Integer
        Test = 1
        Normal = &H1293
        Recovery = &H1281
        DFU = &H1222
        WTF = &H1227
    End Enum
    Private mVendor As Integer = &H5AC ' Vendor ID
    Private mMode As DeviceMode = DeviceMode.Recovery
    Private mDevice As UsbDevice

    Public Sub New(ByVal mode As Integer)
        mMode = CType(mode, DeviceMode)
        Connect() ' as soon you make it, your app will fetch the info if it's connected.
    End Sub

    Public Function getDeviceInfo() As String
        If Connect() = True Then
            Dim mDeviceInfo As UsbDeviceInfo = mDevice.Info
            Return mDeviceInfo.ToString
            WriteLog("GetDeviceInfo - OK")
        Else
            WriteLog("GetDeviceInfo - FAIL")
            Return False
        End If
    End Function

    Public Function AutoBoot() As Boolean
        If SendCommand("setenv auto-boot true") Then
            If SendCommand("saveenv") Then
                If SendCommand("reboot") Then
                    WriteLog("AutoBoot - OK")
                    Return True
                End If
            End If
        Else
            WriteLog("AutoBoot - FAIL")
            Return False
        End If
    End Function

    Public Function Connect() As Boolean
        For Each mode As Integer In [Enum].GetValues(GetType(DeviceMode))
            If Not ReferenceEquals((InlineAssignHelper(mDevice, UsbDevice.OpenUsbDevice(New UsbDeviceFinder(mVendor, mode)))), Nothing) Then
                WriteLog("Connect - OK")
                Return True
            End If
        Next
        WriteLog("Connect - FAIL")
        Return False
    End Function

    Public Function IsRunningMode(ByVal currMode As Integer) As Boolean
        For Each mode As Integer In [Enum].GetValues(GetType(DeviceMode))
            If Not ReferenceEquals((InlineAssignHelper(mDevice, UsbDevice.OpenUsbDevice(New UsbDeviceFinder(mVendor, currMode)))), Nothing) Then
                WriteLog("IsRunningMode - OK")
                Return True
            End If
        Next
        WriteLog("IsRunningMode - FAIL")
        Return False
    End Function

    Public Sub Dispose()
        If mDevice.IsOpen Then
            WriteLog("Dispose - CLOSING")
            If mDevice.Close() Then
                WriteLog("Dispose - OK")
            End If
        End If
    End Sub

    Public Function SendBuffer(ByVal dataBytes As Byte(), ByVal index As Short, ByVal length As Short) As Boolean
        Dim size As Integer, packets As Integer, last As Integer, i As Integer
        size = dataBytes.Length - index

        If length > size Then
            WriteLog("SendBuffer - INVALID DATA")
            Return False
        End If

        packets = length \ &H800

        If (length Mod &H800) = 0 Then
            packets += 1
        End If

        last = length Mod &H800

        If last = 0 Then
            last = &H800
        End If

        Dim sent As Integer = 0
        Dim response As Char() = New Char(5) {}

        For i = 0 To packets - 1
            Dim tosend As Integer = If((i + 1) > packets, &H800, last)
            sent += sent

            If SendRaw(&H21, 1, 0, CShort(i), New Byte(dataBytes(index + (i * &H800)) - 1) {}, CShort(tosend)) Then
                'wont work SendRaw return's true / false need to find a work around
                If Not SendRaw(&HA1, 3, 0, 0, Encoding.[Default].GetBytes(response.ToString()), 6) Then
                    ' != 6 
                    'cant check if its 6 so fuck knows :(
                    If response(4) = "5" Then
                        WriteLog("SendBuffer - Sent Chunk")
                        Continue For
                    End If
                    WriteLog("SendBuffer - Invalid Status")
                    Return False
                End If
                WriteLog("SendBuffer - Failed To Retreive Status")
                Return False
            End If
            WriteLog("SendBuffer - Fail To Send")
            Return False
        Next

        WriteLog("SendBuffer - Executing Buffer")
        SendRaw(&H21, 1, CShort(i), 0, dataBytes, 0)
        'might work probably wont
        For i = 6 To 7
            ' != 6 
            If Not (SendRaw(&H21, 1, CShort(i), 0, Encoding.[Default].GetBytes(response), 6)) Then
                ' need to find a work around
                If response(4) <> "" Then
                    WriteLog("SendBuffer - Failed To Execute")
                    Return False
                End If
            End If
        Next

        WriteLog("SendBuffer - Transfered Buffer")
        Return True
    End Function

    Public Function SendCommand(ByVal command As String) As Boolean
        If command.Length > &H200 Then
            WriteLog("SendCommand - Command Is Too Long")
            Return False
        End If

        Return SendRaw(&H40, 0, 0, 0, command, CShort(command.Length))

    End Function

    Public Function SendPayload(ByVal PayloadName As String) As Boolean
        Dim input As New FileStream(PayloadName, FileMode.Open)
        Dim reader As New BinaryReader(input)
        Dim bytes() As Byte
        bytes = reader.ReadBytes(CInt(input.Length))
        Return SendExploit(bytes)
    End Function

    Public Function SendExploit(ByVal dataBytes As Byte()) As Boolean
        If Not SendBuffer(dataBytes, 0, CShort(dataBytes.Length)) Then
            If SendRaw(&H21, 2, 0, 0, New Byte(-1) {}, 0) Then
                WriteLog("SendExploit - Executed Exploit At 0x21")
                Return True
            End If
        End If

        WriteLog("SendExploit - Failed To Exploit At 0x21")
        Return False
    End Function

    Public Function SendRaw(ByVal requestType As Byte, ByVal request As Byte, ByVal value As Short, ByVal index As Short, ByVal data As String, ByVal length As Short) As Boolean
        Return SendRaw(requestType, request, value, index, Encoding.[Default].GetBytes(data), length)
    End Function

    Public Function SendRaw(ByVal requestType As Byte, ByVal request As Byte, ByVal value As Short, ByVal index As Short, ByVal data As Byte(), ByVal length As Short) As Boolean
        If Not mDevice.IsOpen Then
            WriteLog("SendRaw - Opening Connection")
            If Not Connect() Then
                WriteLog("SendRaw - Failed To Connect")
                Return False
            End If
        End If

        length += 1
        ' allocate null byte
        Dim setupPacket As New UsbSetupPacket(requestType, request, value, index, length)

        Dim transfered As Integer = 0

        ' the SendRAW status is fucked up, i dont want to fix it atm.
        If mDevice.ControlTransfer(setupPacket, data, length, transfered) Then
            '   WriteLog("SendRaw - OK")
            Return True
        Else
            ' WriteLog("SendRaw - FAIL")
            Return False
        End If

    End Function

    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
        target = value : Return value
    End Function

    Public Sub SendRawUsb_0xA1(ByVal command As String)
        WriteLog("SendRawUsb_0xA1 - SENDNING")
        If SendRaw(&HA1, Len(command), 0, 0, 0, 0) Then
            WriteLog("SendRawUsb_0xA1 - OK")
        Else
            WriteLog("SendRawUsb_0xA1 - FAIL")
        End If
    End Sub


    Public Sub SendRawUsb_0x40(ByVal command As String)
        WriteLog("SendRawUsb_0x40 - SENDNING")
        If SendRaw(&H40, Len(command), 0, 0, 0, 0) Then
            WriteLog("SendRawUsb_0x40 - OK")
        Else
            WriteLog("SendRawUsb_0x40 - FAIL")
        End If
    End Sub

    Public Sub SendRawUsb_0x21(ByVal command As String)
        WriteLog("SendRawUsb_0x21 - SENDNING")
        If SendRaw(&H21, Len(command), 0, 0, 0, 0) Then
            WriteLog("SendRawUsb_0x21 - OK")
        Else
            WriteLog("SendRawUsb_0x21 - FAIL")
        End If
    End Sub

    Public Sub Send_Arm7_Go()
        Dim command(12) As String
        WriteLog("Sending Arm7_Go (ipt2-2.1.1 iBSS Exploit).")

        command(0) = "arm7_stop"
        command(1) = "mw 0x9000000 0xe59f3014"
        command(2) = "mw 0x9000004 0xe3a02a02"
        command(3) = "mw 0x9000008 0xe1c320b0"
        command(4) = "mw 0x900000c 0xe3e02000"
        command(5) = "mw 0x9000010 0xe2833c9d"
        command(6) = "mw 0x9000014 0xe58326c0"
        command(7) = "mw 0x9000018 0xeafffffe"
        command(8) = "mw 0x900001c 0x2200f300"
        command(9) = "arm7_go"
        command(10) = "#"
        command(11) = "arm7_stop"

        SendCommand(command(0)) : SendCommand(command(1)) : SendCommand(command(2)) : SendCommand(command(3))
        SendCommand(command(4)) : SendCommand(command(5)) : SendCommand(command(6)) : SendCommand(command(7))
        SendCommand(command(8)) : SendCommand(command(9)) : SendCommand(command(10)) : SendCommand(command(11))

        Console.WriteLine("Done sending Arm7_Go Exploit.")
    End Sub


    Public Function SaveTextToFile(ByVal strData As String, ByVal FullPath As String, Optional ByVal ErrInfo As String = "") As Boolean
        Dim bAns As Boolean = False, objReader As StreamWriter : Try : objReader = New StreamWriter(FullPath) : objReader.Write(strData) : objReader.Close() : bAns = True : Catch Ex As Exception : ErrInfo = Ex.Message : End Try : Return bAns
    End Function

    Public Function GetFileContents(ByVal FullPath As String, Optional ByRef ErrInfo As String = "") As String
        Dim strContents As String = "" : Dim objReader As StreamReader : Try : objReader = New StreamReader(FullPath) : strContents = objReader.ReadToEnd() : objReader.Close() : Catch Ex As Exception : ErrInfo = Ex.Message : End Try : Return strContents
    End Function

    Public Sub WriteLog(ByVal Text As String)
        old = GetFileContents(Application.ProductName & ".txt")
        SaveTextToFile((old & Application.ProductName & " @ " & TimeOfDay & " -> " & LCase(Text)) & vbCrLf, Application.ProductName & ".txt")
    End Sub


End Class


