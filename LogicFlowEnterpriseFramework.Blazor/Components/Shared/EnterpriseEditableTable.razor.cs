using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LogicFlowEnterpriseFramework.Blazor.Components.Shared;

public partial class EnterpriseEditableTable
{
    private readonly List<TableColumn> Columns =
    [
        new(ColumnKeys.ApplicationNo, "Application No.", "150px", true),
        new(ColumnKeys.Company, "Company", "260px", true),
        new(ColumnKeys.ApplicationType, "Type", "220px", true),
        new(ColumnKeys.Officer, "Officer", "160px", true),
        new(ColumnKeys.Status, "Status", "130px", true),
        new(ColumnKeys.Priority, "Priority", "120px", true),
        new(ColumnKeys.SubmittedDate, "Submitted", "140px", true),
        new(ColumnKeys.Amount, "Amount", "130px", false),
        new(ColumnKeys.Active, "Active", "90px", false)
    ];

    private readonly List<string> StatusOptions = ["Draft", "Submitted", "In Review", "Recommended", "Rejected"];
    private readonly List<string> PriorityOptions = ["Low", "Normal", "High", "Critical"];
    private readonly Dictionary<string, string> ColumnFilters = [];
    private readonly HashSet<string> HiddenColumns = [];
    private readonly List<ApplicationRow> Rows = [];

    private string SearchText = string.Empty;
    private string StatusFilter = "All";
    private string SortColumn = ColumnKeys.SubmittedDate;
    private bool SortDescending = true;
    private int Page = 1;
    private int PageSize = 5;
    private int NextId = 100;
    private string? TableMessage;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private IEnumerable<TableColumn> VisibleColumns => Columns.Where(column => IsColumnVisible(column.Key));

