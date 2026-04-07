using TransFundInventory.Data;

namespace TransFundInventory.Helpers
{
    public static class BackupHelper
    {
        public static bool BackupDatabase(string destinationPath)
        {
            try
            {
                var dbPath = DatabaseHelper.GetDbPath();
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("Database file not found.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                File.Copy(dbPath, destinationPath, true);

                // Also backup ProductImages folder if it exists
                var imageFolder = DatabaseHelper.GetImageFolder();
                if (Directory.Exists(imageFolder))
                {
                    var backupDir = Path.GetDirectoryName(destinationPath);
                    var imageBackupDir = Path.Combine(backupDir!, "ProductImages_Backup");
                    if (Directory.Exists(imageBackupDir))
                        Directory.Delete(imageBackupDir, true);
                    CopyDirectory(imageFolder, imageBackupDir);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool RestoreDatabase(string sourcePath)
        {
            try
            {
                var dbPath = DatabaseHelper.GetDbPath();

                // Verify it's a valid SQLite file
                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show("Backup file not found.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                File.Copy(sourcePath, dbPath, true);

                // Restore images if backup has them
                var sourceDir = Path.GetDirectoryName(sourcePath);
                var imageBackupDir = Path.Combine(sourceDir!, "ProductImages_Backup");
                if (Directory.Exists(imageBackupDir))
                {
                    var imageFolder = DatabaseHelper.GetImageFolder();
                    CopyDirectory(imageBackupDir, imageFolder);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
            }
        }
    }
}
