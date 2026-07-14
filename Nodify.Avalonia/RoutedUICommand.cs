using System.ComponentModel.Design;
using System;
using System.Collections.Generic;
using Avalonia.Input;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia;

public class RoutedUICommand : RoutedCommand
{
    /// <summary>
    ///     Default Constructor - needed to allow markup creation
    /// </summary>
    public RoutedUICommand(string name) : base(name)
    {
        _text = String.Empty;
    }

    /// <summary>
    ///     Creates an instance of this class.
    /// </summary>
    /// <param name="text">Descriptive and localizable text for the command</param>
    /// <param name="name">Declared Name of the RoutedCommand for Serialization</param>
    /// <param name="ownerType">Type that is registering the property</param>
    public RoutedUICommand(string text, string name, Type ownerType)
        : this(text, name, ownerType, null)
    {
    }

    /// <summary>
    ///     Creates an instance of this class.
    /// </summary>
    /// <param name="text">Descriptive and localizable text for the command</param>
    /// <param name="name">Declared Name of the RoutedCommand for Serialization</param>
    /// <param name="ownerType">Type that is registering the property</param>
    /// <param name="inputGestures">Default Input Gestures associated</param>
    public RoutedUICommand(string text, string name, Type ownerType, InputGestureCollection? inputGestures)
        : base(name, inputGestures)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }
        _text = text;
    }

    /// <summary>
    ///     Descriptive and localizable text for the command.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _text = value;
        }
    }

    private string _text;
}