Imports System
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports DevExpress.XtraScheduler
Imports System.Data.SqlClient

Namespace FetchAppointmentExample

    Public Partial Class Form1
        Inherits DevExpress.XtraEditors.XtraForm

        ' The counter used to display the number of fetches.
        Public Shared queryExecutionCounter As Integer

#Region "#lastfetchedinterval"
        Const PADDING_DAYS As Integer = 7

#End Region  ' #lastfetchedinterval
        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub Form1_Load(ByVal sender As Object, ByVal e As EventArgs)
            resourcesTableAdapter.Fill(scheduleTestDataSet.Resources)
            appointmentsTableAdapter.Fill(scheduleTestDataSet.Appointments)
            If scheduleTestDataSet.Resources.Rows.Count = 0 OrElse scheduleTestDataSet.Appointments.Rows.Count = 0 Then CreateSampleData()
            AddHandler schedulerStorage1.AppointmentsChanged, AddressOf OnApptChangedInsertedDeleted
            AddHandler schedulerStorage1.AppointmentsInserted, AddressOf OnApptChangedInsertedDeleted
            AddHandler schedulerStorage1.AppointmentsDeleted, AddressOf OnApptChangedInsertedDeleted
            AddHandler schedulerControl1.VisibleIntervalChanged, AddressOf schedulerControl1_VisibleIntervalChanged
            AddHandler schedulerControl1.VisibleResourcesChanged, AddressOf SchedulerControl1_VisibleResourcesChanged
            schedulerControl1.Start = Date.Today
            schedulerControl1.GroupType = SchedulerGroupType.Resource
            schedulerControl1.ActiveView.ResourcesPerPage = 1
            UpdateStatisticsInformationDisplayedOnTheForm()
        End Sub

#Region "#fetchappointments"
        Private Sub schedulerStorage1_FetchAppointments(ByVal sender As Object, ByVal e As FetchAppointmentsEventArgs)
            Dim resourcesVisible As ResourceBaseCollection = New ResourceBaseCollection() With {.Capacity = schedulerControl1.ActiveView.ResourcesPerPage}
            For i As Integer = 0 To schedulerControl1.ActiveView.ResourcesPerPage - 1
                resourcesVisible.Add(schedulerStorage1.Resources(schedulerControl1.ActiveView.FirstVisibleResourceIndex + i))
            Next

            QueryAppointmentDataSource(e, resourcesVisible)
        End Sub

        Private Sub QueryAppointmentDataSource(ByVal e As FetchAppointmentsEventArgs, ByVal resources As ResourceBaseCollection)
            Dim resListString As String = String.Join(",", resources.[Select](Function(res) res.Id.ToString()))
            ' Modify the FillBy query to fetch appointments only for the specified resources. 
            appointmentsTableAdapter.Commands(1).CommandText = String.Format("SELECT Appointments.* FROM Appointments WHERE (OriginalOccurrenceStart >= @Start) AND(OriginalOccurrenceEnd <= @End) AND (ResourceID IN ({0})) OR (Type != 0)", resListString)
            appointmentsTableAdapter.FillBy(scheduleTestDataSet.Appointments, e.Interval.Start.AddDays(-PADDING_DAYS), e.Interval.End.AddDays(PADDING_DAYS))
            queryExecutionCounter += 1
        End Sub

#End Region  ' #fetchappointments
        Private Sub OnApptChangedInsertedDeleted(ByVal sender As Object, ByVal e As PersistentObjectsEventArgs)
            appointmentsTableAdapter.Update(scheduleTestDataSet)
            scheduleTestDataSet.AcceptChanges()
        End Sub

        Private Sub UpdateStatisticsInformationDisplayedOnTheForm()
            lblInfo.Text = String.Format("FetchAppointment query has been executed {0} times," & Microsoft.VisualBasic.Constants.vbCrLf & "data set contains {1} appointments.", queryExecutionCounter, scheduleTestDataSet.Appointments.Count)
        End Sub

        Private Sub schedulerControl1_VisibleIntervalChanged(ByVal sender As Object, ByVal e As EventArgs)
            UpdateStatisticsInformationDisplayedOnTheForm()
        End Sub

        Private Sub SchedulerControl1_VisibleResourcesChanged(ByVal sender As Object, ByVal args As VisibleResourcesChangedEventArgs)
            UpdateStatisticsInformationDisplayedOnTheForm()
        End Sub

        ' Fills the Scheduler with resources and appointments.
        Private Sub CreateSampleData()
            SchedulerHelper.FillResources(schedulerStorage1, 17)
            resourcesTableAdapter.Update(scheduleTestDataSet)
            SchedulerHelper.GenerateAppointments(schedulerStorage1, 150)
            BulkUpdateAppointmentsTable()
            scheduleTestDataSet.AcceptChanges()
            appointmentsTableAdapter.Fill(scheduleTestDataSet.Appointments)
        End Sub

        ' Performs a bulk insert which is much faster than calling the appointmentsTableAdapter.Update. 
        ' The appointmentsTableAdapter.Update method has low performance 
        ' because it updates one row at at time, executes Select query to obtain the identity value for each row 
        ' and writes to the transaction log.
        ' The BulkUpdateAppointmentsTable method does not retrieve ID values, 
        ' so the appointmentsTableAdapter.Fill method should be called subsequently.        
        Private Sub BulkUpdateAppointmentsTable()
            Using connection As SqlConnection = New SqlConnection(Properties.Settings.Default.ScheduleTestConnectionString)
                connection.Open()
                Dim transaction As SqlTransaction = connection.BeginTransaction()
                Using bulkCopy As SqlBulkCopy = New SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
                    bulkCopy.BatchSize = 100
                    bulkCopy.DestinationTableName = "dbo.Appointments"
                    Try
                        bulkCopy.WriteToServer(scheduleTestDataSet.Appointments)
                    Catch __unusedException1__ As Exception
                        transaction.Rollback()
                    End Try
                End Using

                transaction.Commit()
            End Using
        End Sub

        Private Sub cbFetchAppointments_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs)
            If cbFetchAppointments.Checked Then
                schedulerStorage1.EnableSmartFetch = False
                AddHandler schedulerStorage1.FetchAppointments, AddressOf schedulerStorage1_FetchAppointments
            Else
                RemoveHandler schedulerStorage1.FetchAppointments, AddressOf schedulerStorage1_FetchAppointments
                resourcesTableAdapter.Fill(scheduleTestDataSet.Resources)
                appointmentsTableAdapter.Fill(scheduleTestDataSet.Appointments)
                schedulerStorage1.EnableSmartFetch = True
            End If
        End Sub

        Private Sub cbBoldAppointmentDates_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs)
            If cbBoldAppointmentDates.Checked Then
                dateNavigator1.BoldAppointmentDates = True
            Else
                dateNavigator1.BoldAppointmentDates = False
            End If

            dateNavigator1.Refresh()
        End Sub
    End Class
End Namespace
