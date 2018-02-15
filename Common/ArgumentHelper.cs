//--
// <copyright file="Arguments.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//--

//the contents of this file were copied from bzill's netmon analyzer

namespace HomeOS.Hub.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// <para>
    /// ArgumentsDictionary is a dictionary of string keys and object values,
    /// where the keys are the argument names and their values are given by
    /// the associated objects.
    /// </para>
    /// <para>
    /// Descriptions of the allowed arguments (and their default values) are
    /// provided via an array of the ArgumentSpec class (one entry per argument).
    /// Upon construction, the default values are potentially replaced by
    /// values from two other sources: AppSettings, and command line arguments.
    /// AppSettings override any default values, but cannot add new values to
    /// the collection (new values are silently ignored).  Command line args
    /// override both default values and AppSettings, and attempts to add new
    /// values generates an error.
    /// </para>
    /// </summary>
    public class ArgumentsDictionary : Dictionary<string, object>
    {
        /// <summary>
        /// The argument specifications this instance was created with.
        /// </summary>
        private ArgumentSpec[] specs;

        /// <summary>
        /// True if there was a parsing error while reading the application
        /// settings.
        /// </summary>
        private bool appSettingsParseError;

        /// <summary>
        /// True if there was a parsing error while reading the command line
        /// arguments.
        /// </summary>
        private bool commandLineParseError;

        /// <summary>
        /// If there was a commandLineParseError, this property contains the
        /// problematic text.
        /// </summary>
        private string parseErrorArgument;

        /// <summary>
        /// Initializes a new instance of the ArgumentsDictionary class.
        /// The collection is initialized to the default values, which are
        /// then overridden with application settings or command line arguments
        /// as appropriate.
        /// </summary>
        /// <param name="arguments">Command-line arguments.</param>
        /// <param name="argSpecs">
        /// Specifications for arguments we accept.
        /// </param>
        public ArgumentsDictionary(string[] arguments, ArgumentSpec[] argSpecs) :
            base(StringComparer.OrdinalIgnoreCase)
        {
            this.specs = argSpecs;
            this.appSettingsParseError = false;
            this.commandLineParseError = false;

            // -
            // Initialize ArgumentsDictionary dictionary with default values.
            // -
            foreach (ArgumentSpec spec in this.specs)
            {
                this[spec.Name] = spec.DefaultValue;
            }

            // -
            // Override default values with any matching AppSettings.
            // Ignore AppSettings that are not in the default list.
            // -
            foreach (string key in ConfigurationManager.AppSettings)
            {
                string appValue = ConfigurationManager.AppSettings[key];

                // -
                // We ignore AppSettings for which the application has no
                // default value.
                // -
                if (this.ContainsKey(key))
                {
                    // -
                    // We convert AppSettings values (which are always strings)
                    // to whatever type the default value for that setting is.
                    // -
                    if (!this.ConvertAndReplace(key, appValue))
                    {
                        this.appSettingsParseError = true;
                        this.parseErrorArgument = appValue;
                    }
                }
            }

            // -
            // Override current values with any matching command line
            // arguments.  If any non-matching command line arguments
            // are specified, display usage message.
            // -
            if (arguments.Length > 0)
            {
                IEnumerator cmdLineArgs = arguments.GetEnumerator();
                char[] assigners = new char[2] { '=', ':' };
                bool namelessArgUsed = false;

                while (cmdLineArgs.MoveNext())
                {
                    string arg = (string)cmdLineArgs.Current;
                    ArgumentSpec argSpec = null;
                    bool isFlag = false;
                    string value = null;

                    if (arg.Length > 0)
                    {
                        if ((arg[0] == '-') || arg[0] == '/')
                        {
                            // -
                            // This is a flag or option of some kind.
                            // -
                            isFlag = true;

                            // -
                            // Check for full-name options (i.e. "--foo").
                            // -
                            if ((arg.Length > 2) &&
                                (arg[0] == '-') && (arg[1] == '-'))
                            {
                                // -
                                // Check option syntax.
                                // We accept "--foo" (for booleans) and
                                // "--foo=42", "--foo:42", and "--foo 42".
                                // -
                                string name;
                                int index = arg.IndexOfAny(assigners, 2);
                                if (index > 0)
                                {
                                    name = arg.Substring(2, index - 2);
                                    value = arg.Substring(index + 1);
                                }
                                else
                                {
                                    name = arg.Substring(2);
                                }

                                // -
                                // Check for matching name.
                                // -
                                argSpec = this.LookupSpec(name);
                            }
                            else
                            {
                                // -
                                // Check for short options (i.e. "-f" or "/f").
                                // -
                                if ((arg.Length > 1) &&
                                    ((arg[0] == '-') || (arg[0] == '/')))
                                {
                                    // -
                                    // Check option syntax.
                                    // We accept "-f" (for booleans) and
                                    // "-f=42", "-f:42", and "-f 42".
                                    // -
                                    if (arg.Length > 2)
                                    {
                                        if ((arg[2] == '=') ||
                                            (arg[2] == ':'))
                                        {
                                            value = arg.Substring(3);
                                        }
                                        else
                                        {
                                            this.commandLineParseError = true;
                                            this.parseErrorArgument = arg;
                                            break;
                                        }
                                    }

                                    // -
                                    // Check for matching shortcut.
                                    // -
                                    argSpec = this.LookupSpec(arg[1]);
                                }
                            }
                        }
                    }

                    // -
                    // Check for nameless argument.
                    // The spec for the nameless argument is denoted by the
                    // (otherwise invalid) shortcut character '-'.
                    // -
                    // Note that like any other value, a nameless argument may
                    // have zero-length (i.e. be the empty string).
                    // -
                    if (!isFlag && !namelessArgUsed)
                    {
                        argSpec = this.LookupSpec('-');
                        value = arg;
                    }

                    // -
                    // We should have found a match of some sort.
                    // -
                    if (argSpec == null)
                    {
                        this.commandLineParseError = true;
                        this.parseErrorArgument = arg;
                        break;
                    }

                    // -
                    // We now have an spec for the argument.
                    // We treat booleans differently from non-booleans.
                    // -
                    if (argSpec.DefaultValue.GetType() == typeof(bool))
                    {
                        // -
                        // For booleans, we shouldn't have a value to assign
                        // to it (boolean flags just invert current value).
                        // -
                        if (value != null)
                        {
                            this.commandLineParseError = true;
                            this.parseErrorArgument = value;
                            break;
                        }

                        // -
                        // Boolean flags invert their current values.
                        // -
                        this[argSpec.Name] = !(bool)this[argSpec.Name];

                        // -
                        // ToDo: multiple boolean flag support (i.e. "-xvfs").
                        // -
                    }
                    else
                    {
                        // -
                        // For non-booleans, we should either have a value,
                        // or the next argument should be the value.
                        // -
                        if (value == null)
                        {
                            if (cmdLineArgs.MoveNext())
                            {
                                value = (string)cmdLineArgs.Current;
                                if ((value.Length > 0) &&
                                    ((value[0] == '-') || value[0] == '/'))
                                {
                                    this.commandLineParseError = true;
                                    this.parseErrorArgument = value;
                                    break;
                                }
                            }
                            else
                            {
                                this.commandLineParseError = true;
                                this.parseErrorArgument = arg;
                                break;
                            }
                        }

                        // -
                        // Replace the default or appSettings value with this
                        // this value (converted from a string to whatever
                        // type it is supposed to be).
                        // -
                        if (!this.ConvertAndReplace(argSpec.Name, value))
                        {
                            this.commandLineParseError = true;
                            this.parseErrorArgument = value;

                            break;
                        }
                    }
                }

                // -
                // ToDo: Generate better parse error string here?
                // -
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not there was a parsing error while
        /// reading the application settings.
        /// </summary>
        public bool AppSettingsParseError
        {
            get { return this.appSettingsParseError; }
        }

        /// <summary>
        /// Gets a value indicating whether or not there was a parsing error while
        /// reading the command line arguments.
        /// </summary>
        public bool CommandLineParseError
        {
            get { return this.commandLineParseError; }
        }

        /// <summary>
        /// Gets the problematic text associated with any command line
        /// parsing errors.
        /// </summary>
        public string ParseErrorArgument
        {
            get { return this.parseErrorArgument; }
        }

        /// <summary>
        /// Returns a usage message explaining how the user should provide
        /// arguments to the application.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <returns>A string containing the application usage text.</returns>
        public string GetUsage(string appName)
        {
            StringBuilder usage = new StringBuilder(512);
            int longestArg = 0;
            int currentLineLength;

            // -
            // Format a line with the name of app, options, and unnamed
            // arguments.  If it needs to wrap to fit in 80 columns, then
            // we indent subsequent lines by the length of the application
            // name plus a space.
            // -
            usage.Append(appName);
            currentLineLength = appName.Length;
            foreach (ArgumentSpec spec in this.specs)
            {
                if (spec.Hidden)
                {
                    continue;
                }

                bool takesValue = spec.DefaultValue.GetType() != typeof(bool);
                string valueName = spec.Name;
                if (takesValue && (spec.UsageTakes != null))
                {
                    valueName = spec.UsageTakes;
                }

                bool hasShortcut = spec.Shortcut != '/';
                bool namedArgument = spec.Shortcut != '-';
                StringBuilder temp = new StringBuilder(64);
                if (namedArgument)
                {
                    // -
                    // Argument must match name or shortcut.
                    // -
                    temp.Append(" [");
                    if (takesValue && hasShortcut)
                    {
                        temp.Append('[');
                    }

                    temp.Append("--");
                    temp.Append(spec.Name);
                    if (hasShortcut)
                    {
                        temp.Append(" | -");
                        temp.Append(spec.Shortcut);
                        if (takesValue)
                        {
                            temp.Append(']');
                        }
                    }

                    if (takesValue)
                    {
                        temp.Append("[=<");
                        temp.Append(valueName);
                        temp.Append("> | <");
                        temp.Append(valueName);
                        temp.Append(">]");
                    }

                    temp.Append(']');
                }
                else
                {
                    // -
                    // Argument is just a value.
                    // -
                    temp.Append(" <");
                    temp.Append(valueName);
                    temp.Append('>');
                }

                // -
                // Check if we need to line-wrap.
                // -
                if (currentLineLength + temp.Length > 79)
                {
                    usage.AppendLine();
                    usage.Append(' ', appName.Length);
                    currentLineLength = appName.Length;
                }

                usage.Append(temp.ToString());
                currentLineLength += temp.Length;

                // -
                // Keep track of longest specification for later.
                // -
                int length = 2;
                if (namedArgument)
                {
                    if (hasShortcut)
                    {
                        length += 2;
                    }
                    else
                    {
                        length += spec.Name.Length + 2;
                    }
                }
                else
                {
                    length -= 1;
                }

                if (takesValue)
                {
                    length += valueName.Length + 3;
                }

                if (length > longestArg)
                {
                    longestArg = length;
                }
            }

            usage.AppendLine();
            usage.AppendLine();

            // -
            // Add another line per argument with the format and usage info.
            // All usage info (including wrapped portions if any) should be
            // indented to line up with the longest option specification plus
            // a space (which we sanity limit to 40 characters).
            // -
            if (longestArg > 40)
            {
                longestArg = 20;
            }

            currentLineLength = 0;
            foreach (ArgumentSpec spec in this.specs)
            {
                if (spec.Hidden)
                {
                    continue;
                }

                bool takesValue = spec.DefaultValue.GetType() != typeof(bool);
                string valueName = spec.Name;
                if (takesValue && (spec.UsageTakes != null))
                {
                    valueName = spec.UsageTakes;
                }

                bool hasShortcut = spec.Shortcut != '/';
                bool namedArgument = spec.Shortcut != '-';
                StringBuilder temp = new StringBuilder(32);
                temp.Append("  ");
                if (namedArgument)
                {
                    // -
                    // For named arguments, we show the shortcut (or name
                    // if there is no shortcut) and the value name.
                    // -
                    if (hasShortcut)
                    {
                        temp.Append('-');
                        temp.Append(spec.Shortcut);
                    }
                    else
                    {
                        temp.Append("--");
                        temp.Append(spec.Name);
                    }

                    if (takesValue)
                    {
                        temp.Append("=<");
                        temp.Append(valueName);
                        temp.Append('>');
                    }
                }
                else
                {
                    // -
                    // For unnamed arguments, we show the value name.
                    // -
                    temp.Append('<');
                    temp.Append(valueName);
                    temp.Append('>');
                }

                temp.Append(' ', longestArg - temp.Length);
                string[] words = spec.UsageMessage.Split(new char[] { ' ' });
                foreach (string word in words)
                {
                    if (temp.Length + word.Length + 1 > 79)
                    {
                        // -
                        // Start new line.
                        // -
                        usage.AppendLine(temp.ToString());
                        temp = new StringBuilder(64);
                        temp.Append(' ', longestArg);
                    }

                    temp.Append(' ');
                    temp.Append(word);
                }

                usage.AppendLine(temp.ToString());
            }

            return usage.ToString();
        }

        /// <summary>
        /// Routine to replace a value in our dictionary.
        /// The replacement value is first converted from a string into whatever
        /// type the current value is.  A current value must be present.
        /// </summary>
        /// <param name="key">The name of the value to replace.</param>
        /// <param name="value">The value to replace it with.</param>
        /// <returns>True if successful, false otherwise.</returns>
        private bool ConvertAndReplace(string key, string value)
        {
            Type type = this[key].GetType();
            try
            {
                this[key] = Convert.ChangeType(
                    value,
                    type,
                    CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Routine to lookup an argument spec by its string name.
        /// Just a linear search for now.
        /// </summary>
        /// <param name="name">Name of argument to lookup.</param>
        /// <returns>The corresponding argument specification.</returns>
        private ArgumentSpec LookupSpec(string name)
        {
            foreach (ArgumentSpec spec in this.specs)
            {
                if (spec.Name.Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return spec;
                }
            }

            return null;
        }

        /// <summary>
        /// Routine to lookup an argument spec by its shortcut character.
        /// Just a linear search for now.
        /// </summary>
        /// <param name="shortcut">Shortcut character to lookup.</param>
        /// <returns>The corresponding argument specification.</returns>
        private ArgumentSpec LookupSpec(char shortcut)
        {
            shortcut = Char.ToLowerInvariant(shortcut);
            foreach (ArgumentSpec spec in this.specs)
            {
                if (spec.Shortcut == shortcut)
                {
                    return spec;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// <para>
    /// Representation of an application setting that is changeable by the
    /// user either through the AppSettings section of the app's config file,
    /// or through a command line argument.
    /// </para>
    /// <para>
    /// The ArgumentsDictionary class constructor takes an array of these to
    /// define the legitimate arguments that it will accept, their default
    /// values, and some hopefully helpful usage information.
    /// </para>
    /// <para>
    /// A Shortcut value of '/' indicates that the argument has no shortcut.
    /// A Shortcut value of '-' indicates that the argument is nameless.
    /// If Hidden is true, the argument will not be listed in the usage message.
    /// </para>
    /// </summary>
    public class ArgumentSpec
    {
        /// <summary>
        /// The name of the argument.
        /// </summary>
        private string name;

        /// <summary>
        /// The shortcut character for the argument.
        /// </summary>
        private char shortcut;

        /// <summary>
        /// The default value of the argument.
        /// </summary>
        private object defaultValue;

        /// <summary>
        /// True if this argument is not to be included in the usage text for the application.
        /// </summary>
        private bool hidden;

        /// <summary>
        /// A short description of what the argument value is.
        /// </summary>
        private string usageTakes;

        /// <summary>
        /// The usage message for the argument.
        /// </summary>
        private string usageMessage;

        /// <summary>
        /// Initializes a new instance of the ArgumentSpec class.
        /// </summary>
        /// <param name="name">Name of the argument.</param>
        /// <param name="shortcut">Shortcut character.</param>
        /// <param name="value">Default value.</param>
        /// <param name="hidden">True if argument is to be kept hidden.</param>
        /// <param name="takes">Short description of the value an argument takes.</param>
        /// <param name="usage">Usage text for the argument.</param>
        public ArgumentSpec(
            string name,
            char shortcut,
            object value,
            bool hidden,
            string takes,
            string usage)
        {
            this.name = name;
            this.shortcut = Char.ToLowerInvariant(shortcut);
            this.defaultValue = value;
            this.hidden = hidden;
            this.usageTakes = takes;
            this.usageMessage = usage;

            // -
            // Review: The 'takes' string is only meaningful for
            // non-boolean value types.  Check and error here?
            // -
        }

        /// <summary>
        /// Initializes a new instance of the ArgumentSpec class.
        /// </summary>
        /// <param name="name">Name of the argument.</param>
        /// <param name="shortcut">Shortcut character.</param>
        /// <param name="value">Default value.</param>
        /// <param name="takes">Short description of the value an argument takes.</param>
        /// <param name="usage">Usage text for the argument.</param>
        public ArgumentSpec(
            string name,
            char shortcut,
            object value,
            string takes,
            string usage) :
            this(name, shortcut, value, false, takes, usage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ArgumentSpec class.
        /// </summary>
        /// <param name="name">Name of the argument.</param>
        /// <param name="shortcut">Shortcut character.</param>
        /// <param name="value">Default value.</param>
        /// <param name="usage">Usage text for the argument.</param>
        public ArgumentSpec(
            string name,
            char shortcut,
            object value,
            string usage) :
            this(name, shortcut, value, false, null, usage)
        {
        }

        /// <summary>
        /// Gets the name of the argument.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets the shortcut character for the argument.
        /// </summary>
        public char Shortcut
        {
            get { return this.shortcut; }
        }

        /// <summary>
        /// Gets the default value of the argument.
        /// </summary>
        public object DefaultValue
        {
            get { return this.defaultValue; }
        }

        /// <summary>
        /// Gets a value indicating whether this argument is kept
        /// hidden from the user (i.e. not mentioned in the usage
        /// message for the application) or not.
        /// </summary>
        public bool Hidden
        {
            get { return this.hidden; }
        }

        /// <summary>
        /// Gets the description of what values the argument takes.
        /// </summary>
        public string UsageTakes
        {
            get { return this.usageTakes; }
        }

        /// <summary>
        /// Gets the usage text for the argument.
        /// </summary>
        public string UsageMessage
        {
            get { return this.usageMessage; }
        }
    }


}
