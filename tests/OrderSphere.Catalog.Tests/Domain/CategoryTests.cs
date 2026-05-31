using OrderSphere.Catalog.Domain.DomainEvents;

namespace OrderSphere.Catalog.Tests.Domain;

public sealed class CategoryTests
{
    // ── Construction ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsNameAndDescription()
    {
        var category = new Category("Electronics", "All electronics");

        category.Name.Should().Be("Electronics");
        category.Description.Should().Be("All electronics");
    }

    [Fact]
    public void Constructor_IsActiveByDefault()
    {
        var category = new Category("Books");

        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_IsNotDeleted()
    {
        var category = new Category("Books");

        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_EmptyDescription_IsAllowed()
    {
        var category = new Category("Books");

        category.Description.Should().BeEmpty();
    }

    // ── Activate / Deactivate ───────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var category = new Category("Books");

        category.Deactivate();

        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveTrue()
    {
        var category = new Category("Books");
        category.Deactivate();

        category.Activate();

        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_AlreadyActive_RemainsActive()
    {
        var category = new Category("Books");

        category.Activate(); // already active

        category.IsActive.Should().BeTrue();
    }

    // ── UpdateDetails ───────────────────────────────────────────────────────────

    [Fact]
    public void UpdateDetails_ChangesNameAndDescription()
    {
        var category = new Category("OldName", "Old Desc");

        category.UpdateDetails("NewName", "New Desc");

        category.Name.Should().Be("NewName");
        category.Description.Should().Be("New Desc");
    }

    [Fact]
    public void UpdateDetails_EmptyDescription_IsAllowed()
    {
        var category = new Category("Name", "Desc");

        category.UpdateDetails("Name", "");

        category.Description.Should().BeEmpty();
    }

    // ── Delete ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_SetsIsDeletedTrue()
    {
        var category = new Category("Books");

        category.Delete();

        category.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_RaisesCategoryDeletedDomainEvent()
    {
        var category = new Category("Books");

        category.Delete();

        var events = category.PopDomainEvents();
        events.Should().ContainSingle()
            .Which.Should().BeOfType<CategoryDeletedDomainEvent>()
            .Which.CategoryId.Should().Be(category.Id);
    }
}
