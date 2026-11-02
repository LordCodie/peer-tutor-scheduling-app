using System;
using System.Collections.Generic;

namespace FairScheduler;

/// <summary>
/// One person who can be scheduled. Also keeps running totals while the schedule is built.
/// </summary>
public class Participant
{
    public string Name { get; set; }

    // Position in the master list. Used as the final tie-breaker so results are always the same.
    public int ListOrder { get; set; }

    public int RequiredServiceDays { get; set; }
    public int CompletedServiceDays { get; set; }
    public double TotalWorkingHours { get; set; }
    public int GroupSessionCount { get; set; }
    public int OneOnOneSessionCount { get; set; }

    // The weekdays this person is allowed to work.
    public List<DayOfWeek> AvailableDays { get; set; }

    public int RemainingDays
    {
        get { return RequiredServiceDays - CompletedServiceDays; }
    }

    // True for people like Thulani and Katlego who cannot work every weekday.
    public bool HasRestrictedAvailability
    {
        get { return AvailableDays.Count < 5; }
    }

    public Participant(string name, int listOrder, List<DayOfWeek> availableDays)
    {
        Name = name;
        ListOrder = listOrder;
        RequiredServiceDays = 10;
        AvailableDays = availableDays;
    }

    public bool IsAvailableOn(DayOfWeek day)
    {
        return AvailableDays.Contains(day);
    }
}

/// <summary>
/// The stored participant list. To add or change people, edit this class only.
/// </summary>
public static class ParticipantList
{
    public static List<Participant> CreateDefaultParticipants()
    {
        List<DayOfWeek> everyWeekday = new List<DayOfWeek>();
        everyWeekday.Add(DayOfWeek.Monday);
        everyWeekday.Add(DayOfWeek.Tuesday);
        everyWeekday.Add(DayOfWeek.Wednesday);
        everyWeekday.Add(DayOfWeek.Thursday);
        everyWeekday.Add(DayOfWeek.Friday);

        // Thulani and Katlego: Wednesday, Thursday and Friday only
        List<DayOfWeek> midToEndOfWeek = new List<DayOfWeek>();
        midToEndOfWeek.Add(DayOfWeek.Wednesday);
        midToEndOfWeek.Add(DayOfWeek.Thursday);
        midToEndOfWeek.Add(DayOfWeek.Friday);

        string[] names = new string[]
        {
            "Mthandeni", "Garven", "Philani", "Thulani", "Katlego", "Junior", "Nokthula",
            "Mphumelelo", "Mbali", "Av", "Gomolemo", "Bafana", "Gomotsegang"
        };

        List<Participant> list = new List<Participant>();
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            if (name == "Thulani" || name == "Katlego")
            {
                list.Add(new Participant(name, i, new List<DayOfWeek>(midToEndOfWeek)));
            }
            else
            {
                list.Add(new Participant(name, i, new List<DayOfWeek>(everyWeekday)));
            }
        }
        return list;
    }
}
