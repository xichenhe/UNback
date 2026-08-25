using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class KeypadValueInputController : MonoBehaviour
{
    public float correctValue; 

    public UnityEvent<float> SetAbsolute;

    public string Value 
    {
        get { return inputValue; }
        set {  inputValue = value; } 
    }
    public string inputValue;

    public void AddValue(string inputValue)
    {
        if (inputValue == ".")
        {
            if (Value.Contains("."))
                return;
            else
                Value += '.';
            
            if (float.TryParse(Value,out float result))
                return;
            else
            {
                Value = Value.Insert(Value.IndexOf('.'), "0");
                return;
            }
        }
        if(inputValue == "-")
        {
            if (Value.Contains("-"))
                Value = Value.Replace("-","");
            else
                Value = Value.Insert(0, "-");

            return;
        }

        Value += inputValue;
    }
    public void ResetValue()
    {
        Value = "";
    }

    public void Submit()
    {
        float value;
        if (Value == "")
            value = 0;
        else
            float.TryParse(Value,out value);

        SetAbsolute.Invoke(value);

        Value = "";
    }
}
