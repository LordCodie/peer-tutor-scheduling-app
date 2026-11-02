using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FairScheduler;

public class PublicHoliday
{
    public DateTime Date { get; set; }
    public string Name { get; set; }
}

/// <summary>
/// Works out South African public holidays for any year. Nothing is hardcoded to one year:
///  - Fixed-date holidays are listed once and applied to the requested year.
///  - Good Friday and Family Day come from the Easter calculation.
///  - If a holiday falls on a Sunday, the next free day is also a holiday (Public Holidays Act).
///  - Ad-hoc holidays (e.g. election days) can be added in "extra_holidays.txt", one per line:
///        2029-05-30,National Election Day
/// </summary>
public class PublicHolidayManager
{
    private Dictionary<int, List<PublicHoliday>> holidayCache = new Dictionary<int, List<PublicHoliday>>();
    private List<PublicHoliday> extraHolidays = new List<PublicHoliday>();

    public PublicHolidayManager()
    {
        LoadExtraHolidays("extra_holidays.txt");
    }

    /// <summary>Returns all public holidays for one year, sorted by date.</summary>
    public List<PublicHoliday> GetSouthAfricanPublicHolidays(int year)
    {
        if (holidayCache.ContainsKey(year))
        {
            return holidayCache[year];
        }

        List<PublicHoliday> holidays = new List<PublicHoliday>();

        // Fixed-date holidays
        AddHoliday(holidays, year, 1, 1, "New Year's Day");
        AddHoliday(holidays, year, 3, 21, "Human Rights Day");
        AddHoliday(holidays, year, 4, 27, "Freedom Day");
        AddHoliday(holidays, year, 5, 1, "Workers' Day");
        AddHoliday(holidays, year, 6, 16, "Youth Day");
        AddHoliday(holidays, year, 8, 9, "National Women's Day");
        AddHoliday(holidays, year, 9, 24, "Heritage Day");
        AddHoliday(holidays, year, 12, 16, "Day of Reconciliation");
        AddHoliday(holidays, year, 12, 25, "Christmas Day");
        AddHoliday(holidays, year, 12, 26, "Day of Goodwill");

        // Moving holidays based on Easter
        DateTime easterSunday = CalculateEasterSunday(year);
        holidays.Add(MakeHoliday(easterSunday.AddDays(-2), "Good Friday"));
        holidays.Add(MakeHoliday(easterSunday.AddDays(1), "Family Day"));

        // Sunday rule: if a holiday is on a Sunday, the next day that is not already a holiday is added.
        List<PublicHoliday> observedDays = new List<PublicHoliday>();
        for (int i = 0; i < holidays.Count; i++)
        {
            if (holidays[i].Date.DayOfWeek == DayOfWeek.Sunday)
            {
                DateTime observed = holidays[i].Date.AddDays(1);
                while (ContainsDate(holidays, observed) || ContainsDate(observedDays, observed))
                {
                    observed = observed.AddDays(1);
                }
                observedDays.Add(MakeHoliday(observed, holidays[i].Name + " (observed)"));
            }
        }
        holidays.AddRange(observedDays);

        // Ad-hoc holidays from the optional file
        for (int i = 0; i < extraHolidays.Count; i++)
        {
            if (extraHolidays[i].Date.Year == year)
            {
                holidays.Add(extraHolidays[i]);
            }
        }

        SortByDate(holidays);
        holidayCache[year] = holidays;
        return holidays;
    }

    public bool IsPublicHoliday(DateTime date)
    {
        return GetHolidayName(date) != "";
    }

    /// <summary>Returns the holiday name, or an empty string if the date is not a holiday.</summary>
    public string GetHolidayName(DateTime date)
    {
        List<PublicHoliday> holidays = GetSouthAfricanPublicHolidays(date.Year);
        for (int i = 0; i < holidays.Count; i++)
        {
            if (holidays[i].Date.Date == date.Date)
            {
                return holidays[i].Name;
            }
        }
        return "";
    }

    /// <summary>Standard "Anonymous Gregorian" algorithm for the date of Easter Sunday.</summary>
    public DateTime CalculateEasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateTime(year, month, day);
    }

    // ---------- small helper methods ----------

    private void AddHoliday(List<PublicHoliday> list, int year, int month, int day, string name)
    {
        list.Add(MakeHoliday(new DateTime(year, month, day), name));
    }

    private PublicHoliday MakeHoliday(DateTime date, string name)
    {
        PublicHoliday holiday = new PublicHoliday();
        holiday.Date = date;
        holiday.Name = name;
        return holiday;
    }

    private bool ContainsDate(List<PublicHoliday> list, DateTime date)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Date.Date == date.Date) return true;
        }
        return false;
    }

    private void SortByDate(List<PublicHoliday> list)
    {
        for (int i = 1; i < list.Count; i++)
        {
            PublicHoliday current = list[i];
            int j = i - 1;
            while (j >= 0 && list[j].Date > current.Date)
            {
                list[j + 1] = list[j];
                j--;
            }
            list[j + 1] = current;
        }
    }

    private void LoadExtraHolidays(string fileName)
    {
        try
        {
            if (!File.Exists(fileName)) return;

            string[] lines = File.ReadAllLines(fileName);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == "" || line.StartsWith("#")) continue;

                string[] parts = line.Split(',');
                DateTime date;
                if (DateTime.TryParseExact(parts[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out date))
                {
                    string name = parts.Length > 1 ? parts[1].Trim() : "Special public holiday";
                    extraHolidays.Add(MakeHoliday(date, name));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Warning: could not read " + fileName + ": " + ex.Message);
        }
    }
}
