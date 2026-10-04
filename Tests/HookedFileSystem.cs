namespace StockSharp.Tests;

/// <summary>
/// A file system that tells the test about a file right before it is opened, so the test can fail
/// the call or change something at exactly that moment.
/// </summary>
internal sealed class HookedFileSystem(IFileSystem inner) : IFileSystem
{
	private readonly IFileSystem _inner = inner ?? throw new ArgumentNullException(nameof(inner));

	/// <summary>
	/// Called with the path and the access asked for, before the file is opened.
	/// </summary>
	public Action<string, FileAccess> Opening { get; set; }

	public Stream Open(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None)
	{
		Opening?.Invoke(path, access);

		return _inner.Open(path, mode, access, share);
	}

	public long MaxSize
	{
		get => _inner.MaxSize;
		set => _inner.MaxSize = value;
	}

	public FileSystemOverflowBehavior OverflowBehavior
	{
		get => _inner.OverflowBehavior;
		set => _inner.OverflowBehavior = value;
	}

	public long TotalSize => _inner.TotalSize;

	public bool FileExists(string path) => _inner.FileExists(path);
	public bool DirectoryExists(string path) => _inner.DirectoryExists(path);
	public void CreateDirectory(string path) => _inner.CreateDirectory(path);
	public void DeleteDirectory(string path, bool recursive = false) => _inner.DeleteDirectory(path, recursive);
	public void DeleteFile(string path) => _inner.DeleteFile(path);
	public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false) => _inner.MoveFile(sourceFileName, destFileName, overwrite);
	public void MoveDirectory(string sourceDirName, string destDirName) => _inner.MoveDirectory(sourceDirName, destDirName);
	public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false) => _inner.CopyFile(sourceFileName, destFileName, overwrite);
	public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => _inner.EnumerateFiles(path, searchPattern, searchOption);
	public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => _inner.EnumerateDirectories(path, searchPattern, searchOption);
	public DateTime GetCreationTimeUtc(string path) => _inner.GetCreationTimeUtc(path);
	public DateTime GetLastWriteTimeUtc(string path) => _inner.GetLastWriteTimeUtc(path);
	public long GetFileLength(string path) => _inner.GetFileLength(path);
	public void SetReadOnly(string path, bool isReadOnly) => _inner.SetReadOnly(path, isReadOnly);
	public FileAttributes GetAttributes(string path) => _inner.GetAttributes(path);
}
