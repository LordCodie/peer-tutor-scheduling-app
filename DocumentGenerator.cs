using System;
using System.Collections.Generic;
using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace FairScheduler;

/// <summary>Creates the monthly Word (.docx) document. No scheduling logic lives here.</summary>
public class DocumentGenerator
{
    private const string DarkBlue = "1F3864";
    private const string LightGrey = "F2F2F2";

    private string[] monthNames = new string[]
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    public void GenerateMonthlyDocument(ScheduleGenerator generator, FairnessReport report, string filePath)
    {
        using (WordprocessingDocument document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = new Body();
            mainPart.Document.Append(body);

            AddStyles(mainPart);

            AddTitlePage(body, generator);
            AddScheduleOverview(body, generator);          // Section A
            GenerateParticipantSections(body, generator);  // Section B
            GenerateFairnessSummary(body, generator, report); // Section C
            AddPublicHolidaySummary(body, generator);      // Section D

            mainPart.Document.Save();
        }
    }

    // ------------------------------------------------------------------
    // Title page
    // ------------------------------------------------------------------
    private void AddTitlePage(Body body, ScheduleGenerator generator)
    {
        string monthName = monthNames[generator.SelectedMonth - 1];

        for (int i = 0; i < 6; i++)
        {
            body.Append(CreateParagraph("", false, 22, false));
        }
        body.Append(CreateParagraph("Fair Participant Scheduling System", true, 52, true));
        body.Append(CreateParagraph("Monthly Schedule - " + monthName + " " + generator.SelectedYear, false, 36, true));
        body.Append(CreateParagraph("", false, 22, false));
        body.Append(CreateParagraph("Scheduling period: " + FormatDate(generator.PeriodStart) + " to " + FormatDate(LastScheduledDate(generator)), false, 24, true));
        body.Append(CreateParagraph("Generated on " + FormatDate(DateTime.Today), false, 22, true));
        body.Append(CreatePageBreak());
    }

    // ------------------------------------------------------------------
    // Section A
    // ------------------------------------------------------------------
    private void AddScheduleOverview(Body body, ScheduleGenerator generator)
    {
        int groupCount = 0;
        int oneOnOneCount = 0;
        for (int i = 0; i < generator.Sessions.Count; i++)
        {
            if (generator.Sessions[i].SessionType == SessionType.Group) groupCount++;
            else oneOnOneCount++;
        }

        body.Append(CreateHeading("Section A: Schedule Overview", 1));

        List<string[]> rows = new List<string[]>();
        rows.Add(new string[] { "Selected month and year", monthNames[generator.SelectedMonth - 1] + " " + generator.SelectedYear });
        rows.Add(new string[] { "Total scheduled sessions", generator.Sessions.Count.ToString() });
        rows.Add(new string[] { "Group sessions", groupCount.ToString() });
        rows.Add(new string[] { "One-on-one sessions", oneOnOneCount.ToString() });
        rows.Add(new string[] { "Public holidays skipped", SkippedHolidayText(generator) });
        rows.Add(new string[] { "Scheduling period", FormatDate(generator.PeriodStart) + " to " + FormatDate(LastScheduledDate(generator)) +
            " (no service after the " + ScheduleGenerator.LastServiceDayOfSelectedMonth + "th of the selected month; remaining days continue into the following month)" });
        body.Append(CreateTable(new string[] { "Item", "Detail" }, rows));

        body.Append(CreatePageBreak());
    }

    // ------------------------------------------------------------------
    // Section B
    // ------------------------------------------------------------------
    public void GenerateParticipantSections(Body body, ScheduleGenerator generator)
    {
        body.Append(CreateHeading("Section B: Individual Participant Schedules", 1));

        for (int p = 0; p < generator.Participants.Count; p++)
        {
            Participant person = generator.Participants[p];

            body.Append(CreateHeading("Participant Name: " + person.Name, 2));

            List<string[]> rows = new List<string[]>();
            for (int s = 0; s < generator.Sessions.Count; s++)
            {
                Session session = generator.Sessions[s];
                if (session.Includes(person))
                {
                    rows.Add(new string[] { session.DateText, session.DayName, session.TypeText, session.StartText, session.EndText, session.DurationText });
                }
            }
            body.Append(CreateTable(new string[] { "Date", "Day", "Session Type", "Start Time", "End Time", "Hours" }, rows));

            string status = (person.RemainingDays == 0) ? "Completed" : "NOT COMPLETE";
            body.Append(CreateParagraph("", false, 22, false));
            body.Append(CreateParagraph("Total service days: " + person.CompletedServiceDays, false, 22, false));
            body.Append(CreateParagraph("Total working hours: " + person.TotalWorkingHours.ToString("0.##", CultureInfo.InvariantCulture), false, 22, false));
            body.Append(CreateParagraph("Group sessions: " + person.GroupSessionCount, false, 22, false));
            body.Append(CreateParagraph("One-on-one sessions: " + person.OneOnOneSessionCount, false, 22, false));
            body.Append(CreateParagraph("Remaining service days: " + person.RemainingDays, false, 22, false));
            body.Append(CreateParagraph("Completion status: " + status, true, 22, false));

            // Page break between participants
            body.Append(CreatePageBreak());
        }
    }

