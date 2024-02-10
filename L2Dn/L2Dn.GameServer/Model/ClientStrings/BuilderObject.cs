﻿namespace L2Dn.GameServer.Model.ClientStrings;

public class BuilderObject: Builder
{
    private readonly int _index;

public     BuilderObject(int id)
    {
        if ((id < 1) || (id > 9))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Illegal Id: " + id);
        }

        _index = id - 1;
    }

    public override String toString(Object param)
    {
        return param == null ? "null" : param.ToString();
    }

    public override String toString(params Object[] @params)
    {
        if ((@params == null) || (@params.Length == 0))
        {
            return "null";
        }

        return @params[0].ToString();
    }

    public override int getIndex()
    {
        return _index;
    }

    public override String ToString()
    {
        return "[PARAM-" + (_index + 1) + "]";
    }
}