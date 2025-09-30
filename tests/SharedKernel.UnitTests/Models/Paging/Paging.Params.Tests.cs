using SharedKernel.Models.Paging;

using Shouldly;

namespace SharedKernel.UnitTests.Models.Paging;

public sealed class PagingParamsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultValues_AreNull()
    {
        PagingParams @params = new PagingParams();

        @params.PageSize.ShouldBeNull();
        @params.PageIndex.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithPageSize_SetsPageSize()
    {
        PagingParams @params = new PagingParams(PageSize: 10);

        @params.PageSize.ShouldBe(10);
        @params.PageIndex.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithPageIndex_SetsPageIndex()
    {
        PagingParams @params = new PagingParams(PageIndex: 5);

        @params.PageSize.ShouldBeNull();
        @params.PageIndex.ShouldBe(5);
    }

    [Fact]
    public void Constructor_WithBothValues_SetsBothValues()
    {
        PagingParams @params = new PagingParams(PageSize: 10, PageIndex: 5);

        @params.PageSize.ShouldBe(10);
        @params.PageIndex.ShouldBe(5);
    }

    [Fact]
    public void Constructor_WithNegativeValues_AllowsNegativeValues()
    {
        PagingParams @params = new PagingParams(PageSize: -1, PageIndex: -2);

        @params.PageSize.ShouldBe(-1);
        @params.PageIndex.ShouldBe(-2);
    }

    #endregion

    #region EffectivePageNumber Tests

    [Fact]
    public void EffectivePageNumber_NullPageIndex_ReturnsOne()
    {
        PagingParams @params = new PagingParams();

        @params.EffectivePageNumber().ShouldBe(1);
    }

    [Fact]
    public void EffectivePageNumber_ZeroPageIndex_ReturnsOne()
    {
        PagingParams @params = new PagingParams(PageIndex: 0);

        @params.EffectivePageNumber().ShouldBe(1);
    }

    [Fact]
    public void EffectivePageNumber_PositivePageIndex_ReturnsPageIndexPlusOne()
    {
        PagingParams @params = new PagingParams(PageIndex: 5);

        @params.EffectivePageNumber().ShouldBe(6);
    }

    [Fact]
    public void EffectivePageNumber_NegativePageIndex_ReturnsOne()
    {
        PagingParams @params = new PagingParams(PageIndex: -5);

        @params.EffectivePageNumber().ShouldBe(1);
    }

    #endregion

    #region EffectivePageIndex Tests

    [Fact]
    public void EffectivePageIndex_NullPageIndex_ReturnsZero()
    {
        PagingParams @params = new PagingParams();

        @params.EffectivePageIndex().ShouldBe(0);
    }

    [Fact]
    public void EffectivePageIndex_ZeroPageIndex_ReturnsZero()
    {
        PagingParams @params = new PagingParams(PageIndex: 0);

        @params.EffectivePageIndex().ShouldBe(0);
    }

    [Fact]
    public void EffectivePageIndex_PositivePageIndex_ReturnsPageIndex()
    {
        PagingParams @params = new PagingParams(PageIndex: 5);

        @params.EffectivePageIndex().ShouldBe(5);
    }

    [Fact]
    public void EffectivePageIndex_NegativePageIndex_ReturnsZero()
    {
        PagingParams @params = new PagingParams(PageIndex: -5);

        @params.EffectivePageIndex().ShouldBe(0);
    }

    #endregion

    #region HasPagingValues Tests

    [Fact]
    public void HasPagingValues_NullParams_ReturnsFalse()
    {
        PagingParams? @params = null;
        @params.HasPagingValues().ShouldBeFalse();
    }

    [Fact]
    public void HasPagingValues_EmptyParams_ReturnsFalse()
    {
        PagingParams @params = new PagingParams();
        @params.HasPagingValues().ShouldBeFalse();
    }

    [Fact]
    public void HasPagingValues_WithPageSize_ReturnsTrue()
    {
        PagingParams @params = new PagingParams(PageSize: 10);
        @params.HasPagingValues().ShouldBeTrue();
    }

    [Fact]
    public void HasPagingValues_WithPageIndex_ReturnsTrue()
    {
        PagingParams @params = new PagingParams(PageIndex: 0);
        @params.HasPagingValues().ShouldBeTrue();
    }

    [Fact]
    public void HasPagingValues_WithBothValues_ReturnsTrue()
    {
        PagingParams @params = new PagingParams(PageSize: 10, PageIndex: 5);
        @params.HasPagingValues().ShouldBeTrue();
    }

    [Fact]
    public void HasPagingValues_WithNegativePageSize_ReturnsTrue()
    {
        PagingParams @params = new PagingParams(PageSize: -1);
        @params.HasPagingValues().ShouldBeTrue();
    }

    [Fact]
    public void HasPagingValues_WithNegativePageIndex_ReturnsTrue()
    {
        PagingParams @params = new PagingParams(PageIndex: -1);
        @params.HasPagingValues().ShouldBeTrue();
    }

    #endregion
}