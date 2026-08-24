namespace ElectroPi.Application.Dtos.Tickets.Reporting
{
    public class TicketsReportDto
    {
        public int TotalTickets { get; set; }

        public int OpenTickets { get; set; }

        public int InProgressTickets { get; set; }

        public int ResolvedTickets { get; set; }

        public int ClosedTickets { get; set; }

        public int OpenCriticalTickets { get; set; }

        public double AverageResolutionTimeHours { get; set; }

        public List<AgentWorkloadDto> AgentWorkloads { get; set; } = [];
    }
}
