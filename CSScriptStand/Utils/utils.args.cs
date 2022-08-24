using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

// utils.args
// УТИЛИТЫ ДЛЯ РАБОТЫ С АРГУМЕНТАМИ КОМАНДНОЙ СТРОКИ
// ------------------------------------------------------------

//## #namespace

public abstract class CommandArgument : ICloneable
{
    public object Clone() {
        return MemberwiseClone();
    }

    public override string ToString() {
        return ToString(this);
    }

    public static implicit operator string(CommandArgument args) {
        return args.ToString();
    }

    public static string ToString(CommandArgument args) {
        StringBuilder str = new StringBuilder();

        Type objType = args.GetType();
        PropertyInfo[] properties = objType.GetProperties();
        foreach (PropertyInfo property in properties) {
            object[] attrArray = property.GetCustomAttributes(typeof(CommandOptionAttribute), false);
            if (attrArray.Length == 0) {
                continue;
            }
            CommandOptionAttribute attr = (CommandOptionAttribute)attrArray[0];

            object value = property.GetValue(args, null);
            if (value == null) {
                if (attr.DefaultValue != null) {
                    value = attr.DefaultValue;
                }
                else {
                    if (attr.Required) {
                        throw new ArgumentException("Не указано значение для обязательного параметра \"" + property.Name + "\".");
                    }
                    else {
                        continue;
                    }
                }
            }

            if (attr.Flag) {
                if (Equals(value, true)) {
                    if (str.Length > 0) {
                        str.Append(attr.OptionSeparator);
                    }
                    str.Append(attr.Options[0]);
                }
            }
            else {
                if (value is ICollection) {
                    ICollection collection = (ICollection)value;
                    if (collection.Count > 0) {

                        if (str.Length > 0) {
                            str.Append(attr.OptionSeparator);
                        }
                        if (!attr.RepeatOption) {
                            if (attr.Options.Length > 0) {
                                str.Append(attr.Options[0]);
                            }
                        }

                        int collectionOptionsCount = 0;
                        foreach (object item in collection) {

                            if (attr.RepeatOption) {
                                if (str.Length > 0) {
                                    str.Append(' ');
                                }
                                if (attr.Options.Length > 0) {
                                    str.Append(attr.Options[0]);
                                }
                            }

                            if (collectionOptionsCount == 0) {
                                if (str.Length > 0) {
                                    str.Append(attr.OptionSeparator);
                                }
                            }
                            else {
                                str.Append(attr.ValueSeparator);
                            }

                            if (attr.Quoted) {
                                str.Append('\"');
                            }
                            str.Append(GetArgumentValue(item, attr));
                            if (attr.Quoted) {
                                str.Append('\"');
                            }

                            collectionOptionsCount++;
                        }
                    }
                }
                else {

                    if (str.Length > 0) {
                        str.Append(attr.OptionSeparator);
                    }

                    if (attr.Options.Length > 0) {
                        str.Append(attr.Options[0]);
                    }

                    str.Append(attr.ValueSeparator);

                    if (attr.Quoted) {
                        str.Append('\"');
                    }
                    str.Append(GetArgumentValue(value, attr));
                    if (attr.Quoted) {
                        str.Append('\"');
                    }
                }
            }
        }
        return str.ToString();
    }

    private static string GetArgumentValue(object value, CommandOptionAttribute attr) {
        string convertedValue;
        if (attr.Converter != null) {
            IConsoleArgumentConverter converter = (IConsoleArgumentConverter)Activator.CreateInstance(attr.Converter);
            convertedValue = converter.Convert(value);
        }
        else {
            convertedValue = value is bool ? Equals(value, true) ? "true" : "false" : value.ToString();
        }
        return convertedValue;
    }
}

public abstract class CommandArgument<T> : CommandArgument
{
    public new T Clone() {
        return (T)base.Clone();
    }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class CommandOptionAttribute : Attribute
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
    public string ValueSeparator { get; set; }

    public string OptionSeparator { get; set; }

    /// <summary>
    /// Конвертер значений типа <see cref="IConsoleArgumentConverter"/>.
    /// </summary>
    public Type Converter { get; set; }

    /// <summary>
    /// Указывает, является ли параметр обязательным.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Указывается значение по умолчанию в случае, если поле не заполнено.
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// Если в качестве значения указано перечисление, название опции добавляется 1 раз в начале перечисления
    /// </summary>
    public bool RepeatOption { get; set; }

    /// <summary>
    /// Названия аргумента.
    /// </summary>
    public string[] Options { get; private set; }


    public CommandOptionAttribute(params string[] options) {
        Options = options;
        Quoted = true;
        ValueSeparator = " ";
        OptionSeparator = " ";
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
        string value = string.Format(format, args);
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
        foreach (string value in values) {
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
        foreach (string value in values) {
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
