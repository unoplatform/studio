namespace Uno.Lint;

/// <summary>
/// Recursive file walk that never descends into bin, obj, .git and the like, nor into symbolic links or junctions:
/// a link back up the tree would otherwise make the walk loop until the path is too long.
/// </summary>
public static class FileWalker
{
    private static readonly HashSet<string> ExcludedDirNames = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", ".git", ".vs", ".idea" };

    public static IEnumerable<string> Enumerate(string root, Func<string, bool> accept)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            IEnumerable<FileSystemInfo> entries;
            try
            {
                entries = new DirectoryInfo(dir).EnumerateFileSystemInfos().ToList();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (entry is DirectoryInfo directory)
                {
                    // Links only: OneDrive and other cloud placeholders are reparse points too, and hold real files.
                    if (directory.LinkTarget == null && !ExcludedDirNames.Contains(directory.Name))
                    {
                        pending.Push(directory.FullName);
                    }
                }
                else if (accept(entry.FullName))
                {
                    yield return entry.FullName;
                }
            }
        }
    }
}
