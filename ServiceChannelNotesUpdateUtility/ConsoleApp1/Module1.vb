
Imports System.IO
Imports System.Data.OleDb
Imports System.Text
Imports System.Configuration
Imports System.Net
Imports Newtonsoft.Json
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Xml
Imports System.Web
Imports System.Web.Mail
Module Module1
    'variable declartions
    'Madhu-INC0031327-Commented the Notes Log

    'Dim objWalSCComments As StreamWriter
    Dim objWalSCWorkOrder As StreamWriter
    Dim objWalmartSC As StreamWriter
    Dim rootDir As String = ConfigurationSettings.AppSettings("rootDir")
    ' Dim logpath As String = ConfigurationSettings.AppSettings("logpath") & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim WalmartSCWorkOrderPath As String = ConfigurationSettings.AppSettings("WalmartSCWorkOrder") & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim connectOR As New OleDbConnection(Convert.ToString(ConfigurationSettings.AppSettings("OLEDBconString")))

    Dim log As StreamWriter
    Dim fileStream As FileStream = Nothing
    Dim logDirInfo As DirectoryInfo = Nothing
    Dim logFileInfo As FileInfo



    'Main method from where method gets invoked
    Sub Main()

        Console.WriteLine("")

        Dim WorkOrderComments As String = WalmartSCWorkOrderPath & "_" & String.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) & ".txt"
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


        objWalSCWorkOrder = New StreamWriter(fileStream)
        'objWalSCComments.WriteLine("Start Walmart Service Channel Comments " & Now())
        'objWalSCWorkOrder = File.CreateText(WalmartSCWorkOrderPath)
        objWalSCWorkOrder.WriteLine("Start Updatework orders to Service channel " & Now())
        'Madhu-INC0031548 - Utility splitup [Commented the notes]
        ' GetNotes()
        buildstatchgout()

        ' objWalSCComments.WriteLine("Ends " & Now())

        ' objWalSCComments.Flush()
        ' objWalSCComments.Close()

        objWalSCWorkOrder.WriteLine("Ends " & Now())

        objWalSCWorkOrder.Flush()
        objWalSCWorkOrder.Close()

    End Sub
    Private Function buildstatchgout()
        Dim dsBU As DataSet
        dsBU = GetBU()

        If Not dsBU Is Nothing Then
            objWalSCWorkOrder.WriteLine("Total BU going to Process " + Convert.ToString(dsBU.Tables(0).Rows.Count()))

            objWalSCWorkOrder.WriteLine("-------------------------------------------------------------------------------")
            For I = 0 To dsBU.Tables(0).Rows.Count - 1


                If (dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT") = "I0W01") Then
                    Try
                        Dim dteStartDate As DateTime = GetStartDate(dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT"))
                        Dim dteEndDate As DateTime = GetEndDate(dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT"))
                        dteEndDate.AddSeconds(1)
                        objWalSCWorkOrder.WriteLine("  StatChg Email Update workorder for Enterprise BU : " & dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT") & " " & Now())
                        UpdateWalmartSourceCode(dteStartDate, dteEndDate, dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT"))

                    Catch ex As Exception
                        objWalSCWorkOrder.WriteLine("  Error in StatChg Email Update workorder Executeion : " & ex.Message & " " & Now())
                        SendEmail(ex.Message)
                    End Try

                End If
            Next
        End If
    End Function
    Public Function getDBName() As Boolean
        Dim isPRODDB As Boolean = False
        Dim PRODDbList As String = ConfigurationSettings.AppSettings("OraPRODDbList").ToString()
        Dim DbUrl As String = ConfigurationSettings.AppSettings("OLEDBconString").ToString()
        Try
            DbUrl = DbUrl.Substring(DbUrl.Length - 4).ToUpper()
            isPRODDB = (PRODDbList.IndexOf(DbUrl.Trim.ToUpper) > -1)
        Catch ex As Exception
            isPRODDB = False
        End Try
        Return isPRODDB
    End Function
    Private Function GetBU() As DataSet
        Dim ds As System.Data.DataSet = New System.Data.DataSet
        Try
            '' To get teh list of BU 
            Dim getBuQuery As String = "SELECT DISTINCT(ISA_BUSINESS_UNIT) AS BUSINESS_UNIT from PS_ISA_ENTERPRISE"

            Dim Command As OleDbCommand = New OleDbCommand(getBuQuery, connectOR)
            If connectOR.State = ConnectionState.Open Then
                'do nothing
            Else
                connectOR.Open()
            End If

            Dim dataAdapter As OleDbDataAdapter =
                        New OleDbDataAdapter(Command)
            Try
                dataAdapter.Fill(ds)
                connectOR.Close()
            Catch ex As Exception
                objWalSCWorkOrder.WriteLine("Error in GetBU  " & " " & ex.Message & Now())

            End Try
            If Not ds Is Nothing Then
                If ds.Tables(0).Rows.Count() > 0 Then
                    Return ds
                Else
                    Return Nothing
                End If
            Else
                Return Nothing
            End If
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Sub SendEmail(Optional ByVal Message As String = "")

        Dim SDIEmailService As SDiEmailUtilityService.EmailServices = New SDiEmailUtilityService.EmailServices()
        Dim MailAttachmentName As String()
        Dim sTR As String
        Dim MailAttachmentbytes As New List(Of Byte())()
        Dim email As New MailMessage
        Dim MailToSpecial As String = ConfigurationSettings.AppSettings("MailToSpecial")

        'The email address of the sender
        email.From = "WalmartPurchasing@sdi.com"

        'The email address of the recipient. 
        If Not getDBName() Then
            email.To = "webdev@sdi.com"

        Else
            email.To = MailToSpecial
        End If

        'The subject of the email
        email.Subject = "Error in the Update workorder status to Service channel Utility."

        'The Priority attached and displayed for the email
        email.Priority = MailPriority.High

        email.BodyFormat = MailFormat.Html

        email.Body = "<html><body><table><tr><td>Update workorder status to Service channel' Utility has completed with errors.Please Check Logs </td></tr>"

        'email.Body = email.Body & "<tr><td></td><a href='\\BDougherty_XP-l\logs'>\\BDougherty_XP-l\logs\</a></tr></table></body></html>"

        'Send the email and handle any error that occurs
        Try
            'UpdEmailOut.UpdEmailOut.UpdEmailOut(email.Subject, email.From, "sriram.s@avasoft.biz", "", "", "Y", email.Body, connectOR)
            SDIEmailService.EmailUtilityServices("MailandStore", email.From, email.To, email.Subject, String.Empty, String.Empty, email.Body, "StatusChangeEmail0", MailAttachmentName, MailAttachmentbytes.ToArray())
        Catch
            objWalSCWorkOrder.WriteLine("     Error - the email was not sent")
        End Try

    End Sub

    Private Function updateLastSendDate(ByVal strBU As String, ByVal dteEndDate As DateTime) As Boolean
        connectOR.Close()
        Dim strSQLstring As String
        'Dim dteEndDate As DateTime
        Dim ds As New DataSet
        Dim bolerror1 As Boolean
        Dim rowsaffected As Integer


        ' The enddate coming from PS_ISAORDERSTATUSLOG  is being set back to the original enddate.  The PS_ISA_enterprise table
        ' is then updated with the PS_ISAORDERSTATUSLOG's endddate and the next time in, the date in the PS_ISA_enterprise table is
        ' the startdate.  We increased the enddate a second so we could get all the records from the query.  We were never getting
        ' the last record because of milliseconds were off in the date conversions.  Adding a second we were able to get all
        ' the records in the date range....  If you understand this you have a date to sit with the Dali Lama.. Believe me
        ' it works!!!!!!!!  PFD 4.4.2008
        ' reset the dteEndDate back to original

        dteEndDate.AddSeconds(-1)

        strSQLstring = "UPDATE SDIX_EMAIL_DETAIL" & vbCrLf &
                    " SET ISA_LAST_STAT_SEND = TO_DATE('" & dteEndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                    " WHERE ORDER_MFG_BU = '" & strBU & "' "

        Try
            Dim Command = New OleDbCommand(strSQLstring, connectOR)
            objWalSCWorkOrder.WriteLine("  updateEnterprise (1): " & strSQLstring & " " & Now())
            connectOR.Open()
            rowsaffected = Command.ExecuteNonQuery()
            connectOR.Close()
        Catch OleDBExp As OleDbException
            Console.WriteLine("")
            Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
            Console.WriteLine("")
            connectOR.Close()
            objWalSCWorkOrder.WriteLine("  Error - updating the Enterprise send date " & OleDBExp.ToString & " " & Now())
            bolerror1 = True
        End Try


        If bolerror1 = True Then
            Return True
        Else
            Return False
        End If
    End Function

    Function GetEndDate(ByVal BU As String) As DateTime

        Dim strSQLstring As String = ""
        Dim dteEndDate As DateTime = Now

        Dim format As New System.Globalization.CultureInfo("en-US", True)
        strSQLstring = "SELECT" & vbCrLf &
            " to_char(MAX( A.DTTM_STAMP), 'MM/DD/YY HH24:MI:SS') as MAXDATE" & vbCrLf &
            " FROM PS_ISAORDSTATUSLOG A" & vbCrLf &
             " WHERE A.BUSINESS_UNIT_OM = '" & BU & "'"

        Dim dr As OleDbDataReader = Nothing

        Try
            objWalSCWorkOrder.WriteLine("  GetEndDate: " & strSQLstring & " " & Now())

            Dim command As OleDbCommand
            command = New OleDbCommand(strSQLstring, connectOR)
            If connectOR.State = ConnectionState.Open Then
                'do nothing
            Else
                connectOR.Open()
            End If
            dr = command.ExecuteReader
            Try

                If dr.Read Then
                    dteEndDate = (dr.Item("MAXDATE"))
                    dteEndDate = dteEndDate.AddMinutes(+1)
                Else
                    dteEndDate = Now.ToString
                End If
            Catch ex As Exception
                dteEndDate = Now.ToString
                objWalSCWorkOrder.WriteLine("     Error - error reading end date FROM PS_ISAORDSTATUSLOG A" & " " & Now())
            End Try

            dr.Close()
            connectOR.Close()

        Catch OleDBExp As OleDbException
            Try
                dr.Close()
                connectOR.Close()
            Catch exOR As Exception

            End Try
            objWalSCWorkOrder.WriteLine("     Error - error reading end date FROM PS_ISAORDSTATUSLOG A" & " " & Now())
        End Try
        objWalSCWorkOrder.WriteLine("  GetEndDate: " & dteEndDate & " " & Now())

        Return dteEndDate
    End Function
    Function GetStartDate(ByVal BU As String) As DateTime

        Dim strSQLstring As String = ""
        Dim GetStart_Date As DateTime
        Dim format As New System.Globalization.CultureInfo("en-US", True)

        strSQLstring = " Select TO_CHAR((A.ISA_LAST_STAT_SEND), 'MM/DD/YY HH24:MI:SS') AS ISA_LAST_STAT_SEND From SDIX_EMAIL_DETAIL A WHERE A.ORDER_MFG_BU = '" & BU & "'"
        objWalSCWorkOrder.WriteLine("  GetStartDate: " & strSQLstring & " " & Now())

        Dim command1 As OleDbCommand
        command1 = New OleDbCommand(strSQLstring, connectOR)
        If connectOR.State = ConnectionState.Open Then
            'do nothing
        Else
            connectOR.Open()
        End If
        Dim objReader As OleDbDataReader = command1.ExecuteReader()
        Try
            If objReader.Read() Then
                If IsDBNull(objReader.Item("ISA_LAST_STAT_SEND")) Then
                    GetStart_Date = Now.AddDays(-1)
                Else
                    GetStart_Date = objReader.Item("ISA_LAST_STAT_SEND")
                End If

            End If
            objReader.Close()
            connectOR.Close()

        Catch OleDBExp As OleDbException
            objWalSCWorkOrder.WriteLine("     Error - error reading Start date FROM PS_ISA_ENTERPRISE A" & " " & Now())

            Try
                objReader.Close()
                connectOR.Close()
            Catch exOR As Exception

            End Try
        End Try

        objWalSCWorkOrder.WriteLine("  GetStartDate: " & GetStart_Date & " " & Now())
        objWalSCWorkOrder.WriteLine("-----------------------------------------------------------------------------")


        Return GetStart_Date
    End Function
    'Madhu-WAL-1203-Select queryto get the details of WorkOrder[Otimised the query to bring the current line status]

    Private Function UpdateWalmartSourceCode(ByVal dteStartDate As Date, ByVal dteEndDate As Date, ByVal strBU As String)
        Dim bolerror1 As Boolean

        Try
            Dim ds As New DataSet
            Dim strSQLstring As String = String.Empty
            strSQLstring = "SELECT distinct G.BUSINESS_UNIT_OM, G.BUSINESS_UNIT_OM AS G_BUS_UNIT, D.BUSINESS_UNIT, D.ISA_EMPLOYEE_ID, A.ORDER_NO,B.ISA_WORK_ORDER_NO As WORK_ORDER_NO, B.ISA_INTFC_LN AS line_nbr," & vbCrLf &
                " B.ISA_EMPLOYEE_ID AS EMPLID, B.ISA_LINE_STATUS as ORDER_TYPE,B.OPRID_ENTERED_BY," & vbCrLf &
                " TO_CHAR(G.DTTM_STAMP, 'MM/DD/YYYY HH:MI:SS AM') as DTTM_STAMP, " & vbCrLf &  '  & _
                     " (SELECT E.XLATLONGNAME" & vbCrLf &
                                    " FROM XLATTABLE E" & vbCrLf &
                                    " WHERE E.EFFDT =" & vbCrLf &
                                    " (SELECT MAX(E_ED.EFFDT) FROM XLATTABLE E_ED" & vbCrLf &
                                    " WHERE(E.FIELDNAME = E_ED.FIELDNAME)" & vbCrLf &
                                    " AND E.FIELDVALUE = E_ED.FIELDVALUE" & vbCrLf &
                                    " AND E_ED.EFFDT <= SYSDATE)" & vbCrLf &
                                    " AND E.FIELDNAME = 'ISA_LINE_STATUS'" & vbCrLf &
                                    " AND E.FIELDVALUE = G.ISA_LINE_STATUS) as ORDER_STATUS_DESC, " & vbCrLf &
                     " B.DESCR254 As NONSTOCK_DESCRIPTION, C.DESCR60 as STOCK_DESCRIPTION, D.ISA_EMPLOYEE_EMAIL," & vbCrLf &
                     " B.INV_ITEM_ID as INV_ITEM_ID," & vbCrLf &
            " D.FIRST_NAME_SRCH, D.LAST_NAME_SRCH" & vbCrLf &
                     " ,A.origin, LD.PO_ID, SH.ISA_ASN_TRACK_NO" & vbCrLf &
                     " FROM ps_isa_ord_intf_HD A," & vbCrLf  '   & _

            strSQLstring += " ps_isa_ord_intf_LN B," & vbCrLf &
                     " PS_MASTER_ITEM_TBL C," & vbCrLf &
                     " PS_ISA_USERS_TBL D," & vbCrLf &
                     " PS_ISAORDSTATUSLOG G, PS_ISA_ASN_SHIPPED SH, PS_PO_LINE_DISTRIB LD" & vbCrLf &
                     " where G.BUSINESS_UNIT_OM = '" & strBU & "' " & vbCrLf &
                     " AND G.BUSINESS_UNIT_OM = A.BUSINESS_UNIT_OM " & vbCrLf &
                     " AND G.BUSINESS_UNIT_OM = D.BUSINESS_UNIT " & vbCrLf     '   & _

            strSQLstring += "  and A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf &
                     " and A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                     " and C.SETID (+) = 'MAIN1'" & vbCrLf &
                     " and C.INV_ITEM_ID(+) = B.INV_ITEM_ID " & vbCrLf &
                     " AND G.ORDER_NO = A.ORDER_NO " & vbCrLf &
                     " AND B.ISA_INTFC_LN = G.ISA_INTFC_LN" & vbCrLf &
                     " AND G.ISA_LINE_STATUS = B.ISA_LINE_STATUS" & vbCrLf &
                     " AND B.ISA_LINE_STATUS IN ('DLF','ASN','DLP','CNC','RPU')" & vbCrLf &
                     " AND A.BUSINESS_UNIT_OM = D.BUSINESS_UNIT" & vbCrLf &
                     " AND SH.PO_ID (+) = LD.PO_ID And SH.LINE_NBR (+) = LD.LINE_NBR And SH.SCHED_NBR (+) = LD.SCHED_NBR And LD.Req_id (+) = B.order_no AND LD.REQ_LINE_NBR (+) = B.ISA_INTFC_LN" & vbCrLf &
                      "AND G.DTTM_STAMP > TO_DATE('" & dteStartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                      "AND G.DTTM_STAMP <= TO_DATE('" & dteEndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
            " AND UPPER(B.ISA_EMPLOYEE_ID) = UPPER(D.ISA_EMPLOYEE_ID)" & vbCrLf &
                      " ORDER BY ORDER_NO, LINE_NBR, DTTM_STAMP"

            Try
                objWalSCWorkOrder.WriteLine("  UpdateWalmartSourceCode Q1New: " & strSQLstring)
                Try
                    Dim st As New Stopwatch()
                    st.Start()
                    ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
                    st.Stop()
                    Dim ts As TimeSpan = st.Elapsed
                    Dim elapsedTime As String = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)
                    objWalSCWorkOrder.WriteLine("Query Execution Time " + elapsedTime)
                    objWalSCWorkOrder.WriteLine("Fetched Datas:" + Convert.ToString(ds.Tables(0).Rows.Count()))

                Catch ex As Exception
                    ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
                    objWalSCWorkOrder.WriteLine("Query Execution Time " + Now())
                    objWalSCWorkOrder.WriteLine("Fetched Datas:" + Convert.ToString(ds.Tables(0).Rows.Count()))
                    SendEmail(ex.Message)

                End Try

                Dim I As Integer
                Dim lstOfString As List(Of String) = New List(Of String)
                For I = 0 To ds.Tables(0).Rows.Count - 1

                    Try
                        Dim OrderNo As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
                        If OrderNo.ToUpper.Substring(0, 1) = "W" Then
                            If Not lstOfString.Contains(OrderNo) Then
                                objWalSCWorkOrder.WriteLine("Order No: " + Convert.ToString(OrderNo) + "Count " + Convert.ToString(I))
                                lstOfString.Add(OrderNo)
                                Dim WorkOrder As String = ds.Tables(0).Rows(I).Item("WORK_ORDER_NO")
                                objWalSCWorkOrder.WriteLine("WorkOrder No: " + Convert.ToString(WorkOrder))
                                Dim EnteredBy As String = ds.Tables(0).Rows(I).Item("OPRID_ENTERED_BY")
                                If Not String.IsNullOrEmpty(WorkOrder) Then
                                    Dim strSQLQuery As String = "select THIRDPARTY_COMP_ID from SDIX_USERS_TBL where ISA_EMPLOYEE_ID='" & EnteredBy & "' "
                                    Dim dsUser As DataSet = ORDBAccess.GetAdapter(strSQLQuery, connectOR)
                                    Dim Order As String()
                                    If dsUser.Tables.Count > 0 Then
                                        Dim THIRDPARTY_COMP_ID As String = String.Empty
                                        Try
                                            THIRDPARTY_COMP_ID = dsUser.Tables(0).Rows(0).Item("THIRDPARTY_COMP_ID")
                                            objWalSCWorkOrder.WriteLine("THIRDPARTY_COMP_ID: " + Convert.ToString(THIRDPARTY_COMP_ID))
                                        Catch ex As Exception
                                            THIRDPARTY_COMP_ID = "0"
                                            objWalSCWorkOrder.WriteLine("Catch-THIRDPARTY_COMP_ID: " + Convert.ToString(THIRDPARTY_COMP_ID))
                                        End Try
                                        Dim OrderStatusDetail As New OrderStatusDetail
                                        Dim orderDetail As String = OrdrStatus(OrderNo)
                                        objWalSCWorkOrder.WriteLine("Current Order Status: " + Convert.ToString(orderDetail))
                                        If orderDetail.Trim() <> "" Then
                                            Order = orderDetail.Split("^"c)
                                            OrderStatusDetail.orderStatus = Order(0)
                                            OrderStatusDetail.statusDesc = Order(1)
                                            OrderStatusDetail.dueDate = Order(2)
                                            OrderStatusDetail.message = "Success"
                                            objWalSCWorkOrder.WriteLine("Order No: " + Convert.ToString(OrderNo) + "Status" + Convert.ToString(OrderStatusDetail.statusDesc))
                                            If OrderStatusDetail.message = "Success" Then
                                                'WAL-622: SC Updates for Canceled Orders And Partial Deliveries 
                                                'Mythili - WAL-824 Need Service Channel API change to map PUR (Ready for Pickup) from In Progress / Parts on Order to new Service Channel Extended Status “In Progress / Parts Ready for Pickup
                                                If OrderStatusDetail.statusDesc = "Delivered" Or OrderStatusDetail.statusDesc = "En Route from Vendor" Or OrderStatusDetail.statusDesc = "Partially Delivered" Or OrderStatusDetail.statusDesc = "Cancelled" Or OrderStatusDetail.statusDesc = "Ready for Pickup" Then
                                                    Dim CheckWOStatus As String = CheckWorkOrderStatus(WorkOrder, THIRDPARTY_COMP_ID)
                                                    objWalSCWorkOrder.WriteLine("CheckWOStatus: " + Convert.ToString(CheckWOStatus))
                                                    If CheckWOStatus.ToUpper() <> "COMPLETED" And CheckWOStatus <> "Failed" Then
                                                        Dim WOStatus As String = String.Empty
                                                        If OrderStatusDetail.statusDesc = "Delivered" Then
                                                            WOStatus = "PARTS DELIVERED"
                                                        ElseIf OrderStatusDetail.statusDesc = "En Route from Vendor" Then
                                                            WOStatus = "PARTS SHIPPED"
                                                        ElseIf OrderStatusDetail.statusDesc = "Partially Delivered" Then
                                                            WOStatus = "PARTIAL PARTS DELIVERED"
                                                        ElseIf OrderStatusDetail.statusDesc = "Cancelled" Then
                                                            WOStatus = "INCOMPLETE"
                                                        ElseIf OrderStatusDetail.statusDesc = "Ready for Pickup" Then
                                                            WOStatus = "PARTS READY FOR PICKUP"
                                                        End If
                                                        If CheckWOStatus <> WOStatus Then
                                                            Dim PurchaseNo As String = PurchaseOrderNo(WorkOrder, THIRDPARTY_COMP_ID)
                                                            If PurchaseNo <> "Failed" Then
                                                                If Not String.IsNullOrEmpty(THIRDPARTY_COMP_ID) Then
                                                                    If THIRDPARTY_COMP_ID = ConfigurationSettings.AppSettings("CBRECompanyID").ToString() Then
                                                                        UpdateWorkOrderStatus(WorkOrder, "CBRE", WOStatus)
                                                                        UpdateWorkOrderStatus(PurchaseNo, "Walmart", WOStatus)
                                                                    Else
                                                                        UpdateWorkOrderStatus(WorkOrder, "Walmart", WOStatus)
                                                                    End If
                                                                Else
                                                                    UpdateWorkOrderStatus(WorkOrder, "Walmart", WOStatus)
                                                                End If

                                                            End If
                                                        End If
                                                    End If

                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Catch ex As Exception
                        objWalSCWorkOrder.WriteLine("Method:UpdateWalmartSourceCode - " + ex.Message & " " & Now())
                    End Try
                Next

            Catch OleDBExp As OleDbException
                Console.WriteLine("")
                Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
                Console.WriteLine("")
                connectOR.Close()
                objWalSCWorkOrder.WriteLine("     Error - error reading transaction FROM PS_ISAORDSTATUSLOG A" & " " & Now())
                Return True
            End Try

            If IsDBNull(ds.Tables(0).Rows.Count) Or (ds.Tables(0).Rows.Count) = 0 Then
                Console.WriteLine("Fetched Datas 0")
                objWalSCWorkOrder.WriteLine("Fetched Datas 0")
                objWalSCWorkOrder.WriteLine("     Warning - no status changes to process at this time for All Statuses" & " " & Now())
                Try
                    connectOR.Close()
                Catch ex As Exception
                    objWalSCWorkOrder.WriteLine("Error in UpdateWalmartSourcecodemetod  " & " " & ex.Message & Now())
                    SendEmail(ex.Message)

                End Try
                Return False
            Else
                Console.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
                objWalSCWorkOrder.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()) & " " & Now())
            End If
        Catch ex As Exception
            objWalSCWorkOrder.WriteLine("Error in UpdateWalmartSourcecodemetod  " & " " & ex.Message & Now())
            SendEmail(ex.Message)

        End Try
        bolerror1 = updateLastSendDate(strBU, dteEndDate)
        If bolerror1 = True Then
            SendEmail()
        End If

    End Function
    'Madhu-WAL-1203-Check  the Work Order status in service channel

    Public Function CheckWorkOrderStatus(ByVal workOrder As String, THIRDPARTY_COMP_ID As String) As String
        Try
            Dim APIresponse As String = String.Empty
            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
                If Not String.IsNullOrEmpty(THIRDPARTY_COMP_ID) Then
                    If THIRDPARTY_COMP_ID = ConfigurationSettings.AppSettings("CBRECompanyID").ToString() Then
                        APIresponse = AuthenticateService("CBRE")
                    Else
                        APIresponse = AuthenticateService("Walmart")
                    End If
                End If
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationSettings.AppSettings("ServiceChannelBaseAddress") + "/odata/" + "/workorders(" + workOrder + ")?$select=Status"
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        Dim response = httpClient.GetAsync(apiURL).Result
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                            Dim objCheckWo As CheckWo = JsonConvert.DeserializeObject(Of CheckWo)(workorderAPIResponse)
                            Return objCheckWo.Status.Primary
                            objWalSCWorkOrder.WriteLine("Method: CheckWorkOrderStatus() Result-" + Convert.ToString(objCheckWo.Status.Extended))
                        Else
                            objWalSCWorkOrder.WriteLine("Method: CheckWorkOrderStatus() Result- Failed in API response")
                            Return "Failed"
                        End If
                    End If
                Else
                    objWalSCWorkOrder.WriteLine("Method:CheckWorkOrderStatus - " + APIresponse)

                End If
            End If
        Catch ex As Exception
            Return "Failed"
            objWalSCWorkOrder.WriteLine("Method:CheckWorkOrderStatus - " + ex.Message)
        End Try
    End Function
    Public Function PurchaseOrderNo(ByVal workOrder As String, THIRDPARTY_COMP_ID As String) As String
        Try
            Dim APIresponse = String.Empty
            Dim objWorkOrderDetails As New WorkOrderDetails
            'Commented the CBRE Authentication for getting work order details
            If Not String.IsNullOrEmpty(THIRDPARTY_COMP_ID) Then
                If THIRDPARTY_COMP_ID = ConfigurationSettings.AppSettings("CBRECompanyID").ToString() Then
                    APIresponse = AuthenticateService("CBRE")
                Else
                    APIresponse = AuthenticateService("Walmart")
                End If
            Else
                APIresponse = AuthenticateService("Walmart")
            End If
            ' APIresponse = Await AuthenticateService(Walmart)
            If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                If (Not APIresponse.Contains("error_description")) Then
                    Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                    Dim apiURL = ConfigurationSettings.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder
                    Dim httpClient As HttpClient = New HttpClient()
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                    httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                    Dim response = httpClient.GetAsync(apiURL).Result
                    If response.IsSuccessStatusCode Then
                        Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                        If workorderAPIResponse <> "[]" And Not String.IsNullOrEmpty(workorderAPIResponse) And Not String.IsNullOrWhiteSpace(workorderAPIResponse) Then
                            objWorkOrderDetails = JsonConvert.DeserializeObject(Of WorkOrderDetails)(workorderAPIResponse)
                            Return objWorkOrderDetails.PurchaseNumber
                            objWalSCWorkOrder.WriteLine("Method: PurchaseOrderNo() Result-" + Convert.ToString(objWorkOrderDetails.PurchaseNumber))
                        Else
                            objWalSCWorkOrder.WriteLine("Method: PurchaseOrderNo() Result- Failed in API response")
                            Return "Failed"
                        End If
                    Else
                        Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                        objWalSCWorkOrder.WriteLine("Method: PurchaseOrderNo() Result- Failed in API response")
                        Return "Failed"
                    End If
                End If
                objWalSCWorkOrder.WriteLine("Method:PurchaseOrderNo - " + APIresponse)
            End If
        Catch ex As Exception
            Return "Failed"
            objWalSCWorkOrder.WriteLine("Method:PurchaseOrderNo - " + ex.Message)
        End Try
    End Function
    'Madhu-WAL-1203-Update the Work Order to service channel

    Public Function UpdateWorkOrderStatus(ByVal workOrder As String, credType As String, status As String) As String
        Try
            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
                Dim APIresponse = AuthenticateService(credType)
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationSettings.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder + "/status"
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        'Dim response = httpClient.GetAsync(apiURL).Result
                        Dim objPartParam As New UpdateWorkOrderBO
                        objPartParam.Note = String.Empty
                        objPartParam.Status = New Status
                        objPartParam.Status.Primary = "In Progress"
                        objPartParam.Status.Extended = status

                        Dim serializedparameter = JsonConvert.SerializeObject(objPartParam)
                        Dim response = httpClient.PutAsync(apiURL, New StringContent(serializedparameter, Encoding.UTF8, "application/json")).Result
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                            objWalSCWorkOrder.WriteLine("Result-" + Convert.ToString(workorderAPIResponse) & " " & Now())
                            objWalSCWorkOrder.WriteLine("--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------")

                            Return "Success"
                        Else
                            objWalSCWorkOrder.WriteLine("Result- Failed in API response" & " " & Now())
                            objWalSCWorkOrder.WriteLine("--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------")

                            Return "Failed"
                        End If
                    End If
                Else
                    objWalSCWorkOrder.WriteLine("Method:UpdateWorkOrderStatus - " + APIresponse)
                End If


            End If
        Catch ex As Exception
            Return "Failed"
            objWalSCWorkOrder.WriteLine("Method:UpdateWorkOrderStatus - " + ex.Message & " " & Now())
        End Try
    End Function
    'Madhu-WAL-1203-Check  the Order status

    Public Function OrdrStatus(orderno As String) As String
        Try
            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            connectOR.Open()
            Dim orderDetail As String = String.Empty
            Dim qString As String = "select sysadm8.ord_stat_summary('" + orderno + "') from dual"
            orderDetail = ORDBAccess.GetScalar(qString, connectOR)
            Return orderDetail
        Catch ex As Exception
            objWalSCWorkOrder.WriteLine("Method: OrdrStatus(): " + Convert.ToString(ex.Message))
        End Try

    End Function
    ' Method to checks for user type as walmart or third party
    'Private Function GetNotes()
    '    Try
    '        Dim ds As New DataSet
    '        Dim addminutes As Int16 = Convert.ToInt16(ConfigurationSettings.AppSettings("StartDateNotes"))
    '        Dim StartDate As DateTime = Now().AddMinutes(addminutes)
    '        Dim EndDate As DateTime = Now()
    '        Dim sqlstring As String = ""
    '        sqlstring = "select A.ORDER_NO, A.ISA_INTFC_LN, A.ISA_WORK_ORDER_NO,B.PO_ID,A.ISA_LINE_STATUS," & vbCrLf &
    '            "C.NOTES_1000,A.ISA_EMPLOYEE_ID from PS_ISA_ORD_INTF_LN A,PS_PO_LINE_DISTRIB B,ps_isa_xpd_comment C" & vbCrLf &
    '            "where A.business_unit_OM = 'I0W01' AND   A.ISA_LINE_STATUS IN ('DSP','ASN')" & vbCrLf &
    '            "AND A.BUSINESS_UNIT_PO = B.BUSINESS_UNIT" & vbCrLf &
    '            "AND A.ORDER_NO = B.REQ_ID" & vbCrLf &
    '            "AND A.ISA_INTFC_LN = B.REQ_LINE_NBR" & vbCrLf &
    '            "AND B.BUSINESS_UNIT = C.BUSINESS_UNIT" & vbCrLf &
    '            "AND B.PO_ID = C.PO_ID" & vbCrLf &
    '            "AND B.LINE_NBR= C.LINE_NBR" & vbCrLf &
    '            "AND C.ISA_PROBLEM_CODE NOT IN ('AK','WS')" & vbCrLf &
    '            "AND C.DTTM_STAMP > TO_DATE('" & StartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
    '            "AND C.DTTM_STAMP <= TO_DATE('" & EndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf

    '         objWalSCComments.WriteLine("   Supplier comments Query: " & sqlstring)
    '         objWalSCComments.WriteLine("Start Supplier comment Service Channel " & Now())

    '        ds = ORDBAccess.GetAdapter(sqlstring, connectOR)

    '        If ds.Tables(0).Rows.Count > 0 Then
    '            Dim I As Integer
    '            For I = 0 To ds.Tables(0).Rows.Count - 1
    '                Dim PO_num As String = String.Empty
    '                Try
    '                    Dim Line_Status As String = ds.Tables(0).Rows(I).Item("ISA_LINE_STATUS")
    '                    PO_num = ds.Tables(0).Rows(I).Item("PO_ID")
    '                    Dim OrderNum As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
    '                    Dim WorkOrder As String = ds.Tables(0).Rows(I).Item("ISA_WORK_ORDER_NO")
    '                    Dim Emp_id As String = ds.Tables(0).Rows(I).Item("ISA_EMPLOYEE_ID")
    '                    Dim strComments As String = ds.Tables(0).Rows(I).Item("NOTES_1000")
    '                    Dim Third_party_comp_id As String = ""
    '                    Try
    '                        Dim Sqlstring2 As String = "select THIRDPARTY_COMP_ID from SDIX_USERS_TBL where ISA_EMPLOYEE_ID = '" & Emp_id & "'"
    '                        connectOR.Open()
    '                        Third_party_comp_id = ORDBAccess.GetScalar(Sqlstring2, connectOR)
    '                        connectOR.Close()
    '                    Catch ex As Exception
    '                        Third_party_comp_id = "0"
    '                    End Try

    '                    Dim CredType As String = ""
    '                    If Third_party_comp_id <> "100" Then
    '                        CredType = "Walmart"
    '                    End If
    '                    UpdateNotes(WorkOrder, CredType, strComments, PO_num, OrderNum)
    '                Catch ex As Exception
    '                    objWalSCComments.WriteLine("Result- Failed in updating notes for the PO " + PO_num)
    '                End Try

    '            Next
    '            objWalSCComments.WriteLine("/////////////////////////////////////////////////////////////////////////////////////////////")
    '        Else
    '            objWalSCComments.WriteLine("No data fetched")
    '        End If

    '    Catch ex As Exception
    '        objWalSCComments.WriteLine("GetNotes" & " " & ex.Message & Now())

    '    End Try
    'End Function
    'This method is used for sending Success or failure message by validating the http response
    'Public Function UpdateNotes(ByVal workOrder As String, credType As String, Note As String, Ponum As String, Ordernum As String) As String
    '    Try
    '        If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
    '            Dim APIresponse = AuthenticateService(credType)
    '            If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
    '                If (Not APIresponse.Contains("error_description")) Then
    '                    Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
    '                    Dim apiURL = ConfigurationSettings.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder + "/notes"
    '                    Dim httpClient As HttpClient = New HttpClient()
    '                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
    '                    httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
    '                    Dim objNoteParam As New UpdateNote
    '                    objNoteParam.Note = Note
    '                    objNoteParam.MailedTo = ""
    '                    objNoteParam.ActionRequired = False
    '                    objNoteParam.ScheduledDate = Now
    '                    objNoteParam.Visibility = 0
    '                    objNoteParam.Actor = ""
    '                    objNoteParam.NotifyFollowers = False
    '                    objNoteParam.DoNotSendEmail = True

    '                    Dim serializedparameter = JsonConvert.SerializeObject(objNoteParam)
    '                    Dim response = httpClient.PostAsync(apiURL, New StringContent(serializedparameter, Encoding.UTF8, "application/json")).Result
    '                    If response.IsSuccessStatusCode Then
    '                        Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
    '                        objWalSCComments.WriteLine("Result - Success " + Convert.ToString(workorderAPIResponse) + " Work Order-" + workOrder + " PO ID-" + Ponum + " Order No-" + Ordernum + " CredType-" + credType)
    '                        Return "Success"
    '                    Else
    '                        objWalSCComments.WriteLine("Result- Failed in API response Work Order-" + workOrder + " PO ID-" + Ponum + " Order No-" + Ordernum + " CredType-" + credType)
    '                        Return "Failed"
    '                    End If
    '                End If
    '            End If
    '        End If
    '    Catch ex As Exception
    '        Return "Failed"
    '        objWalSCComments.WriteLine("Method:UpdateNotes - " + ex.Message)
    '    End Try
    'End Function

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
Public Class OrderStatusDetail
    Public Property message As String
    Public Property orderStatus As String
    Public Property statusDesc As String
    Public Property dueDate As String
End Class
Public Class WOStatus
    Public Property Primary As String
    Public Property Extended As String
    Public Property CanCreateInvoice As String
End Class
Public Class Notes
    Public Property Last As Last
End Class
Public Class Last
    Public Property NoteData As String = String.Empty
End Class

Public Class Location
    Public Property StoreId As String = String.Empty
End Class
Public Class Asset
    Public Property Tag As String = String.Empty
End Class


Public Class WorkOrderDetails
    Public Property Notes As Notes
    Public Property Location As Location
    Public Property Asset As Asset
    Public Property PurchaseNumber As String = String.Empty

End Class

Public Class CheckWo
    Public Property OdataContext As String
    Public Property Status As WOStatus
End Class
Public Class UpdateWorkOrderBO
    Public Property Status As Status
    Public Property Note As String
End Class
Public Class Status
    Public Property Primary As String
    Public Property Extended As String
End Class






