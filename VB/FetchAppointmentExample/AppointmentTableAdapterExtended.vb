Imports System

Namespace FetchAppointmentExample.ScheduleTestDataSetTableAdapters

#Region "#AppointmentsTableAdapterEx"
    Public Partial Class AppointmentsTableAdapter

        Public ReadOnly Property Commands As Data.SqlClient.SqlCommand()
            Get
                Return _commandCollection
            End Get
        End Property
    End Class
#End Region  ' #AppointmentsTableAdapterEx
End Namespace
