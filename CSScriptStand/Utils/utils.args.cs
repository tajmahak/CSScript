using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

// utils.args
// УТИЛИТЫ ДЛЯ РАБОТЫ С АРГУМЕНТАМИ КОМАНДНОЙ СТРОКИ
// ------------------------------------------------------------

//## #namespace

public static class CommandArgumentUtils
{
    public static string ToString(ConsoleArguments args) {
        var str = new StringBuilder();

        var objType = args.GetType();
        var properties = objType.GetProperties();
        foreach (var property in properties) {
            var attrArray = property.GetCustomAttributes(typeof(CommandArgumentAttribute), false);
            if (attrArray.Length == 0) {
                continue;
            }
            var attr = (CommandArgumentAttribute)attrArray[0];

            var value = property.GetValue(args, null);
            if (value == null) {
                continue;
            }

            if (attr.Flag) {
                if (Equals(value, true)) {
                    if (str.Length > 0) {
                        str.Append(' ');
                    }
                    str.Append(attr.Names[0]);
                }
            }
            else {
                if (attr.Names.Length > 0) {
                    if (str.Length > 0) {
                        str.Append(' ');
                    }
                    str.Append(attr.Names[0]);
                }
                if (value is IEnumerable && !(value is string)) {
                    var enumerable = (IEnumerable)value;
                    foreach (var item in enumerable) {

                        str.Append(attr.Separator);

                        if (attr.Quoted) {
                            str.Append('\"');
                        }
                        str.Append(__GetArgumentValue(item, attr));
                        if (attr.Quoted) {
                            str.Append('\"');
                        }
                    }
                }
                else {
                    str.Append(attr.Separator);
                    if (attr.Quoted) {
                        str.Append('\"');
                    }
                    str.Append(__GetArgumentValue(value, attr));
                    if (attr.Quoted) {
                        str.Append('\"');
                    }
                }
            }
        }
        return str.ToString();
    }

    private static string __GetArgumentValue(object value, CommandArgumentAttribute attr) {
        string convertedValue;
        if (attr.Converter != null) {
            var converter = (IConsoleArgumentConverter)Activator.CreateInstance(attr.Converter);
            convertedValue = converter.Convert(value);
        }
        else {
            convertedValue = value is bool ? Equals(value, true) ? "true" : "false" : value.ToString();
        }
        return convertedValue;
    }
}

public abstract class ConsoleArguments : ICloneable
{
    public object Clone() {
        return MemberwiseClone();
    }

    public override string ToString() {
        return CommandArgumentUtils.ToString(this);
    }

    public static implicit operator string(ConsoleArguments args) {
        return args.ToString();
    }
}

public abstract class ConsoleArguments<T> : ConsoleArguments
{
    public new T Clone() {
        return (T)base.Clone();
    }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class CommandArgumentAttribute : Attribute
{
    /// <summary>
    /// Указывает, является ли параметр флагом (без указания значения)
    /// </summary>
    public bool Flag { get; set; }

    /// <summary>
    /// Будет ли значение параметра указано в кавычках
    /// </summary>
    public bool Quoted { get; set; }

    /// <summary>
    /// Разделитель между названием параметра и значением.
    /// </summary>
    public string Separator { get; set; }

    /// <summary>
    /// Конвертер значений типа <see cref="IConsoleArgumentConverter"/>.
    /// </summary>
    public Type Converter { get; set; }

    /// <summary>
    /// Названия аргумента.
    /// </summary>
    public string[] Names { get; private set; }

    public CommandArgumentAttribute(params string[] names) {
        Names = names;
        Quoted = true;
        Separator = " ";
    }
}

public interface IConsoleArgumentConverter
{
    string Convert(object value);
}

/// <summary>
/// Конструктор строки аргументов.
/// </summary>
public class CommandArgumentBuilder
{
    public CommandArgumentBuilder() {

    }

    public CommandArgumentBuilder(string value) : this() {
        Add(value);
    }

    public CommandArgumentBuilder Add(string format, params object[] args) {
        var value = string.Format(format, args);
        value = value.Replace(Environment.NewLine, " ");
        if (builder.Length > 0 && builder[builder.Length - 1] != ' ') {
            builder.Append(' ');
        }
        builder.Append(value);
        return this;
    }

    public CommandArgumentBuilder Add(bool condition, string format, params object[] args) {
        if (condition) {
            Add(format, args);
        }
        return this;
    }

    public CommandArgumentBuilder Add(IEnumerable<string> values) {
        foreach (var value in values) {
            Add(value);
        }
        return this;
    }

    public CommandArgumentBuilder Add(bool condition, IEnumerable<string> values) {
        if (condition) {
            Add(values);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote(string value) {
        if (value.StartsWith("\"") && value.EndsWith("\"")) {
            Add(value);
        }
        else {
            Add("\"{0}\"", value);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote(bool condition, string value) {
        if (condition) {
            AddQuote(value);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote(IEnumerable<string> values) {
        foreach (var value in values) {
            AddQuote(value);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote(bool condition, IEnumerable<string> values) {
        if (condition) {
            AddQuote(values);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote() {
        Add("\"");
        return this;
    }

    public CommandArgumentBuilder Clone() {
        return new CommandArgumentBuilder(ToString());
    }


    public override string ToString() {
        return builder.ToString();
    }

    public static implicit operator string(CommandArgumentBuilder builder) {
        return builder.ToString();
    }

    private readonly StringBuilder builder = new StringBuilder();
}
