﻿using System.Linq.Expressions;
using Business.Utils.ExpressionExtensions;

namespace Business.Utils.StringExtensions;

public static class StringExtension
{
    public static string GetCacheKey<T>(this Expression<Func<T, bool>> expression) where T : class
    {
        var expressionStringBuilder = new ExpressionStringBuilderVisitor();
        expressionStringBuilder.Visit(expression);
        return expressionStringBuilder.GetText();
    }


    

    public static List<string> ChunkText(this string text, int chunkSize = 500, int overlap = 100)
    {
        List<string> chunks = new List<string>();
        int startIndex = 0;

        while (startIndex < text.Length)
        {
            int endIndex = Math.Min(startIndex + chunkSize, text.Length);
            string chunk = text.Substring(startIndex, endIndex - startIndex);

            chunks.Add(chunk);

            // Calculate the next start index, considering the overlap
            startIndex += chunkSize - overlap;
        }

        return chunks;
    }
}