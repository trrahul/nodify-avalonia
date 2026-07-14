namespace Nodify.Avalonia.Workflow.Common
{
    /// <summary>
    /// Rendering variant for an icon: <see cref="Filled"/> icons paint the glyph body with the
    /// icon brush instead of rendering an outline only.
    /// </summary>
    public enum IconVariant
    {
        Regular = 0,
        Filled
    }

    /// <summary>
    /// The set of icons used across the workflow designer. Each value maps to a
    /// <see cref="Avalonia.Media.Geometry"/> glyph declared in <c>Themes/Icons.axaml</c> (looked
    /// up by enum name via <see cref="IconToGeometryConverter"/>). Unknown values fall back to a
    /// placeholder glyph.
    /// </summary>
    public enum WorkflowIcon
    {
        None = 0,
        ArrowDown,
        ArrowDownload,
        ArrowLeft,
        ArrowMaximize,
        ArrowMinimize,
        ArrowMove,
        ArrowMoveInward,
        ArrowNext,
        ArrowPrevious,
        ArrowRight,
        ArrowTurnBidirectionalDownRight,
        ArrowUp,
        Beaker,
        Box,
        BranchFork,
        Building,
        ClipboardTask,
        CloudArrowDown,
        CloudArrowUp,
        Color,
        DismissCircle,
        Edit,
        Flow,
        FlowDot,
        HandWave,
        Info,
        Keyboard,
        LineDashes,
        LineHorizontal1DashDotDash,
        LockClosed,
        LockOpen,
        Map,
        PaddingRight,
        PenDismiss,
        Play,
        PlugConnectedAdd,
        PlugDisconnected,
        RectangleLandscapeSparkle,
        Rocket,
        Save,
        Search,
        SelectObject,
        Settings,
        Square,
        SquareDismiss,
        SquareEraser,
        SquareHint,
        SquareShadow,
        SquaresNested,
        ZoomFit,
        ZoomIn,
        ZoomOut,
        Warning,
        Add,
        ChevronRight,
        FullScreenMaximize,
        MoreHorizontal,
    }
}
