using System;
using System.Collections.Generic;
using System.Globalization;

namespace FairScheduler;

public enum SessionType
{
    Group,
    OneOnOne
}

/// <summary>
/// One scheduled session: a date, a time range and the people attending.
/// </summary>
public class Session
{
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public SessionType SessionType { get; set; }
    public List<Participant> Participants { get; set; }

    public Session()
    {
        Participants = new List<Participant>();
    }

    // Every attendee gets the FULL duration (hours are never divided between people).
    public double Duration
    {
        get { return (EndTime - StartTime).TotalHours; }
    }

    public string DateText
    {
        get { return Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture); }
    }

    public string DayName
    {
        get { return Date.DayOfWeek.ToString(); }
    }

    public string TypeText
    {
        get { return SessionType == SessionType.Group ? "Group" : "One-on-One"; }
    }

    public string StartText
    {
        get { return StartTime.ToString(@"hh\:mm"); }
    }

    public string EndText
    {
        get { return EndTime.ToString(@"hh\:mm"); }
    }

    public string DurationText
    {
        get { return Duration.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    public string ParticipantNames
    {
        get
        {
            string text = "";
            for (int i = 0; i < Participants.Count; i++)
            {
                if (i > 0) text = text + ", ";
                text = text + Participants[i].Name;
            }
            return text;
        }
    }

    public bool Includes(Participant person)
    {
        return Participants.Contains(person);
    }
}