    // ------------------------------------------------------------------
    // Section C
    // ------------------------------------------------------------------
    public void GenerateFairnessSummary(Body body, ScheduleGenerator generator, FairnessReport report)
    {
        body.Append(CreateHeading("Section C: Fairness Summary", 1));

        List<string[]> rows = new List<string[]>();
        for (int p = 0; p < generator.Participants.Count; p++)
        {
            Participant person = generator.Participants[p];
            double behindHighest = report.MaxHours - person.TotalWorkingHours;
            rows.Add(new string[]
            {
                person.Name,
                person.CompletedServiceDays.ToString(),
                person.TotalWorkingHours.ToString("0.##", CultureInfo.InvariantCulture),
                behindHighest.ToString("0.##", CultureInfo.InvariantCulture),
                person.GroupSessionCount.ToString(),
                person.OneOnOneSessionCount.ToString()
            });
        }
        body.Append(CreateTable(new string[] { "Participant", "Service Days", "Total Hours", "Hours Below Highest", "Group Sessions", "One-on-One Sessions" }, rows));

        body.Append(CreateParagraph("", false, 22, false));
        body.Append(CreateParagraph("Difference in service days (highest - lowest): " + report.DaysDifference, false, 22, false));
        body.Append(CreateParagraph("Difference in total hours (highest - lowest): " + report.HoursDifference.ToString("0.##", CultureInfo.InvariantCulture), false, 22, false));
        body.Append(CreateParagraph("Difference in group sessions (highest - lowest): " + report.GroupDifference + " (allowed: 1)", false, 22, false));

        if (report.AllFairnessRequirementsMet)
        {
            body.Append(CreateParagraph("Result: All fairness requirements were achieved.", true, 24, false));
        }
        else
        {
            body.Append(CreateParagraph("Result: Perfect fairness could NOT be achieved. Reasons:", true, 24, false));
            for (int i = 0; i < report.Reasons.Count; i++)
            {
                body.Append(CreateParagraph("- " + report.Reasons[i], false, 22, false));
            }
        }

        if (generator.Warnings.Count > 0)
        {
            body.Append(CreateParagraph("", false, 22, false));
            body.Append(CreateParagraph("Validation warnings:", true, 22, false));
            for (int i = 0; i < generator.Warnings.Count; i++)
            {
                body.Append(CreateParagraph("- " + generator.Warnings[i], false, 22, false));
            }
        }

        body.Append(CreatePageBreak());
    }

    // ------------------------------------------------------------------
    // Section D
    // ------------------------------------------------------------------
    private void AddPublicHolidaySummary(Body body, ScheduleGenerator generator)
    {
        body.Append(CreateHeading("Section D: Public Holiday Summary", 1));

        if (generator.PeriodHolidays.Count == 0)
        {
            body.Append(CreateParagraph("There are no South African public holidays in the scheduling period.", false, 22, false));
            return;
        }

        List<string[]> rows = new List<string[]>();
        for (int i = 0; i < generator.PeriodHolidays.Count; i++)
        {
            PublicHoliday holiday = generator.PeriodHolidays[i];
            string effect = "Skipped - no sessions";
            if (holiday.Date.DayOfWeek == DayOfWeek.Saturday || holiday.Date.DayOfWeek == DayOfWeek.Sunday)
            {
                effect = "Falls on a weekend - no effect";
            }
            rows.Add(new string[] { FormatDate(holiday.Date), holiday.Date.DayOfWeek.ToString(), holiday.Name, effect });
        }
        body.Append(CreateTable(new string[] { "Date", "Day", "Holiday", "Effect on schedule" }, rows));
    }

