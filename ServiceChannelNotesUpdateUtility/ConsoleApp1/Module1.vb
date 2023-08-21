
Imports System.IO
Imports System.Data.OleDb
Imports System.Text
Imports System.Configuration
Imports System.Net
Imports Newtonsoft.Json
Imports System.Net.Http
Imports System.Net.Http.Headers
Module Module1
    'variable declartions

    Dim objWalSCComments As StreamWriter
    Dim objWalmartSC As StreamWriter
    Dim rootDir As String = ConfigurationSettings.AppSettings("rootDir")
    Dim logpath As String = ConfigurationSettings.AppSettings("logpath") & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim connectOR As New OleDbConnection(Convert.ToString(ConfigurationSettings.AppSettings("OLEDBconString")))

    Dim log As StreamWriter
    Dim fileStream As FileStream = Nothing
    Dim logDirInfo As DirectoryInfo = Nothing
    Dim logFileInfo As FileInfo



    'Main method from where method gets invoked
    Sub Main()

        Console.WriteLine("")

        Dim WorkOrderComments As String = logpath & "_" & String.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) & ".txt"
        logFileInfo = New FileInfo(WorkOrderComments)
        logDirInfo = New DirectoryInfo(logFileInfo.DirectoryName)

        'Checks whether the file exists, if it exists then append, else create the file
        If Not logDirInfo.Exists Then
            logDirInfo.Create()
        End If
        If Not logFileInfo.Exists Then
            fileStream = logFileInfo.Create()
        Else
            fileStream = New FileStream(WorkOrderComments, FileMode.Append)
        End If


        objWalSCComments = New StreamWriter(fileStream)
        objWalSCComments.WriteLine("Start Walmart Service Channel Comments " & Now())

        GetNotes()

        objWalSCComments.WriteLine("Ends " & Now())

        objWalSCComments.Flush()
        objWalSCComments.Close()

    End Sub

    ' Method to checks for user type as walmart or third party
    Private Function GetNotes()
        Try
            Dim ds As New DataSet
            Dim addminutes As Int16 = Convert.ToInt16(ConfigurationSettings.AppSettings("StartDateNotes"))
            Dim StartDate As DateTime = Now().AddMinutes(addminutes)
            Dim EndDate As DateTime = Now()
            Dim sqlstring As String = ""
            sqlstring = "select A.ORDER_NO, A.ISA_INTFC_LN, A.ISA_WORK_ORDER_NO,B.PO_ID,A.ISA_LINE_STATUS," & vbCrLf &
                "C.NOTES_1000,A.ISA_EMPLOYEE_ID from PS_ISA_ORD_INTF_LN A,PS_PO_LINE_DISTRIB B,ps_isa_xpd_comment C" & vbCrLf &
                "where A.business_unit_OM = 'I0W01' AND   A.ISA_LINE_STATUS IN ('DSP','ASN')" & vbCrLf &
                "AND A.BUSINESS_UNIT_PO = B.BUSINESS_UNIT" & vbCrLf &
                "AND A.ORDER_NO = B.REQ_ID" & vbCrLf &
                "AND A.ISA_INTFC_LN = B.REQ_LINE_NBR" & vbCrLf &
                "AND B.BUSINESS_UNIT = C.BUSINESS_UNIT" & vbCrLf &
                "AND B.PO_ID = C.PO_ID" & vbCrLf &
                "AND B.LINE_NBR= C.LINE_NBR" & vbCrLf &
                "AND C.ISA_PROBLEM_CODE NOT IN ('AK','WS')" & vbCrLf &
                "AND C.DTTM_STAMP > TO_DATE('" & StartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                "AND C.DTTM_STAMP <= TO_DATE('" & EndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf

            objWalSCComments.WriteLine("   Supplier comments Query: " & sqlstring)
            objWalSCComments.WriteLine("Start Supplier comment Service Channel " & Now())

            ds = ORDBAccess.GetAdapter(sqlstring, connectOR)

            If ds.Tables(0).Rows.Count > 0 Then
                Dim I As Integer
                For I = 0 To ds.Tables(0).Rows.Count - 1
                    Dim PO_num As String = String.Empty
                    Try
                        Dim Line_Status As String = ds.Tables(0).Rows(I).Item("ISA_LINE_STATUS")
                        PO_num = ds.Tables(0).Rows(I).Item("PO_ID")
                        Dim OrderNum As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
                        Dim WorkOrder As String = ds.Tables(0).Rows(I).Item("ISA_WORK_ORDER_NO")
                        Dim Emp_id As String = ds.Tables(0).Rows(I).Item("ISA_EMPLOYEE_ID")
                        Dim strComments As String = ds.Tables(0).Rows(I).Item("NOTES_1000")
                        Dim Third_party_comp_id As String = ""
                        Try
                            Dim Sqlstring2 As String = "select THIRDPARTY_COMP_ID from SDIX_USERS_TBL where ISA_EMPLOYEE_ID = '" & Emp_id & "'"
                            connectOR.Open()
                            Third_party_comp_id = ORDBAccess.GetScalar(Sqlstring2, connectOR)
                            connectOR.Close()
                        Catch ex As Exception
                            Third_party_comp_id = "0"
                        End Try

                        Dim CredType As String = ""
                        If Third_party_comp_id <> "100" Then
                            CredType = "Walmart"
                        End If
                        UpdateNotes(WorkOrder, CredType, strComments, PO_num, OrderNum)
                    Catch ex As Exception
                        objWalSCComments.WriteLine("Result- Failed in updating notes for the PO " + PO_num)
                    End Try

                Next
                objWalSCComments.WriteLine("/////////////////////////////////////////////////////////////////////////////////////////////")
            Else
                objWalSCComments.WriteLine("No data fetched")
            End If

        Catch ex As Exception

        End Try
    End Function
    'This method is used for sending Success or failure message by validating the http response
    Public Function UpdateNotes(ByVal workOrder As String, credType As String, Note As String, Ponum As String, Ordernum As String) As String
        Try
            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
                Dim APIresponse = AuthenticateService(credType)
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationSettings.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder + "/notes"
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        Dim objNoteParam As New UpdateNote
                        objNoteParam.Note = Note
                        objNoteParam.MailedTo = ""
                        objNoteParam.ActionRequired = False
                        objNoteParam.ScheduledDate = Now
                        objNoteParam.Visibility = 0
                        objNoteParam.Actor = ""
                        objNoteParam.NotifyFollowers = False
                        objNoteParam.DoNotSendEmail = True

                        Dim serializedparameter = JsonConvert.SerializeObject(objNoteParam)
                        Dim response = httpClient.PostAsync(apiURL, New StringContent(serializedparameter, Encoding.UTF8, "application/json")).Result
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                            objWalSCComments.WriteLine("Result - Success " + Convert.ToString(workorderAPIResponse) + " Work Order-" + workOrder + " PO ID-" + Ponum + " Order No-" + Ordernum + " CredType-" + credType)
                            Return "Success"
                        Else
                            objWalSCComments.WriteLine("Result- Failed in API response Work Order-" + workOrder + " PO ID-" + Ponum + " Order No-" + Ordernum + " CredType-" + credType)
                            Return "Failed"
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Return "Failed"
            objWalSCComments.WriteLine("Method:UpdateNotes - " + ex.Message)
        End Try
    End Function

    'This method is used for Authentication the credential type
    Public Function AuthenticateService(credType As String) As String
        Try
            Dim httpClient As HttpClient = New HttpClient()
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
            Dim username As String = String.Empty
            Dim password As String = String.Empty
            Dim clientKey As String = String.Empty
            If credType = "Walmart" Then
                username = ConfigurationSettings.AppSettings("WMUName")
                password = ConfigurationSettings.AppSettings("WMPassword")
                clientKey = ConfigurationSettings.AppSettings("WMClientKey")
            Else
                username = ConfigurationSettings.AppSettings("CBREUName")
                password = ConfigurationSettings.AppSettings("CBREPassword")
                clientKey = ConfigurationSettings.AppSettings("CBREClientKey")
            End If
            Dim apiurl As String = ConfigurationSettings.AppSettings("ServiceChannelLoginEndPoint")
            Dim formContent = New FormUrlEncodedContent({New KeyValuePair(Of String, String)("username", username), New KeyValuePair(Of String, String)("password", password), New KeyValuePair(Of String, String)("grant_type", "password")})
            httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", clientKey) 'Add("Authorization", "Basic " + clientKey)
            Dim response = httpClient.PostAsync(apiurl, formContent).Result
            If response.IsSuccessStatusCode Then
                Dim APIResponse = response.Content.ReadAsStringAsync().Result
                Return APIResponse
            Else
                Dim APIResponse = response.Content.ReadAsStringAsync().Result
                'Dim eobj As ExceptionHelper = New ExceptionHelper()
                'eobj.writeExceptionMessage(APIResponse, "AuthenticateService")
                If APIResponse.Contains("error_description") Then Return APIResponse
                Return "Server Error"
            End If

        Catch ex As Exception
            objWalmartSC.WriteLine("Method:AuthenticateService - " + ex.Message)
        End Try
        Return "Server Error"
    End Function

End Module

Public Class ValidateUserResponseBO
        Public Property access_token As String
        Public Property refresh_token As String
    End Class

    Public Class UpdateNote
        Public Property Note As String
        Public Property MailedTo As String
        Public Property ActionRequired As Boolean
        Public Property ScheduledDate As DateTime
        Public Property Visibility As Integer
        Public Property Actor As String
        Public Property NotifyFollowers As Boolean
        Public Property DoNotSendEmail As Boolean
    End Class


