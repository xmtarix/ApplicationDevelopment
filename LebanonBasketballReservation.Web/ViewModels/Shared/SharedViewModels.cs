namespace LebanonBasketballReservation.Web.ViewModels.Shared;

/// <summary>Placeholder shown when a list has nothing in it, with an optional call to action.</summary>
public class EmptyStateModel
{
    public string Icon { get; set; } = "bi-inbox";
    public string Title { get; set; } = "Nothing here yet";
    public string? Message { get; set; }
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionIcon { get; set; }
}

/// <summary>Drives the shared pagination partial.</summary>
public class PagerModel
{
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }

    /// <summary>Extra query values carried onto every page link, so filters survive paging.</summary>
    public Dictionary<string, string?> RouteValues { get; set; } = new();

    public string PageParameterName { get; set; } = "page";

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>A window of page numbers around the current page, so long lists stay compact.</summary>
    public IEnumerable<int> VisiblePages(int window = 2)
    {
        var first = Math.Max(1, Page - window);
        var last = Math.Min(TotalPages, Page + window);
        for (var i = first; i <= last; i++) yield return i;
    }
}
