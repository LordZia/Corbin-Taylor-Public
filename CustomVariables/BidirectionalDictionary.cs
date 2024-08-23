using System.Collections;
using System.Collections.Generic;
using UnityEngine;
class BidirectionalDictionary<TKey, TValue>
{
    private Dictionary<TKey, TValue> keyToValue = new Dictionary<TKey, TValue>();
    private Dictionary<TValue, TKey> valueToKey = new Dictionary<TValue, TKey>();

    public void Add(TKey key, TValue value)
    {
        keyToValue[key] = value;
        valueToKey[value] = key;
    }

    public TValue GetValueByKey(TKey key)
    {
        return keyToValue[key];
    }

    public TKey GetKeyByValue(TValue value)
    {
        return valueToKey[value];
    }
}
