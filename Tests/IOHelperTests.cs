namespace StockSharp.Tests;

[TestClass]
public class IOHelperTests
{
	[TestMethod]
	public void IsNetworkPath_UNCPath()
	{
		// UNC paths (Universal Naming Convention)
		@"\\server\share".IsNetworkPath().AssertTrue();
		@"\\192.168.1.1\share".IsNetworkPath().AssertTrue();
		@"\\server\share\folder\file.txt".IsNetworkPath().AssertTrue();
		@"\\.\pipe\mypipe".IsNetworkPath().AssertTrue();
		@"//server/share".IsNetworkPath().AssertTrue();
		@"//192.168.1.1/share/folder".IsNetworkPath().AssertTrue();
	}

	[TestMethod]
	public void IsNetworkPath_LocalPath()
	{
		// Local Windows paths with drive letter
		@"C:\Windows\System32".IsNetworkPath().AssertFalse();
		@"D:\Data\file.txt".IsNetworkPath().AssertFalse();
		@"E:\".IsNetworkPath().AssertFalse();
		@"Z:\NetworkShare".IsNetworkPath().AssertFalse();
		@"Y:\Data\file.txt".IsNetworkPath().AssertFalse();
		@"Folder1".IsNetworkPath().AssertFalse();
		@"temp".IsNetworkPath().AssertFalse();
	}

	[TestMethod]
	public void IsNetworkPath_UnixPath()
	{
		// Unix-style paths are NOT network paths
		@"/usr/bin/bash".IsNetworkPath().AssertFalse();
		@"/home/user/file.txt".IsNetworkPath().AssertFalse();
	}

	[TestMethod]
	public void IsNetworkPath_EmptyOrNull()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => string.Empty.IsNetworkPath());
		Assert.ThrowsExactly<ArgumentNullException>(() => ((string)null).IsNetworkPath());
	}

	[TestMethod]
	public void IsNetworkPath_RelativePath()
	{
		@"folder\file.txt".IsNetworkPath().AssertFalse();
		@".\file.txt".IsNetworkPath().AssertFalse();
		@"..\file.txt".IsNetworkPath().AssertFalse();
	}

	[TestMethod]
	public void IsNetworkPath_HttpPath()
	{
		@"http://example.com/file.txt".IsNetworkPath().AssertTrue();
		@"https://example.com/folder/".IsNetworkPath().AssertTrue();
		@"ftp://ftp.example.com/data".IsNetworkPath().AssertTrue();
	}

	[TestMethod]
	public void IsNetworkPath_ShortPath()
	{
		// Paths shorter than 3 characters throw ArgumentOutOfRangeException
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => "C:".IsNetworkPath());
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => "ab".IsNetworkPath());
	}
}
