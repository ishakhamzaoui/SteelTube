using System;
using System.IO;

namespace SteelTube.Infrastructure.Persistence
{
    /// <summary>
    /// Resolves where the SQLite file lives (SAD 23: conceptually
    /// "%PROGRAMDATA%\SteelTube\" or an equivalent Windows application-data
    /// location, rather than inside the installation directory).
    /// </summary>
    public static class DatabasePathProvider
    {
        private const string FolderName = "SteelTube";
        private const string FileName = "SteelTube.db";

        /// <summary>
        /// Default production location. On a real install this folder is
        /// created by the installer (SAD 55) with appropriate permissions;
        /// on a dev machine, CommonApplicationData is normally writable by
        /// standard users without elevation.
        /// </summary>
        public static string GetDefaultPath()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(baseDir, FolderName, FileName);
        }
    }
}