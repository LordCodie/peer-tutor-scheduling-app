Here is the complete prompt in Markdown (`.md`) format. You can copy it into a file named `Fair_Participant_Scheduling_System_Prompt.md`.

Writing

# Project: Fair Participant Scheduling System in C#

## 1\. Role and Objective

Act as an experienced C# developer and scheduling system designer.

I want you to develop a **Fair Participant Scheduling System** using C#.

The system must generate a monthly work schedule for 13 participants. The main objective is to distribute working hours and session opportunities fairly so that every participant gets an equal opportunity to earn.

The application must:

- Generate a fair monthly schedule.
- Distribute working hours equally among all participants.
- Allocate two main group sessions per week.
- Allocate individual one-on-one sessions to participants who are not assigned to the main group sessions.
- Respect individual availability restrictions.
- Exclude South African public holidays.
- Respect campus operating hours.
- Display the schedule in a readable terminal table.
- Generate a separate, professionally structured monthly document containing each participant's schedule and working hours.

Keep the code beginner-friendly and easy to debug.

## 2\. Participants

There are 13 participants:

1. Mthandeni
2. Garven
3. Philani
4. Thulani
5. Katlego
6. Junior
7. Nokthula
8. Mphumelelo
9. Mbali
10. Av
11. Gomolemo
12. Bafana
13. Gomotsegang

All participants must receive fair and equal scheduling opportunities.

## 3\. Working Hours and Service Requirements

Each participant has the following requirements:

- Every participant must complete 10 days of service.
- Each participant can work a maximum of 6 hours per day.
- No participant may work more than 6 hours in a single day.
- A participant can serve through either a group session or a one-on-one session.
- Group sessions count as working hours for every participant assigned to that group.

### Group Session Example

If four participants work together from 09:00 to 14:00:

- Each participant receives 5 hours of service.
- The group session counts as 5 hours for each individual.
- The hours must not be divided by the number of participants.

The application must calculate hours individually for each participant.

## 4\. Weekly Session Structure

There must be two main group sessions per week.

Each main group session:

- Lasts up to 6 hours.
- Takes place during campus operating hours.
- Can accommodate multiple participants.
- Counts the full session duration towards every assigned participant's service hours.

Participants who are not assigned to the main group sessions must be allocated one-on-one sessions.

### One-on-One Sessions

- Each one-on-one session can last up to 6 hours.
- These sessions must also take place during campus operating hours.
- They must be distributed fairly among participants.
- Participants must not be assigned overlapping sessions.
- Participants must not exceed their daily or overall service limits.

The scheduling system should aim to distribute main group session opportunities fairly as well.

## 5\. Campus Operating Hours

Campus operating hours are:

- Opening time: 09:00
- Closing time: 16:00
- Lunch: 12:00

Important rules:

- The campus operates from 09:00 to 16:00.
- Lunch is included in the working hours.
- Do not automatically deduct lunch from the session duration.
- The maximum daily service duration remains 6 hours.
- Sessions must not extend beyond 16:00.

Valid examples include:

- 09:00–15:00 (6 hours)
- 10:00–16:00 (6 hours)
- 09:00–14:00 (5 hours)

## 6\. Availability Restrictions

Two participants have specific availability restrictions:

- Thulani can only serve on Wednesdays, Thursdays and Fridays.
- Katlego can only serve on Wednesdays, Thursdays and Fridays.

The system must never schedule either participant on:

- Mondays
- Tuesdays
- Saturdays
- Sundays

All other participants can be scheduled on any eligible weekday, subject to the remaining scheduling rules.

## 7\. Scheduling Deadline and Monthly Scheduling Rules

There is a special scheduling deadline:

- No participant may serve beyond the 17th of the selected month.
- Participants may continue serving during the following month after the 17th.
- The system must therefore support schedules that extend into the next calendar month.

For example, if the selected month is January:

- Service can be scheduled during January up to and including January 17.
- Remaining service days can be scheduled from January 18 onwards in February.
- The system must not schedule anyone beyond January 17 within January itself.

Interpret the 17th as the final permitted service date in the selected month, not as a restriction preventing service in the following month.

