class GarminUpdater
{
    // Конфигурация
    private const string TargetDriveKeyword = "Garmin";
    private const string TargetSubfolder = "Garmin";
    private static readonly string[] FoldersToCopy = ["BirdsEye", "CustomMaps", "GPX"];
    private const string BackupFolderName = "nav_backup";

    static void Main()
    {
        Console.WriteLine("=== Скрипт обновления файлов для Garmin ===");
        Console.WriteLine("Нажмите любую клавишу для начала работы");
        Console.ReadLine();

        bool repeat;
        do
        {
            Console.Clear();
            Console.WriteLine("=== Скрипт обновления файлов для Garmin ===");
            Console.WriteLine();

            ExecuteUpdateScript();

            Console.WriteLine();
            Console.WriteLine("Выберите действие:");
            Console.WriteLine("1 - Повторить обновление");
            Console.WriteLine("2 - Выход");
            Console.Write("Ваш выбор: ");

            repeat = Console.ReadLine() == "1";
        } while (repeat);

        Console.WriteLine("Работа завершена. Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    static void ExecuteUpdateScript()
    {
        string scriptDirectory = AppDomain.CurrentDomain.BaseDirectory;

        try
        {
            // Поиск диска Garmin
            var targetDrive = FindGarminDrive();
            if (targetDrive == null)
            {
                ShowConnectedDrives();
                return;
            }

            string targetRoot = Path.Combine(targetDrive.RootDirectory.FullName, TargetSubfolder);
            Console.WriteLine($"\nНайден диск: {targetDrive.Name} - {targetDrive.VolumeLabel}");

            // Создание резервной копии и очистка
            CreateBackupAndClean(targetRoot);

            // Копирование новых файлов
            CopyNewFiles(scriptDirectory, targetRoot);

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("Операция завершена успешно!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nКритическая ошибка: {ex.Message}");
            Console.WriteLine($"Детали: {ex.StackTrace}");
        }
    }

    static DriveInfo FindGarminDrive()
    {
        return DriveInfo.GetDrives()
            .FirstOrDefault(d => d.IsReady &&
                               !string.IsNullOrEmpty(d.VolumeLabel) &&
                               d.VolumeLabel.Contains(TargetDriveKeyword, StringComparison.OrdinalIgnoreCase));
    }

    static void ShowConnectedDrives()
    {
        Console.WriteLine($"Диск с названием, содержащим '{TargetDriveKeyword}', не найден.");
        Console.WriteLine("\nПодключенные диски:");
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            Console.WriteLine($"  {drive.Name} - {drive.VolumeLabel ?? "Без метки"} ({drive.DriveFormat}, {drive.TotalFreeSpace / (1024 * 1024 * 1024):N1} ГБ свободно)");
        }
    }

    static void CreateBackupAndClean(string targetRoot)
    {
        // Находим существующие папки для бекапа
        var existingFolders = FoldersToCopy
            .Where(folderName => Directory.Exists(Path.Combine(targetRoot, folderName)))
            .ToList();

        if (existingFolders.Count == 0)
        {
            Console.WriteLine("\nСуществующие папки для резервного копирования не найдены.");
            Console.WriteLine("Пропуск этапа резервного копирования и очистки.");
            return;
        }

        // Создаем структуру для бекапа
        string backupPath = CreateBackupStructure();
        Console.WriteLine($"\nСоздание резервной копии в: {backupPath}");

        // Создаем резервные копии
        CreateBackups(targetRoot, backupPath, existingFolders);

        // Очищаем папки
        Console.WriteLine("\nОчистка файлов на диске...");
        CleanTargetFolders(targetRoot);
    }

    static string CreateBackupStructure()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string backupRootPath = Path.Combine(desktopPath, BackupFolderName);

        Directory.CreateDirectory(backupRootPath);

        string dateFolderPath = Path.Combine(backupRootPath, DateTime.Now.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dateFolderPath);

        string timeFolderPath = Path.Combine(dateFolderPath, DateTime.Now.ToString("HH_mm_ss"));
        Directory.CreateDirectory(timeFolderPath);

        return timeFolderPath;
    }

