using System;
using System.Collections.Generic;

namespace FairScheduler;

/// <summary>Results of the fairness measurement (see CheckFairness).</summary>
public class FairnessReport
{
    public int MaxDays { get; set; }
    public int MinDays { get; set; }
    public double MaxHours { get; set; }
    public double MinHours { get; set; }
    public int MaxGroupSessions { get; set; }
    public int MinGroupSessions { get; set; }
    public int MaxOneOnOneSessions { get; set; }
    public int MinOneOnOneSessions { get; set; }

    public int DaysDifference { get { return MaxDays - MinDays; } }
    public double HoursDifference { get { return MaxHours - MinHours; } }
    public int GroupDifference { get { return MaxGroupSessions - MinGroupSessions; } }
    public int OneOnOneDifference { get { return MaxOneOnOneSessions - MinOneOnOneSessions; } }

    public bool DaysAreEqual { get; set; }
    public bool HoursAreEqual { get; set; }
    public bool GroupIsBalanced { get; set; }   // gap of 1 or less
    public bool AllFairnessRequirementsMet { get; set; }

    // Explains WHY something is not equal (empty when everything is fair).
    public List<string> Reasons { get; set; }

    public FairnessReport()
    {
        Reasons = new List<string>();
    }
}

/// <summary>
/// Builds the schedule. Deterministic: the same month and year always give the same result.
/// This class does NOT print anything and does NOT create documents.
/// </summary>
public class ScheduleGenerator
{
    // ----- Settings you can change -----
    public const int ParticipantsPerDay = 7;       // how many people work on a normal day
    public const int GroupSize = 5;                // people in a main group session
    public const int LastServiceDayOfSelectedMonth = 17;
    public const double MaxHoursPerDay = 6;
    public const int ExpectedParticipantCount = 13;
    public static readonly TimeSpan CampusOpen = new TimeSpan(9, 0, 0);
    public static readonly TimeSpan CampusClose = new TimeSpan(16, 0, 0);
    // -----------------------------------

    private const double SessionHours = 6;

    private List<Participant> participants;
    private PublicHolidayManager holidayManager;
    private int selectedMonth;
    private int selectedYear;

    // Key = "name|yyyy-MM-dd", value = hours worked that day. Used by CheckDailyHourLimit.
    private Dictionary<string, double> hoursPerDay = new Dictionary<string, double>();

