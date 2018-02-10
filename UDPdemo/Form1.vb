Imports System.Net.Sockets
Imports System.Net

Public Class Form1

    Dim dataBytes(2) As Byte
    Const clientActive As Byte = &H80       '80 xx xx
    Const rbpiActive As Byte = &H40         '40 xx xx
    Const arduinoActive As Byte = &H20      '20 xx xx
    Const arduinoInput As Byte = &H10      '10 xx xx
    Const commandOne As Byte = 8          'x8 xx xx
    Const commandTwo As Byte = 4          'x4 xx xx
    Const piPortName As Int32 = 9999
    Const piIP As Int64 = 323223523 '&HC0A80003                '192.168.0.3
    Dim raspberryPi As New IPEndPoint(piIP, piPortName)
    Delegate Sub SendUDPDel(dataGram() As Byte, length As Byte, destination As IPEndPoint)
    ' Public pointToSendUDP As New SendUDPDel(AddressOf udpClient.SendAsync)
    Dim Transport As TimerLayer

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Transport = New TimerLayer
        For i As Byte = 0 To 2
            dataBytes(i) = &H0
        Next
        dataBytes(2) = clientActive                     'Send the data that alerts Raspberry Pi that this program is running
        Transport.TransportSend(dataBytes)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        dataBytes(2) = dataBytes(2) Xor commandOne        'toggles ccommand 1 bit
        Transport.TransportSend(dataBytes)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Static flag As Boolean
        If flag = False Then
            flag = True
            TextBox1.Text = "Unblocked"
        Else
            flag = False
            TextBox1.Text = ""
        End If
        dataBytes(2) = dataBytes(2) Xor commandTwo        'toggles ccommand 2 bit
        Transport.TransportSend(dataBytes)
    End Sub

    Public Sub UpdateGUI(dataGram() As Byte)
        Dim messageLength = dataGram.Length
        Dim checkPi As Boolean = dataGram(2) And rbpiActive
        Dim checkArduino As Boolean = dataGram(2) And arduinoActive
        Dim arduinoIn As Boolean = dataGram(2) And arduinoInput
        CheckBox1.Checked = checkPi
        CheckBox2.Checked = checkArduino
        CheckBox3.Checked = arduinoIn
        If checkPi Then
            TextBox1.Text = dataGram(1).ToString + dataGram(0).ToString
        End If

    End Sub


End Class


Public Class UDPClass
    Dim udpClient As New UdpClient(45557)
    Dim udpListener As New UdpClient(45555)

    Sub Connect()
        udpClient.Connect("192.168.0.3", 9999)
    End Sub

    Sub New()

    End Sub

    Public Sub UDP_Send(data() As Byte)
        Const dataLength As Byte = 3
        udpClient.Send(data, dataLength)
    End Sub

    Public Function ReceiveData() As Byte()
        Dim receivedData As Byte() = Nothing
        Dim recieveEndPoint As New IPEndPoint(IPAddress.Any, 0)
        Try
            Dim result = udpListener.ReceiveAsync()             'should call in other thread
            receivedData = result.Result.Buffer
            recieveEndPoint = result.Result.RemoteEndPoint
            result.Dispose()
        Catch
        End Try
        Return receivedData
    End Function


End Class

Public Class TimerLayer
    Dim ApplicationLayer As New System.Windows.Forms.Control()
    Dim TimerControl As New System.Windows.Forms.Control()
    Delegate Sub UpdateGuiDel(dataGram() As Byte)
    Public pointToUpdateGui As New UpdateGuiDel(AddressOf Form1.UpdateGUI)
    Dim timer2 As New System.Timers.Timer(80)
    Dim TransportLayer As New UDPClass
    Dim passingObject As New Object()
    Sub New()
        AddHandler timer2.Elapsed, AddressOf OnTimedEvent
        TransportLayer.Connect()
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''PLEASE TRY
        timer2.SynchronizingObject = TimerControl
        timer2.Start()
        ApplicationLayer.CreateControl()
    End Sub

    Public Sub OnTimedEvent(source As Object, e As System.Timers.ElapsedEventArgs)
        Dim result = TransportLayer.ReceiveData()
        ApplicationLayer.Invoke(pointToUpdateGui, New Object() {result}) 'Remove new object creation?
    End Sub

    Public Sub TransportSend(data() As Byte)
        TransportLayer.UDP_Send(data)
    End Sub

End Class