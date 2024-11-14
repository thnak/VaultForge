string defaultText = "hello {0}";
var formated = string.Format(defaultText, "my", "world");
Console.WriteLine(formated);