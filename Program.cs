using System;
using System.Collections.Generic;
using System.IO;

namespace FairScheduler;

public class Program
{
    private static string[] monthNames = new string[]
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    public static void Main(string[] args)
    {
        Console.WriteLine("=== Fair Participant Scheduling System ===");
        Console.WriteLine();

        try
        {
            // 1. Ask for and validate the month and year
            int month = AskForMonth();
            int year = AskForYear();

            // 2. Participants are stored in the program (see Participant.cs)
            List<Participant> participants = ParticipantList.CreateDefaultParticipants();

            // 3. Build the schedule
            PublicHolidayManager holidayManager = new PublicHolidayManager();
            ScheduleGenerator generator = new ScheduleGenerator(participants, holidayManager, month, year);
            generator.GenerateSchedule();
            FairnessReport report = generator.CheckFairness();

            // 4. Show it in the terminal
            TerminalDisplay display = new TerminalDisplay();
            display.DisplayPublicHolidays(generator);
            display.DisplayMonthlySchedule(generator);
            display.DisplayParticipantSummary(generator);
            display.DisplayFairnessReport(report);
            display.DisplayWarnings(generator.Warnings);

            // 5. Create the Word document
            string fileName = "Monthly_Schedule_" + monthNames[month - 1] + "_" + year + ".docx";
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            DocumentGenerator documentGenerator = new DocumentGenerator();
            documentGenerator.GenerateMonthlyDocument(generator, report, fullPath);

            Console.WriteLine();
            Console.WriteLine("Word document saved to:");
            Console.WriteLine("  " + fullPath);
        }
        catch (IOException ex)
        {
            Console.WriteLine();
            Console.WriteLine("Could not save the document. If it is open in Word, close it and run again.");
            Console.WriteLine("Details: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Something went wrong: " + ex.Message);
        }

        Console.WriteLine();
        Console.WriteLine("Press Enter to close.");
        Console.ReadLine();
    }

    // Accepts "January", "jan" (at least 3 letters) or "1".
    private static int AskForMonth()
    {
        while (true)
        {
            Console.Write("Enter the month (for example January): ");
            string input = Console.ReadLine();
            if (input == null) input = "";
            input = input.Trim();

            int number;
            if (int.TryParse(input, out number))
            {
                if (number >= 1 && number <= 12) return number;
            }
            else if (input.Length >= 3)
            {
                for (int i = 0; i < monthNames.Length; i++)
                {
                    if (monthNames[i].StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    {
                        return i + 1;
                    }
                }
            }
            Console.WriteLine("  That is not a valid month. Please try again.");
        }
    }

    private static int AskForYear()
    {
        while (true)
        {
            Console.Write("Enter the year (for example 2027): ");
            string input = Console.ReadLine();
            int year;
            if (int.TryParse(input, out year) && year >= 2000 && year <= 2100)
            {
                return year;
            }
            Console.WriteLine("  Please enter a year between 2000 and 2100.");
        }
    }
}
