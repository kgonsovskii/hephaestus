namespace model;

public static class VbsRandomer
{
    public static async Task<string> ReadFileWithRetryAsync(string filePath, int maxAttempts, int delayMilliseconds)
    {
        int attempts = 0;
        while (attempts < maxAttempts)
        {
            try
            {
                return await System.IO.File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts < maxAttempts)
                {
                    await Task.Delay(delayMilliseconds);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        return null;
    }

    public static string Modify(string inContent)
    {
        var lines = inContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
        var outputLines = new List<string>();
        int lineCounter = 0;
        foreach (var line in lines)
        {
            lineCounter++;
            if (lineCounter % 2 == 0 || lineCounter == 1)
            {
                outputLines.Add(GenerateRandomVbScriptLine());
            }
            outputLines.Add(line);
        }
        outputLines.Add(GenerateRandomVbScriptLine());

        return string.Join(Environment.NewLine, outputLines);
    }

    public static string GenerateRandomVariableName(int length = 10)
    {
        const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var random = new Random();
        char firstChar = letters[random.Next(letters.Length)];
        var restChars = new char[length - 1];

        for (int i = 0; i < restChars.Length; i++)
        {
            restChars[i] = chars[random.Next(chars.Length)];
        }

        return firstChar + new string(restChars);
    }

    private static string GenerateRandomVbScriptLine()
    {
        var random = new Random();
        int variableLength = 7 + random.Next(0, 4); // Ensure variable length is at least 7 characters
        string variable = GenerateRandomVariableName(variableLength);
        int value1 = random.Next(1, 101);
        int value2 = random.Next(1, 101);

        string operationLine = $"{variable} = {value1}";
        string comparison = $"{variable} < {value2}";

        string ifThenElseLine =
            $"If {comparison} Then{Environment.NewLine}    {variable} = {variable} + 1{Environment.NewLine}Else{Environment.NewLine}    {variable} = {variable} - 1{Environment.NewLine}End If";
        string line = $"Dim {variable}{Environment.NewLine}{operationLine}{Environment.NewLine}{ifThenElseLine}";
        return line;
    }
}