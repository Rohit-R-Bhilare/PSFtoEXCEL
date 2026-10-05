using ClosedXML.Excel;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();


// ===============================
// UPLOAD PSF FILE
// ===============================
app.MapPost("/api/psf/upload", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new
        {
            message = "Please select a PSF file."
        });
    }

    if (!Path.GetExtension(file.FileName)
        .Equals(".psf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            message = "Only .PSF files are allowed."
        });
    }

    var records = new List<Dictionary<string, string>>();

    using var reader = new StreamReader(file.OpenReadStream());

    Dictionary<string, string>? currentRecord = null;

    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();

        if (string.IsNullOrWhiteSpace(line))
            continue;

        line = line.Trim();

        // Example record separator
        if (line.Equals("Employee", StringComparison.OrdinalIgnoreCase))
        {
            if (currentRecord != null && currentRecord.Count > 0)
            {
                records.Add(currentRecord);
            }

            currentRecord = new Dictionary<string, string>();

            continue;
        }

        if (currentRecord == null)
        {
            currentRecord = new Dictionary<string, string>();
        }

        // Read KEY = VALUE
        var parts = line.Split('=', 2);

        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            currentRecord[key] = value;
        }
    }

    if (currentRecord != null && currentRecord.Count > 0)
    {
        records.Add(currentRecord);
    }

    return Results.Ok(new
    {
        fileName = file.FileName,
        totalRecords = records.Count,
        data = records
    });
})
.DisableAntiforgery();


// ===============================
// EXPORT EXCEL
// ===============================
app.MapPost("/api/psf/export",
    (List<Dictionary<string, string>> records) =>
    {
        if (records == null || records.Count == 0)
        {
            return Results.BadRequest("No data available.");
        }

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("PSF Data");

        // Get all columns
        var columns = records
            .SelectMany(x => x.Keys)
            .Distinct()
            .ToList();

        // Headers
        for (int i = 0; i < columns.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = columns[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        // Data
        for (int row = 0; row < records.Count; row++)
        {
            for (int col = 0; col < columns.Count; col++)
            {
                string column = columns[col];

                if (records[row].TryGetValue(column, out string? value))
                {
                    worksheet.Cell(row + 2, col + 1).Value = value;
                }
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return Results.File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "PSF_Data.xlsx"
        );
    })
.DisableAntiforgery();


app.Run();