    static void CreateBackups(string sourceRoot, string backupRoot, List<string> folders)
    {
        Console.WriteLine($"Копируется {folders.Count} папок в резервную копию:");

        foreach (string folderName in folders)
        {
            string sourcePath = Path.Combine(sourceRoot, folderName);
            string backupPath = Path.Combine(backupRoot, folderName);

            Console.Write($"  {folderName}... ");

            try
            {
                CopyDirectory(sourcePath, backupPath, true);
                Console.WriteLine("УСПЕХ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    static void CleanTargetFolders(string targetRoot)
    {
        foreach (string folderName in FoldersToCopy)
        {
            string targetPath = Path.Combine(targetRoot, folderName);

            if (!Directory.Exists(targetPath))
                continue;

            Console.WriteLine($"  {folderName}... ");

            try
            {
                CleanFolder(targetPath, folderName);
                Console.WriteLine("  УСПЕХ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Ошибка: {ex.Message}");
            }
        }
    }

    static void CopyNewFiles(string sourceRoot, string targetRoot)
    {
        Console.WriteLine("\nОбновление файлов на диске...");
        int updatedCount = 0;

        foreach (string folderName in FoldersToCopy)
        {
            string sourcePath = Path.Combine(sourceRoot, folderName);
            string targetPath = Path.Combine(targetRoot, folderName);

            Console.Write($"  {folderName}... ");

            try
            {
                CopyDirectory(sourcePath, targetPath, true);
                Console.WriteLine("УСПЕХ");
                updatedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Директория не найдена: {sourceDir}");

        // Копируем файлы
        foreach (var file in dir.GetFiles())
        {
            try
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }
            catch (IOException ex)
            {
                // Логируем ошибки ввода-вывода, но продолжаем работу
                Console.WriteLine($"    Предупреждение при копировании {file.Name}: {ex.Message}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"    Нет доступа к файлу: {file.Name}");
            }
        }

        // Рекурсивно копируем поддиректории
        if (recursive)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }

    static void CleanFolder(string folderPath, string folderName)
    {
        if (!Directory.Exists(folderPath))
            return;

        switch (folderName)
        {
            case "BirdsEye":
            case "CustomMaps":
                CleanFolderExceptImg(folderPath);
                break;
            case "GPX":
                CleanGpxFolder(folderPath);
                break;
        }
    }

    static void CleanFolderExceptImg(string folderPath)
    {
        // Удаляем файлы в корне, кроме .img
        foreach (var file in Directory.GetFiles(folderPath))
        {
            if (!Path.GetExtension(file).Equals(".img", StringComparison.OrdinalIgnoreCase))
            {
                SafeDeleteFile(file);
            }
        }
    }

    static void CleanGpxFolder(string gpxPath)
    {
        // Удаляем файлы в корне GPX, кроме .img
        CleanFolderExceptImg(gpxPath);

        // Обрабатываем подпапки GPX
        string[] specialFolders = ["Nav", "Archive"];

        foreach (string folderName in specialFolders)
        {
            string folderPath = Path.Combine(gpxPath, folderName);

            if (!Directory.Exists(folderPath))
                continue;

            // Для Nav и Archive удаляем файлы, кроме .img
            foreach (var file in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories))
            {
                if (!Path.GetExtension(file).Equals(".img", StringComparison.OrdinalIgnoreCase))
                {
                    SafeDeleteFile(file);
                }
            }
        }
    }

    static void SafeDeleteFile(string filePath)
    {
        try
        {
            Console.WriteLine($"    Удаляем файл: {Path.GetFullPath(filePath)}");
            File.Delete(filePath);
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"    Нет доступа для удаления: {Path.GetFileName(filePath)}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"    Ошибка при удалении {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }
}