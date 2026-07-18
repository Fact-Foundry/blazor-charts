namespace FactFoundry.Blazor.Charts.Models;

/// <summary>
/// One node in a commit DAG, as consumed by <c>CommitGraph</c>. The component knows
/// nothing about git itself — any directed acyclic graph of nodes-with-parents renders.
/// A LibGit2Sharp walk maps onto this directly: <see cref="Id"/>/<see cref="ParentIds"/>
/// from the commit's SHAs, <see cref="Refs"/> from the tip/tag decorations.
/// </summary>
public class CommitNode
{
    /// <summary>Unique identifier for this commit (e.g. a full SHA).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Ids of this commit's parents, first-parent first. Empty for a root commit.
    /// A commit with two or more parents renders as a merge.
    /// </summary>
    public IReadOnlyList<string> ParentIds { get; set; } = [];

    /// <summary>
    /// Optional ref decorations shown as badges on this commit (branch tips, tags,
    /// HEAD). A ref prefixed <c>tag:</c> (the <c>git log --decorate</c> convention)
    /// is styled as a tag; everything else is styled as a branch.
    /// </summary>
    public IReadOnlyList<string> Refs { get; set; } = [];

    /// <summary>Optional commit subject line, shown inline beside the lane strip.</summary>
    public string? Message { get; set; }

    /// <summary>Optional author name, shown in the hover tooltip.</summary>
    public string? Author { get; set; }

    /// <summary>Optional commit timestamp, shown in the hover tooltip.</summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>
    /// Optional count of files changed by this commit. When set (with or without
    /// <see cref="Insertions"/>/<see cref="Deletions"/>) a stats row appears in the
    /// hover tooltip. Maps to LibGit2Sharp's <c>Patch.FilesChanged</c>.
    /// </summary>
    public int? FilesChanged { get; set; }

    /// <summary>Optional inserted-line count, shown green in the tooltip stats row. Maps to <c>Patch.LinesAdded</c>.</summary>
    public int? Insertions { get; set; }

    /// <summary>Optional deleted-line count, shown red in the tooltip stats row. Maps to <c>Patch.LinesDeleted</c>.</summary>
    public int? Deletions { get; set; }
}
