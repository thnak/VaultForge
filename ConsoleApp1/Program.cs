int[] GenerateIndices(int fileStreamsCount, int stripeCount)
{
    int[] indices = new int[fileStreamsCount];
    int lastIndex = fileStreamsCount - 1;
    indices[lastIndex] = lastIndex - (stripeCount % fileStreamsCount);
    
    int index = 0;
    for (int i = 0; i < lastIndex; i++)
    {
        indices[i] = index++;
        if (indices[i] == indices[lastIndex])
        {
            indices[i] = index++;
        }
    }
    
    return indices;
}


for (int i = 0; i < 10; i++)
{
    var indices = GenerateIndices(4, i);
    Console.WriteLine($"{i}: " + string.Join(", ", indices));
}