    private IEnumerable<ApplicationRow> FilteredRows
    {
        get
        {
            IEnumerable<ApplicationRow> rows = Rows;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                rows = rows.Where(row =>
                    row.ApplicationNo.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    row.Company.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    row.ApplicationType.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    row.Officer.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (StatusFilter != "All")
            {
                rows = rows.Where(row => row.Status == StatusFilter);
            }

            foreach (var filter in ColumnFilters.Where(filter => !string.IsNullOrWhiteSpace(filter.Value)))
            {
                rows = rows.Where(row => GetColumnValue(row, filter.Key).Contains(filter.Value, StringComparison.OrdinalIgnoreCase));
            }

            return SortRows(rows).ToList();
        }
    }

    private IEnumerable<ApplicationRow> PagedRows => FilteredRows
        .Skip((Page - 1) * PageSize)
        .Take(PageSize);

    private IEnumerable<ApplicationRow> SelectedRows => Rows.Where(row => row.IsSelected);

    private int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredRows.Count() / (double)PageSize));
    private int TableColumnCount => VisibleColumns.Count() + 2;
    private int FirstVisibleRecord => FilteredRows.Any() ? ((Page - 1) * PageSize) + 1 : 0;
    private int LastVisibleRecord => Math.Min(Page * PageSize, FilteredRows.Count());
    private bool AreVisibleRowsSelected => PagedRows.Any() && PagedRows.All(row => row.IsSelected);

    protected override void OnInitialized()
    {
        ResetRows();
    }

    private void ResetRows()
    {
        Rows.Clear();
        Rows.AddRange(
        [
            new(1, "SPM1202600847", "BINTANG PLASTICS INDUSTRIES SDN. BHD.", "Confirmation Letter for Exemption (SPM)", "Sarah Admin", "Submitted", "High", new DateOnly(2026, 6, 16), 12800, true),
            new(2, "MCH1202600204", "AGRO TEKNIK HOLDINGS SDN. BHD.", "Machinery and Equipment", "Daniel Lee", "Draft", "Normal", new DateOnly(2026, 6, 15), 8400, true),
            new(3, "SEL1202600119", "NOVA SELECTED SERVICES SDN. BHD.", "Selected Services", "Priya Nair", "In Review", "Critical", new DateOnly(2026, 6, 14), 22500, true),
            new(4, "SPM1202600088", "MERANTI INDUSTRIAL PARTS SDN. BHD.", "Confirmation Letter for Exemption (SPM)", "Ahmad Rahman", "Recommended", "Low", new DateOnly(2026, 6, 12), 5600, false, true),
            new(5, "MCH1202600042", "KINABALU AGRO EQUIPMENT SDN. BHD.", "Machinery and Equipment", "Sarah Admin", "Rejected", "Normal", new DateOnly(2026, 6, 10), 9100, false),
            new(6, "SEL1202600036", "ORBIT BUSINESS SERVICES SDN. BHD.", "Selected Services", "Daniel Lee", "Submitted", "High", new DateOnly(2026, 6, 9), 17400, true)
        ]);
        NextId = 100;
    }

    private void OnSearchChanged(ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString() ?? string.Empty;
        Page = 1;
    }

    private void OnStatusFilterChanged(ChangeEventArgs args)
    {
        StatusFilter = args.Value?.ToString() ?? "All";
        Page = 1;
    }

    private void OnPageSizeChanged(ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var pageSize))
        {
            PageSize = pageSize;
            Page = 1;
        }
    }

    private void ToggleSort(string column)
    {
        if (SortColumn == column)
        {
            SortDescending = !SortDescending;
            return;
        }

        SortColumn = column;
        SortDescending = false;
    }

    private string GetSortGlyph(string column)
    {
        if (SortColumn != column)
        {
            return string.Empty;
        }

        return SortDescending ? "DESC" : "ASC";
    }

    private string GetHeaderClass(string column)
    {
        return SortColumn == column ? "is-sorted" : string.Empty;
    }

    private bool IsColumnVisible(string column)
    {
        return !HiddenColumns.Contains(column);
    }

    private void ToggleColumn(string column)
    {
        if (!HiddenColumns.Add(column))
        {
            HiddenColumns.Remove(column);
        }
    }

    private string GetColumnFilter(string column)
    {
        return ColumnFilters.TryGetValue(column, out var filter) ? filter : string.Empty;
    }

    private void SetColumnFilter(string column, string value)
    {
        ColumnFilters[column] = value;
        Page = 1;
    }

    private void ToggleRowSelection(ApplicationRow row)
    {
        row.IsSelected = !row.IsSelected;
    }

    private void ToggleAllVisible()
    {
        var shouldSelect = !AreVisibleRowsSelected;
        foreach (var row in PagedRows)
        {
            row.IsSelected = shouldSelect;
        }
    }

    private void EditRow(ApplicationRow row)
    {
        row.BeginEdit();
        TableMessage = null;
    }

    private void SaveRow(ApplicationRow row)
    {
        if (!ValidateRow(row))
        {
            TableMessage = "Resolve validation errors before saving.";
            return;
        }

        row.Commit();
        TableMessage = $"{row.ApplicationNo} saved.";
    }

    private void CancelEdit(ApplicationRow row)
    {
        row.Rollback();
        TableMessage = $"{row.ApplicationNo} changes discarded.";
    }

    private void AddRow()
    {
        var row = new ApplicationRow(
            NextId++,
            $"NEW{DateTimeOffset.Now:MMddHHmmss}",
            string.Empty,
            "Confirmation Letter for Exemption (SPM)",
            "Unassigned",
            "Draft",
            "Normal",
            DateOnly.FromDateTime(DateTime.Today),
            0,
            true);

        row.BeginEdit();
        row.IsDirty = true;
        Rows.Insert(0, row);
        Page = 1;
        TableMessage = "New editable row added.";
    }

    private void DuplicateRow(ApplicationRow source)
    {
        var row = source.Clone(NextId++, $"{source.ApplicationNo}-COPY");
        row.BeginEdit();
        row.IsDirty = true;
        Rows.Insert(Math.Max(0, Rows.IndexOf(source) + 1), row);
        TableMessage = $"{source.ApplicationNo} duplicated.";
    }

    private void DeleteRow(ApplicationRow row)
    {
        Rows.Remove(row);
        ClampPage();
        TableMessage = $"{row.ApplicationNo} deleted.";
    }

    private void DeleteSelected()
    {
        var selected = SelectedRows.Where(row => !row.IsLocked).ToList();
        foreach (var row in selected)
        {
            Rows.Remove(row);
        }

        ClampPage();
        TableMessage = $"{selected.Count} selected rows deleted.";
    }

    private void MarkSelectedReviewed()
    {
        foreach (var row in SelectedRows.Where(row => !row.IsLocked))
        {
            row.Status = "In Review";
            MarkDirty(row);
        }

        TableMessage = "Selected rows marked as reviewed.";
    }

    private void SaveAll()
    {
        var dirtyRows = Rows.Where(row => row.IsDirty).ToList();
        if (dirtyRows.Any(row => !ValidateRow(row)))
        {
            TableMessage = "Resolve validation errors before bulk save.";
            return;
        }

        foreach (var row in dirtyRows)
        {
            row.Commit();
        }

        TableMessage = $"{dirtyRows.Count} rows saved.";
    }

    private async Task ExportCsvAsync()
    {
        await DownloadCsvAsync("editable-applications-filtered.csv", FilteredRows);
        TableMessage = $"CSV prepared for {FilteredRows.Count()} filtered rows.";
    }

    private async Task ExportSelectedAsync()
    {
        await DownloadCsvAsync("editable-applications-selected.csv", SelectedRows);
        TableMessage = $"CSV prepared for {SelectedRows.Count()} selected rows.";
    }

    private async Task DownloadCsvAsync(string fileName, IEnumerable<ApplicationRow> rows)
    {
        var csv = BuildCsv(rows);
        await JS.InvokeVoidAsync("logicFlowDownloads.downloadText", fileName, csv, "text/csv;charset=utf-8");
    }

    private static string BuildCsv(IEnumerable<ApplicationRow> rows)
    {
        var lines = new List<string>
        {
            "Application No,Company,Application Type,Officer,Status,Priority,Submitted Date,Amount,Active"
        };

        lines.AddRange(rows.Select(row => string.Join(",",
            Csv(row.ApplicationNo),
            Csv(row.Company),
            Csv(row.ApplicationType),
            Csv(row.Officer),
            Csv(row.Status),
            Csv(row.Priority),
            Csv(row.SubmittedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            Csv(row.Amount.ToString(CultureInfo.InvariantCulture)),
            Csv(row.IsActive ? "Yes" : "No"))));

        return string.Join(Environment.NewLine, lines);
    }

    private static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private void ResetTable()
    {
        SearchText = string.Empty;
        StatusFilter = "All";
        ColumnFilters.Clear();
        HiddenColumns.Clear();
        SortColumn = ColumnKeys.SubmittedDate;
        SortDescending = true;
        Page = 1;
        ResetRows();
        TableMessage = "Table reset.";
    }

    private void PreviousPage()
    {
        Page = Math.Max(1, Page - 1);
    }

    private void NextPage()
    {
        Page = Math.Min(TotalPages, Page + 1);
    }

    private void ClampPage()
    {
        Page = Math.Min(Page, TotalPages);
    }

    private void UpdateText(ApplicationRow row, string column, string value)
    {
        switch (column)
        {
            case ColumnKeys.Company:
                row.Company = value;
                break;
            case ColumnKeys.ApplicationType:
                row.ApplicationType = value;
                break;
            case ColumnKeys.Officer:
                row.Officer = value;
                break;
            case ColumnKeys.Status:
                row.Status = value;
                break;
            case ColumnKeys.Priority:
                row.Priority = value;
                break;
        }

        MarkDirty(row);
    }

    private void UpdateDate(ApplicationRow row, string? value)
    {
        if (DateOnly.TryParse(value, out var submittedDate))
        {
            row.SubmittedDate = submittedDate;
            MarkDirty(row);
        }
    }

    private void UpdateAmount(ApplicationRow row, string? value)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            row.Amount = amount;
            MarkDirty(row);
        }
    }

    private void ToggleActive(ApplicationRow row)
    {
        row.IsActive = !row.IsActive;
        MarkDirty(row);
    }

    private void MarkDirty(ApplicationRow row)
    {
        row.IsDirty = true;
        ValidateRow(row);
    }

    private static bool ValidateRow(ApplicationRow row)
    {
        row.Errors.Clear();

        if (string.IsNullOrWhiteSpace(row.Company))
        {
            row.Errors[ColumnKeys.Company] = "Company is required.";
        }

        if (string.IsNullOrWhiteSpace(row.Officer))
        {
            row.Errors[ColumnKeys.Officer] = "Officer is required.";
        }

        if (row.Amount.CompareTo(decimal.Zero) < 0)
        {
            row.Errors[ColumnKeys.Amount] = "Amount cannot be negative.";
        }

        return row.Errors.Count == 0;
    }

    private static string GetEditorClass(ApplicationRow row, string column)
    {
        return row.Errors.ContainsKey(column) ? "enterprise-cell-error" : string.Empty;
    }

    private static RenderFragment RenderError(ApplicationRow row, string column) => builder =>
    {
        if (!row.Errors.TryGetValue(column, out var error))
        {
            return;
        }

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "enterprise-validation-message");
        builder.AddContent(2, error);
        builder.CloseElement();
    };

    private static string GetRowClass(ApplicationRow row)
    {
        var classes = new List<string>();
        if (row.IsDirty)
        {
            classes.Add("is-dirty");
        }

        if (row.IsLocked)
        {
            classes.Add("is-locked");
        }

        if (row.Errors.Any())
        {
            classes.Add("has-errors");
        }

        return string.Join(" ", classes);
    }

    private static string GetStatusClass(string status)
    {
        return status switch
        {
            "Recommended" => "table-badge table-badge--success",
            "Submitted" or "In Review" => "table-badge table-badge--info",
            _ => "table-badge table-badge--warning"
        };
    }

    private static string GetPriorityClass(string priority)
    {
        return priority switch
        {
            "Critical" => "enterprise-priority enterprise-priority--critical",
            "High" => "enterprise-priority enterprise-priority--high",
            "Low" => "enterprise-priority enterprise-priority--low",
            _ => "enterprise-priority"
        };
    }

    private static string GetColumnValue(ApplicationRow row, string column)
    {
        return column switch
        {
            ColumnKeys.ApplicationNo => row.ApplicationNo,
            ColumnKeys.Company => row.Company,
            ColumnKeys.ApplicationType => row.ApplicationType,
            ColumnKeys.Officer => row.Officer,
            ColumnKeys.Status => row.Status,
            ColumnKeys.Priority => row.Priority,
            ColumnKeys.SubmittedDate => row.SubmittedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ColumnKeys.Amount => row.Amount.ToString(CultureInfo.InvariantCulture),
            ColumnKeys.Active => row.IsActive ? "Active" : "Inactive",
            _ => string.Empty
        };
    }

    private IEnumerable<ApplicationRow> SortRows(IEnumerable<ApplicationRow> rows)
    {
        return SortColumn switch
        {
            ColumnKeys.SubmittedDate => SortDescending ? rows.OrderByDescending(row => row.SubmittedDate) : rows.OrderBy(row => row.SubmittedDate),
            ColumnKeys.Amount => SortDescending ? rows.OrderByDescending(row => row.Amount) : rows.OrderBy(row => row.Amount),
            ColumnKeys.Active => SortDescending ? rows.OrderByDescending(row => row.IsActive) : rows.OrderBy(row => row.IsActive),
            ColumnKeys.Company => SortDescending ? rows.OrderByDescending(row => row.Company) : rows.OrderBy(row => row.Company),
            ColumnKeys.ApplicationType => SortDescending ? rows.OrderByDescending(row => row.ApplicationType) : rows.OrderBy(row => row.ApplicationType),
            ColumnKeys.Officer => SortDescending ? rows.OrderByDescending(row => row.Officer) : rows.OrderBy(row => row.Officer),
            ColumnKeys.Status => SortDescending ? rows.OrderByDescending(row => row.Status) : rows.OrderBy(row => row.Status),
            ColumnKeys.Priority => SortDescending ? rows.OrderByDescending(row => row.Priority) : rows.OrderBy(row => row.Priority),
            _ => SortDescending ? rows.OrderByDescending(row => row.ApplicationNo) : rows.OrderBy(row => row.ApplicationNo)
        };
    }

    private sealed record TableColumn(string Key, string Label, string Width, bool Filterable);

    private static class ColumnKeys
    {
        public const string ApplicationNo = "applicationNo";
        public const string Company = "company";
        public const string ApplicationType = "applicationType";
        public const string Officer = "officer";
        public const string Status = "status";
        public const string Priority = "priority";
        public const string SubmittedDate = "submittedDate";
        public const string Amount = "amount";
        public const string Active = "active";
    }

    private sealed class ApplicationRow
    {
        private ApplicationRowSnapshot? snapshot;

        public ApplicationRow(int id, string applicationNo, string company, string applicationType, string officer, string status, string priority, DateOnly submittedDate, decimal amount, bool isActive, bool isLocked = false)
        {
            Id = id;
            ApplicationNo = applicationNo;
            Company = company;
            ApplicationType = applicationType;
            Officer = officer;
            Status = status;
            Priority = priority;
            SubmittedDate = submittedDate;
            Amount = amount;
            IsActive = isActive;
            IsLocked = isLocked;
        }

        public int Id { get; }
        public string ApplicationNo { get; set; }
        public string Company { get; set; }
        public string ApplicationType { get; set; }
        public string Officer { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateOnly SubmittedDate { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; }
        public bool IsEditing { get; private set; }
        public bool IsDirty { get; set; }
        public bool IsSelected { get; set; }
        public Dictionary<string, string> Errors { get; } = [];

        public void BeginEdit()
        {
            snapshot = Capture();
            IsEditing = true;
        }

        public void Commit()
        {
            snapshot = Capture();
            IsEditing = false;
            IsDirty = false;
            Errors.Clear();
        }

        public void Rollback()
        {
            if (snapshot is not null)
            {
                ApplicationNo = snapshot.ApplicationNo;
                Company = snapshot.Company;
                ApplicationType = snapshot.ApplicationType;
                Officer = snapshot.Officer;
                Status = snapshot.Status;
                Priority = snapshot.Priority;
                SubmittedDate = snapshot.SubmittedDate;
                Amount = snapshot.Amount;
                IsActive = snapshot.IsActive;
            }

            IsEditing = false;
            IsDirty = false;
            Errors.Clear();
        }

        public ApplicationRow Clone(int id, string applicationNo)
        {
            return new ApplicationRow(id, applicationNo, Company, ApplicationType, Officer, "Draft", Priority, DateOnly.FromDateTime(DateTime.Today), Amount, IsActive);
        }

        private ApplicationRowSnapshot Capture()
        {
            return new ApplicationRowSnapshot(ApplicationNo, Company, ApplicationType, Officer, Status, Priority, SubmittedDate, Amount, IsActive);
        }
    }

    private sealed record ApplicationRowSnapshot(string ApplicationNo, string Company, string ApplicationType, string Officer, string Status, string Priority, DateOnly SubmittedDate, decimal Amount, bool IsActive);
}
