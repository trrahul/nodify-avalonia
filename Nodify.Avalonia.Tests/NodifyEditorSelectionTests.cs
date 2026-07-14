using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Headless.XUnit;
using Nodify.Avalonia.Helpers;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class NodifyEditorSelectionTests
{
    [AvaloniaFact]
    public void SelectArea_Replace_StartsTransactionBeforeClearingSelection()
    {
        var editor = new NodifyEditor { Width = 400, Height = 300 };
        var original = CreateContainer(editor, new Point(0, 0));
        var replacement = CreateContainer(editor, new Point(200, 0));
        editor.ItemsSource = new[] { original, replacement };

        using var fixture = TestWindowFixture.Show(editor);
        original.IsSelected = true;

        ItemContainer[]? selectionSeenByStartCommand = null;
        var selectionChanges = new List<(bool IsSelecting, ItemContainer[] SelectedItems)>();
        editor.ItemsSelectStartedCommand = new TestCommand(_ =>
            selectionSeenByStartCommand = GetSelectedItems(editor));
        editor.SelectionChanged += (_, _) =>
            selectionChanges.Add((editor.IsSelecting, GetSelectedItems(editor)));

        editor.SelectArea(new Rect(190, -10, 70, 70));

        Assert.Equal(new[] { original }, selectionSeenByStartCommand);
        var change = Assert.Single(selectionChanges);
        Assert.True(change.IsSelecting);
        Assert.Equal(new[] { replacement }, change.SelectedItems);
    }

    [AvaloniaFact]
    public void RubberBandReplace_PreservesSelectedContainerThatIsNoLongerSelectable()
    {
        var editor = new NodifyEditor { Width = 400, Height = 300 };
        var noLongerSelectable = CreateContainer(editor, new Point(0, 0));
        var insideSelectionArea = CreateContainer(editor, new Point(200, 0));
        editor.ItemsSource = new[] { noLongerSelectable, insideSelectionArea };

        using var fixture = TestWindowFixture.Show(editor);
        editor.SelectAll();
        noLongerSelectable.IsSelectable = false;

        editor.BeginSelecting(new Point(190, -10), SelectionHelper.SelectionType.Replace);
        editor.UpdateSelection(new Point(260, 60));
        editor.EndSelecting();

        Assert.True(noLongerSelectable.IsSelected);
        Assert.True(insideSelectionArea.IsSelected);
    }

    private static ItemContainer CreateContainer(NodifyEditor editor, Point location) => new(editor)
    {
        Width = 50,
        Height = 50,
        DesiredSizeForSelection = new Size(50, 50),
        Location = location
    };

    private static ItemContainer[] GetSelectedItems(NodifyEditor editor)
        => editor.SelectedItems!.Cast<ItemContainer>().ToArray();

}