The system must calculate how many service days each participant still needs and distribute the remaining days into the next month.

## 8\. South African Public Holidays

The system must consider official South African public holidays.

Requirements:

- Retrieve or maintain a reliable list of South African public holidays for the selected year.
- Skip public holidays completely.
- Do not schedule group sessions or one-on-one sessions on public holidays.
- Account for holidays that fall during the selected month and the following month.
- Correctly account for movable holidays such as Good Friday and Family Day.
- Avoid hardcoding holidays for only one year.

If an online holiday source is unavailable, implement a maintainable public holiday calculation or configuration method.

Clearly identify skipped public holidays in the generated schedule.

## 9\. Fairness and Scheduling Algorithm

Fairness is the most important requirement.

The scheduling algorithm must ensure that:

1. Every participant receives exactly 10 service days.
2. Every participant receives equal total working hours wherever mathematically possible.
3. Nobody exceeds 6 hours per day.
4. Nobody exceeds 10 service days.
5. Participants receive a fair distribution of group and individual sessions.
6. Thulani and Katlego only receive Wednesday, Thursday or Friday sessions.
7. No public holidays are used.
8. No overlapping sessions are assigned to the same participant.
9. All sessions fall within campus operating hours.
10. No participant is unfairly assigned more hours than another.

### Important Fairness Consideration

A service day and working hours are two different measurements.

The system must track both independently:

- Total service days per participant.
- Total working hours per participant.

For example, if one participant completes 6 hours on a particular day, that counts as one service day and 6 working hours.

If another participant completes 4 hours on a different day, that counts as one service day and 4 working hours.

The system should prioritize equal total hours and equal service days, and avoid unfair differences in the distribution of group sessions.

If perfect equality is impossible due to calendar restrictions, clearly report:

- The reason equality could not be achieved.
- Each participant's final service days.
- Each participant's final working hours.
- The difference between the highest and lowest totals.

Do not silently generate an unfair schedule.

## 10\. Monthly Input

When the application starts, prompt the user to enter:

- Month (for example, January).
- Year (for example, 2027).

The application must then:

1. Validate the entered month and year.
2. Identify all eligible working dates.
3. Identify South African public holidays.
4. Calculate available scheduling days.
5. Generate the schedule for the selected month.
6. Continue into the following month where necessary.
7. Ensure that every participant receives their required 10 service days.

The application should not require the participant list to be entered every time. Store the participant information in the program or a simple configuration file.

## 11\. Terminal Output

Display the generated schedule in a clean, readable table in the terminal.

The main schedule table should contain:

| Date | Day | Session Type | Start Time | End Time | Participants | Duration |
| --- | --- | --- | --- | --- | --- | --- |
| 04/01/2027 | Monday | Group | 09:00 | 15:00 | Mthandeni, Garven, Philani | 6 |
| 06/01/2027 | Wednesday | One-on-One | 09:00 | 15:00 | Thulani | 6 |
| 07/01/2027 | Thursday | Group | 10:00 | 16:00 | Katlego, Junior, Mbali | 6 |

Also generate a participant summary table:

| Participant | Service Days | Total Hours | Group Sessions | One-on-One Sessions | Remaining Days |
| --- | --- | --- | --- | --- | --- |

The terminal output must:

- Be properly aligned.
- Be easy to read.
- Clearly distinguish group and individual sessions.
- Show public holidays that were skipped.
- Show whether each participant has completed their 10 service days.

## 12\. Monthly Document Generation

Generate a separate document for the selected month.

The document must be professionally formatted and contain the following sections.

### Section A: Schedule Overview

- Selected month and year.
- Total number of scheduled sessions.
- Number of group sessions.
- Number of one-on-one sessions.
- Public holidays skipped.
- Scheduling period, including any dates in the following month.

### Section B: Individual Participant Schedules

Create a separate subsection for every participant.

Each subsection must contain:

**Participant Name:** Mthandeni

| Date | Day | Session Type | Start Time | End Time | Hours |
| --- | --- | --- | --- | --- | --- |

At the end of each participant's schedule, show:

