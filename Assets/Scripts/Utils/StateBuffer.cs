using UnityEngine;

public class StateBuffer<T>
{
    public StateBuffer(int size)
    {
        elements = new T[size];
        ticks = new int[size];
    }

    public int Count => count;
    public int Capacity => ticks.Length;

    public T this[int index]
    {
        get
        {
            Debug.Assert(index >= 0 && index < count);
            return elements[index];
        }
    }

    public int FirstTick()
    {
        return count > 0 ? ticks[first] : -1;
    }

    public int LastTick()
    {
        return count > 0 ? ticks[(first + count - 1) % ticks.Length] : -1;
    }

    public T First()
    {
        Debug.Assert(count > 0);
        return elements[first];
    }

    public T Last()
    {
        Debug.Assert(count > 0);
        return elements[(first + count - 1) % ticks.Length];
    }

    public bool TryGetValue(int tick, out T result)
    {
        var index = first;
        for (int i = 0; i < count; ++i, ++index)
        {
            if (index == ticks.Length)
                index = 0;

            if (ticks[index] == tick)
            {
                result = elements[index];
                return true;
            }
        }

        result = default(T);
        return false;
    }

    public bool GetStates(int tick, float fraction, ref int lowIndex, ref int highIndex, ref float outputFraction)
    {
        lowIndex = GetValidIndexLower(tick);
        highIndex = GetValidIndexHigher(tick + 1);

        if (lowIndex == -1 || highIndex == -1)
            return false;

        int lowTick = GetTickByIndex(lowIndex);
        int highTick = GetTickByIndex(highIndex);

        float total = (float)(highTick - lowTick);
        float relativeTime = tick - lowTick + fraction;

        outputFraction = relativeTime / total;

        return true;
    }

    int GetValidIndexLower(int tick)
    {
        var index = first;

        int bestResultIndex = -1;
        for (int i = 0; i < count; ++i, ++index)
        {
            if (index == ticks.Length)
                index = 0;

            if (ticks[index] == tick)
            {
                bestResultIndex = index;
                break;
            }

            if (ticks[index] < tick)
            {
                bestResultIndex = index;
            }
        }

        return bestResultIndex;
    }

    int GetValidIndexHigher(int tick)
    {
        var index = first;

        int bestResultIndex = -1;
        for (int i = 0; i < count; ++i, ++index)
        {
            if (index == ticks.Length)
                index = 0;

            if (ticks[index] == tick || ticks[index] > tick)
            {
                bestResultIndex = index;
                break;
            }
        }

        return bestResultIndex;
    }

    public int GetTickByIndex(int index)
    {
        Debug.Assert(index >= 0 && index < count);
        return ticks[index];
    }

    public void Add(int tick, T element)
    {
        var last = LastTick();
        if (last != -1 && last >= tick)
            throw new System.InvalidOperationException(string.Format("Ticks must be increasing when adding (last = {0}, trying to add {1})", last, tick));

        var index = (first + count) % ticks.Length;

        ticks[index] = tick;
        elements[index] = element;

        if (count < ticks.Length)
            count++;
        else
            first = (first + 1) % ticks.Length;
    }

    public void Clear()
    {
        first = 0;
        count = 0;

        for (int i = 0; i < ticks.Length; ++i)
            elements[i] = default(T);

        for (int i = 0; i < ticks.Length; ++i)
            ticks[i] = 0;
    }

    int first;
    int count;

    T[] elements;
    int[] ticks;
}
