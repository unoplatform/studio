using Xunit;

namespace Uno.Lint.Tests;

/// <summary>Locating an edit's new text in the saved file, for harnesses that send no line numbers.</summary>
public class EditRangeTests
{
    [Fact]
    public void Reports_nothing_when_the_new_text_is_ambiguous()
    {
        // new_string "/>" occurs on several lines: any line would be a guess.
        var full = "<StackPanel>\n<Border Background=\"#111111\"/>\n<TextBlock Text=\"hi\"/>\n</StackPanel>";
        Assert.False(EditRange.TryLocate(full, " Foo=\"1\"/>", "/>", replaceAll: false, out _));
    }

    [Fact]
    public void Skips_unchanged_context_lines_carried_in_the_new_text()
    {
        var full = "<Grid>\n<Border Background=\"#111111\"/>\n<Border Background=\"#222222\"/>\n</Grid>";
        Assert.True(EditRange.TryLocate(full,
            "<Border Background=\"#111111\"/>\n<TextBlock/>",
            "<Border Background=\"#111111\"/>\n<Border Background=\"#222222\"/>",
            replaceAll: false, out var lines));
        Assert.Equal([3], lines);
    }

    [Fact]
    public void Locates_added_text_across_line_ending_and_indent_differences()
    {
        var full = "line1\r\n    <Border\r\n        CornerRadius=\"8\"/>\r\nline4\r\n";

        Assert.True(EditRange.TryLocate(full, "", "    <Border\n        CornerRadius=\"8\"/>", false, out var lines));
        Assert.Equal([2, 3], lines.Order());

        Assert.True(EditRange.TryLocate(full, "", "<Border CornerRadius=\"8\"/>", false, out lines));
        Assert.Equal([2], lines.Order());

        Assert.False(EditRange.TryLocate(full, "", "<Grid/>", false, out _));
    }

    [Theory]
    [InlineData("a\nb\nc", 3)]
    [InlineData("a\r\nb\r\nc", 3)]
    [InlineData("a\rb\rc", 3)]
    [InlineData("a\r\nb\rc\n", 4)]
    public void Every_line_of_a_new_file_counts_whatever_its_line_endings(string text, int lines)
    {
        Assert.Equal(lines, EditRange.AllLines(text).Count);
        Assert.Equal(lines, TextLines.Split(text).Length);
    }
}