- Total service days.
- Total working hours.
- Number of group sessions.
- Number of one-on-one sessions.
- Remaining service days.
- Completion status.

Repeat this structure for all 13 participants.

### Section C: Fairness Summary

Include a comparison of all participants showing:

- Total service days.
- Total hours.
- Difference in hours between participants.
- Whether the fairness requirements were achieved.

### Section D: Public Holiday Summary

List all South African public holidays that affected the selected scheduling period.

### Document Format

Preferably generate a Microsoft Word document (`.docx`).

Use a clean layout with:

- A title page or clear document heading.
- Proper headings.
- Tables.
- Consistent date and time formatting.
- Page breaks between participant sections where appropriate.

Save the document using a descriptive filename, for example:

`Monthly_Schedule_January_2027.docx`

If generating Word documents is not practical, provide a CSV or HTML alternative.

## 13\. C# Development Requirements

Use C# and keep the code beginner-friendly.

Important development guidelines:

- Avoid unnecessarily advanced C# syntax.
- Avoid complicated LINQ queries.
- Avoid overly complex lambda expressions.
- Avoid advanced design patterns.
- Use straightforward classes, methods, loops, lists and conditional statements.
- Use meaningful variable and method names.
- Add comments explaining important sections.
- Separate the scheduling logic from terminal output and document generation.
- Use simple exception handling and input validation.
- Make the code easy to debug and modify.

### Suggested Classes

**1\.****`Participant`**

- Name
- RequiredServiceDays
- CompletedServiceDays
- TotalWorkingHours
- GroupSessionCount
- OneOnOneSessionCount
- AvailableDays

**2\.****`Session`**

- Date
- StartTime
- EndTime
- SessionType
- Participants
- Duration

**3\.****`ScheduleGenerator`**

- GenerateSchedule()
- AssignGroupSessions()
- AssignOneOnOneSessions()
- CheckParticipantAvailability()
- CheckDailyHourLimit()
- CheckFairness()

**4\.****`PublicHolidayManager`**

- GetSouthAfricanPublicHolidays()
- IsPublicHoliday()

**5\.****`TerminalDisplay`**

- DisplayMonthlySchedule()
- DisplayParticipantSummary()
- DisplayPublicHolidays()

**6\.****`DocumentGenerator`**

- GenerateMonthlyDocument()
- GenerateParticipantSections()
- GenerateFairnessSummary()

You may add other simple classes if necessary.

## 14\. Validation and Error Handling

Before finalizing a schedule, validate that:

- All 13 participants are included.
- Every participant has 10 service days.
- No participant works more than 6 hours per day.
- No participant has overlapping sessions.
- Thulani and Katlego only work Wednesday, Thursday and Friday.
- No public holiday contains a scheduled session.
- No session starts before 09:00.
- No session ends after 16:00.
- No participant is scheduled on an unavailable date.
- All service hours are correctly calculated.
- The fairness summary matches the actual schedule.

If the scheduling requirements cannot all be satisfied, the application must display a clear warning and explain which constraints caused the problem.

## 15\. Expected Deliverables

I want you to provide:

1. A complete C# console application.
2. All necessary C# classes and methods.
3. A clear explanation of how the scheduling algorithm works.
4. A terminal table displaying the generated schedule.
5. A terminal summary showing each participant's hours and service days.
6. Automatic South African public holiday handling.
7. Automatic monthly Word document generation.
8. Input validation and error handling.
9. A sample generated schedule using January 2027.
10. Instructions on how to create, run, debug and modify the application in Visual Studio.

## 16\. Important Instructions Before Writing Code

Before implementing the solution:

- Analyze the scheduling requirements.
- Identify any conflicting or mathematically impossible requirements.
- Explain how fairness will be measured.
- Explain how the 10 service days will be distributed across the selected and following month.
- Explain how the two weekly group sessions and individual sessions will work together.

Do not simply generate a random timetable.

The scheduling algorithm must be deterministic and prioritize fairness.

Write the implementation in manageable sections, explain each section, and ensure that the final code can be copied into a C# Console Application and run with minimal modification.