    // ==================================================================
    //  Small helper methods that build Word elements
    // ==================================================================
    private string FormatDate(DateTime date)
    {
        return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    private DateTime LastScheduledDate(ScheduleGenerator generator)
    {
        if (generator.Sessions.Count == 0) return generator.PeriodStart;
        return generator.Sessions[generator.Sessions.Count - 1].Date;
    }

    private string SkippedHolidayText(ScheduleGenerator generator)
    {
        if (generator.SkippedHolidays.Count == 0) return "None";
        string text = "";
        for (int i = 0; i < generator.SkippedHolidays.Count; i++)
        {
            if (i > 0) text = text + "; ";
            text = text + FormatDate(generator.SkippedHolidays[i].Date) + " " + generator.SkippedHolidays[i].Name;
        }
        return text;
    }

    // Heading styles (Heading1 and Heading2) so Word's navigation pane works.
    private void AddStyles(MainDocumentPart mainPart)
    {
        StyleDefinitionsPart stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        Styles styles = new Styles();
        styles.Append(CreateHeadingStyle("Heading1", "heading 1", 32, DarkBlue, 0));
        styles.Append(CreateHeadingStyle("Heading2", "heading 2", 26, "2E74B5", 1));
        stylesPart.Styles = styles;
    }

    private Style CreateHeadingStyle(string styleId, string styleName, int halfPointSize, string colour, int outlineLevel)
    {
        Style style = new Style();
        style.Type = StyleValues.Paragraph;
        style.StyleId = styleId;

        style.Append(new StyleName() { Val = styleName });
        style.Append(new UIPriority() { Val = 9 });
        style.Append(new PrimaryStyle());

        StyleParagraphProperties paragraphProperties = new StyleParagraphProperties();
        paragraphProperties.Append(new KeepNext());
        paragraphProperties.Append(new SpacingBetweenLines() { Before = "240", After = "120" });
        paragraphProperties.Append(new OutlineLevel() { Val = outlineLevel });
        style.Append(paragraphProperties);

        StyleRunProperties runProperties = new StyleRunProperties();
        runProperties.Append(new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" });
        runProperties.Append(new Bold());
        runProperties.Append(new Color() { Val = colour });
        runProperties.Append(new FontSize() { Val = halfPointSize.ToString() });
        style.Append(runProperties);

        return style;
    }

    private Paragraph CreateHeading(string text, int level)
    {
        Paragraph paragraph = new Paragraph();
        ParagraphProperties properties = new ParagraphProperties();
        properties.Append(new ParagraphStyleId() { Val = "Heading" + level });
        paragraph.Append(properties);
        paragraph.Append(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        return paragraph;
    }

    // fontSize is in half-points (22 = 11pt).
    private Paragraph CreateParagraph(string text, bool bold, int fontSize, bool centred)
    {
        Paragraph paragraph = new Paragraph();
        if (centred)
        {
            ParagraphProperties properties = new ParagraphProperties();
            properties.Append(new Justification() { Val = JustificationValues.Center });
            paragraph.Append(properties);
        }
        paragraph.Append(CreateRun(text, bold, fontSize, "000000"));
        return paragraph;
    }

    private Run CreateRun(string text, bool bold, int fontSize, string colour)
    {
        RunProperties runProperties = new RunProperties();
        runProperties.Append(new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" });
        if (bold) runProperties.Append(new Bold());
        runProperties.Append(new Color() { Val = colour });
        runProperties.Append(new FontSize() { Val = fontSize.ToString() });

        Run run = new Run();
        run.Append(runProperties);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return run;
    }

    private Paragraph CreatePageBreak()
    {
        return new Paragraph(new Run(new Break() { Type = BreakValues.Page }));
    }

    private Table CreateTable(string[] headers, List<string[]> rows)
    {
        Table table = new Table();

        TableProperties tableProperties = new TableProperties();
        TableBorders borders = new TableBorders();
        borders.Append(new TopBorder() { Val = BorderValues.Single, Size = 4, Color = "999999" });
        borders.Append(new LeftBorder() { Val = BorderValues.Single, Size = 4, Color = "999999" });
        borders.Append(new BottomBorder() { Val = BorderValues.Single, Size = 4, Color = "999999" });
        borders.Append(new RightBorder() { Val = BorderValues.Single, Size = 4, Color = "999999" });
        borders.Append(new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4, Color = "999999" });
        borders.Append(new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4, Color = "999999" });
        tableProperties.Append(borders);
        tableProperties.Append(new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct });
        table.Append(tableProperties);

        // Word expects a column grid, one entry per column.
        TableGrid grid = new TableGrid();
        for (int c = 0; c < headers.Length; c++)
        {
            grid.Append(new GridColumn());
        }
        table.Append(grid);

        // Header row (dark blue with white text)
        TableRow headerRow = new TableRow();
        for (int c = 0; c < headers.Length; c++)
        {
            headerRow.Append(CreateCell(headers[c], true, DarkBlue, "FFFFFF"));
        }
        table.Append(headerRow);

        // Data rows (alternating light grey)
        for (int r = 0; r < rows.Count; r++)
        {
            TableRow row = new TableRow();
            string fill = (r % 2 == 1) ? LightGrey : "FFFFFF";
            for (int c = 0; c < rows[r].Length; c++)
            {
                row.Append(CreateCell(rows[r][c], false, fill, "000000"));
            }
            table.Append(row);
        }

        return table;
    }

    private TableCell CreateCell(string text, bool bold, string fillColour, string textColour)
    {
        TableCell cell = new TableCell();

        TableCellProperties cellProperties = new TableCellProperties();
        cellProperties.Append(new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fillColour });
        cell.Append(cellProperties);

        Paragraph paragraph = new Paragraph();
        paragraph.Append(CreateRun(text, bold, 20, textColour));
        cell.Append(paragraph);

        return cell;
    }
}
