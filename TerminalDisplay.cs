using System;
using System.Collections.Generic;
using System.Globalization;

namespace FairScheduler;

/// <summary>Everything that is printed to the console. No scheduling logic lives here.</summary>
public class TerminalDisplay
{
    public void DisplayPublicHolidays(ScheduleGenerator generator)
    {
        Console.WriteLine();
        Console.WriteLine("PUBLIC HOLIDAYS IN THE SCHEDULING PERIOD (" +
            generator.PeriodStart.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " to " +
            generator.PeriodEnd.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ")");

        if (generator.PeriodHolidays.Count == 0)
        {
            Console.WriteLine("  None.");
            return;
        }

        for (int i = 0; i < generator.PeriodHolidays.Count; i++)
        {
            PublicHoliday holiday = generator.PeriodHolidays[i];
            string note = "SKIPPED - no sessions scheduled";
            if (holiday.Date.DayOfWeek == DayOfWeek.Saturday || holiday.Date.DayOfWeek == DayOfWeek.Sunday)
            {
                note = "falls on a weekend - no effect";
            }
            Console.WriteLine("  " + holiday.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + "  " +
                holiday.Date.DayOfWeek.ToString().PadRight(9) + "  " + holiday.Name + "  [" + note + "]");
        }
    }

    public void DisplayMonthlySchedule(ScheduleGenerator generator)
    {
        Console.WriteLine();
        Console.WriteLine("SCHEDULE (group sessions are shown in cyan)");

        string[] headers = new string[] { "Date", "Day", "Session Type", "Start Time", "End Time", "Participants", "Duration" };
        List<string[]> rows = new List<string[]>();
        List<bool> highlight = new List<bool>();

        for (int i = 0; i < generator.Sessions.Count; i++)
        {
            Session s = generator.Sessions[i];
            rows.Add(new string[] { s.DateText, s.DayName, s.TypeText, s.StartText, s.EndText, s.ParticipantNames, s.DurationText });
            highlight.Add(s.SessionType == SessionType.Group);
        }

        PrintTable(headers, rows, highlight);
    }

    public void DisplayParticipantSummary(ScheduleGenerator generator)
    {
        Console.WriteLine();
        Console.WriteLine("PARTICIPANT SUMMARY");

        string[] headers = new string[] { "Participant", "Service Days", "Total Hours", "Group Sessions", "One-on-One Sessions", "Remaining Days", "Status" };
        List<string[]> rows = new List<string[]>();
        List<bool> highlight = new List<bool>();

        for (int i = 0; i < generator.Participants.Count; i++)
        {
            Participant p = generator.Participants[i];
            string status = (p.RemainingDays == 0) ? "Completed" : "NOT COMPLETE";
            rows.Add(new string[]
            {
                p.Name,
                p.CompletedServiceDays.ToString(),
                p.TotalWorkingHours.ToString("0.##", CultureInfo.InvariantCulture),
                p.GroupSessionCount.ToString(),
                p.OneOnOneSessionCount.ToString(),
                p.RemainingDays.ToString(),
                status
            });
            highlight.Add(p.RemainingDays != 0);
        }

        PrintTable(headers, rows, highlight);
    }

    public void DisplayFairnessReport(FairnessReport report)
    {
        Console.WriteLine();
        Console.WriteLine("FAIRNESS REPORT");
        Console.WriteLine("  Service days : highest " + report.MaxDays + ", lowest " + report.MinDays + ", difference " + report.DaysDifference);
        Console.WriteLine("  Total hours  : highest " + report.MaxHours.ToString("0.##", CultureInfo.InvariantCulture) +
            ", lowest " + report.MinHours.ToString("0.##", CultureInfo.InvariantCulture) +
            ", difference " + report.HoursDifference.ToString("0.##", CultureInfo.InvariantCulture));
        Console.WriteLine("  Group sessions     : highest " + report.MaxGroupSessions + ", lowest " + report.MinGroupSessions + ", difference " + report.GroupDifference + " (allowed: 1)");
        Console.WriteLine("  One-on-one sessions: highest " + report.MaxOneOnOneSessions + ", lowest " + report.MinOneOnOneSessions + ", difference " + report.OneOnOneDifference);

        if (report.AllFairnessRequirementsMet)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  RESULT: All fairness requirements were achieved.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  RESULT: Perfect fairness could NOT be achieved. Reasons:");
            for (int i = 0; i < report.Reasons.Count; i++)
            {
                Console.WriteLine("   - " + report.Reasons[i]);
            }
            Console.ResetColor();
        }
    }

    public void DisplayWarnings(List<string> warnings)
    {
        Console.WriteLine();
        if (warnings.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("VALIDATION: all checks passed (no rule violations found).");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("VALIDATION WARNINGS:");
        for (int i = 0; i < warnings.Count; i++)
        {
            Console.WriteLine("  ! " + warnings[i]);
        }
        Console.ResetColor();
    }

    // ---------------------------------------------------------------
    // Draws a table with every column as wide as its longest value.
    // ---------------------------------------------------------------
    private void PrintTable(string[] headers, List<string[]> rows, List<bool> highlight)
    {
        int[] widths = new int[headers.Length];
        for (int c = 0; c < headers.Length; c++)
        {
            widths[c] = headers[c].Length;
        }
        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < headers.Length; c++)
            {
                if (rows[r][c].Length > widths[c]) widths[c] = rows[r][c].Length;
            }
        }

        string border = "+";
        for (int c = 0; c < widths.Length; c++)
        {
            border = border + new string('-', widths[c] + 2) + "+";
        }

        Console.WriteLine(border);
        Console.WriteLine(BuildRow(headers, widths));
        Console.WriteLine(border);

        for (int r = 0; r < rows.Count; r++)
        {
            if (highlight[r]) Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(BuildRow(rows[r], widths));
            Console.ResetColor();
        }
        Console.WriteLine(border);
    }

    private string BuildRow(string[] cells, int[] widths)
    {
        string line = "|";
        for (int c = 0; c < cells.Length; c++)
        {
            line = line + " " + cells[c].PadRight(widths[c]) + " |";
        }
        return line;
    }
}
