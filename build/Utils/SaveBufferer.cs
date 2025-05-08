using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Utils;

public class SaveBufferer(DbContext context, int threshold)
{
    private int _counter = 0;
    private int _totalCounter = 0;

    public async Task BufferedSave()
    {
        _totalCounter++;
        if (++_counter < threshold)
            return;
        await Save();
        _counter = 0;
    }

    public async Task Save()
    {
        await context.SaveChangesAsync();
        Console.WriteLine($"Saved {_totalCounter} package records");
    }
}
