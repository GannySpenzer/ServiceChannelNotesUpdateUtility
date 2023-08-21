Imports System.Data.OleDb
Public Class ORDBAccess
    Public Shared Function GetAdapter(ByVal p_strQuery As String, ByVal connection As OleDbConnection) As DataSet

        Dim UserdataSet As System.Data.DataSet = New System.Data.DataSet

        Try
            Dim Command As OleDbCommand = New OleDbCommand(p_strQuery, connection)
            If connection.State = ConnectionState.Open Then
                'do nothing
            Else
                connection.Open()
            End If
            Dim dataAdapter As OleDbDataAdapter =
                    New OleDbDataAdapter(Command)

            dataAdapter.Fill(UserdataSet)
            connection.Close()
        Catch objException As Exception
            'MsgBox(objException.ToString, MsgBoxStyle.Critical)
            Try
                connection.Close()
            Catch ex As Exception

            End Try
        End Try

        Return UserdataSet

    End Function

    Public Shared Function GetReader(ByVal p_strQuery As String, ByVal connection As OleDbConnection) As OleDbDataReader
        Try
            Dim Command = New OleDbCommand(p_strQuery, connection)
            connection.Open()
            Dim datareader As OleDbDataReader
            datareader = Command.ExecuteReader(CommandBehavior.CloseConnection)
            Return datareader
        Catch objException As Exception
            'MsgBox(objException.ToString, MsgBoxStyle.Critical)
        End Try

    End Function

    Public Shared Function GetScalar(ByVal p_strQuery As String, ByVal connection As OleDbConnection) As String
        Try

            Dim Command = New OleDbCommand(p_strQuery, connection)
            'connection.Open()
            Dim strReturn As String
            strReturn = Command.ExecuteScalar()
            'connection.Close()
            Return strReturn
        Catch objException As Exception
            'MsgBox(objException.ToString, MsgBoxStyle.Critical)
        End Try
    End Function

    Public Shared Function ExecNonQuery(ByVal p_strQuery As String, ByVal connection As OleDbConnection) As Integer

        Dim rowsAffected As Integer

        Try
            Dim Command = New OleDbCommand(p_strQuery, connection)
            'connection.open()
            rowsAffected = Command.ExecuteNonQuery()
            'connection.close()
            Return rowsAffected
        Catch objException As Exception
            'MsgBox(objException.ToString, MsgBoxStyle.Critical)
        End Try
    End Function

End Class
