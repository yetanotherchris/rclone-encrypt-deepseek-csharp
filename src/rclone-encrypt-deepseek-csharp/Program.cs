using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Text;
using RcloneEncrypt.Crypto;

var rootCommand = new RootCommand("Encrypts and decrypts files using the rclone encryption defaults")
{
    new Option<string?>(
        ["--password"],
        "Password for encryption/decryption. WARNING: Use an environment variable (RCLONE_ENCRYPT_PASSWORD) instead of command-line flags to avoid exposing your password in process lists and shell history."),
    new Option<string?>(
        ["--salt"],
        "Optional salt for key derivation. If not provided, rclone's built-in default salt is used."),
    new Option<string>(
        ["--filename-encoding"],
        () => "base32",
        "Filename encoding: base32 (default) or base64"),
    new Option<FileInfo?>(
        ["-i", "--input-file"],
        "Input file path"),
    new Option<FileInfo?>(
        ["-o", "--output-file"],
        "Output file path (optional - if not specified, output is written to stdout for text or a generated filename for binary)")
};

rootCommand.SetHandler(async (invocationContext) =>
{
    var password = invocationContext.ParseResult.GetValueForOption(rootCommand.Options[0] as Option<string?>)!;
    var salt = invocationContext.ParseResult.GetValueForOption(rootCommand.Options[1] as Option<string?>)!;
    var filenameEncodingStr = invocationContext.ParseResult.GetValueForOption(rootCommand.Options[2] as Option<string>)!;
    var inputFile = invocationContext.ParseResult.GetValueForOption(rootCommand.Options[3] as Option<FileInfo?>)!;
    var outputFile = invocationContext.ParseResult.GetValueForOption(rootCommand.Options[4] as Option<FileInfo?>)!;
    var console = invocationContext.Console;

    // Check if password was provided via command line (security warning)
    var passwordFromFlag = invocationContext.ParseResult.HasOption(rootCommand.Options[0] as Option<string?>)!;
    if (passwordFromFlag)
    {
        console.Error.WriteLine("WARNING: Password provided via --password flag. This is insecure because the password");
        console.Error.WriteLine("         may remain in your shell history and be visible in process listings.");
        console.Error.WriteLine("         Consider using the RCLONE_ENCRYPT_PASSWORD environment variable instead.");
        console.Error.WriteLine("         Remember to clear your terminal history after use.");
        console.Error.WriteLine();
    }

    // Check env var if password not provided via flag
    if (string.IsNullOrEmpty(password))
    {
        password = Environment.GetEnvironmentVariable("RCLONE_ENCRYPT_PASSWORD");
    }

    // Prompt interactively for password if not provided via flag or env var
    if (string.IsNullOrEmpty(password) && !Console.IsInputRedirected)
    {
        console.Out.Write("Enter password: ");
        password = ReadPassword();
        console.Out.WriteLine();
    }

    // Prompt for salt if not provided via flag
    if (salt is null && !Console.IsInputRedirected)
    {
        console.Out.Write("Enter salt (optional, press Enter to use default rclone salt): ");
        var saltInput = Console.ReadLine();
        if (!string.IsNullOrEmpty(saltInput))
            salt = saltInput;
    }

    if (string.IsNullOrEmpty(password))
    {
        console.Error.WriteLine("Error: password is required.");
        invocationContext.ExitCode = 1;
        return;
    }

    if (inputFile is null || !inputFile.Exists)
    {
        console.Error.WriteLine("Error: input file is required and must exist.");
        invocationContext.ExitCode = 1;
        return;
    }

    var filenameEncoding = FilenameEncodingHelper.Parse(filenameEncodingStr);
    var keyMaterial = KeyDerivation.DeriveKeyMaterial(password, salt);
    var inputBytes = await File.ReadAllBytesAsync(inputFile.FullName);

    var isEncrypted = HasRcloneMagic(inputBytes);

    byte[] outputBytes;
    string? outputFilename;

    if (isEncrypted)
    {
        outputBytes = FileEncryption.DecryptFile(keyMaterial.FileKey, inputBytes);
        outputFilename = DecryptFilename(inputFile.Name, keyMaterial, filenameEncoding);
    }
    else
    {
        outputBytes = FileEncryption.EncryptFile(keyMaterial.FileKey, inputBytes);
        outputFilename = EncryptFilename(inputFile.Name, keyMaterial, filenameEncoding);
    }

    if (outputFile is not null)
    {
        await File.WriteAllBytesAsync(outputFile.FullName, outputBytes);
        console.Out.WriteLine($"Written to: {outputFile.FullName}");
    }
    else
    {
        if (isEncrypted && outputFilename is not null)
        {
            var outputPath = Path.Combine(Path.GetDirectoryName(inputFile.FullName)!, outputFilename);
            await File.WriteAllBytesAsync(outputPath, outputBytes);
            console.Out.WriteLine($"Written to: {outputPath}");
        }
        else
        {
            // Try to output as text, fall back to binary
            try
            {
                var text = Encoding.UTF8.GetString(outputBytes);
                console.Out.WriteLine(text);
            }
            catch
            {
                var outputPath = inputFile.FullName + (isEncrypted ? ".decrypted" : ".encrypted");
                await File.WriteAllBytesAsync(outputPath, outputBytes);
                console.Out.WriteLine($"Written to: {outputPath}");
            }
        }
    }
});

return await new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build()
    .InvokeAsync(args);

static string? DecryptFilename(string encryptedName, KeyMaterial keyMaterial, FilenameEncoding encoding)
{
    try
    {
        var encryptedBytes = FilenameEncodingHelper.Decode(encryptedName, encoding);
        var decryptedBytes = FileEncryption.DecryptName(keyMaterial.NameKey, keyMaterial.NameIv, encryptedBytes);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    catch
    {
        return null;
    }
}

static string EncryptFilename(string plainName, KeyMaterial keyMaterial, FilenameEncoding encoding)
{
    var plainBytes = Encoding.UTF8.GetBytes(plainName);
    var encryptedBytes = FileEncryption.EncryptName(keyMaterial.NameKey, keyMaterial.NameIv, plainBytes);
    return FilenameEncodingHelper.Encode(encryptedBytes, encoding);
}

static bool HasRcloneMagic(byte[] data)
{
    if (data.Length < 8) return false;
    var magic = "RCLONE\x00\x00"u8;
    for (var i = 0; i < 8; i++)
        if (data[i] != magic[i])
            return false;
    return true;
}

static string ReadPassword()
{
    var password = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter)
            break;
        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password.Remove(password.Length - 1, 1);
        }
        else if (key.Key != ConsoleKey.Backspace)
        {
            password.Append(key.KeyChar);
        }
    }
    return password.ToString();
}
