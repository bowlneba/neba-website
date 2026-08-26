using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.Pagination")]
public sealed class PaginationTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render nothing when there is only one page")]
    public void Render_ShouldRenderNothing_WhenOnlyOnePage()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 1, totalPages: 1);

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render nothing when there are no pages")]
    public void Render_ShouldRenderNothing_WhenZeroPages()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 1, totalPages: 0);

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render a page link for every page when multiple pages exist")]
    public void Render_ShouldRenderPageLinkForEveryPage_WhenMultiplePagesExist()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 1, totalPages: 3);

        // Assert
        cut.FindAll("a[aria-label^='Page ']").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should mark the current page link as active")]
    public void Render_ShouldMarkCurrentPage_AsActive()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 2, totalPages: 3);

        // Assert
        var activeLink = cut.Find("a.page-btn.active");
        activeLink.TextContent.Trim().ShouldBe("2");
        activeLink.GetAttribute("aria-current").ShouldBe("page");
    }

    [Fact(DisplayName = "Should not render a previous link on the first page")]
    public void Render_ShouldNotRenderPreviousLink_OnFirstPage()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 1, totalPages: 3);

        // Assert
        cut.FindAll("a[aria-label='Previous page']").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render a previous link when not on the first page")]
    public void Render_ShouldRenderPreviousLink_WhenNotOnFirstPage()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 2, totalPages: 3, baseHref: "/account/users");

        // Assert
        cut.Find("a[aria-label='Previous page']").GetAttribute("href").ShouldBe("/account/users?page=1");
    }

    [Fact(DisplayName = "Should not render a next link on the last page")]
    public void Render_ShouldNotRenderNextLink_OnLastPage()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 3, totalPages: 3);

        // Assert
        cut.FindAll("a[aria-label='Next page']").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render a next link when not on the last page")]
    public void Render_ShouldRenderNextLink_WhenNotOnLastPage()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 1, totalPages: 3, baseHref: "/news");

        // Assert
        cut.Find("a[aria-label='Next page']").GetAttribute("href").ShouldBe("/news?page=2");
    }

    [Fact(DisplayName = "Should build page links using the supplied BaseHref")]
    public void Render_ShouldBuildPageLinks_UsingSuppliedBaseHref()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 1, totalPages: 2, baseHref: "/news");

        // Assert
        cut.Find("a.page-btn[aria-label='Page 2']").GetAttribute("href").ShouldBe("/news?page=2");
    }

    [Fact(DisplayName = "Should use the supplied AriaLabel on the nav element")]
    public void Render_ShouldUseSuppliedAriaLabel_OnNavElement()
    {
        // Arrange & Act
        var cut = RenderPagination(pageNumber: 1, totalPages: 2, ariaLabel: "User pages");

        // Assert
        cut.Find("nav.pagination-nav").GetAttribute("aria-label").ShouldBe("User pages");
    }

    private IRenderedComponent<Pagination> RenderPagination(
        int pageNumber,
        int totalPages,
        string baseHref = "/news",
        string? ariaLabel = null)
        => _ctx.Render<Pagination>(parameters =>
        {
            parameters.Add(p => p.PageNumber, pageNumber);
            parameters.Add(p => p.TotalPages, totalPages);
            parameters.Add(p => p.BaseHref, baseHref);
            if (ariaLabel is not null)
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            }
        });
}