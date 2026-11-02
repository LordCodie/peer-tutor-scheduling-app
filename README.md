# Fair Participant Scheduling System (C# / .NET 8)

## Files
| File | Purpose |
|---|---|
| `Participant.cs` | `Participant` class and the stored list of 13 people (edit names/availability here) |
| `Session.cs` | `Session` class and `SessionType` (Group / One-on-One) |
| `PublicHolidayManager.cs` | South African holidays for any year (Easter calculation, Sunday rule, optional `extra_holidays.txt`) |
| `ScheduleGenerator.cs` | All scheduling logic, fairness measurement and validation |
| `TerminalDisplay.cs` | Console tables only |
| `DocumentGenerator.cs` | Word `.docx` creation only |
| `Program.cs` | Asks for month/year and runs everything |
| `Sample_Output_January_2027.txt` | Sample terminal output |

## Assumptions (please check)
1. **Cut-off:** January 1-17 plus the whole of February are usable; January 18-31 are never used.
2. **Capacity:** 7 participants work per day (`ParticipantsPerDay`), group size is 5 (`GroupSize`). Both are constants at the top of `ScheduleGenerator`.
3. **Session length:** every session is exactly 6 hours (09:00-15:00 or 10:00-16:00). Lunch is not deducted. This guarantees everyone ends on exactly 10 days = 60 hours.
4. **One session per person per day** (a second would break the 6-hour cap).
5. **Group days:** Wednesday (09:00-15:00) and Thursday (10:00-16:00), so Thulani and Katlego can take part.
6. **One-on-ones** run at the same time as each other, alternating 09:00 and 10:00 starts.

## How the algorithm works
For each eligible date in order (weekday, not a holiday, not after the 17th of the selected month):
1. **Who works?** List everyone who still needs days and is available that weekday. Rank them: restricted-availability people first (Thulani, Katlego), then the most remaining days, then list order. Take the first 7.
2. **Group session?** On Wednesday and Thursday, from those 7 pick the 5 with the fewest group sessions so far (tie: list order).
3. **One-on-ones:** the rest each get a 6-hour one-on-one.
4. Update totals, stop when everyone has 10 days.
5. Validate everything (section 14 of your spec) and print any warnings.

No randomness is used, so the same month and year always give the same schedule.

**Fairness** = gap between highest and lowest participant. Days and hours target 0; group sessions target 1 or less. If a target is missed, the report explains why.

## Visual Studio steps
1. File > New > Project > **Console App** (C#, .NET 8) > name it `FairScheduler`. Untick "Do not use top-level statements" if shown, or just replace `Program.cs`.
2. Delete the generated `Program.cs` content and add/paste each `.cs` file from this folder (Project > Add New Item > Class, or drag the files in).
3. Install the package: Tools > NuGet Package Manager > Package Manager Console:
   `Install-Package DocumentFormat.OpenXml -Version 3.0.2`
   (or just copy `FairScheduler.csproj` over yours).
4. Press **F5** (or Ctrl+F5). Enter `January` and `2027`.
5. The Word file is saved in the output folder (`bin\Debug\net8.0\`), named `Monthly_Schedule_January_2027.docx`.

**Debugging:** click in the margin to set a breakpoint, for example inside `SelectTodaysParticipants` or `AssignGroupSessions`, and hover over variables. Any rule violation appears under "VALIDATION WARNINGS".

**Modifying:** change `ParticipantsPerDay`, `GroupSize`, the cut-off day or the session hours at the top of `ScheduleGenerator.cs`. Change people or availability in `Participant.cs`. For one-off holidays (e.g. election days) create `extra_holidays.txt` next to the program with lines like `2029-05-30,National Election Day`.

## Important
The sample output was produced by a Python re-implementation of the same algorithm, because the .NET SDK was not available where I wrote the code. The C# has not been compiled yet. If Visual Studio reports an error on first build, send me the message and I will fix it.