    public List<Session> Sessions { get; private set; }
    public List<string> Warnings { get; private set; }
    public List<DateTime> EligibleDates { get; private set; }
    public List<PublicHoliday> SkippedHolidays { get; private set; }   // holidays that removed a possible working day
    public List<PublicHoliday> PeriodHolidays { get; private set; }    // every holiday in the two months
    public List<Participant> Participants { get { return participants; } }
    public int SelectedMonth { get { return selectedMonth; } }
    public int SelectedYear { get { return selectedYear; } }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }

    public ScheduleGenerator(List<Participant> participantList, PublicHolidayManager manager, int month, int year)
    {
        participants = participantList;
        holidayManager = manager;
        selectedMonth = month;
        selectedYear = year;
        Sessions = new List<Session>();
        Warnings = new List<string>();
        EligibleDates = new List<DateTime>();
        SkippedHolidays = new List<PublicHoliday>();
        PeriodHolidays = new List<PublicHoliday>();
    }

    // =====================================================================
    //  MAIN ENTRY POINT
    // =====================================================================
    public void GenerateSchedule()
    {
        Sessions.Clear();
        Warnings.Clear();
        hoursPerDay.Clear();

        BuildEligibleDates();

        // Go through the working dates in order, one day at a time.
        for (int i = 0; i < EligibleDates.Count; i++)
        {
            if (AllParticipantsFinished()) break;

            DateTime date = EligibleDates[i];

            // 1. Decide WHO works today (most urgent people first).
            List<Participant> todaysWorkers = SelectTodaysParticipants(date);
            if (todaysWorkers.Count == 0) continue;

            // 2. Wednesday and Thursday are the two main group session days.
            List<Participant> groupMembers = new List<Participant>();
            if (date.DayOfWeek == DayOfWeek.Wednesday || date.DayOfWeek == DayOfWeek.Thursday)
            {
                groupMembers = AssignGroupSessions(date, todaysWorkers);
            }

            // 3. Everyone else working today gets a one-on-one session.
            AssignOneOnOneSessions(date, todaysWorkers, groupMembers);
        }

        if (!AllParticipantsFinished())
        {
            Warnings.Add("Not every participant could complete " + participants[0].RequiredServiceDays +
                         " service days before the end of the following month (not enough eligible dates).");
        }

        ValidateSchedule();
    }

    // =====================================================================
    //  STEP 1: which dates can be used?
    // =====================================================================
    private void BuildEligibleDates()
    {
        EligibleDates.Clear();
        SkippedHolidays.Clear();
        PeriodHolidays.Clear();

        PeriodStart = new DateTime(selectedYear, selectedMonth, 1);
        DateTime nextMonthStart = PeriodStart.AddMonths(1);
        PeriodEnd = nextMonthStart.AddMonths(1).AddDays(-1);

        DateTime date = PeriodStart;
        while (date <= PeriodEnd)
        {
            bool isWeekend = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
            string holidayName = holidayManager.GetHolidayName(date);

            if (holidayName != "")
            {
                PublicHoliday holiday = new PublicHoliday();
                holiday.Date = date;
                holiday.Name = holidayName;
                PeriodHolidays.Add(holiday);
            }

            // Rule: nobody serves after the 17th of the SELECTED month.
            bool isAfterDeadline = (date.Month == selectedMonth && date.Day > LastServiceDayOfSelectedMonth);

            if (!isWeekend && !isAfterDeadline)
            {
                if (holidayName != "")
                {
                    PublicHoliday skipped = new PublicHoliday();
                    skipped.Date = date;
                    skipped.Name = holidayName;
                    SkippedHolidays.Add(skipped);
                }
                else
                {
                    EligibleDates.Add(date);
                }
            }

            date = date.AddDays(1);
        }
    }

    // =====================================================================
    //  STEP 2: choose who works on a date
    // =====================================================================
    private List<Participant> SelectTodaysParticipants(DateTime date)
    {
        List<Participant> candidates = new List<Participant>();
        for (int i = 0; i < participants.Count; i++)
        {
            Participant person = participants[i];
            if (person.RemainingDays > 0 && CheckParticipantAvailability(person, date))
            {
                candidates.Add(person);
            }
        }

        SortByPriority(candidates);

        // Keep only the first few (ParticipantsPerDay).
        List<Participant> chosen = new List<Participant>();
        for (int i = 0; i < candidates.Count && i < ParticipantsPerDay; i++)
        {
            chosen.Add(candidates[i]);
        }
        return chosen;
    }

    /// <summary>True when "a" should be scheduled before "b".</summary>
    private bool HasHigherPriority(Participant a, Participant b)
    {
        // Rule 1: people with restricted availability go first (they have fewer chances).
        if (a.HasRestrictedAvailability != b.HasRestrictedAvailability)
        {
            return a.HasRestrictedAvailability;
        }
        // Rule 2: the person with the most service days still to do goes first.
        if (a.RemainingDays != b.RemainingDays)
        {
            return a.RemainingDays > b.RemainingDays;
        }
        // Rule 3: otherwise keep the order of the master list (so results are repeatable).
        return a.ListOrder < b.ListOrder;
    }

    // Simple insertion sort (keeps the code free of lambdas).
    private void SortByPriority(List<Participant> list)
    {
        for (int i = 1; i < list.Count; i++)
        {
            Participant current = list[i];
            int j = i - 1;
            while (j >= 0 && HasHigherPriority(current, list[j]))
            {
                list[j + 1] = list[j];
                j--;
            }
            list[j + 1] = current;
        }
    }

    // =====================================================================
    //  STEP 3: group session
    // =====================================================================
    /// <summary>
    /// From today's workers, picks the people with the FEWEST group sessions so far.
    /// Returns the people placed in the group (empty list if a group is not possible).
    /// </summary>
    public List<Participant> AssignGroupSessions(DateTime date, List<Participant> todaysWorkers)
    {
        List<Participant> groupMembers = new List<Participant>();

        for (int round = 0; round < GroupSize; round++)
        {
            Participant best = null;
            for (int i = 0; i < todaysWorkers.Count; i++)
            {
                Participant candidate = todaysWorkers[i];
                if (groupMembers.Contains(candidate)) continue;

                if (best == null ||
                    candidate.GroupSessionCount < best.GroupSessionCount ||
                    (candidate.GroupSessionCount == best.GroupSessionCount && candidate.ListOrder < best.ListOrder))
                {
                    best = candidate;
                }
            }
            if (best != null) groupMembers.Add(best);
        }

        // A "group" of fewer than 2 people is just a one-on-one session.
        if (groupMembers.Count < 2)
        {
            return new List<Participant>();
        }

        Session session = new Session();
        session.Date = date;
        session.SessionType = SessionType.Group;
        session.Participants = groupMembers;

        // Wednesday group: 09:00-15:00.  Thursday group: 10:00-16:00.
        if (date.DayOfWeek == DayOfWeek.Wednesday)
        {
            session.StartTime = new TimeSpan(9, 0, 0);
        }
        else
        {
            session.StartTime = new TimeSpan(10, 0, 0);
        }
        session.EndTime = session.StartTime + TimeSpan.FromHours(SessionHours);

        AddSession(session);
        return groupMembers;
    }

    // =====================================================================
    //  STEP 4: one-on-one sessions
    // =====================================================================
    public void AssignOneOnOneSessions(DateTime date, List<Participant> todaysWorkers, List<Participant> groupMembers)
    {
        int oneOnOneNumber = 0;

        for (int i = 0; i < todaysWorkers.Count; i++)
        {
            Participant person = todaysWorkers[i];
            if (groupMembers.Contains(person)) continue;

            Session session = new Session();
            session.Date = date;
            session.SessionType = SessionType.OneOnOne;
            session.Participants.Add(person);

            // Alternate start times (09:00 / 10:00) so the day is spread across the campus hours.
            if (oneOnOneNumber % 2 == 0)
            {
                session.StartTime = new TimeSpan(9, 0, 0);
            }
            else
            {
                session.StartTime = new TimeSpan(10, 0, 0);
            }
            session.EndTime = session.StartTime + TimeSpan.FromHours(SessionHours);

            AddSession(session);
            oneOnOneNumber++;
        }
    }

    // =====================================================================
    //  Adding a session and updating each person's totals
    // =====================================================================
    private void AddSession(Session session)
    {
        // Safety checks before anyone is booked.
        for (int i = 0; i < session.Participants.Count; i++)
        {
            Participant person = session.Participants[i];
            if (!CheckParticipantAvailability(person, session.Date))
            {
                Warnings.Add(person.Name + " is not available on " + session.DateText + ". Session skipped.");
                return;
            }
            if (!CheckDailyHourLimit(person, session.Date, session.Duration))
            {
                Warnings.Add(person.Name + " would exceed " + MaxHoursPerDay + " hours on " + session.DateText + ". Session skipped.");
                return;
            }
        }

        for (int i = 0; i < session.Participants.Count; i++)
        {
            Participant person = session.Participants[i];
            person.CompletedServiceDays++;
            person.TotalWorkingHours += session.Duration;    // full duration, NOT divided
            if (session.SessionType == SessionType.Group)
            {
                person.GroupSessionCount++;
            }
            else
            {
                person.OneOnOneSessionCount++;
            }

            string key = person.Name + "|" + session.Date.ToString("yyyy-MM-dd");
            if (hoursPerDay.ContainsKey(key))
            {
                hoursPerDay[key] += session.Duration;
            }
            else
            {
                hoursPerDay[key] = session.Duration;
            }
        }

        Sessions.Add(session);
    }

    // =====================================================================
    //  Rule checks
    // =====================================================================
    public bool CheckParticipantAvailability(Participant person, DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) return false;
        if (holidayManager.IsPublicHoliday(date)) return false;
        if (date.Year == selectedYear && date.Month == selectedMonth && date.Day > LastServiceDayOfSelectedMonth) return false;
        return person.IsAvailableOn(date.DayOfWeek);
    }

    /// <summary>True if adding "hoursToAdd" keeps the person at or below 6 hours that day.</summary>
    public bool CheckDailyHourLimit(Participant person, DateTime date, double hoursToAdd)
    {
        string key = person.Name + "|" + date.ToString("yyyy-MM-dd");
        double alreadyWorked = 0;
        if (hoursPerDay.ContainsKey(key))
        {
            alreadyWorked = hoursPerDay[key];
        }
        return (alreadyWorked + hoursToAdd) <= MaxHoursPerDay;
    }

    private bool AllParticipantsFinished()
    {
        for (int i = 0; i < participants.Count; i++)
        {
            if (participants[i].RemainingDays > 0) return false;
        }
        return true;
    }

    // =====================================================================
    //  Fairness measurement
    // =====================================================================
    /// <summary>
    /// Fairness is measured as the gap between the highest and lowest participant.
    ///   Service days: target gap 0     Hours: target gap 0     Group sessions: target gap 1 or less
    /// </summary>
    public FairnessReport CheckFairness()
    {
        FairnessReport report = new FairnessReport();

        report.MaxDays = participants[0].CompletedServiceDays;
        report.MinDays = participants[0].CompletedServiceDays;
        report.MaxHours = participants[0].TotalWorkingHours;
        report.MinHours = participants[0].TotalWorkingHours;
        report.MaxGroupSessions = participants[0].GroupSessionCount;
        report.MinGroupSessions = participants[0].GroupSessionCount;
        report.MaxOneOnOneSessions = participants[0].OneOnOneSessionCount;
        report.MinOneOnOneSessions = participants[0].OneOnOneSessionCount;

        for (int i = 1; i < participants.Count; i++)
        {
            Participant p = participants[i];
            if (p.CompletedServiceDays > report.MaxDays) report.MaxDays = p.CompletedServiceDays;
            if (p.CompletedServiceDays < report.MinDays) report.MinDays = p.CompletedServiceDays;
            if (p.TotalWorkingHours > report.MaxHours) report.MaxHours = p.TotalWorkingHours;
            if (p.TotalWorkingHours < report.MinHours) report.MinHours = p.TotalWorkingHours;
            if (p.GroupSessionCount > report.MaxGroupSessions) report.MaxGroupSessions = p.GroupSessionCount;
            if (p.GroupSessionCount < report.MinGroupSessions) report.MinGroupSessions = p.GroupSessionCount;
            if (p.OneOnOneSessionCount > report.MaxOneOnOneSessions) report.MaxOneOnOneSessions = p.OneOnOneSessionCount;
            if (p.OneOnOneSessionCount < report.MinOneOnOneSessions) report.MinOneOnOneSessions = p.OneOnOneSessionCount;
        }

        report.DaysAreEqual = (report.DaysDifference == 0);
        report.HoursAreEqual = (report.HoursDifference == 0);
        report.GroupIsBalanced = (report.GroupDifference <= 1);

        bool everyoneComplete = true;
        for (int i = 0; i < participants.Count; i++)
        {
            if (participants[i].RemainingDays != 0) everyoneComplete = false;
        }

        if (!report.DaysAreEqual)
        {
            report.Reasons.Add("Service days differ by " + report.DaysDifference +
                " (highest " + report.MaxDays + ", lowest " + report.MinDays +
                "). The eligible dates ran out before everyone finished, or availability restrictions (for example Wednesday to Friday only) left too few usable days.");
        }
        if (!report.HoursAreEqual)
        {
            report.Reasons.Add("Working hours differ by " + report.HoursDifference.ToString("0.##") +
                " hours (highest " + report.MaxHours.ToString("0.##") + ", lowest " + report.MinHours.ToString("0.##") +
                "). This follows from the unequal service days above.");
        }
        if (!report.GroupIsBalanced)
        {
            report.Reasons.Add("Group sessions differ by " + report.GroupDifference +
                " (more than the allowed 1). Restricted availability and the number of group days limited how evenly places could be shared.");
        }
        if (!everyoneComplete)
        {
            report.Reasons.Add("At least one participant has not reached the required number of service days.");
        }

        report.AllFairnessRequirementsMet = (report.Reasons.Count == 0);
        return report;
    }

    // =====================================================================
    //  Final validation (section 14 of the specification)
    // =====================================================================
    private void ValidateSchedule()
    {
        // 1. All participants included
        if (participants.Count != ExpectedParticipantCount)
        {
            Warnings.Add("Expected " + ExpectedParticipantCount + " participants but found " + participants.Count + ".");
        }

        for (int p = 0; p < participants.Count; p++)
        {
            Participant person = participants[p];

            // Recalculate everything from the sessions, then compare with the running totals.
            int daysFromSessions = 0;
            double hoursFromSessions = 0;
            int groupFromSessions = 0;
            int oneFromSessions = 0;
            List<Session> personSessions = new List<Session>();

            for (int s = 0; s < Sessions.Count; s++)
            {
                if (Sessions[s].Includes(person))
                {
                    personSessions.Add(Sessions[s]);
                    daysFromSessions++;
                    hoursFromSessions += Sessions[s].Duration;
                    if (Sessions[s].SessionType == SessionType.Group) groupFromSessions++;
                    else oneFromSessions++;
                }
            }

            // 2. Required service days
            if (daysFromSessions != person.RequiredServiceDays)
            {
                Warnings.Add(person.Name + " has " + daysFromSessions + " service days instead of " + person.RequiredServiceDays + ".");
            }

            // 11. Totals must match the real schedule
            if (daysFromSessions != person.CompletedServiceDays || hoursFromSessions != person.TotalWorkingHours ||
                groupFromSessions != person.GroupSessionCount || oneFromSessions != person.OneOnOneSessionCount)
            {
                Warnings.Add("The summary totals for " + person.Name + " do not match the actual sessions.");
            }

            for (int a = 0; a < personSessions.Count; a++)
            {
                Session first = personSessions[a];

                // 5. Restricted days
                if (!person.IsAvailableOn(first.Date.DayOfWeek))
                {
                    Warnings.Add(person.Name + " is scheduled on " + first.DayName + " " + first.DateText + " (not available).");
                }
                // 6. Public holidays
                if (holidayManager.IsPublicHoliday(first.Date))
                {
                    Warnings.Add("A session is scheduled on public holiday " + first.DateText + ".");
                }
                // Deadline rule
                if (first.Date.Year == selectedYear && first.Date.Month == selectedMonth && first.Date.Day > LastServiceDayOfSelectedMonth)
                {
                    Warnings.Add(person.Name + " is scheduled after the " + LastServiceDayOfSelectedMonth + "th (" + first.DateText + ").");
                }
                // Campus hours
                if (first.StartTime < CampusOpen || first.EndTime > CampusClose)
                {
                    Warnings.Add("Session on " + first.DateText + " is outside campus hours.");
                }

                // Overlaps and daily hours: compare with every later session of this person.
                double hoursThatDay = first.Duration;
                for (int b = a + 1; b < personSessions.Count; b++)
                {
                    Session second = personSessions[b];
                    if (first.Date.Date == second.Date.Date)
                    {
                        hoursThatDay += second.Duration;
                        if (first.StartTime < second.EndTime && second.StartTime < first.EndTime)
                        {
                            Warnings.Add(person.Name + " has overlapping sessions on " + first.DateText + ".");
                        }
                    }
                }
                if (hoursThatDay > MaxHoursPerDay)
                {
                    Warnings.Add(person.Name + " works " + hoursThatDay + " hours on " + first.DateText + " (limit " + MaxHoursPerDay + ").");
                }
            }
        }
    }
}
